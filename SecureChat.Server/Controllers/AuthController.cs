using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

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
	public class AuthController(UserRepository users, TokenService tokens, IConfiguration config) : BaseController
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
