using System.ComponentModel.DataAnnotations;

namespace SecureChat.DTOs
{
	public record RegisterRequest(
		[Required, MinLength(3), MaxLength(16)] string Username,
		[Required, MaxLength(32)] string DisplayName,
		[Required, EmailAddress, MaxLength(64)] string Email,
		[Required] string HashedPassword,
		[Required] string HashedBKey,
		[Required] string KeySalt,
		[Required] string PublicKey
	);

	public record LoginRequest(
		[Required] string UsernameOrEmail,
		[Required] string HashedPassword,
		string? DeviceName
	);

	public record RefreshRequest(
		[Required] string RefreshToken
	);

	public record AuthResponse(
		string AccessToken,
		string RefreshToken,
		DateTime ExpiresAt,
		UserResponse User
	);
}
