using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SecureChat.Server.Migrations
{
    /// <inheritdoc />
    public partial class InitialSecureChatSeed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    username = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    display_name = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    avatar_url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    bio_text = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hashed_password = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hashed_b_key = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hashed_recovery_key = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    key_salt = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    public_key = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    show_read_status = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    show_online_status = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.user_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BlockedUsers",
                columns: table => new
                {
                    block_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    blocker_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    blocked_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedUsers", x => x.block_id);
                    table.ForeignKey(
                        name: "FK_BlockedUsers_Users_blocked_id",
                        column: x => x.blocked_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockedUsers_Users_blocker_id",
                        column: x => x.blocker_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "FriendRequests",
                columns: table => new
                {
                    request_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sender_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recipient_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp"),
                    responded_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FriendRequests", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_FriendRequests_Users_recipient_id",
                        column: x => x.recipient_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FriendRequests_Users_sender_id",
                        column: x => x.sender_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Friends",
                columns: table => new
                {
                    friendship_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_a_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_b_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Friends", x => x.friendship_id);
                    table.CheckConstraint("chk_friends_order", "user_a_id < user_b_id");
                    table.ForeignKey(
                        name: "FK_Friends_Users_user_a_id",
                        column: x => x.user_a_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Friends_Users_user_b_id",
                        column: x => x.user_b_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "UserSessions",
                columns: table => new
                {
                    session_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    device_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    refresh_token = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp"),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_used_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.session_id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallLogs",
                columns: table => new
                {
                    call_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conversation_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    call_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    started_by = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp"),
                    ended_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallLogs", x => x.call_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CallParticipants",
                columns: table => new
                {
                    participant_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    call_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    joined_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    left_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallParticipants", x => new { x.participant_id, x.call_id });
                    table.ForeignKey(
                        name: "FK_CallParticipants_CallLogs_call_id",
                        column: x => x.call_id,
                        principalTable: "CallLogs",
                        principalColumn: "call_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ConversationMembers",
                columns: table => new
                {
                    member_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conversation_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    role = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    nickname = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    encrypted_key = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    joined_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp"),
                    left_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    show_notifications = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    banned_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_read_msg_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConversationMembers", x => x.member_id);
                    table.ForeignKey(
                        name: "FK_ConversationMembers_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Conversations",
                columns: table => new
                {
                    conversation_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conversation_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    Name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    avatar_url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_by = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_activity_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.conversation_id);
                    table.ForeignKey(
                        name: "FK_Conversations_Users_created_by",
                        column: x => x.created_by,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Messages",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conversation_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    original_sender_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sender_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reply_to_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message_type = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    content = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    content_iv = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp"),
                    deleted_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    edited_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.message_id);
                    table.ForeignKey(
                        name: "FK_Messages_ConversationMembers_sender_id",
                        column: x => x.sender_id,
                        principalTable: "ConversationMembers",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversations",
                        principalColumn: "conversation_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Messages_Messages_reply_to_id",
                        column: x => x.reply_to_id,
                        principalTable: "Messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Messages_Users_original_sender_id",
                        column: x => x.original_sender_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageAttachments",
                columns: table => new
                {
                    attachment_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_url = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_type = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_hash = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    width = table.Column<int>(type: "int", nullable: true),
                    height = table.Column<int>(type: "int", nullable: true),
                    thumbnail_url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    duration_secs = table.Column<int>(type: "int", nullable: true),
                    file_iv = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    thumbnail_iv = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    uploaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageAttachments", x => x.attachment_id);
                    table.ForeignKey(
                        name: "FK_MessageAttachments_Messages_message_id",
                        column: x => x.message_id,
                        principalTable: "Messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageMentions",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    member_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageMentions", x => new { x.message_id, x.member_id });
                    table.ForeignKey(
                        name: "FK_MessageMentions_ConversationMembers_member_id",
                        column: x => x.member_id,
                        principalTable: "ConversationMembers",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageMentions_Messages_message_id",
                        column: x => x.message_id,
                        principalTable: "Messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessagePins",
                columns: table => new
                {
                    message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    conversation_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pinned_by = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    pinned_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessagePins", x => new { x.message_id, x.conversation_id });
                    table.ForeignKey(
                        name: "FK_MessagePins_ConversationMembers_pinned_by",
                        column: x => x.pinned_by,
                        principalTable: "ConversationMembers",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MessagePins_Conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "Conversations",
                        principalColumn: "conversation_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessagePins_Messages_message_id",
                        column: x => x.message_id,
                        principalTable: "Messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageReactions",
                columns: table => new
                {
                    reaction_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    member_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reaction = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "current_timestamp")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageReactions", x => x.reaction_id);
                    table.ForeignKey(
                        name: "FK_MessageReactions_ConversationMembers_member_id",
                        column: x => x.member_id,
                        principalTable: "ConversationMembers",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageReactions_Messages_message_id",
                        column: x => x.message_id,
                        principalTable: "Messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "MessageStatuses",
                columns: table => new
                {
                    status_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    member_id = table.Column<string>(type: "varchar(8)", maxLength: 8, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    delivered_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MessageStatuses", x => x.status_id);
                    table.ForeignKey(
                        name: "FK_MessageStatuses_ConversationMembers_member_id",
                        column: x => x.member_id,
                        principalTable: "ConversationMembers",
                        principalColumn: "member_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MessageStatuses_Messages_message_id",
                        column: x => x.message_id,
                        principalTable: "Messages",
                        principalColumn: "message_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.InsertData(
                table: "Users",
                columns: new[] { "user_id", "avatar_url", "bio_text", "created_at", "display_name", "email", "hashed_b_key", "hashed_password", "hashed_recovery_key", "key_salt", "public_key", "show_online_status", "show_read_status", "updated_at", "username" },
                values: new object[,]
                {
                    { "U0000001", null, null, new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "Hoang Hieu", "u1@securechat.local", "hash_demo_value", "hash_demo_value", "hash_demo_value", "hash_demo_value", "encrypted_demo_value", true, true, new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "hoanghieu" },
                    { "U0000002", null, null, new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "Minh Quan", "u2@securechat.local", "hash_demo_value", "hash_demo_value", "hash_demo_value", "hash_demo_value", "encrypted_demo_value", true, true, new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "minhquan" },
                    { "U0000003", null, null, new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "Linh Nguyen", "u3@securechat.local", "hash_demo_value", "hash_demo_value", "hash_demo_value", "hash_demo_value", "encrypted_demo_value", true, true, new DateTime(2025, 1, 10, 8, 0, 0, 0, DateTimeKind.Utc), "linhnguyen" }
                });

            migrationBuilder.InsertData(
                table: "BlockedUsers",
                columns: new[] { "block_id", "blocked_id", "blocker_id", "created_at" },
                values: new object[] { "B0000001", "U0000003", "U0000002", new DateTime(2025, 1, 10, 8, 8, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                table: "Conversations",
                columns: new[] { "conversation_id", "avatar_url", "created_at", "created_by", "last_activity_at", "last_message_id", "Name", "conversation_type" },
                values: new object[,]
                {
                    { "C0000001", null, new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), "U0000001", new DateTime(2025, 1, 10, 8, 21, 0, 0, DateTimeKind.Utc), null, null, (byte)0 },
                    { "C0000002", null, new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), "U0000001", new DateTime(2025, 1, 10, 8, 28, 0, 0, DateTimeKind.Utc), null, "NT106 Team", (byte)1 }
                });

            migrationBuilder.InsertData(
                table: "FriendRequests",
                columns: new[] { "request_id", "created_at", "recipient_id", "responded_at", "sender_id", "status" },
                values: new object[] { "RQ000001", new DateTime(2025, 1, 10, 8, 8, 0, 0, DateTimeKind.Utc), "U0000001", null, "U0000003", (byte)0 });

            migrationBuilder.InsertData(
                table: "Friends",
                columns: new[] { "friendship_id", "created_at", "user_a_id", "user_b_id" },
                values: new object[] { "F0000001", new DateTime(2025, 1, 10, 8, 5, 0, 0, DateTimeKind.Utc), "U0000001", "U0000002" });

            migrationBuilder.InsertData(
                table: "UserSessions",
                columns: new[] { "session_id", "created_at", "device_name", "expires_at", "last_used_at", "refresh_token", "user_id" },
                values: new object[,]
                {
                    { "S0000001", new DateTime(2025, 1, 10, 8, 2, 0, 0, DateTimeKind.Utc), "Windows 11 Dev Machine", new DateTime(2025, 2, 9, 8, 2, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 10, 8, 2, 0, 0, DateTimeKind.Utc), "refresh_token_demo_u1", "U0000001" },
                    { "S0000002", new DateTime(2025, 1, 10, 8, 2, 0, 0, DateTimeKind.Utc), "Windows 11 QA Laptop", new DateTime(2025, 2, 9, 8, 2, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 10, 8, 2, 0, 0, DateTimeKind.Utc), "refresh_token_demo_u2", "U0000002" }
                });

            migrationBuilder.InsertData(
                table: "ConversationMembers",
                columns: new[] { "member_id", "banned_until", "conversation_id", "encrypted_key", "joined_at", "last_read_msg_id", "left_at", "nickname", "role", "show_notifications", "user_id" },
                values: new object[,]
                {
                    { "M0000001", null, "C0000001", "encrypted_demo_value", new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), null, null, "Hieu", (byte)2, (byte)2, "U0000001" },
                    { "M0000002", null, "C0000001", "encrypted_demo_value", new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), null, null, "Quan", (byte)0, (byte)2, "U0000002" },
                    { "M0000003", null, "C0000002", "encrypted_demo_value", new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), null, null, "Admin Hieu", (byte)2, (byte)2, "U0000001" },
                    { "M0000004", null, "C0000002", "encrypted_demo_value", new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), null, null, "Mod Quan", (byte)1, (byte)2, "U0000002" },
                    { "M0000005", null, "C0000002", "encrypted_demo_value", new DateTime(2025, 1, 10, 8, 12, 0, 0, DateTimeKind.Utc), null, null, "Linh", (byte)0, (byte)2, "U0000003" }
                });

            migrationBuilder.InsertData(
                table: "CallLogs",
                columns: new[] { "call_id", "conversation_id", "ended_at", "started_at", "started_by", "status", "call_type" },
                values: new object[] { "CL000001", "C0000002", new DateTime(2025, 1, 10, 8, 28, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 10, 8, 24, 0, 0, DateTimeKind.Utc), "M0000003", (byte)2, (byte)0 });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "message_id", "content", "content_iv", "conversation_id", "deleted_at", "edited_at", "original_sender_id", "reply_to_id", "sender_id", "sent_at", "message_type" },
                values: new object[,]
                {
                    { "MSG00001", "hello bro", "iv_demo_value", "C0000001", null, null, "U0000001", null, "M0000001", new DateTime(2025, 1, 10, 8, 15, 0, 0, DateTimeKind.Utc), (byte)0 },
                    { "MSG00003", "encrypt ok ch?a?", "iv_demo_value", "C0000001", null, null, "U0000001", null, "M0000001", new DateTime(2025, 1, 10, 8, 21, 0, 0, DateTimeKind.Utc), (byte)0 },
                    { "MSG00004", "hello team, test group chat nhé", "iv_demo_value", "C0000002", null, null, "U0000001", null, "M0000003", new DateTime(2025, 1, 10, 8, 15, 0, 0, DateTimeKind.Utc), (byte)0 },
                    { "MSG00006", "done r?i, nh? test forgot password", "iv_demo_value", "C0000002", null, null, "U0000003", null, "M0000005", new DateTime(2025, 1, 10, 8, 21, 0, 0, DateTimeKind.Utc), (byte)0 }
                });

            migrationBuilder.InsertData(
                table: "CallParticipants",
                columns: new[] { "call_id", "participant_id", "joined_at", "left_at", "status" },
                values: new object[,]
                {
                    { "CL000001", "M0000003", new DateTime(2025, 1, 10, 8, 24, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 10, 8, 28, 0, 0, DateTimeKind.Utc), (byte)1 },
                    { "CL000001", "M0000004", new DateTime(2025, 1, 10, 8, 24, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 10, 8, 28, 0, 0, DateTimeKind.Utc), (byte)1 },
                    { "CL000001", "M0000005", null, null, (byte)3 }
                });

            migrationBuilder.InsertData(
                table: "MessagePins",
                columns: new[] { "conversation_id", "message_id", "pinned_at", "pinned_by" },
                values: new object[] { "C0000002", "MSG00006", new DateTime(2025, 1, 10, 8, 28, 0, 0, DateTimeKind.Utc), "M0000003" });

            migrationBuilder.InsertData(
                table: "MessageStatuses",
                columns: new[] { "status_id", "delivered_at", "member_id", "message_id", "read_at" },
                values: new object[,]
                {
                    { "ST000001", new DateTime(2025, 1, 10, 8, 15, 0, 0, DateTimeKind.Utc), "M0000002", "MSG00001", new DateTime(2025, 1, 10, 8, 18, 0, 0, DateTimeKind.Utc) },
                    { "ST000003", new DateTime(2025, 1, 10, 8, 21, 0, 0, DateTimeKind.Utc), "M0000003", "MSG00006", new DateTime(2025, 1, 10, 8, 24, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                table: "Messages",
                columns: new[] { "message_id", "content", "content_iv", "conversation_id", "deleted_at", "edited_at", "original_sender_id", "reply_to_id", "sender_id", "sent_at", "message_type" },
                values: new object[,]
                {
                    { "MSG00002", "check API ch?a?", "iv_demo_value", "C0000001", null, null, "U0000002", "MSG00001", "M0000002", new DateTime(2025, 1, 10, 8, 18, 0, 0, DateTimeKind.Utc), (byte)0 },
                    { "MSG00005", "ok bro, SignalR realtime ?n", "iv_demo_value", "C0000002", null, null, "U0000002", "MSG00004", "M0000004", new DateTime(2025, 1, 10, 8, 18, 0, 0, DateTimeKind.Utc), (byte)0 },
                    { "MSG00007", "file sent: api_test_plan.pdf", "iv_demo_value", "C0000002", null, null, "U0000001", "MSG00006", "M0000003", new DateTime(2025, 1, 10, 8, 24, 0, 0, DateTimeKind.Utc), (byte)4 }
                });

            migrationBuilder.InsertData(
                table: "MessageAttachments",
                columns: new[] { "attachment_id", "duration_secs", "file_hash", "file_iv", "file_name", "file_size", "file_type", "file_url", "height", "message_id", "thumbnail_iv", "thumbnail_url", "uploaded_at", "width" },
                values: new object[] { "A0000001", null, "hash_demo_value", "iv_demo_value", "api_test_plan.pdf", 102400L, "application/pdf", "encrypted_demo_value", null, "MSG00007", null, null, new DateTime(2025, 1, 10, 8, 24, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.InsertData(
                table: "MessageMentions",
                columns: new[] { "member_id", "message_id" },
                values: new object[] { "M0000005", "MSG00005" });

            migrationBuilder.InsertData(
                table: "MessageReactions",
                columns: new[] { "reaction_id", "created_at", "member_id", "message_id", "reaction" },
                values: new object[] { "RE000001", new DateTime(2025, 1, 10, 8, 21, 0, 0, DateTimeKind.Utc), "M0000001", "MSG00002", "??" });

            migrationBuilder.InsertData(
                table: "MessageStatuses",
                columns: new[] { "status_id", "delivered_at", "member_id", "message_id", "read_at" },
                values: new object[] { "ST000002", new DateTime(2025, 1, 10, 8, 18, 0, 0, DateTimeKind.Utc), "M0000001", "MSG00002", new DateTime(2025, 1, 10, 8, 21, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "idx_is_blocked",
                table: "BlockedUsers",
                column: "blocker_id");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedUsers_blocked_id",
                table: "BlockedUsers",
                column: "blocked_id");

            migrationBuilder.CreateIndex(
                name: "uq_block_pair",
                table: "BlockedUsers",
                columns: new[] { "blocker_id", "blocked_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_call_logs_conversation",
                table: "CallLogs",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_CallLogs_started_by",
                table: "CallLogs",
                column: "started_by");

            migrationBuilder.CreateIndex(
                name: "idx_call_participants_user",
                table: "CallParticipants",
                column: "participant_id");

            migrationBuilder.CreateIndex(
                name: "IX_CallParticipants_call_id",
                table: "CallParticipants",
                column: "call_id");

            migrationBuilder.CreateIndex(
                name: "idx_conv_members_user",
                table: "ConversationMembers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ConversationMembers_last_read_msg_id",
                table: "ConversationMembers",
                column: "last_read_msg_id");

            migrationBuilder.CreateIndex(
                name: "uq_convmems_user",
                table: "ConversationMembers",
                columns: new[] { "conversation_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_conv_activity",
                table: "Conversations",
                column: "last_activity_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_created_by",
                table: "Conversations",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_last_message_id",
                table: "Conversations",
                column: "last_message_id");

            migrationBuilder.CreateIndex(
                name: "idx_friend_req_receiver",
                table: "FriendRequests",
                columns: new[] { "recipient_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_friendreq_pair",
                table: "FriendRequests",
                columns: new[] { "sender_id", "recipient_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Friends_user_b_id",
                table: "Friends",
                column: "user_b_id");

            migrationBuilder.CreateIndex(
                name: "uq_friends_pair",
                table: "Friends",
                columns: new[] { "user_a_id", "user_b_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_attachments_hash",
                table: "MessageAttachments",
                column: "file_hash");

            migrationBuilder.CreateIndex(
                name: "IX_MessageAttachments_message_id",
                table: "MessageAttachments",
                column: "message_id");

            migrationBuilder.CreateIndex(
                name: "idx_mentions_user",
                table: "MessageMentions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "idx_message_pins_conv",
                table: "MessagePins",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "IX_MessagePins_pinned_by",
                table: "MessagePins",
                column: "pinned_by");

            migrationBuilder.CreateIndex(
                name: "IX_MessageReactions_member_id",
                table: "MessageReactions",
                column: "member_id");

            migrationBuilder.CreateIndex(
                name: "uq_reaction_msg_users",
                table: "MessageReactions",
                columns: new[] { "message_id", "member_id", "reaction" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_messages_conversation",
                table: "Messages",
                columns: new[] { "conversation_id", "sent_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "idx_messages_sender",
                table: "Messages",
                column: "sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_original_sender_id",
                table: "Messages",
                column: "original_sender_id");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_reply_to_id",
                table: "Messages",
                column: "reply_to_id");

            migrationBuilder.CreateIndex(
                name: "idx_msg_status_user",
                table: "MessageStatuses",
                columns: new[] { "member_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "uq_status_msg_user",
                table: "MessageStatuses",
                columns: new[] { "message_id", "member_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "Users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_users_username",
                table: "Users",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_user_id",
                table: "UserSessions",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uq_sessions_refresh_token",
                table: "UserSessions",
                column: "refresh_token",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_ConversationMembers_started_by",
                table: "CallLogs",
                column: "started_by",
                principalTable: "ConversationMembers",
                principalColumn: "member_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallLogs_Conversations_conversation_id",
                table: "CallLogs",
                column: "conversation_id",
                principalTable: "Conversations",
                principalColumn: "conversation_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_CallParticipants_ConversationMembers_participant_id",
                table: "CallParticipants",
                column: "participant_id",
                principalTable: "ConversationMembers",
                principalColumn: "member_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMembers_Conversations_conversation_id",
                table: "ConversationMembers",
                column: "conversation_id",
                principalTable: "Conversations",
                principalColumn: "conversation_id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ConversationMembers_Messages_last_read_msg_id",
                table: "ConversationMembers",
                column: "last_read_msg_id",
                principalTable: "Messages",
                principalColumn: "message_id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Conversations_Messages_last_message_id",
                table: "Conversations",
                column: "last_message_id",
                principalTable: "Messages",
                principalColumn: "message_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConversationMembers_Users_user_id",
                table: "ConversationMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_Conversations_Users_created_by",
                table: "Conversations");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Users_original_sender_id",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_ConversationMembers_sender_id",
                table: "Messages");

            migrationBuilder.DropForeignKey(
                name: "FK_Messages_Conversations_conversation_id",
                table: "Messages");

            migrationBuilder.DropTable(
                name: "BlockedUsers");

            migrationBuilder.DropTable(
                name: "CallParticipants");

            migrationBuilder.DropTable(
                name: "FriendRequests");

            migrationBuilder.DropTable(
                name: "Friends");

            migrationBuilder.DropTable(
                name: "MessageAttachments");

            migrationBuilder.DropTable(
                name: "MessageMentions");

            migrationBuilder.DropTable(
                name: "MessagePins");

            migrationBuilder.DropTable(
                name: "MessageReactions");

            migrationBuilder.DropTable(
                name: "MessageStatuses");

            migrationBuilder.DropTable(
                name: "UserSessions");

            migrationBuilder.DropTable(
                name: "CallLogs");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "ConversationMembers");

            migrationBuilder.DropTable(
                name: "Conversations");

            migrationBuilder.DropTable(
                name: "Messages");
        }
    }
}
