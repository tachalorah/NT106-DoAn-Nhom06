using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SecureChat.Client
{
    // ─── Model dữ liệu mock ───────────────────────
    public class ConversationItem
    {
        public string Id          { get; set; }
        public string Name        { get; set; }
        public string LastMessage { get; set; }
        public string Time        { get; set; }
        public int    UnreadCount { get; set; }
        public bool   IsGroup     { get; set; }
        public bool   IsOnline    { get; set; }
        public bool   IsMuted     { get; set; }
        public string SenderPrefix{ get; set; } // "Huyền: " cho nhóm
    }

    public class ChatMessage
    {
        public string  Text      { get; set; }
        public bool    IsOutgoing{ get; set; }
        public string  Time      { get; set; }
        public bool    IsRead    { get; set; }
        public string  SenderName{ get; set; }
        public Color   SenderColor{ get; set; }
    }

    /// <summary>
    /// MainForm - Layout 2 cột giống Telegram Desktop:
    ///   Sidebar (280px) | Chat Area (phần còn lại)
    /// </summary>
    public class frmMainChat : Form
    {
        // ── Layout panels ──────────────────────────
        private Panel           _pnlSidebar;
        private Panel           _pnlChatArea;
        private Splitter        _splitter;

        // ── Sidebar ────────────────────────────────
        private Panel           _pnlSidebarHeader;
        private TelegramTextBox _tbSearch;
        private Panel           _pnlConvList;
        private Panel           _selectedItem;
        private string          _activeConvId;

        // ── Chat Area ──────────────────────────────
        private TelegramHeader  _chatHeader;
        private Panel           _pnlMessages;
        private Panel           _pnlInputBar;
        private TelegramTextBox _tbMessage;
        private Button          _btnSend, _btnAttach, _btnEmoji, _btnMic;
        private Label           _lblTyping;
        private Panel           _pnlEmpty; // khi chưa chọn chat

        // ── Data ───────────────────────────────────
        private List<ConversationItem> _conversations = new();
        private List<ChatMessage>      _messages      = new();
        private string                 _currentConvId = null;

        public frmMainChat()
        {
            InitMockData();
            InitializeComponent();
        }

        // ══════════════════════════════════════════
        // MOCK DATA
        // ══════════════════════════════════════════
        private void InitMockData()
        {
            _conversations = new List<ConversationItem>
            {
                new() { Id="1", Name="Nguyễn Văn A",  LastMessage="Ok anh nhé 👍",           Time="10:42", UnreadCount=3,  IsGroup=false, IsOnline=true  },
                new() { Id="2", Name="Nhóm NT106 Q22",LastMessage="push code lên đi mọi người!",Time="10:31", UnreadCount=12, IsGroup=true,  SenderPrefix="Huyền: " },
                new() { Id="3", Name="Trần Thị B",    LastMessage="Bạn đang online không?",  Time="09:15", UnreadCount=0,  IsGroup=false, IsOnline=true  },
                new() { Id="4", Name="Lê Minh C",     LastMessage="File PDF đây nè",         Time="Hôm qua",UnreadCount=0, IsGroup=false, IsOnline=false },
                new() { Id="5", Name="Nhóm An Toàn TT",LastMessage="Xem slide buổi tới nhé",Time="Hôm qua",UnreadCount=1,  IsGroup=true,  SenderPrefix="Thầy: ", IsMuted=true },
                new() { Id="6", Name="Phạm Minh Đức", LastMessage="😂😂😂",                  Time="T2",    UnreadCount=0,  IsGroup=false, IsOnline=false },
            };
            _messages = new List<ChatMessage>
            {
                new() { Text="Ê, mày có free chiều nay không?", IsOutgoing=false, Time="10:30", SenderName="Nguyễn Văn A", SenderColor=TG.AvatarColors[4] },
                new() { Text="Có nha, từ 3h. Cần gì vậy?",      IsOutgoing=true,  Time="10:31", IsRead=true  },
                new() { Text="Oke vậy mình ra quán café nhé!",  IsOutgoing=false, Time="10:32"  },
                new() { Text="Đồng ý 🎉 Mình tới lúc 3h15 nha", IsOutgoing=true,  Time="10:33", IsRead=true  },
                new() { Text="Ok anh nhé 👍",                    IsOutgoing=false, Time="10:42"  },
            };
        }

        // ══════════════════════════════════════════
        // INIT UI
        // ══════════════════════════════════════════
        private void InitializeComponent()
        {
            Text = "SecureChat";
            Size = new Size(960, 640);
            MinimumSize = new Size(760, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = TG.WindowBg;
            Font = TG.FontRegular(9.5f);

            // ── SIDEBAR ───────────────────────────────
            _pnlSidebar = new Panel
            {
                Width = 280, Dock = DockStyle.Left,
                BackColor = Color.White,
            };
            BuildSidebar();

            // ── SPLITTER ──────────────────────────────
            _splitter = new Splitter
            {
                Width = 1, BackColor = TG.Divider,
                Dock = DockStyle.Left, MinExtra = 400, MinSize = 220,
            };

            // ── CHAT AREA ─────────────────────────────
            _pnlChatArea = new Panel { Dock = DockStyle.Fill, BackColor = TG.ChatBg };
            BuildChatArea();

            Controls.AddRange(new Control[] { _pnlChatArea, _splitter, _pnlSidebar });

            // Select first conversation by default
            SelectConversation("1");
        }

        // ══════════════════════════════════════════
        // SIDEBAR
        // ══════════════════════════════════════════
        private void BuildSidebar()
        {
            // Header
            _pnlSidebarHeader = new Panel
            {
                Height = 52, Dock = DockStyle.Top,
                BackColor = TG.TitleBarBg,
            };

            // Hamburger / menu button
            var btnMenu = new Button
            {
                Text = "☰", FlatStyle = FlatStyle.Flat,
                Font = TG.FontRegular(14f), ForeColor = Color.White,
                Size = new Size(40, 52), Location = new Point(0, 0),
                BackColor = Color.Transparent, Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            btnMenu.FlatAppearance.BorderSize = 0;

            // Thêm dòng này
            btnMenu.Click += (s, e) => new frmContacts().ShowDialog(this);

            var lblTitle = new Label
            {
                Text = "SecureChat",
                Font = TG.FontSemiBold(11f), ForeColor = Color.White,
                BackColor = Color.Transparent, AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
            };

            var btnNewChat = new Button
            {
                Text = "✏", FlatStyle = FlatStyle.Flat,
                Font = TG.FontRegular(14f), ForeColor = Color.White,
                Size = new Size(40, 52), BackColor = Color.Transparent,
                Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter,
            };
            btnNewChat.FlatAppearance.BorderSize = 0;

            // ← THÊM ĐOẠN NÀY
            var btnContacts = new Button
            {
                Text = "👥",
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Font = new Font("Segoe UI Emoji", 14f),
                Size = new Size(40, 52),
                Cursor = Cursors.Hand,
            };
            btnContacts.FlatAppearance.BorderSize = 0;
            btnContacts.Click += (s, e) => new frmContacts().ShowDialog(this);
            // ← KẾT THÚC ĐOẠN THÊM

            _pnlSidebarHeader.Controls.AddRange(new Control[] { btnMenu, lblTitle, btnNewChat });
            _pnlSidebarHeader.Resize += (s, e) =>
            {
                btnMenu.Location = new Point(0, 0);
                lblTitle.SetBounds(44, 0, _pnlSidebarHeader.Width - 90, 52);
                // btnNewChat.Location = new Point(_pnlSidebarHeader.Width - 40, 0);
                btnNewChat.Location = new Point(_pnlSidebarHeader.Width - 80, 0); // dịch sang trái
                btnContacts.Location = new Point(_pnlSidebarHeader.Width - 40, 0); // nút mới ở phải cùng
            };

            // Search bar
            var pnlSearch = new Panel { Height = 48, Dock = DockStyle.Top, BackColor = Color.White, Padding = new Padding(8, 8, 8, 4) };
            _tbSearch = new TelegramTextBox { Height = 32, Dock = DockStyle.Fill };
            _tbSearch.SetPlaceholder("🔍  Tìm kiếm...");
            _tbSearch.TextChanged += (s, e) => FilterConversations(_tbSearch.Text);
            pnlSearch.Controls.Add(_tbSearch);

            // Conversation list (scrollable)
            _pnlConvList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White,
            };
            BuildConversationList();

            _pnlSidebar.Controls.AddRange(new Control[] { _pnlConvList, pnlSearch, _pnlSidebarHeader });
        }

        private void BuildConversationList(string filter = "")
        {
            _pnlConvList.Controls.Clear();
            int y = 0;

            foreach (var conv in _conversations)
            {
                if (!string.IsNullOrEmpty(filter) &&
                    !conv.Name.ToLower().Contains(filter.ToLower()) &&
                    !conv.LastMessage.ToLower().Contains(filter.ToLower()))
                    continue;

                var item = BuildConversationItem(conv);
                item.Location = new Point(0, y);
                item.Width = _pnlConvList.Width;
                _pnlConvList.Controls.Add(item);
                if (conv.Id == _activeConvId) HighlightItem(item);
                y += 68;

                // Capture for resize
                _pnlConvList.Resize += (s, e) =>
                {
                    foreach (Control c in _pnlConvList.Controls)
                        c.Width = _pnlConvList.Width;
                };
            }
        }

        private Panel BuildConversationItem(ConversationItem conv)
        {
            var pnl = new Panel
            {
                Height = 68, BackColor = Color.White,
                Tag = conv.Id, Cursor = Cursors.Hand,
            };

            // Hover effect
            pnl.MouseEnter += (s, e) => { if ((string)pnl.Tag != _activeConvId) pnl.BackColor = TG.SidebarHover; };
            pnl.MouseLeave += (s, e) => { if ((string)pnl.Tag != _activeConvId) pnl.BackColor = Color.White; };
            pnl.Click += (s, e) => SelectConversation((string)pnl.Tag);

            // Avatar
            var avatar = new AvatarControl { Size = new Size(48, 48), Location = new Point(10, 10), ShowOnline = conv.IsOnline };
            avatar.SetName(conv.Name);
            avatar.Click += (s, e) => SelectConversation((string)pnl.Tag);

            // Name
            var lblName = new Label
            {
                Text = conv.Name,
                Font = TG.FontSemiBold(9.5f), ForeColor = TG.TextName,
                AutoSize = false, BackColor = Color.Transparent,
                Location = new Point(66, 12), Height = 20,
            };

            // Last message
            string preview = (conv.SenderPrefix ?? "") + conv.LastMessage;
            var lblMsg = new Label
            {
                Text = preview,
                Font = TG.FontRegular(8.5f), ForeColor = TG.TextSecondary,
                AutoSize = false, BackColor = Color.Transparent,
                Location = new Point(66, 34), Height = 18,
            };

            // Time
            var lblTime = new Label
            {
                Text = conv.Time,
                Font = TG.FontRegular(8f), ForeColor = TG.TextTime,
                AutoSize = true, BackColor = Color.Transparent,
            };

            // Badge
            var badge = new UnreadBadge { Count = conv.UnreadCount, IsMuted = conv.IsMuted };

            // Group icon
            if (conv.IsGroup)
            {
                var grpIcon = new Label { Text = "👥", AutoSize = true, BackColor = Color.Transparent, Font = new Font("Segoe UI Emoji", 8f) };
                grpIcon.Location = new Point(66, 12);
                lblName.Location = new Point(88, 12);
                pnl.Controls.Add(grpIcon);
            }

            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblMsg, lblTime, badge });

            // Divider line
            pnl.Paint += (s, e) =>
            {
                e.Graphics.DrawLine(new Pen(TG.DividerLight), 66, 67, pnl.Width, 67);
            };

            // Resize inner controls
            pnl.Resize += (s, e) =>
            {
                lblName.Width = pnl.Width - 66 - 70;
                lblMsg.Width  = pnl.Width - 66 - 40;
                lblTime.Location = new Point(pnl.Width - lblTime.Width - 12, 14);
                badge.Location   = new Point(pnl.Width - badge.Width - 12, 38);
            };

            // Propagate click from children
            foreach (Control c in pnl.Controls)
            {
                c.Click += (s, e) => SelectConversation((string)pnl.Tag);
                c.MouseEnter += (s, e) => { if ((string)pnl.Tag != _activeConvId) pnl.BackColor = TG.SidebarHover; };
                c.MouseLeave += (s, e) => { if ((string)pnl.Tag != _activeConvId) pnl.BackColor = Color.White; };
            }

            return pnl;
        }

        private void HighlightItem(Panel item)
        {
            item.BackColor = TG.SidebarActive;
            foreach (Control c in item.Controls)
            {
                if (c is Label lbl)
                {
                    lbl.ForeColor = Color.White;
                    lbl.BackColor = Color.Transparent;
                }
            }
        }

        private void ResetItemColors()
        {
            foreach (Control c in _pnlConvList.Controls)
            {
                if (c is Panel pnl)
                {
                    pnl.BackColor = Color.White;
                    foreach (Control child in pnl.Controls)
                    {
                        if (child is Label lbl)
                        {
                            if (lbl.Font.Bold) lbl.ForeColor = TG.TextName;
                            else lbl.ForeColor = TG.TextSecondary;
                            lbl.BackColor = Color.Transparent;
                        }
                    }
                }
            }
        }

        private void FilterConversations(string filter)
        {
            BuildConversationList(filter);
        }

        private void SelectConversation(string convId)
        {
            _activeConvId = convId;
            _currentConvId = convId;

            ResetItemColors();
            foreach (Control c in _pnlConvList.Controls)
            {
                if (c is Panel p && (string)p.Tag == convId)
                    HighlightItem(p);
            }

            var conv = _conversations.Find(x => x.Id == convId);
            if (conv != null) LoadChat(conv);
        }

        // ══════════════════════════════════════════
        // CHAT AREA
        // ══════════════════════════════════════════
        private void BuildChatArea()
        {
            // Empty state
            _pnlEmpty = new Panel { Dock = DockStyle.Fill, BackColor = TG.ChatBg };
            var lblEmpty = new Label
            {
                Text = "Chọn một cuộc trò chuyện để bắt đầu",
                Font = TG.FontRegular(12f), ForeColor = TG.TextSecondary,
                AutoSize = true, BackColor = Color.Transparent,
            };
            _pnlEmpty.Controls.Add(lblEmpty);
            _pnlEmpty.Resize += (s, e) => lblEmpty.Location = new Point((_pnlEmpty.Width - lblEmpty.Width) / 2, (_pnlEmpty.Height - lblEmpty.Height) / 2);

            // Chat header
            _chatHeader = new TelegramHeader();
            _chatHeader.ShowBack = false;

            // Message area (custom drawn)
            _pnlMessages = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = TG.ChatBg,
                AutoScroll = true,
                Padding = new Padding(12, 8, 12, 8),
            };

            // Typing indicator
            _lblTyping = new Label
            {
                Height = 20, Dock = DockStyle.Top,
                Font = TG.FontRegular(8.5f), ForeColor = TG.Blue,
                BackColor = TG.ChatBg, Padding = new Padding(14, 2, 0, 0),
                Visible = false,
            };

            // Input bar
            BuildInputBar();

            _pnlChatArea.Controls.AddRange(new Control[] { _pnlInputBar, _lblTyping, _pnlMessages, _chatHeader, _pnlEmpty });
        }

        private void BuildInputBar()
        {
            _pnlInputBar = new Panel
            {
                Height = 56, Dock = DockStyle.Bottom,
                BackColor = Color.White,
            };

            // Border top
            _pnlInputBar.Paint += (s, e) =>
                e.Graphics.DrawLine(new Pen(TG.Divider), 0, 0, _pnlInputBar.Width, 0);

            _btnEmoji = MakeIconButton("😊", 36);
            _tbMessage = new TelegramTextBox { Height = 36 };
            _tbMessage.SetPlaceholder("Nhắn tin...");
            _tbMessage.KeyDown += TbMessage_KeyDown;

            _btnAttach = MakeIconButton("📎", 36);
            _btnMic    = MakeIconButton("🎤", 36);
            _btnSend   = new TelegramButton
            {
                Text = "↑", Height = 36, Width = 36,
                Font = TG.FontSemiBold(14f), Radius = 18,
            };
            _btnSend.Click += BtnSend_Click;
            _tbMessage.TextChanged += (s, e) =>
            {
                bool hasText = !string.IsNullOrWhiteSpace(_tbMessage.Text);
                _btnSend.Visible = hasText;
                _btnMic.Visible  = !hasText;
            };

            _pnlInputBar.Controls.AddRange(new Control[] { _btnEmoji, _tbMessage, _btnAttach, _btnMic, _btnSend });
            _pnlInputBar.Resize += (s, e) =>
            {
                int y = 10, h = 36;
                _btnEmoji.Location = new Point(8, y);
                _btnAttach.Location= new Point(_pnlInputBar.Width - 44 * 2 + 4, y);
                _btnMic.Location   = new Point(_pnlInputBar.Width - 44, y);
                _btnSend.Location  = new Point(_pnlInputBar.Width - 44, y);
                _tbMessage.SetBounds(_btnEmoji.Right + 6, y, _btnAttach.Left - _btnEmoji.Right - 12, h);
            };
        }

        private Button MakeIconButton(string icon, int size)
        {
            var btn = new Button
            {
                Text = icon, Font = new Font("Segoe UI Emoji", 14f),
                FlatStyle = FlatStyle.Flat, Size = new Size(size, size),
                BackColor = Color.Transparent, ForeColor = TG.Blue,
                Cursor = Cursors.Hand, TextAlign = ContentAlignment.MiddleCenter,
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void LoadChat(ConversationItem conv)
        {
            _pnlEmpty.Visible = false;
            _chatHeader.Visible = true;
            _pnlMessages.Visible = true;
            _pnlInputBar.Visible = true;

            // Update header
            _chatHeader.Title = conv.Name;
            _chatHeader.SetAvatar(conv.Name);
            _chatHeader.Subtitle = conv.IsOnline ? "online" : conv.IsGroup ? $"{(conv.IsGroup ? "5" : "1")} thành viên" : "offline";
            _chatHeader.AddRightButton("🔍", (s, e) => { });
            _chatHeader.AddRightButton("⋮",  (s, e) => { });

            // Mark unread = 0
            conv.UnreadCount = 0;

            // Build messages
            BuildMessages(conv);

            // Typing indicator mock for conv 1
            _lblTyping.Visible = conv.Id == "1";
            _lblTyping.Text = $"{conv.Name.Split(' ')[0]} đang nhập...";
        }

        private void BuildMessages(ConversationItem conv)
        {
            _pnlMessages.Controls.Clear();
            int y = 8;

            // Date separator
            var dateLbl = MakeDateSeparator("Hôm nay");
            dateLbl.Location = new Point(0, y);
            _pnlMessages.Controls.Add(dateLbl);
            y += 32;

            bool isGroup = conv.IsGroup;
            foreach (var msg in _messages)
            {
                var bubble = BuildMessageBubble(msg, isGroup);
                bubble.Location = new Point(0, y);
                _pnlMessages.Controls.Add(bubble);
                y += bubble.Height + 4;
            }

            // Scroll to bottom
            _pnlMessages.AutoScrollPosition = new Point(0, y + 100);
        }

        private Panel MakeDateSeparator(string text)
        {
            var pnl = new Panel { Height = 28, BackColor = Color.Transparent };
            pnl.Resize += (s, e) =>
            {
                pnl.Invalidate();
                foreach (Control c in pnl.Controls) c.Width = pnl.Width;
            };
            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Pill background
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                var sz = e.Graphics.MeasureString(text, TG.FontRegular(8.5f));
                int pw = (int)sz.Width + 20, ph = 22;
                int px = (pnl.Width - pw) / 2, py = (pnl.Height - ph) / 2;
                var pillRect = new Rectangle(px, py, pw, ph);
                using var path = RoundedPanel.GetRoundedPath(pillRect, 10);
                e.Graphics.FillPath(new SolidBrush(Color.FromArgb(0x51, 0x68, 0x76, 0x95)), path);
                e.Graphics.DrawString(text, TG.FontRegular(8.5f), Brushes.White, pillRect, sf);
            };
            return pnl;
        }

        private Panel BuildMessageBubble(ChatMessage msg, bool isGroup)
        {
            var pnl = new Panel { BackColor = Color.Transparent };

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawBubble(e.Graphics, msg, pnl.Width, isGroup);
            };

            // Measure height
            int maxW = 400;
            using var g = CreateGraphics();
            var textSize = g.MeasureString(msg.Text, TG.FontRegular(9.5f), maxW - 24);
            int h = (int)textSize.Height + 36; // padding + time row
            if (isGroup && !msg.IsOutgoing) h += 18; // sender name
            pnl.Height = h + 8;

            // Context menu (right-click)
            var ctxMenu = new ContextMenuStrip();
            ctxMenu.Items.Add("↩ Trả lời");
            ctxMenu.Items.Add("↗ Chuyển tiếp");
            if (msg.IsOutgoing) ctxMenu.Items.Add("✎ Chỉnh sửa");
            ctxMenu.Items.Add(new ToolStripSeparator());
            ctxMenu.Items.Add("🗑 Xóa").ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A);
            pnl.ContextMenuStrip = ctxMenu;

            return pnl;
        }

        private void DrawBubble(Graphics g, ChatMessage msg, int panelWidth, bool isGroup)
        {
            int maxW = Math.Min(panelWidth - 100, 380);
            int padding = 12;

            // Measure text
            var textSize = g.MeasureString(msg.Text, TG.FontRegular(9.5f), maxW - padding * 2);
            int bubbleW = Math.Max((int)textSize.Width + padding * 2 + 60, 80);
            int bubbleH = (int)textSize.Height + padding * 2 + (isGroup && !msg.IsOutgoing ? 18 : 0) + 2;
            int cornerR = TG.RadiusBubble;

            int x, y = 4;
            if (msg.IsOutgoing)
                x = panelWidth - bubbleW - 12;
            else
                x = isGroup ? 56 : 12;

            Color bgColor  = msg.IsOutgoing ? TG.MsgOutBg : TG.MsgInBg;
            Color txtColor = TG.TextPrimary;

            // Draw shadow (subtle)
            var shadowRect = new Rectangle(x + 1, y + 2, bubbleW, bubbleH);
            g.FillRoundedRect(new SolidBrush(Color.FromArgb(15, 0, 0, 0)), shadowRect, cornerR);

            // Draw bubble
            var bubbleRect = new Rectangle(x, y, bubbleW, bubbleH);
            g.FillRoundedRect(new SolidBrush(bgColor), bubbleRect, cornerR);

            // Tail (little triangle at corner)
            if (msg.IsOutgoing)
            {
                var tailPts = new[] { new Point(x + bubbleW, y + bubbleH - cornerR), new Point(x + bubbleW + 5, y + bubbleH), new Point(x + bubbleW, y + bubbleH) };
                g.FillPolygon(new SolidBrush(bgColor), tailPts);
            }
            else
            {
                var tailPts = new[] { new Point(x, y + bubbleH - cornerR), new Point(x - 5, y + bubbleH), new Point(x, y + bubbleH) };
                g.FillPolygon(new SolidBrush(bgColor), tailPts);
            }

            int textY = y + padding;

            // Sender name (group)
            if (isGroup && !msg.IsOutgoing && !string.IsNullOrEmpty(msg.SenderName))
            {
                g.DrawString(msg.SenderName, TG.FontSemiBold(8.5f), new SolidBrush(msg.SenderColor), x + padding, textY);
                textY += 18;
            }

            // Avatar (group, incoming)
            if (isGroup && !msg.IsOutgoing)
            {
                var avatarRect = new Rectangle(8, y, 40, 40);
                g.FillEllipse(new SolidBrush(msg.SenderColor != default ? msg.SenderColor : TG.Blue), avatarRect);
                string init = string.IsNullOrEmpty(msg.SenderName) ? "?" : msg.SenderName.Substring(0, 1).ToUpper();
                using var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                g.DrawString(init, TG.FontSemiBold(9.5f), Brushes.White, avatarRect, sf2);
            }

            // Message text
            g.DrawString(msg.Text, TG.FontRegular(9.5f), new SolidBrush(txtColor), new RectangleF(x + padding, textY, bubbleW - padding * 2, textSize.Height + 2));

            // Time + status
            var timeSize = g.MeasureString(msg.Time, TG.FontRegular(7.5f));
            float timeX = x + bubbleW - timeSize.Width - padding;
            float timeY = y + bubbleH - timeSize.Height - 4;
            g.DrawString(msg.Time, TG.FontRegular(7.5f), new SolidBrush(TG.TextTime), timeX, timeY);

            // Read ticks (outgoing)
            if (msg.IsOutgoing)
            {
                string ticks = msg.IsRead ? "✓✓" : "✓";
                Color tickColor = msg.IsRead ? TG.Blue : TG.TextTime;
                g.DrawString(ticks, TG.FontRegular(7.5f), new SolidBrush(tickColor), timeX - 20, timeY);
            }
        }

        // ══════════════════════════════════════════
        // SEND MESSAGE
        // ══════════════════════════════════════════
        private void TbMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Return && !e.Shift)
            {
                e.SuppressKeyPress = true;
                SendMessage();
            }
        }

        private void BtnSend_Click(object sender, EventArgs e) => SendMessage();

        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_tbMessage.Text)) return;
            string text = _tbMessage.Text.Trim();
            _tbMessage.Text = "";

            var newMsg = new ChatMessage
            {
                Text = text, IsOutgoing = true,
                Time = DateTime.Now.ToString("HH:mm"),
                IsRead = false,
            };
            _messages.Add(newMsg);

            // Update conversation preview
            var conv = _conversations.Find(x => x.Id == _currentConvId);
            if (conv != null) { conv.LastMessage = text; conv.Time = newMsg.Time; }

            // Rebuild
            if (conv != null) BuildMessages(conv);
            BuildConversationList();
            SelectConversation(_currentConvId);
        }
    }

    // Extension để vẽ rounded rect trên Graphics
    public static class GraphicsExtensions
    {
        public static void FillRoundedRect(this Graphics g, Brush brush, Rectangle rect, int radius)
        {
            using var path = RoundedPanel.GetRoundedPath(rect, radius);
            g.FillPath(brush, path);
        }
    }
}
