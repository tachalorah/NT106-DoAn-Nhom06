using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SecureChat.Models
{
	[Table("Friends")]
	public class Friend
	{
		[Key]
		[Column("friendship_id")]
		[MaxLength(8)]
		public string FriendshipID { get; set; } = "";

		[Required]
		[Column("user_a_id")]
		[MaxLength(8)]
		public string UserAID { get; set; } = "";

		[Required]
		[Column("user_b_id")]
		[MaxLength(8)]
		public string UserBID { get; set; } = "";

		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

		[ForeignKey(nameof(UserAID))]
		public User UserA { get; set; } = null!;

		[ForeignKey(nameof(UserBID))]
		public User UserB { get; set; } = null!;
	}

	[Table("FriendRequests")]
	public class FriendRequest
	{
		[Key]
		[Column("request_id")]
		[MaxLength(8)]
		public string RequestID { get; set; } = "";

		[Required]
		[Column("sender_id")]
		[MaxLength(8)]
		public string SenderID { get; set; } = "";

		[Required]
		[Column("recipient_id")]
		[MaxLength(8)]
		public string RecipientID { get; set; } = "";

		[Column("status")]
		public FriendRequestStatus Status { get; set; } = FriendRequestStatus.Pending;

		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

		[Column("responded_at")]
		public DateTime? RespondedAt { get; set; }

		[ForeignKey(nameof(SenderID))]
		public User Sender { get; set; } = null!;

		[ForeignKey(nameof(RecipientID))]
		public User Recipient { get; set; } = null!;
	}

	[Table("BlockedUsers")]
	public class BlockedUser
	{
		[Key]
		[Column("block_id")]
		[MaxLength(8)]
		public string BlockID { get; set; } = "";

		[Required]
		[Column("blocker_id")]
		[MaxLength(8)]
		public string BlockerID { get; set; } = "";

		[Required]
		[Column("blocked_id")]
		[MaxLength(8)]
		public string BlockedID { get; set; } = "";

		[Column("created_at")]
		public DateTime CreatedAt { get; set; }

		[ForeignKey(nameof(BlockerID))]
		[InverseProperty(nameof(User.BlockedUsers))]
		public User Blocker { get; set; } = null!;

		[ForeignKey(nameof(BlockedID))]
		public User Blocked { get; set; } = null!;
	}
}
