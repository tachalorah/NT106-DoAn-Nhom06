using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

using SecureChat.DTOs;
using SecureChat.Models;
using SecureChat.Repositories;
using SecureChat.Services;

namespace SecureChat.Controllers
{
	[ApiController]
	[Route("api/auth")]
  public class AuthController(UserRepository users, TokenService tokens, IConfiguration config, ForgotPasswordService forgotPasswordService, ILogger<AuthController> logger) : BaseController
	{
		[HttpPost("register")]
		public async Task<IActionResult> Register([FromBody] RegisterRequest req)
		{
			if (await users.ExistsByUsernameAsync(req.Username))
				return Conflict(new { error = "Tên người dùng đã được sử dụng." });
			if (await users.ExistsByEmailAsync(req.Email))
				return Conflict(new { error = "Email đã được sử dụng." });
			
			await users.CreateAsync(new User {
				UserID = NewID(),
				Username = req.Username,
				DisplayName = req.DisplayName,
				Email = req.Email,
				HashedPassword = req.HashedPassword,
				HashedBKey = req.HashedBKey,
				KeySalt = req.KeySalt,
				PublicKey = req.PublicKey
			});

			return Ok(new { message = "Đăng ký thành công." });
		}

		[HttpPost("login")]
		public async Task<IActionResult> Login([FromBody] LoginRequest req)
		{
			var user = req.UsernameOrEmail.Contains('@')
				? await users.GetByEmailAsync(req.UsernameOrEmail)
				: await users.GetByUsernameAsync(req.UsernameOrEmail);

			if (user is null || user.HashedPassword != req.HashedPassword)
				return Unauthorized(new { error = "Thông tin đăng nhập không hợp lệ." });

			var sessionID = NewID();
			var accessToken = tokens.GenerateAccessToken(user.UserID, sessionID);
			var refreshToken = TokenService.GenerateRefreshToken();
			var expiry = TokenService.RefreshTokenExpiry(config);

			await users.CreateSessionAsync(new UserSession {
				SessionID    = sessionID,
				UserID       = user.UserID,
				DeviceName   = req.DeviceName ?? "Unknown",
				RefreshToken = refreshToken,
				ExpiresAt    = expiry,
				LastUsedAt   = DateTime.UtcNow
			});

			return Ok(new AuthResponse(accessToken, refreshToken, expiry, UserResponse.From(user)));
		}

		[HttpPost("refresh")]
		public async Task<IActionResult> RefreshAccessToken([FromBody] RefreshRequest req)
		{
			var session = await users.GetSessionByRefreshTokenAsync(req.RefreshToken);
			if (session is null)
				return Unauthorized(new { error = "Refresh token không hợp lệ." });

			if (session.ExpiresAt < DateTime.UtcNow) {
				await users.DeleteSessionAsync(session.SessionID);
				return Unauthorized(new { error = "Refresh token đã hết hạn." });
			}

			var newAccess  = tokens.GenerateAccessToken(session.UserID, session.SessionID);
			var newExpiry  = TokenService.RefreshTokenExpiry(config);

			await users.UpdateSessionAsync(session.SessionID, req.RefreshToken, newExpiry);
			return Ok(new AuthResponse(newAccess, req.RefreshToken, newExpiry, UserResponse.From(session.User)));
		}

		[HttpPost("forgot-password/request-otp")]
		public async Task<IActionResult> RequestForgotPasswordOtp([FromBody] ForgotPasswordRequestOtpRequest req)
		{
          if (string.IsNullOrWhiteSpace(req.Email))
			{
				return BadRequest(new { message = "Invalid email format.", errorCode = "INVALID_EMAIL" });
			}

			var email = req.Email.Trim();
			var user = await users.GetByEmailAsync(email);
			if (user is not null)
			{
              await forgotPasswordService.CreateOtpAsync(email);
			}

			return Ok(new ForgotPasswordRequestOtpResponse("If the email is registered, an OTP has been sent."));
		}

		[HttpPost("forgot-password/verify-otp")]
		public async Task<IActionResult> VerifyForgotPasswordOtp([FromBody] ForgotPasswordVerifyOtpRequest req)
		{
          if (string.IsNullOrWhiteSpace(req.Email))
			{
				return BadRequest(new { message = "Invalid email format.", errorCode = "INVALID_EMAIL" });
			}

			if (string.IsNullOrWhiteSpace(req.Otp) || req.Otp.Length != 6 || !req.Otp.All(char.IsDigit))
			{
				return BadRequest(new { message = "Invalid OTP.", errorCode = "INVALID_OTP" });
			}

			var email = req.Email.Trim();
			var otp = req.Otp.Trim();

			var user = await users.GetByEmailAsync(email);
			if (user is null)
			{
				return BadRequest(new { message = "Invalid OTP.", errorCode = "INVALID_OTP" });
			}

            var result = await forgotPasswordService.VerifyOtpAsync(email, otp);
			if (result.Status == OtpVerifyStatus.Expired)
			{
				return BadRequest(new { message = "OTP expired.", errorCode = "EXPIRED_OTP" });
			}

			if (result.Status != OtpVerifyStatus.Success || string.IsNullOrWhiteSpace(result.ResetToken))
			{
				return BadRequest(new { message = "Invalid OTP.", errorCode = "INVALID_OTP" });
			}

			return Ok(new ForgotPasswordVerifyOtpResponse("OTP verified.", result.ResetToken));
		}

		[HttpPost("forgot-password/reset")]
		public async Task<IActionResult> ResetForgotPassword([FromBody] ForgotPasswordResetRequest req)
		{
          if (string.IsNullOrWhiteSpace(req.ResetToken))
			{
				return BadRequest(new { message = "Invalid reset token.", errorCode = "INVALID_TOKEN" });
			}

			if (!IsStrongPassword(req.NewPassword))
			{
				return BadRequest(new { message = "Password does not meet complexity requirements.", errorCode = "WEAK_PASSWORD" });
			}

			var tokenResult = await forgotPasswordService.ValidateResetTokenAsync(req.ResetToken);
			if (tokenResult.Status == ResetTokenStatus.Expired)
			{
				return BadRequest(new { message = "Reset token expired.", errorCode = "EXPIRED_TOKEN" });
			}

			if (tokenResult.Status != ResetTokenStatus.Valid || string.IsNullOrWhiteSpace(tokenResult.Email))
			{
				return BadRequest(new { message = "Invalid reset token.", errorCode = "INVALID_TOKEN" });
			}

			var user = await users.GetByEmailAsync(tokenResult.Email);
			if (user is null)
			{
				logger.LogWarning("Forgot-password reset skipped: user not found for email {Email}", tokenResult.Email);
				return BadRequest(new { message = "Invalid reset token.", errorCode = "INVALID_TOKEN" });
			}

			await users.UpdateHashedPasswordOnlyAsync(user.UserID, req.NewPassword);
           logger.LogInformation("Forgot-password reset completed for user {UserID}", user.UserID);
			return Ok(new ForgotPasswordResetResponse("Password reset successful."));
		}

		private static bool IsStrongPassword(string? password)
		{
			if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
			{
				return false;
			}

			return Regex.IsMatch(password, "[A-Z]")
				&& Regex.IsMatch(password, "[a-z]")
				&& Regex.IsMatch(password, "[0-9]")
				&& Regex.IsMatch(password, "[^a-zA-Z0-9]");
		}

		[Authorize]
		[HttpDelete("logout")]
		public async Task<IActionResult> Logout()
		{
			var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
			if (jti is not null)
				await users.DeleteSessionAsync(jti);
			return NoContent();
		}


		[Authorize]
		[HttpGet("sessions")]
		public async Task<IActionResult> GetSessions()
		{
			var userID = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var sessions = await users.GetAllSessionsByUserAsync(userID);
			return Ok(sessions.Select(s => new SessionResponse(s.SessionID, s.DeviceName, s.CreatedAt, s.ExpiresAt)));
		}

		[Authorize]
		[HttpDelete("sessions/{sessionID}")]
		public async Task<IActionResult> RevokeSession(string sessionID)
		{
			var userID = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			var session = await users.GetSessionByIdAsync(sessionID);
			if (session is null || session.UserID != userID)
				return NotFound();
			await users.DeleteSessionAsync(sessionID);
			return NoContent();
		}

		[Authorize]
		[HttpDelete("sessions")]
		public async Task<IActionResult> RevokeAllSessions()
		{
			var userID = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
			await users.RevokeAllSessionsAsync(userID);
			return NoContent();
		}
	}
}
