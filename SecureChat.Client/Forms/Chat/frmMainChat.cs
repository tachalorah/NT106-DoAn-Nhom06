using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace SecureChat.Client
{
    public partial class frmMainChat : Form
    {
        // ── Layout panels ──────────────────────────
        private Panel _pnlSidebar = null!;       // 280px bên trái
        private Panel _pnlChat;          // phần còn lại
        private Panel _pnlSettingsMenu;  // slide ra từ bên PHẢI đè lên chat area
        private bool _settingsVisible = false;
        private System.Windows.Forms.Timer _slideTimer;
        private int _settingsTargetX;  // vị trí X đích khi animate

        // ── Sidebar controls ───────────────────────
        private Button _btnHamburger;
        private TelegramTextBox _tbSearch;
        private Panel _pnlConvList;

        // ── Chat area controls ─────────────────────
        private Panel _pnlChatHeader;
        private Panel _pnlMessages;
        private Panel _pnlInputBar;
        private TelegramTextBox _tbMessage;
        private Label _lblChatName, _lblChatStatus;
        private AvatarControl _chatAvatar;
        private ContextMenuStrip _chatMoreMenu;
        private ToolStripMenuItem _mnuMuteNotifications;
        private ToolStripMenuItem _mnuUnmuteNow;
        private ToolStripMenuItem _mnuDisableSound;
        private ToolStripMenuItem _mnuMuteForever;
        private ToolStripMenuItem _mnuMuteFor;
        private ToolStripItem[] _muteOptionItems;
        private bool _notificationsMuted;
        private bool _notificationsSoundEnabled = true;
        private DateTime? _muteUntilUtc;

        // ── Settings menu controls ─────────────────
        private Panel _pnlSettingsHeader;

        // ── Mock data ──────────────────────────────
        private string _activeConvId = "1";

        private readonly List<(string Id, string Name, string Preview, string Time, int Unread, bool IsGroup)> _convs = new()
        {
            ("1", "Telegram",    "Quack Cyber added Sim 18a3",     "10:10 PM", 0,  false),
            ("2", "dk test",     "Quack Cyber: Hello hello hello!", "12:51 PM", 0,  false),
            ("3", "Tuấn Thành",  "Sure",                           "10 Mar",   0,  false),
        };

        private readonly List<(string Text, bool Out, string Time)> _msgs = new()
        {
            ("Hello hello hello!",                                                                      false, "12:51 PM"),
            ("Tuấn Thành you've been removed from the group chat",                                      false, "1:01 PM"),
            ("Quack Cyber added Sim 18a3",                                                              false, "10:10 PM"),
            ("Tuấn Thành, you've been wonderful friends for so long. I could never imagine you doing this to me.", true, "10:15 PM"),
            ("Search it",                                                                                true,  "10:16 PM"),
        };

        private readonly Dictionary<string, bool> _settingsToggles = new();


        public frmMainChat()
        {
            InitUI();
        }

        // ════════════════════════════════════════════
        //  INIT
        // ════════════════════════════════════════════
        private void InitUI()
        {
            Text = "SecureChat";
            Size = new Size(1000, 660);
            MinimumSize = new Size(760, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.White;
            Font = TG.FontRegular(9.5f);

            BuildSidebar();
            BuildChatArea();
            BuildSettingsMenu();

            // Thứ tự add: sidebar → chat → settings (settings ở trên cùng)
            Controls.Add(_pnlChat);
            Controls.Add(_pnlSidebar);
            Controls.Add(_pnlSettingsMenu);  // add cuối = hiện trên cùng

            Resize += (s, e) => AdjustLayout();
            AdjustLayout();

            LoadConversation("1");
            // inside InitUI(), after LoadConversation("1");
            SetupWallpaperWatcher();
        }

        private void AdjustLayout()
        {
            int sw = 280;                        // sidebar width
            int smw = 260;                       // settings menu width
            _pnlSidebar.SetBounds(0, 0, sw, ClientSize.Height);
            _pnlChat.SetBounds(sw, 0, ClientSize.Width - sw, ClientSize.Height);

            // Settings menu: slide overlay từ bên TRÁI
            // visibleX = 0 (menu dán vào mép trái của form)
            // hiddenX  = -smw (ẩn ngoài bên trái)
            int visibleX = 0;
            int hiddenX = -smw;
            if (!_settingsVisible)
                _pnlSettingsMenu.SetBounds(hiddenX, 0, smw, ClientSize.Height);
            else
                _pnlSettingsMenu.SetBounds(visibleX, 0, smw, ClientSize.Height);
        }

        // ════════════════════════════════════════════
        //  SIDEBAR
        // ════════════════════════════════════════════
        private void BuildSidebar()
        {
            _pnlSidebar = new Panel { BackColor = Color.White };

            // ── Header xanh ──────────────────────────
            var pnlHeader = new Panel { Height = 52, BackColor = TG.TitleBarBg, Dock = DockStyle.Top };

            _btnHamburger = new Button
            {
                Text = "☰",
                FlatStyle = FlatStyle.Flat,
                Font = TG.FontRegular(14f),
                ForeColor = Color.White,
                Size = new Size(48, 52),
                Location = new Point(0, 0),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            _btnHamburger.FlatAppearance.BorderSize = 0;
            _btnHamburger.Click += (s, e) => ToggleSettingsMenu();

            var lblTitle = new Label
            {
                Text = "SecureChat",
                Font = TG.FontSemiBold(11f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(50, 0),
                Height = 52,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var btnEdit = new Button
            {
                Text = "✏",
                FlatStyle = FlatStyle.Flat,
                Font = TG.FontRegular(13f),
                ForeColor = Color.White,
                Size = new Size(40, 52),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            btnEdit.FlatAppearance.BorderSize = 0;

            pnlHeader.Controls.AddRange(new Control[] { _btnHamburger, lblTitle, btnEdit });
            pnlHeader.Resize += (s, e) =>
            {
                lblTitle.Width = pnlHeader.Width - 96;
                btnEdit.Location = new Point(pnlHeader.Width - 42, 0);
            };

            // ── Search ────────────────────────────────
            var pnlSearch = new Panel { Height = 44, Dock = DockStyle.Top, BackColor = Color.White, Padding = new Padding(8, 6, 8, 4) };
            _tbSearch = new TelegramTextBox { Height = 32, Dock = DockStyle.Fill };
            _tbSearch.SetPlaceholder("🔍  Search");
            pnlSearch.Controls.Add(_tbSearch);

            // ── Conversation list ─────────────────────
            _pnlConvList = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            BuildConvList();

            _pnlSidebar.Controls.Add(_pnlConvList);
            _pnlSidebar.Controls.Add(pnlSearch);
            _pnlSidebar.Controls.Add(pnlHeader);
        }

        private void BuildConvList()
        {
            _pnlConvList.Controls.Clear();
            int y = 0;
            foreach (var c in _convs)
            {
                var row = BuildConvRow(c.Id, c.Name, c.Preview, c.Time, c.Unread, c.IsGroup);
                row.Location = new Point(0, y);
                // Let the row resize automatically with the parent
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                row.Width = _pnlConvList.ClientSize.Width;
                _pnlConvList.Controls.Add(row);
                y += 68;
            }
        }

        private Panel BuildConvRow(string id, string name, string preview, string time, int unread, bool isGroup)
        {
            var pnl = new Panel { Height = 68, BackColor = Color.White, Tag = id, Cursor = Cursors.Hand };

            bool isActive() => (string)pnl.Tag == _activeConvId;

            // Avatar
            var avatar = new AvatarControl { Size = new Size(48, 48), Location = new Point(10, 10) };
            avatar.SetName(name);
            avatar.ShowOnline = false;

            // Name + preview
            var lblName = new Label
            {
                Text = name,
                Font = TG.FontSemiBold(9.5f),
                ForeColor = TG.TextName,
                AutoSize = false,
                Height = 20,
                Location = new Point(66, 10),
                BackColor = Color.Transparent,
            };
            var lblPreview = new Label
            {
                Text = preview,
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false,
                Height = 18,
                Location = new Point(66, 32),
                BackColor = Color.Transparent,
                AutoEllipsis = true
            };

            // Time (top-right)
            var lblTime = new Label
            {
                Text = time,
                Font = TG.FontRegular(7.5f),
                ForeColor = TG.TextTime,
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            // Unread badge (rounded painted)
            var lblUnread = new Label
            {
                Size = new Size(22, 22),
                AutoSize = false,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = TG.FontSemiBold(8f),
                Visible = unread > 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            lblUnread.Paint += (s, e) =>
            {
                if (!lblUnread.Visible) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(TG.Blue);
                e.Graphics.FillEllipse(brush, 0, 0, lblUnread.Width - 1, lblUnread.Height - 1);
                string txt = unread > 99 ? "99+" : unread.ToString();
                using var fore = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(txt, lblUnread.Font, fore, lblUnread.ClientRectangle, sf);
            };

            // Add controls
            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblPreview, lblTime, lblUnread });

            // Click behavior
            Action doClick = () => { _activeConvId = id; BuildConvList(); LoadConversation(id); };
            pnl.Click += (s, e) => doClick();
            avatar.Click += (s, e) => doClick();

            // Hover + selection visuals
            pnl.MouseEnter += (s, e) => { if (!isActive()) pnl.BackColor = TG.SidebarHover; };
            pnl.MouseLeave += (s, e) => { if (!isActive()) pnl.BackColor = Color.White; };

            // Resize child layout when row width changes
            pnl.Resize += (s, e) =>
            {
                // Clamp to prevent negative widths when parent is narrow
                lblName.Width = Math.Max(0, pnl.Width - 66 - 70);
                lblPreview.Width = Math.Max(0, pnl.Width - 66 - 12);
                lblTime.Location = new Point(pnl.Width - lblTime.Width - 12, 12);
                lblUnread.Location = new Point(pnl.Width - lblUnread.Width - 12, 34);
                // Update colors for active state
                if (isActive())
                {
                    pnl.BackColor = TG.SidebarActive;
                    lblName.ForeColor = Color.White;
                    lblPreview.ForeColor = Color.FromArgb(220, 240, 255);
                    lblTime.ForeColor = Color.FromArgb(200, 230, 255);
                }
                else
                {
                    pnl.BackColor = Color.White;
                    lblName.ForeColor = TG.TextName;
                    lblPreview.ForeColor = TG.TextSecondary;
                    lblTime.ForeColor = TG.TextTime;
                }
            };

            // Propagate click/hover for child controls
            foreach (Control c in pnl.Controls)
            {
                if (c == avatar) continue; // avatar wired already
                c.Click += (s, e) => doClick();
                c.MouseEnter += (s, e) => { if (!isActive()) pnl.BackColor = TG.SidebarHover; };
                c.MouseLeave += (s, e) => { if (!isActive()) pnl.BackColor = Color.White; };
            }

            // initialize positions (so anchors work correctly right away)
            pnl.Width = _pnlConvList?.ClientSize.Width ?? pnl.Width;
            pnl.PerformLayout();

            return pnl;
        }

        // ════════════════════════════════════════════
        //  CHAT AREA
        // ════════════════════════════════════════════
        private void BuildChatArea()
        {
            _pnlChat = new Panel { BackColor = Color.White };

            // ── Chat Header ───────────────────────────
            _pnlChatHeader = new Panel { Height = 52, BackColor = Color.White, Dock = DockStyle.Top };
            _pnlChatHeader.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(TG.Divider), 0, 51, _pnlChatHeader.Width, 51);

            _chatAvatar = new AvatarControl { Size = new Size(36, 36), Location = new Point(12, 8) };
            _lblChatName = new Label
            {
                Font = TG.FontSemiBold(10f),
                ForeColor = TG.TextPrimary,
                AutoSize = false,
                Height = 22,
                Location = new Point(56, 8),
                BackColor = Color.Transparent,
            };
            _lblChatStatus = new Label
            {
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false,
                Height = 16,
                Location = new Point(56, 30),
                BackColor = Color.Transparent,
            };

            // Right buttons: search, video call, more
            var btnSearch = MakeChatHeaderBtn("🔍");
            var btnVideo = MakeChatHeaderBtn("📹");
            var btnMore = MakeChatHeaderBtn("⋮");
            btnMore.Click += (s, e) =>
            {
                EnsureChatMoreMenu();
                _chatMoreMenu.Show(btnMore, new Point(btnMore.Width - _chatMoreMenu.Width, btnMore.Height + 2));
            };

            _pnlChatHeader.Controls.AddRange(new Control[] { _chatAvatar, _lblChatName, _lblChatStatus, btnSearch, btnVideo, btnMore });
            _pnlChatHeader.Resize += (s, e) =>
            {
                _lblChatName.Width = _pnlChatHeader.Width - 56 - 130;
                _lblChatStatus.Width = _pnlChatHeader.Width - 56 - 130;
                btnMore.Location = new Point(_pnlChatHeader.Width - 42, 8);
                btnVideo.Location = new Point(_pnlChatHeader.Width - 84, 8);
                btnSearch.Location = new Point(_pnlChatHeader.Width - 126, 8);
            };

            // ── Messages area ─────────────────────────
            _pnlMessages = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(12, 8, 12, 8),
            };

            typeof(Panel).InvokeMember("DoubleBuffered",
    System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
    null, _pnlMessages, new object[] { true });
            _pnlMessages.Paint += PaintChatBackground;
            // Click vào vùng chat → đóng settings menu
            _pnlMessages.Click += (s, e) => { if (_settingsVisible) HideSettingsMenu(); };

            // ── Input bar ─────────────────────────────
            BuildInputBar();

            _pnlChat.Controls.Add(_pnlMessages);
            _pnlChat.Controls.Add(_pnlInputBar);
            _pnlChat.Controls.Add(_pnlChatHeader);

            _pnlMessages.Resize += (s, e) => _pnlMessages.Invalidate();
        }

        private void EnsureChatMoreMenu()
        {
            if (_chatMoreMenu != null) return;

            _chatMoreMenu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                BackColor = Color.White,
                ForeColor = TG.TextPrimary,
                Font = new Font("Segoe UI", 10f),
                Renderer = new ToolStripProfessionalRenderer(new ChatMenuColorTable())
            };

            _mnuMuteNotifications = CreateChatMenuItem("🔕  Mute notifications", (_, __) => ToggleMuteNotificationsQuick());

            _mnuUnmuteNow = CreateChatMenuItem("🔊  Unmute now", (_, __) => UnmuteNow());
            _mnuDisableSound = CreateChatMenuItem("🔇  Disable sound", (_, __) => ToggleDisableSound());
            _mnuMuteForever = CreateChatMenuItem("⛔  Mute forever", (_, __) => SetMuteForever());
            _mnuMuteFor = CreateChatMenuItem("⏳  Mute for...", null);

            _mnuMuteFor.DropDownItems.Add(CreateChatSubMenuItem("30 minutes", (_, __) => SetMuteFor(TimeSpan.FromMinutes(30))));
            _mnuMuteFor.DropDownItems.Add(CreateChatSubMenuItem("1 hour", (_, __) => SetMuteFor(TimeSpan.FromHours(1))));
            _mnuMuteFor.DropDownItems.Add(CreateChatSubMenuItem("8 hours", (_, __) => SetMuteFor(TimeSpan.FromHours(8))));
            _mnuMuteFor.DropDownItems.Add(CreateChatSubMenuItem("1 day", (_, __) => SetMuteFor(TimeSpan.FromDays(1))));
            _mnuMuteFor.DropDownItems.Add(CreateChatSubMenuItem("1 week", (_, __) => SetMuteFor(TimeSpan.FromDays(7))));

            _muteOptionItems = new ToolStripItem[]
            {
                _mnuUnmuteNow,
                _mnuDisableSound,
                _mnuMuteForever,
                _mnuMuteFor
            };

            foreach (var item in _muteOptionItems)
                _mnuMuteNotifications.DropDownItems.Add(item);

            var mnuViewInfo = CreateChatMenuItem("ℹ️  View group info", (_, __) => OpenGroupInfo());
            var mnuManageGroup = CreateChatMenuItem("🎛️  Manage group", (_, __) => OpenEditGroupFromChat());
            var mnuCreatePoll = CreateChatMenuItem("📊  Create poll", (_, __) => CreatePoll());
            var mnuClearHistory = CreateChatMenuItem("🧹  Clear history", (_, __) => ClearHistory());
            var mnuDeleteLeave = CreateChatMenuItem("🚪  Delete and leave", (_, __) => DeleteAndLeave(), Color.FromArgb(0xE2, 0x4B, 0x4A));

            _chatMoreMenu.Items.Add(_mnuMuteNotifications);
            _chatMoreMenu.Items.Add(new ToolStripSeparator());
            _chatMoreMenu.Items.Add(mnuViewInfo);
            _chatMoreMenu.Items.Add(mnuManageGroup);
            _chatMoreMenu.Items.Add(mnuCreatePoll);
            _chatMoreMenu.Items.Add(mnuClearHistory);
            _chatMoreMenu.Items.Add(new ToolStripSeparator());
            _chatMoreMenu.Items.Add(mnuDeleteLeave);

            RefreshMuteMenuState();
        }

        private ToolStripMenuItem CreateChatMenuItem(string text, EventHandler onClick, Color? foreColor = null)
        {
            var item = new ToolStripMenuItem
            {
                Text = text,
                ForeColor = foreColor ?? TG.TextPrimary,
                Font = new Font("Segoe UI Emoji", 10f),
                Padding = new Padding(10, 8, 10, 8)
            };
            item.Click += onClick;
            return item;
        }

        private ToolStripMenuItem CreateChatSubMenuItem(string text, EventHandler onClick)
        {
            var item = new ToolStripMenuItem
            {
                Text = text,
                ForeColor = TG.TextPrimary,
                Font = new Font("Segoe UI", 10f),
                Padding = new Padding(8, 6, 8, 6)
            };
            item.Click += onClick;
            return item;
        }

        private void ToggleDisableSound()
        {
            _notificationsSoundEnabled = !_notificationsSoundEnabled;
            RefreshMuteMenuState();
            MessageBox.Show(this,
                _notificationsSoundEnabled ? "Notification sound enabled for this chat." : "Notification sound disabled for this chat.",
                "Notifications",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SetMuteForever()
        {
            // Toggle behavior: choose again => unmute forever off
            if (_notificationsMuted && !_muteUntilUtc.HasValue)
            {
                UnmuteNow();
                return;
            }

            _notificationsMuted = true;
            _muteUntilUtc = null;
            RefreshMuteMenuState();
            MessageBox.Show(this,
                "Notifications are muted forever for this chat.",
                "Notifications",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void SetMuteFor(TimeSpan duration)
        {
            _notificationsMuted = true;
            _muteUntilUtc = DateTime.UtcNow.Add(duration);
            RefreshMuteMenuState();
            MessageBox.Show(this,
                $"Notifications are muted for {FormatDuration(duration)}.",
                "Notifications",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration.TotalMinutes < 60) return $"{(int)duration.TotalMinutes} minutes";
            if (duration.TotalHours < 24) return $"{(int)duration.TotalHours} hours";
            return $"{(int)duration.TotalDays} days";
        }

        private void RefreshMuteMenuState()
        {
            if (_mnuMuteNotifications == null) return;

            if (_muteUntilUtc.HasValue && DateTime.UtcNow >= _muteUntilUtc.Value)
            {
                _notificationsMuted = false;
                _muteUntilUtc = null;
            }

            // If mute forever -> show direct "Unmute" action without submenu popup.
            if (_notificationsMuted && !_muteUntilUtc.HasValue)
            {
                if (_mnuMuteNotifications.DropDownItems.Count > 0)
                    _mnuMuteNotifications.DropDownItems.Clear();

                _mnuMuteNotifications.Text = "🔊  Unmute";
            }
            else
            {
                if (_mnuMuteNotifications.DropDownItems.Count == 0)
                {
                    foreach (var item in _muteOptionItems)
                        _mnuMuteNotifications.DropDownItems.Add(item);
                }

                _mnuMuteNotifications.Text = _notificationsMuted
                    ? $"🔕  Muted until {_muteUntilUtc.Value.ToLocalTime():HH:mm}"
                    : "🔔  Mute notifications";
            }

            if (_mnuUnmuteNow != null)
                _mnuUnmuteNow.Visible = _notificationsMuted;
            if (_mnuDisableSound != null)
                _mnuDisableSound.Checked = !_notificationsSoundEnabled;
            if (_mnuMuteForever != null)
                _mnuMuteForever.Checked = _notificationsMuted && !_muteUntilUtc.HasValue;
            if (_mnuMuteFor != null)
                _mnuMuteFor.Checked = _notificationsMuted && _muteUntilUtc.HasValue;
        }

        private void ToggleMuteNotificationsQuick()
        {
            // Direct toggle only when menu is in "Unmute" mode.
            if (_notificationsMuted && !_muteUntilUtc.HasValue)
            {
                UnmuteNow();
            }
        }

        private void UnmuteNow()
        {
            _notificationsMuted = false;
            _muteUntilUtc = null;
            RefreshMuteMenuState();

            MessageBox.Show(this,
                "Notifications are enabled for this chat.",
                "Notifications",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void OpenGroupInfo()
        {
            using var dlg = new SecureChat.Client.Forms.Chat.frmGroupInfo();
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ShowDialog(this);
        }

        private void OpenEditGroupFromChat()
        {
            using var dlg = new SecureChat.Client.Forms.Chat.frmEditGroup(_lblChatName.Text);
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            _lblChatName.Text = dlg.GroupName;
        }

        private void CreatePoll()
        {
            using var dlg = new SecureChat.Client.Forms.Chat.frmCreatePoll();
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            var pollText = $"📊 {dlg.PollQuestion}";
            if (!string.IsNullOrWhiteSpace(dlg.PollDescription))
                pollText += $"\n{dlg.PollDescription}";

            for (var i = 0; i < dlg.PollOptions.Count; i++)
                pollText += $"\n{i + 1}. {dlg.PollOptions[i]}";

            _msgs.Add((pollText, true, DateTime.Now.ToString("h:mm tt")));
            BuildMessages();
        }

        private void ClearHistory()
        {
            using var dlg = new SecureChat.Client.Forms.Chat.frmClearHistory(_lblChatName.Text);
            if (dlg.ShowDialog(this) != DialogResult.OK || !dlg.DeleteConfirmed)
                return;

            _msgs.Clear();
            BuildMessages();
        }

        private void DeleteAndLeave()
        {
            var members = new[] { "Duck Cyber", "Sim 18a3", "Tuấn Thành", "Hoang Hieu" };
            using var dlg = new SecureChat.Client.Forms.Chat.frmLeaveGroup(_lblChatName.Text, "Duck Cyber", members);
            if (dlg.ShowDialog(this) != DialogResult.OK || !dlg.LeaveConfirmed)
                return;

            _msgs.Clear();
            BuildMessages();
            _lblChatStatus.Text = $"You left this group · New owner: {dlg.AppointedAdminName}";
        }

        // Cache ảnh nền để không load lại mỗi lần paint
        private Image _wallpaper = null;
        private bool _wallpaperLoaded = false;


        private Image LoadWallpaper()
        {
            if (_wallpaperLoaded) return _wallpaper;
            _wallpaperLoaded = true;

            string imagesDir = Path.Combine(Application.StartupPath, "Resources", "Images");
            string[] candidates = {
        Path.Combine(imagesDir, "chat_bg.jpg"),
        Path.Combine(imagesDir, "chat_bg.png"),
        Path.Combine(imagesDir, "wallpaper.jpg"),
        Path.Combine(imagesDir, "wallpaper.png"),
        Path.Combine(imagesDir, "background.jpg"),
        Path.Combine(imagesDir, "background.png"),
    };

            foreach (var path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    // read bytes into memory to avoid locking the file on disk
                    var data = File.ReadAllBytes(path);
                    using var ms = new MemoryStream(data);
                    using var img = Image.FromStream(ms);
                    _wallpaper = new Bitmap(img); // clone so stream can be disposed
                    return _wallpaper;
                }
                catch
                {
                    // ignore and try next candidate
                }
            }

            // fallback gradient
            _wallpaper = CreateFallbackWallpaper(800, 600);
            return _wallpaper;
        }

        // add inside frmMainChat class
        private FileSystemWatcher? _wallpaperWatcher;

        private void SetupWallpaperWatcher()
        {
            try
            {
                string dir = Path.Combine(Application.StartupPath, "Resources", "Images");
                if (!Directory.Exists(dir)) return;

                _wallpaperWatcher = new FileSystemWatcher(dir)
                {
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite | NotifyFilters.Size,
                    Filter = "*.*",
                    IncludeSubdirectories = false,
                    EnableRaisingEvents = true
                };

                _wallpaperWatcher.Changed += OnWallpaperFileChanged;
                _wallpaperWatcher.Created += OnWallpaperFileChanged;
                _wallpaperWatcher.Renamed += OnWallpaperFileChanged;
                _wallpaperWatcher.Deleted += OnWallpaperFileChanged;
            }
            catch
            {
                // ignore watcher setup errors
            }
        }

        private void OnWallpaperFileChanged(object sender, FileSystemEventArgs e)
        {
            // only react to the names LoadWallpaper() searches for
            string[] names = { "chat_bg.jpg", "chat_bg.png", "wallpaper.jpg", "wallpaper.png", "background.jpg", "background.png" };
            string file = Path.GetFileName(e.FullPath);
            bool match = false;
            foreach (var n in names) { if (string.Equals(n, file, StringComparison.OrdinalIgnoreCase)) { match = true; break; } }
            if (!match) return;

            // short delay to let writers finish, then reload on UI thread
            Task.Delay(200).ContinueWith(_ =>
            {
                if (IsHandleCreated) BeginInvoke(new Action(ReloadWallpaper));
            }, TaskScheduler.Default);
        }

        private void ReloadWallpaper()
        {
            _wallpaperLoaded = false;
            _wallpaper?.Dispose();
            _wallpaper = null;
            _pnlMessages?.Invalidate();
        }

        private Bitmap CreateFallbackWallpaper(int w, int h)
        {
            var bmp = new Bitmap(w, h);
            using var g = Graphics.FromImage(bmp);
            using var brush = new LinearGradientBrush(
                new Rectangle(0, 0, w, h),
                Color.FromArgb(0xDB, 0xE8, 0xD5),
                Color.FromArgb(0xB5, 0xCC, 0xA8),
                LinearGradientMode.Vertical);
            g.FillRectangle(brush, 0, 0, w, h);
            return bmp;
        }

        private void PaintChatBackground(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;
            var img = LoadWallpaper();
            if (img != null)
            {
                // Sử dụng ClientRectangle để lấy vùng hiển thị thực tế
                Rectangle displayRect = panel.ClientRectangle;

                float scaleX = (float)displayRect.Width / img.Width;
                float scaleY = (float)displayRect.Height / img.Height;
                float scale = Math.Max(scaleX, scaleY);

                int drawW = (int)(img.Width * scale);
                int drawH = (int)(img.Height * scale);

                // Căn giữa hình ảnh
                int offsetX = (displayRect.Width - drawW) / 2;
                int offsetY = (displayRect.Height - drawH) / 2;

                e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                e.Graphics.SmoothingMode = SmoothingMode.HighQuality;

                // Vẽ hình nền
                e.Graphics.DrawImage(img, offsetX, offsetY, drawW, drawH);

            }
        }

        private Button MakeChatHeaderBtn(string icon)
        {
            var btn = new Button
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 13f),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                BackColor = Color.Transparent,
                ForeColor = TG.TextSecondary,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(15, 0, 0, 0);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 0, 0, 0);
            return btn;
        }


        private void BuildInputBar()
        {
            _pnlInputBar = new Panel
            {
                Height = 56,
                Dock = DockStyle.Bottom,
                BackColor = Color.White,
            };
            _pnlInputBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(TG.Divider), 0, 0, _pnlInputBar.Width, 0);

            // Layout: [📎] [text field] [😊] [🎤 / ↑]
            var btnAttach = MakeInputBtn("📎");

            _tbMessage = new TelegramTextBox { Height = 36 };
            _tbMessage.SetPlaceholder("Write a message...");
            _tbMessage.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Return && !e.Shift) { e.SuppressKeyPress = true; SendMessage(); }
            };

            var btnEmoji = MakeInputBtn("😊");
            var btnMic = MakeInputBtn("🎤");
            var btnSend = new TelegramButton
            {
                Text = "↑",
                Height = 36,
                Width = 36,
                Font = TG.FontSemiBold(14f),
                Radius = 18,
                Visible = false,
            };
            btnSend.Click += (s, e) => SendMessage();

            _tbMessage.TextChanged += (s, e) =>
            {
                bool hasText = !string.IsNullOrWhiteSpace(_tbMessage.Text);
                btnSend.Visible = hasText;
                btnMic.Visible = !hasText;
            };

            _pnlInputBar.Controls.AddRange(new Control[] { btnAttach, _tbMessage, btnEmoji, btnMic, btnSend });
            _pnlInputBar.Resize += (s, e) =>
            {
                int y = 10;
                btnAttach.Location = new Point(8, y);
                btnEmoji.Location = new Point(Math.Max(0, _pnlInputBar.Width - 84), y);
                btnMic.Location = new Point(Math.Max(0, _pnlInputBar.Width - 44), y);
                btnSend.Location = new Point(Math.Max(0, _pnlInputBar.Width - 44), y);
                _tbMessage.SetBounds(btnAttach.Right + 6, y,
                    Math.Max(0, btnEmoji.Left - btnAttach.Right - 12), 36);
            };
        }

        private Button MakeInputBtn(string icon)
        {
            var btn = new Button
            {
                Text = icon,
                Font = new Font("Segoe UI Emoji", 14f),
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                BackColor = Color.Transparent,
                ForeColor = TG.Blue,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(15, 0, 0, 0);
            btn.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 0, 0, 0);
            return btn;
        }

        // ════════════════════════════════════════════
        //  SETTINGS MENU (slide overlay từ trái)
        // ════════════════════════════════════════════
        private void BuildSettingsMenu()
        {
            int smw = 260;
            _pnlSettingsMenu = new Panel
            {
                BackColor = Color.White,
                Visible = true,   // luôn visible, chỉ di chuyển X
                Width = smw,
                Left = -smw,      // bắt đầu ẩn ngoài màn hình bên trái
            };
            // Border trái để phân cách với chat area
            _pnlSettingsMenu.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(TG.Divider), 0, 0, 0, _pnlSettingsMenu.Height);

            // ── Slide animation timer ─────────────────
            _slideTimer = new System.Windows.Forms.Timer { Interval = 12 };
            _slideTimer.Tick += (s, e) =>
            {
                int cur = _pnlSettingsMenu.Left;
                int target = _settingsTargetX;
                int step = (target - cur) / 3;
                if (Math.Abs(step) < 2) step = target > cur ? 2 : -2;

                int next = cur + step;
                if ((step > 0 && next >= target) || (step < 0 && next <= target))
                {
                    _pnlSettingsMenu.Left = target;
                    _slideTimer.Stop();
                    // Nếu vừa ẩn xong thì không cần làm gì thêm
                }
                else
                {
                    _pnlSettingsMenu.Left = next;
                }
            };

            // ── Header với avatar user ─────────────────
            _pnlSettingsHeader = new Panel
            {
                Height = 120,
                BackColor = TG.TitleBarBg,
                Dock = DockStyle.Top,
            };

            var btnClose = new Button
            {
                Text = "✕",
                FlatStyle = FlatStyle.Flat,
                Font = TG.FontRegular(12f),
                ForeColor = Color.White,
                Size = new Size(36, 36),
                Location = new Point(8, 8),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.Click += (s, e) => HideSettingsMenu();

            var userAvatar = new AvatarControl { Size = new Size(56, 56), Location = new Point(14, 42) };
            userAvatar.SetName("Quack Cyber");

            var lblUserName = new Label
            {
                Text = "Quack Cyber",
                Font = TG.FontSemiBold(11f),
                ForeColor = Color.White,
                AutoSize = false,
                Height = 22,
                Location = new Point(80, 52),
                BackColor = Color.Transparent,
            };
            var lblEmojiStatus = new Label
            {
                Text = "Set Emoji Status",
                Font = TG.FontRegular(8.5f),
                ForeColor = Color.FromArgb(160, 220, 255),
                AutoSize = false,
                Height = 16,
                Location = new Point(80, 76),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
            };

            var btnChevron = new Button
            {
                Text = "▾",
                FlatStyle = FlatStyle.Flat,
                Font = TG.FontRegular(11f),
                ForeColor = Color.White,
                Size = new Size(28, 28),
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            btnChevron.FlatAppearance.BorderSize = 0;

            // ── Add Account row (ẩn mặc định, hiện khi bấm chevron) ──
            var pnlAddAccount = new Panel
            {
                Height = 48,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                Visible = false,
                Dock = DockStyle.Top,
            };
            pnlAddAccount.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Vẽ circle xanh với dấu +
                var rect = new Rectangle(14, 12, 24, 24);
                using var brush = new SolidBrush(TG.Blue);
                e.Graphics.FillEllipse(brush, rect);
                using var pen = new Pen(Color.White, 2f);
                e.Graphics.DrawLine(pen, 26, 18, 26, 30); // dọc
                e.Graphics.DrawLine(pen, 20, 24, 32, 24); // ngang
                // Divider
                e.Graphics.DrawLine(new Pen(TG.DividerLight), 56, 47, pnlAddAccount.Width, 47);
            };
            var lblAddAccount = new Label
            {
                Text = "Add Account",
                Font = TG.FontRegular(10f),
                ForeColor = TG.TextPrimary,
                AutoSize = false,
                Height = 48,
                Location = new Point(56, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };
            pnlAddAccount.Controls.Add(lblAddAccount);
            pnlAddAccount.Resize += (s, e) => lblAddAccount.Width = Math.Max(0, pnlAddAccount.Width - 56);
            pnlAddAccount.MouseEnter += (s, e) => pnlAddAccount.BackColor = TG.SidebarHover;
            pnlAddAccount.MouseLeave += (s, e) => pnlAddAccount.BackColor = Color.White;
            lblAddAccount.MouseEnter += (s, e) => pnlAddAccount.BackColor = TG.SidebarHover;
            lblAddAccount.MouseLeave += (s, e) => pnlAddAccount.BackColor = Color.White;

            // Toggle chevron + hiện/ẩn Add Account
            Action toggleAccountPanel = () =>
            {
                pnlAddAccount.Visible = !pnlAddAccount.Visible;
                btnChevron.Text = pnlAddAccount.Visible ? "▲" : "▾";
            };
            btnChevron.Click += (s, e) => toggleAccountPanel();
            userAvatar.Click += (s, e) => toggleAccountPanel();
            lblUserName.Click += (s, e) => toggleAccountPanel();

            _pnlSettingsHeader.Controls.AddRange(new Control[] { btnClose, userAvatar, lblUserName, lblEmojiStatus, btnChevron });
            _pnlSettingsHeader.Resize += (s, e) =>
            {
                lblUserName.Width = _pnlSettingsHeader.Width - 84 - 36;
                lblEmojiStatus.Width = _pnlSettingsHeader.Width - 84 - 36;
                btnChevron.Location = new Point(_pnlSettingsHeader.Width - 36, 52);
            };

            // ── Menu items ────────────────────────────
            var menuItems = new (string Emoji, string Label, bool HasToggle)[]
{
    ("👤", "My Profile",      false),
    ("👥", "New Group",       false),
    ("📣", "New Channel",     false),
    ("🪪", "Contacts",        false),
    ("📞", "Calls",           false),
    ("🔖", "Saved Messages",  false),
    ("⚙️", "Settings",        false),
    ("🌙", "Night Mode",      true),
};

            var pnlMenuList = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlMenuList.Resize += (s, e) =>
            {
                foreach (Control c in pnlMenuList.Controls)
                    c.Width = pnlMenuList.ClientSize.Width;
            };
            int my = 8;
            foreach (var item in menuItems)
            {
                string key = item.Label;
                bool toggleOn = _settingsToggles.TryGetValue(key, out bool v) ? v : false;

                var row = BuildSettingsRow(item.Emoji, key, item.HasToggle, toggleOn, newState => _settingsToggles[key] = newState);
                row.Location = new Point(0, my);
                row.Width = smw;
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                pnlMenuList.Controls.Add(row);
                my += 48;

                row.Click += (s, e) => OnSettingsMenuClick(key);
                foreach (Control c in row.Controls)
                    c.Click += (s, e) => OnSettingsMenuClick(key);
            }

            var lblVersion = new Label
            {
                Text = "SecureChat v1.0 · NT106 Nhóm 6",
                Font = TG.FontRegular(7.5f),
                ForeColor = TG.TextHint,
                AutoSize = false,
                Height = 20,
                Dock = DockStyle.Bottom,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };

            _pnlSettingsMenu.Controls.Add(pnlMenuList);
            _pnlSettingsMenu.Controls.Add(lblVersion);
            _pnlSettingsMenu.Controls.Add(pnlAddAccount);   // dock Top, nằm dưới header
            _pnlSettingsMenu.Controls.Add(_pnlSettingsHeader);
        }

        private Panel BuildSettingsRow(string emoji, string label, bool hasToggle, bool initialOn, Action<bool>? onToggle = null)
        {
            var pnl = new Panel { Height = 48, BackColor = Color.White, Cursor = Cursors.Hand };

            var lblEmoji = new Label
            {
                Text = emoji,
                Font = new Font("Segoe UI Emoji", 14f),
                Size = new Size(40, 48),
                Location = new Point(14, 0),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };

            var lblLabel = new Label
            {
                Text = label,
                Font = TG.FontRegular(10f),
                ForeColor = TG.TextPrimary,
                AutoSize = false,
                Height = 48,
                Location = new Point(56, 0),
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = Color.Transparent,
            };

            pnl.Controls.AddRange(new Control[] { lblEmoji, lblLabel });
            lblEmoji.Click += (s, e) => OnSettingsMenuClick(label);
            pnl.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(TG.DividerLight), 56, 47, pnl.Width, 47);
            pnl.Resize += (s, e) =>
            {
                // clamp width to avoid negative values when panel is very narrow
                lblLabel.Width = Math.Max(0, pnl.Width - 56 - (hasToggle ? 56 : 12));
            };

            if (hasToggle)
            {
                bool on = initialOn;
                var toggle = new Panel { Size = new Size(44, 24), BackColor = Color.Transparent, Cursor = Cursors.Hand };
                toggle.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var r = new Rectangle(0, 2, 40, 20);
                    using var path = RoundedPanel.GetRoundedPath(r, 10);
                    using var brush = new SolidBrush(on ? TG.Blue : Color.FromArgb(0xCC, 0xCC, 0xCC));
                    e.Graphics.FillPath(brush, path);
                    int cx = on ? 22 : 2;
                    using var white = new SolidBrush(Color.White);
                    e.Graphics.FillEllipse(white, cx, 4, 16, 16);
                };
                toggle.Click += (s, e) =>
                {
                    on = !on;
                    onToggle?.Invoke(on); // update class-level store (or caller)
                    toggle.Invalidate();
                };
                pnl.Controls.Add(toggle);
                pnl.Resize += (s, e) => toggle.Location = new Point(Math.Max(0, pnl.Width - 52), 12);
            }

            return pnl;
        }

        private void OnSettingsMenuClick(string label)
        {
            HideSettingsMenu();
            switch (label)
            {
                case "My Profile":   /* TODO: new frmProfile().ShowDialog(this); */ break;
                case "New Group":    /* TODO: new frmCreateGroup().ShowDialog(this); */ break;
                case "Contacts":
                    // modal, parent = this so frmContacts.StartPosition = CenterParent works
                    var contacts = new frmContacts();
                    contacts.ShowDialog(this);
                    break;
                case "Settings":     /* TODO: new frmSettings().ShowDialog(this); */ break;
            }
        }

        // ════════════════════════════════════════════
        //  SETTINGS MENU TOGGLE
        // ════════════════════════════════════════════
        private void ToggleSettingsMenu()
        {
            if (_settingsVisible) HideSettingsMenu();
            else ShowSettingsMenu();
        }


        private void ShowSettingsMenu()
        {
            _settingsVisible = true;
            int smw = _pnlSettingsMenu.Width;
            // Nếu đang ẩn hoàn toàn bên trái, đảm bảo Left = -smw để animation mượt
            if (_pnlSettingsMenu.Left <= -smw)
                _pnlSettingsMenu.Left = -smw;
            _settingsTargetX = 0; // vị trí khi hiện (dán vào mép trái của form)
            _pnlSettingsMenu.BringToFront();
            _slideTimer.Start();
        }

        private void HideSettingsMenu()
        {
            _settingsVisible = false;
            _settingsTargetX = -_pnlSettingsMenu.Width;   // trượt ra ngoài bên trái
            _slideTimer.Start();
        }

        // ════════════════════════════════════════════
        //  LOAD CONVERSATION
        // ════════════════════════════════════════════
        private void LoadConversation(string convId)
        {
            var conv = _convs.Find(c => c.Id == convId);
            if (conv == default) return;

            _chatAvatar.SetName(conv.Name);
            _lblChatName.Text = conv.Name;
            _lblChatStatus.Text = conv.IsGroup ? "5 members" : "last seen recently";

            BuildMessages();
        }

        private void BuildMessages()
        {
            _pnlMessages.Controls.Clear();
            _pnlMessages.Invalidate();

            int y = 8;

            // Date separator (docked to top so it resizes/centers automatically)
            var sep = new Label
            {
                Text = "March 10",
                Font = TG.FontRegular(8.5f),
                ForeColor = Color.White,
                AutoSize = false,
                Height = 24,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Padding = new Padding(8, 2, 8, 2),
            };
            // Add first so DockStyle.Top places it at the top of the scrollable area
            _pnlMessages.Controls.Add(sep);
            y += sep.Height + 4;

            var bubbles = new List<Control>();
            foreach (var msg in _msgs)
            {
                var bubble = BuildBubble(msg.Text, msg.Out, msg.Time);
                // Make bubble respect panel resizing
                bubble.Location = new Point(0, y);
                bubble.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                bubble.Width = Math.Max(0, _pnlMessages.ClientSize.Width - _pnlMessages.Padding.Horizontal);
                _pnlMessages.Controls.Add(bubble);
                bubbles.Add(bubble);
                y += bubble.Height + 4;
            }

            // Scroll to the last message reliably
            if (bubbles.Count > 0)
                _pnlMessages.ScrollControlIntoView(bubbles[^1]);
        }

        private Panel BuildBubble(string text, bool isOut, string time)
        {
            var pnl = new Panel { BackColor = Color.Transparent };
            int pad = 12;

            // Compute max width as ~66% of messages panel width (fallback to 360)
            int maxW = 360;
            if (_pnlMessages != null && _pnlMessages.ClientSize.Width > 0)
                maxW = Math.Max(220, (int)(_pnlMessages.ClientSize.Width * 0.66f) - _pnlMessages.Padding.Horizontal);

            // Measure text on the messages surface for consistent metrics
            SizeF sz;
            using (var g = _pnlMessages.CreateGraphics())
            {
                sz = g.MeasureString(text, TG.FontRegular(9.5f), maxW - pad * 2);
            }

            int bw = Math.Min(maxW, Math.Max((int)sz.Width + pad * 2 + 30, 90));
            int bh = (int)sz.Height + pad * 2;
            pnl.Height = bh + 12;

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                int x = isOut ? pnl.ClientSize.Width - bw - 10 : 10;
                int y = 4;
                Color bg = isOut ? TG.MsgOutBg : TG.MsgInBg;

                // Shadow
                using var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
                var shadowRect = new Rectangle(x + 1, y + 2, bw, bh);
                using var shadowPath = RoundedPanel.GetRoundedPath(shadowRect, TG.RadiusBubble);
                e.Graphics.FillPath(shadowBrush, shadowPath);

                // Bubble body
                var bubbleRect = new Rectangle(x, y, bw, bh);
                using var bubblePath = RoundedPanel.GetRoundedPath(bubbleRect, TG.RadiusBubble);
                e.Graphics.FillPath(new SolidBrush(bg), bubblePath);

                // Tail
                if (isOut)
                {
                    var tail = new[] { new Point(x + bw, y + bh - 8), new Point(x + bw + 5, y + bh), new Point(x + bw, y + bh) };
                    e.Graphics.FillPolygon(new SolidBrush(bg), tail);
                }
                else
                {
                    var tail = new[] { new Point(x, y + bh - 8), new Point(x - 5, y + bh), new Point(x, y + bh) };
                    e.Graphics.FillPolygon(new SolidBrush(bg), tail);
                }

                // Text
                e.Graphics.DrawString(text, TG.FontRegular(9.5f), new SolidBrush(TG.TextPrimary),
                    new RectangleF(x + pad, y + pad, bw - pad * 2, sz.Height + 4));

                // Time
                var timeSz = e.Graphics.MeasureString(time, TG.FontRegular(7.5f));
                float tx = x + bw - timeSz.Width - pad;
                float ty = y + bh - timeSz.Height - 4;
                e.Graphics.DrawString(time, TG.FontRegular(7.5f), new SolidBrush(TG.TextTime), tx, ty);

                // Ticks for outgoing
                if (isOut)
                    e.Graphics.DrawString("✓✓", TG.FontRegular(7.5f), new SolidBrush(TG.Blue), tx - 18, ty);
            };

            // Context menu
            var ctx = new ContextMenuStrip();
            ctx.Items.Add("↩  Reply");
            ctx.Items.Add("↗  Forward");
            if (isOut) ctx.Items.Add("✎  Edit");
            ctx.Items.Add("📌  Pin");
            ctx.Items.Add(new ToolStripSeparator());
            var del = ctx.Items.Add("🗑  Delete");
            del.ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A);
            pnl.ContextMenuStrip = ctx;

            return pnl;
        }

        // ════════════════════════════════════════════
        //  SEND MESSAGE
        // ════════════════════════════════════════════
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_tbMessage.Text)) return;
            string text = _tbMessage.Text.Trim();
            _tbMessage.Text = "";
            _msgs.Add((text, true, DateTime.Now.ToString("h:mm tt")));
            BuildMessages();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _slideTimer?.Stop();
            _slideTimer?.Dispose();
            _wallpaperWatcher?.Dispose();
            _wallpaper?.Dispose();
            _chatMoreMenu?.Dispose();
            base.OnFormClosed(e);
        }

        private sealed class ChatMenuColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => Color.FromArgb(0xF2, 0xF5, 0xF9);
            public override Color MenuItemBorder => Color.FromArgb(0xD9, 0xE1, 0xEB);
            public override Color ToolStripDropDownBackground => Color.White;
            public override Color SeparatorDark => Color.FromArgb(0xEA, 0xEE, 0xF3);
            public override Color SeparatorLight => Color.FromArgb(0xEA, 0xEE, 0xF3);
            public override Color ImageMarginGradientBegin => Color.White;
            public override Color ImageMarginGradientMiddle => Color.White;
            public override Color ImageMarginGradientEnd => Color.White;
        }

        private Image LoadAndTintIcon(string fileName, Color tint)
        {
            try
            {
                string path = Path.Combine(Application.StartupPath, "Resources", "Icons", fileName);
                if (!File.Exists(path)) return null;

                // Load into memory to avoid file locks
                byte[] data = File.ReadAllBytes(path);
                using var ms = new MemoryStream(data);
                using var srcImg = Image.FromStream(ms);

                // Ensure we have a 32bpp ARGB bitmap (preserve alpha)
                var src = new Bitmap(srcImg.Width, srcImg.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(src))
                {
                    g.Clear(Color.Transparent);
                    g.DrawImage(srcImg, new Rectangle(0, 0, srcImg.Width, srcImg.Height));
                }

                // Prepare result bitmap
                var result = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (var g = Graphics.FromImage(result))
                using (var attr = new ImageAttributes())
                {
                    // Multiply each color channel by tint (preserves icon alpha)
                    float r = tint.R / 255f;
                    float gC = tint.G / 255f;
                    float b = tint.B / 255f;

                    var matrix = new ColorMatrix(new float[][]
                    {
                new float[] { r, 0, 0, 0, 0 },
                new float[] { 0, gC, 0, 0, 0 },
                new float[] { 0, 0, b, 0, 0 },
                new float[] { 0, 0, 0, 1, 0 },
                new float[] { 0, 0, 0, 0, 1 }
                    });

                    attr.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

                    g.Clear(Color.Transparent);
                    g.DrawImage(src, new Rectangle(0, 0, src.Width, src.Height),
                                0, 0, src.Width, src.Height, GraphicsUnit.Pixel, attr);
                }

                src.Dispose();
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadAndTintIcon failed for '{fileName}': {ex}");
                return null;
            }
        }
    }
}