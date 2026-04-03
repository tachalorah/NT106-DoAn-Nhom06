using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;
using SecureChat.Models;

namespace SecureChat.Repositories
{
	public class UserRepository(AppDbContext db)
	{
		public async Task<User> CreateAsync(User user)
		{
			user.CreatedAt = DateTime.UtcNow;
			user.UpdatedAt = DateTime.UtcNow;

			db.Users.Add(user);
			await db.SaveChangesAsync();
			return user;
		}

		public async Task<User?> GetByIdAsync(string userID)
			=> await db.Users.FindAsync(userID);

		public async Task<User?> GetByUsernameAsync(string username)
			=> await db.Users.FirstOrDefaultAsync(u => u.Username == username);

		public async Task<User?> GetByEmailAsync(string email)
			=> await db.Users.FirstOrDefaultAsync(u => u.Email == email);

		public async Task<List<User>> GetAllAsync()
			=> await db.Users
			.Where(u => u.IsActive)
			.OrderBy(u => u.Username)
			.ToListAsync();

		public async Task<List<User>> SearchAsync(string query)
			=> await db.Users
			.Where(u => u.IsActive &&
					(EF.Functions.Like(u.Username, $"%{query}%") ||
					 EF.Functions.Like(u.DisplayName, $"%{query}%")))
			.ToListAsync();

		public async Task<bool> ExistsByIdAsync(string userID)
			=> await db.Users.AnyAsync(u => u.UserID == userID);

		public async Task<bool> ExistsByUsernameAsync(string username)
			=> await db.Users.AnyAsync(u => u.Username == username);

		public async Task<bool> ExistsByEmailAsync(string email)
			=> await db.Users.AnyAsync(u => u.Email == email);

		public async Task UpdateAsync(User user)
		{
			user.UpdatedAt = DateTime.UtcNow;
			db.Users.Update(user);
			await db.SaveChangesAsync();
		}

		public async Task UpdatePasswordAsync(string userID, string newPasswordHash)
		{
			var user = await db.Users.FindAsync(userID)
				?? throw new KeyNotFoundException($"User {userID} not found.");

			user.PasswordHash = newPasswordHash;
			user.UpdatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public async Task UpdateEncryptionKeysAsync(
				string userID,
				string newKeySalt,
				string newEncryptionKey)
		{
			var user = await db.Users.FindAsync(userID)
				?? throw new KeyNotFoundException($"User {userID} not found.");

			user.KeySalt = newKeySalt;
			user.EncryptionKey = newEncryptionKey;
			user.UpdatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public async Task UpdateAvatarAsync(string userID, string? avatarURL)
		{
			var user = await db.Users.FindAsync(userID)
				?? throw new KeyNotFoundException($"User {userID} not found.");

			user.AvatarURL = avatarURL;
			user.UpdatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public async Task VerifyAsync(string userID)
		{
			var user = await db.Users.FindAsync(userID)
				?? throw new KeyNotFoundException($"User {userID} not found.");

			user.IsVerified = true;
			user.UpdatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public async Task SetActivationStatusAsync(string userID, bool isActive)
		{
			var user = await db.Users.FindAsync(userID)
				?? throw new KeyNotFoundException($"User {userID} not found.");

			user.IsActive = isActive;
			user.UpdatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public async Task DeactivateAsync(string userID)
			=> await SetActivationStatusAsync(userID, false);

		public async Task UpdatePrivacySettingsAsync(
				string userID,
				bool showReadStatus,
				bool showOnlineStatus)
		{
			var user = await db.Users.FindAsync(userID)
				?? throw new KeyNotFoundException($"User {userID} not found.");

			user.ShowReadStatus = showReadStatus;
			user.ShowOnlineStatus = showOnlineStatus;
			user.UpdatedAt = DateTime.UtcNow;
			await db.SaveChangesAsync();
		}

		public async Task DeleteAsync(string userID)
		{
			var user = await db.Users.FindAsync(userID);
			if (user is null) return;

			db.Users.Remove(user);
			await db.SaveChangesAsync();
		}

		/*
		 * SESSIONS
		 */

		public async Task<UserSession> CreateSessionAsync(UserSession session)
		{
			session.CreatedAt = DateTime.UtcNow;
			db.UserSessions.Add(session);
			await db.SaveChangesAsync();
			return session;
		}

		public async Task<UserSession?> GetSessionByIdAsync(string sessionID)
			=> await db.UserSessions
			.Include(s => s.User)
			.FirstOrDefaultAsync(s => s.SessionID == sessionID);

		public async Task<UserSession?> GetSessionByRefreshTokenAsync(string refreshToken)
			=> await db.UserSessions
			.Include(s => s.User)
			.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);

		public async Task<List<UserSession>> GetActiveSessionsByUserAsync(string userID)
			=> await db.UserSessions
			.Where(s => s.UserID == userID && s.ExpiresAt > DateTime.UtcNow)
			.OrderByDescending(s => s.CreatedAt)
			.ToListAsync();

		public async Task<List<UserSession>> GetAllSessionsByUserAsync(string userID)
			=> await db.UserSessions
			.Where(s => s.UserID == userID)
			.OrderByDescending(s => s.CreatedAt)
			.ToListAsync();

		public async Task<UserSession> RotateRefreshTokenAsync(
				string sessionID,
				string newRefreshToken,
				DateTime newExpiresAt)
		{
			var session = await db.UserSessions.FindAsync(sessionID)
				?? throw new KeyNotFoundException($"UserSession {sessionID} not found.");

			session.RefreshToken = newRefreshToken;
			session.ExpiresAt = newExpiresAt;
			await db.SaveChangesAsync();
			return session;
		}

		public async Task DeleteSessionAsync(string sessionID)
		{
			var session = await db.UserSessions.FindAsync(sessionID);
			if (session is null) return;

			db.UserSessions.Remove(session);
			await db.SaveChangesAsync();
		}

		public async Task RevokeAllSessionsAsync(string userID)
		{
			var sessions = await db.UserSessions
				.Where(s => s.UserID == userID)
				.ToListAsync();

			db.UserSessions.RemoveRange(sessions);
			await db.SaveChangesAsync();
		}

		public async Task<int> PurgeExpiredSessionsAsync()
		{
			var expired = await db.UserSessions
				.Where(s => s.ExpiresAt <= DateTime.UtcNow)
				.ToListAsync();

			db.UserSessions.RemoveRange(expired);
			await db.SaveChangesAsync();
			return expired.Count;
		}
	}
}
