using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureChat.Models
{
	[Table("Users")]
	public class User
	{
		[Key]
		[Column("user_id")]
		[MaxLength(8)]
		public string UserID { get; set; } = "";

		[Required]
		[Column("username")]
		[MaxLength(16)]
		public string Username { get; set; } = "";

		[Required]
		[Column("display_name")]
		[MaxLength(32)]
		public string DisplayName { get; set; } = "";

		[Required]
		[Column("email")]
		[MaxLength(64)]
		public string Email { get; set; } = "";

		[Column("avatar_url")]
		public string? AvatarURL { get; set; }

		[Column("bio_text")]
		public string? BioText { get; set; }

		[Required]
		[Column("password_hash")]
		public string PasswordHash { get; set; } = "";

		[Required]
		[Column("key_salt")]
		public string KeySalt { get; set; } = "";

		[Required]
		[Column("encryption_key")]
		public string EncryptionKey { get; set; } = "";

		[Required]
		[Column("is_active")]
		public bool IsActive { get; set; } = true;

		[Required]
		[Column("is_verified")]
		public bool IsVerified { get; set; } = false;

		[Required]
		[Column("show_read_status")]
		public bool ShowReadStatus { get; set; } = true;

		[Required]
		[Column("show_online_status")]
		public bool ShowOnlineStatus { get; set; } = true;

		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

		[Column("updated_at")]
		public DateTime UpdatedAt { get; set; }

		[InverseProperty(nameof(BlockedUser.Blocker))]
		public ICollection<BlockedUser> BlockedUsers { get; set; } = [];

		[InverseProperty(nameof(ConversationMember.User))]
		public ICollection<ConversationMember> ConversationMemberships { get; set; } = [];

		[InverseProperty(nameof(Friend.UserA))]
		public ICollection<Friend> FriendshipsA { get; set; } = [];

		[InverseProperty(nameof(Friend.UserB))]
		public ICollection<Friend> FriendshipsB { get; set; } = [];

		[InverseProperty(nameof(FriendRequest.Recipient))]
		public ICollection<FriendRequest> FriendRequestsReceived { get; set; } = [];

		[InverseProperty(nameof(FriendRequest.Sender))]
		public ICollection<FriendRequest> FriendRequestsSent { get; set; } = [];

		[InverseProperty(nameof(UserSession.User))]
		public ICollection<UserSession> Sessions { get; set; } = [];
	}

	[Table("UserSessions")]
	public class UserSession
	{
		[Key]
		[Column("session_id")]
		[MaxLength(8)]
		public string SessionID { get; set; } = "";

		[Required]
		[Column("user_id")]
		[MaxLength(8)]
		public string UserID { get; set; } = "";

		[Column("device_name")]
		[MaxLength(64)]
		public string? DeviceName { get; set; }

		[Column("device_token")]
		public string? DeviceToken { get; set; }

		[Required]
		[Column("refresh_token")]
		public string RefreshToken { get; set; } = "";

		[Required]
		[Column("expires_at")]
		public DateTime ExpiresAt { get; set; }

		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

		[ForeignKey(nameof(UserID))]
		public User User { get; set; } = null!;
	}
}
