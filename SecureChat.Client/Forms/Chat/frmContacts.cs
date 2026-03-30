using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SecureChat.Client
{
    public enum FriendStatus { None, Pending, Friend, Blocked }

    public class ContactItem
    {
        public string       Id       { get; set; }
        public string       Name     { get; set; }
        public string       Username { get; set; }
        public bool         IsOnline { get; set; }
        public FriendStatus Status   { get; set; }
        public string       LastSeen { get; set; }
    }

    public class FriendRequestItem
    {
        public string Id         { get; set; }
        public string Name       { get; set; }
        public string Username   { get; set; }
        public int    MutualCount{ get; set; }
        public bool   IsIncoming { get; set; } // true=nhận, false=đã gửi
    }

    /// <summary>
    /// Màn hình Contacts:
    ///   Tab 1 - Danh sách bạn bè (Tin nhắn riêng / Nhóm)
    ///   Tab 2 - Lời mời kết bạn (Đã nhận / Đã gửi)
    ///   Tab 3 - Tìm kiếm bạn bè
    /// </summary>
    public class frmContacts : Form
    {
        private TabControl  _tabs;
        private TabPage     _tabContacts, _tabRequests, _tabSearch;

        // Tab 1
        private TabControl  _contactSubTabs;
        private Panel       _pnlPrivate, _pnlGroup;

        // Tab 2
        private TabControl  _requestSubTabs;
        private Panel       _pnlIncoming, _pnlSent;
        private Label       _lblBadge;
        private int         _incomingCount = 3;

        // Tab 3
        private TelegramTextBox _tbSearch;
        private Panel           _pnlSearchResults;
        private Label           _lblSearchHint;

        private List<ContactItem>       _contacts  = new();
        private List<FriendRequestItem> _requests  = new();

        public frmContacts()
        {
            InitMockData();
            InitializeComponent();
        }

        private void InitMockData()
        {
            _contacts = new List<ContactItem>
            {
                new() { Id="1", Name="Nguyễn Văn A",   Username="@nguyenvana",   IsOnline=true,  Status=FriendStatus.Friend, LastSeen="online"     },
                new() { Id="2", Name="Trần Thị B",     Username="@tranthib",     IsOnline=true,  Status=FriendStatus.Friend, LastSeen="online"     },
                new() { Id="3", Name="Lê Minh C",      Username="@leminhhc",     IsOnline=false, Status=FriendStatus.Friend, LastSeen="1 giờ trước"},
                new() { Id="4", Name="Phạm Minh Đức",  Username="@phamminhduc",  IsOnline=false, Status=FriendStatus.Friend, LastSeen="Hôm qua"    },
                new() { Id="5", Name="Nhóm NT106 Q22", Username="",              IsOnline=false, Status=FriendStatus.Friend, LastSeen="5 thành viên"},
                new() { Id="6", Name="Nhóm An Toàn TT",Username="",             IsOnline=false, Status=FriendStatus.Friend, LastSeen="8 thành viên"},
            };
            _requests = new List<FriendRequestItem>
            {
                new() { Id="r1", Name="Trần Thị H",     Username="@tranthih",     MutualCount=2, IsIncoming=true  },
                new() { Id="r2", Name="Phạm Văn D",     Username="@phamvand",     MutualCount=0, IsIncoming=true  },
                new() { Id="r3", Name="Lê Thị E",       Username="@lethie",       MutualCount=5, IsIncoming=true  },
                new() { Id="r4", Name="Nguyễn Quốc K",  Username="@nguyenquock",  MutualCount=1, IsIncoming=false },
            };
        }

        private void InitializeComponent()
        {
            Text = "Danh bạ";
            Size = new Size(400, 620);
            MinimumSize = new Size(360, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            Font = TG.FontRegular(9.5f);

            var header = new TelegramHeader { Title = "Danh bạ & Bạn bè" };
            header.ShowBack = true;
            header.BackClicked += (s, e) => Close();
            Controls.Add(header);

            // ── Main TabControl ────────────────────────
            _tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                Font = TG.FontRegular(9.5f),
                ItemSize = new Size(120, 32),
                SizeMode = TabSizeMode.Fixed,
            };
            // Override tab drawing
            _tabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            _tabs.DrawItem += DrawTabItem;
            _tabs.Selected += (s, e) => _tabs.Invalidate();

            _tabContacts = new TabPage("  Danh sách  ") { BackColor = Color.White, UseVisualStyleBackColor = false };
            _tabRequests = new TabPage("  Lời mời  ")   { BackColor = Color.White, UseVisualStyleBackColor = false };
            _tabSearch   = new TabPage("  Tìm kiếm  ")  { BackColor = Color.White, UseVisualStyleBackColor = false };
            _tabs.TabPages.AddRange(new[] { _tabContacts, _tabRequests, _tabSearch });

            BuildContactsTab();
            BuildRequestsTab();
            BuildSearchTab();

            var pnlContent = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            pnlContent.Controls.Add(_tabs);
            Controls.Add(pnlContent);
        }

        private void DrawTabItem(object sender, DrawItemEventArgs e)
        {
            var tab = _tabs.TabPages[e.Index];
            bool selected = e.Index == _tabs.SelectedIndex;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillRectangle(new SolidBrush(Color.White), e.Bounds);

            if (selected)
            {
                var underline = new Rectangle(e.Bounds.Left, e.Bounds.Bottom - 2, e.Bounds.Width, 2);
                e.Graphics.FillRectangle(new SolidBrush(TG.Blue), underline);
            }

            Color fgColor = selected ? TG.Blue : TG.TextSecondary;
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tab.Text.Trim(), TG.FontSemiBold(9.5f), new SolidBrush(fgColor), e.Bounds, sf);

            // Badge on requests tab
            if (e.Index == 1 && _incomingCount > 0)
            {
                int bx = e.Bounds.Right - 22, by = e.Bounds.Top + 6;
                var badgeRect = new Rectangle(bx, by, 18, 18);
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(0xE2, 0x4B, 0x4A)), badgeRect);
                using var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(_incomingCount.ToString(), TG.FontSemiBold(7.5f), Brushes.White, badgeRect, sf2);
            }
        }

        // ── TAB 1: CONTACTS ─────────────────────────
        private void BuildContactsTab()
        {
            _contactSubTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size((_tabContacts.Width - 4) / 2, 30),
                SizeMode = TabSizeMode.Fixed,
                Font = TG.FontRegular(9f),
            };
            _contactSubTabs.DrawItem += (s, e) =>
            {
                var t = _contactSubTabs.TabPages[e.Index];
                bool sel = e.Index == _contactSubTabs.SelectedIndex;
                e.Graphics.FillRectangle(sel ? new SolidBrush(Color.FromArgb(0xE3, 0xF2, 0xFD)) : Brushes.White, e.Bounds);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(t.Text, TG.FontSemiBold(9f), new SolidBrush(sel ? TG.Blue : TG.TextSecondary), e.Bounds, sf);
            };

            var tpPrivate = new TabPage("Tin nhắn riêng") { BackColor = Color.White, UseVisualStyleBackColor = false };
            var tpGroup   = new TabPage("Nhóm")           { BackColor = Color.White, UseVisualStyleBackColor = false };
            _contactSubTabs.TabPages.AddRange(new[] { tpPrivate, tpGroup });

            _pnlPrivate = BuildContactList(_contacts.FindAll(c => c.Username != ""));
            _pnlGroup   = BuildContactList(_contacts.FindAll(c => c.Username == ""));

            tpPrivate.Controls.Add(_pnlPrivate);
            tpGroup.Controls.Add(_pnlGroup);
            _tabContacts.Controls.Add(_contactSubTabs);
        }

        private Panel BuildContactList(List<ContactItem> items)
        {
            var pnl = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            int y = 0;
            foreach (var item in items)
            {
                var row = BuildContactRow(item);
                row.Location = new Point(0, y);
                pnl.Controls.Add(row);
                pnl.Resize += (s, e) => row.Width = pnl.Width;
                y += 62;
            }
            return pnl;
        }

        private Panel BuildContactRow(ContactItem c)
        {
            var pnl = new Panel { Height = 62, BackColor = Color.White, Cursor = Cursors.Hand };
            pnl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
            pnl.MouseLeave += (s, e) => pnl.BackColor = Color.White;

            var avatar = new AvatarControl { Size = new Size(44, 44), Location = new Point(10, 9), ShowOnline = c.IsOnline };
            avatar.SetName(c.Name);

            var lblName = new Label
            {
                Text = c.Name, Font = TG.FontSemiBold(9.5f), ForeColor = TG.TextName,
                AutoSize = false, Height = 20, Location = new Point(62, 12), BackColor = Color.Transparent,
            };
            var lblSub = new Label
            {
                Text = string.IsNullOrEmpty(c.Username) ? c.LastSeen : c.Username,
                Font = TG.FontRegular(8.5f), ForeColor = TG.TextSecondary,
                AutoSize = false, Height = 18, Location = new Point(62, 32), BackColor = Color.Transparent,
            };

            // Message button
            var btnMsg = new TelegramButton
            {
                Text = "💬", Width = 36, Height = 28,
                Font = new Font("Segoe UI Emoji", 11f),
                Radius = TG.RadiusSmall, NormalColor = Color.Transparent,
                TextColor = TG.Blue,
            };
            btnMsg.Click += (s, e) =>
            {
                var mainForm = Application.OpenForms["MainForm"] as frmMainChat ?? new frmMainChat();
                mainForm.Show();
            };

            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnMsg });
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 62, 61, pnl.Width, 61);
            pnl.Resize += (s, e) =>
            {
                lblName.Width = pnl.Width - 62 - 50;
                lblSub.Width  = pnl.Width - 62 - 50;
                btnMsg.Location = new Point(pnl.Width - 46, 17);
            };
            foreach (Control ctrl in pnl.Controls)
                ctrl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;

            return pnl;
        }

        // ── TAB 2: FRIEND REQUESTS ───────────────────
        private void BuildRequestsTab()
        {
            _requestSubTabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.FlatButtons,
                DrawMode = TabDrawMode.OwnerDrawFixed,
                ItemSize = new Size(180, 30),
                SizeMode = TabSizeMode.Fixed,
                Font = TG.FontRegular(9f),
            };
            _requestSubTabs.DrawItem += (s, e) =>
            {
                var t = _requestSubTabs.TabPages[e.Index];
                bool sel = e.Index == _requestSubTabs.SelectedIndex;
                e.Graphics.FillRectangle(sel ? new SolidBrush(Color.FromArgb(0xE3, 0xF2, 0xFD)) : Brushes.White, e.Bounds);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(t.Text, TG.FontSemiBold(9f), new SolidBrush(sel ? TG.Blue : TG.TextSecondary), e.Bounds, sf);
            };

            var tpIncoming = new TabPage($"Đã nhận ({_incomingCount})") { BackColor = Color.White, UseVisualStyleBackColor = false };
            var tpSent     = new TabPage("Đã gửi (1)")                  { BackColor = Color.White, UseVisualStyleBackColor = false };
            _requestSubTabs.TabPages.AddRange(new[] { tpIncoming, tpSent });

            // Incoming requests
            var pnlIn = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            int y = 0;
            foreach (var req in _requests.FindAll(r => r.IsIncoming))
            {
                var row = BuildRequestRow(req, isIncoming: true);
                row.Location = new Point(0, y);
                pnlIn.Controls.Add(row);
                pnlIn.Resize += (s, e) => row.Width = pnlIn.Width;
                y += 86;
            }
            tpIncoming.Controls.Add(pnlIn);

            // Sent requests
            var pnlSent = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            y = 0;
            foreach (var req in _requests.FindAll(r => !r.IsIncoming))
            {
                var row = BuildRequestRow(req, isIncoming: false);
                row.Location = new Point(0, y);
                pnlSent.Controls.Add(row);
                pnlSent.Resize += (s, e) => row.Width = pnlSent.Width;
                y += 86;
            }
            tpSent.Controls.Add(pnlSent);

            _tabRequests.Controls.Add(_requestSubTabs);
        }

        private Panel BuildRequestRow(FriendRequestItem req, bool isIncoming)
        {
            var pnl = new Panel { Height = 86, BackColor = Color.White };

            var avatar = new AvatarControl { Size = new Size(44, 44), Location = new Point(12, 10) };
            avatar.SetName(req.Name);

            var lblName = new Label
            {
                Text = req.Name, Font = TG.FontSemiBold(9.5f), ForeColor = TG.TextName,
                AutoSize = false, Height = 20, Location = new Point(64, 12), BackColor = Color.Transparent,
            };
            var lblSub = new Label
            {
                Text = req.MutualCount > 0 ? $"Bạn bè chung: {req.MutualCount}" : req.Username,
                Font = TG.FontRegular(8.5f), ForeColor = TG.TextSecondary,
                AutoSize = false, Height = 16, Location = new Point(64, 32), BackColor = Color.Transparent,
            };

            // Action buttons
            if (isIncoming)
            {
                var btnAccept = new TelegramButton { Text = "Chấp nhận", Height = 28, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f) };
                var btnDecline= new TelegramButton { Text = "Từ chối", Height = 28, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f), IsOutlined = true };
                btnAccept.Click  += (s, e) => { RemoveRequest(pnl, isAccepted: true);  _incomingCount--; _tabs.Invalidate(); };
                btnDecline.Click += (s, e) => RemoveRequest(pnl, isAccepted: false);

                pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnAccept, btnDecline });
                pnl.Resize += (s, e) =>
                {
                    lblName.Width = lblSub.Width = pnl.Width - 64 - 12;
                    int bw = (pnl.Width - 64 - 24) / 2;
                    btnAccept.SetBounds(64, 54, bw, 28);
                    btnDecline.SetBounds(64 + bw + 8, 54, bw, 28);
                };
            }
            else
            {
                var btnCancel = new TelegramButton { Text = "Hủy lời mời", Height = 28, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f), IsOutlined = true };
                btnCancel.Click += (s, e) => RemoveRequest(pnl, false);

                pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnCancel });
                pnl.Resize += (s, e) =>
                {
                    lblName.Width = lblSub.Width = pnl.Width - 64 - 12;
                    btnCancel.SetBounds(64, 54, pnl.Width - 64 - 12, 28);
                };
            }

            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 0, 85, pnl.Width, 85);
            return pnl;
        }

        private void RemoveRequest(Panel row, bool isAccepted)
        {
            string msg = isAccepted ? "Đã chấp nhận lời mời kết bạn!" : "Đã từ chối lời mời.";
            MessageBox.Show(msg, "SecureChat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            row.Visible = false;
        }

        // ── TAB 3: TÌM KIẾM ─────────────────────────
        private void BuildSearchTab()
        {
            var pnlSearch = new Panel { Height = 52, Dock = DockStyle.Top, BackColor = Color.White, Padding = new Padding(12, 8, 12, 6) };
            _tbSearch = new TelegramTextBox { Height = 36, Dock = DockStyle.Fill };
            _tbSearch.SetPlaceholder("🔍  Tìm theo tên hoặc @username...");
            _tbSearch.TextChanged += (s, e) => DoSearch(_tbSearch.Text);
            pnlSearch.Controls.Add(_tbSearch);

            _lblSearchHint = new Label
            {
                Text = "Nhập tên hoặc username để tìm kiếm",
                Font = TG.FontRegular(9.5f), ForeColor = TG.TextHint,
                AutoSize = false, BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
            };

            _pnlSearchResults = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            _pnlSearchResults.Controls.Add(_lblSearchHint);

            _tabSearch.Controls.AddRange(new Control[] { _pnlSearchResults, pnlSearch });
        }

        private void DoSearch(string query)
        {
            _pnlSearchResults.Controls.Clear();

            if (string.IsNullOrWhiteSpace(query)) { _pnlSearchResults.Controls.Add(_lblSearchHint); return; }

            // Mock search results
            var results = new List<ContactItem>
            {
                new() { Id="s1", Name="Nguyễn Văn A",   Username="@nguyenvana",  Status=FriendStatus.Friend,  IsOnline=true  },
                new() { Id="s2", Name="Nguyễn Thị H",   Username="@nguyenthih",  Status=FriendStatus.None,    IsOnline=false },
                new() { Id="s3", Name="Nguyễn Minh K",  Username="@kminh99",     Status=FriendStatus.Pending, IsOnline=false },
            };

            if (query.Length < 2) { _pnlSearchResults.Controls.Add(_lblSearchHint); return; }

            int y = 0;
            var lblHdr = new Label
            {
                Text = $"Kết quả cho \"{query}\"",
                Font = TG.FontRegular(8.5f), ForeColor = TG.TextSecondary,
                AutoSize = false, Height = 24, BackColor = Color.Transparent,
                Padding = new Padding(12, 4, 0, 0),
            };
            lblHdr.Location = new Point(0, y);
            lblHdr.Width = _pnlSearchResults.Width;
            _pnlSearchResults.Controls.Add(lblHdr);
            y += 26;

            foreach (var r in results)
            {
                var row = BuildSearchRow(r);
                row.Location = new Point(0, y);
                _pnlSearchResults.Controls.Add(row);
                _pnlSearchResults.Resize += (s, e) => row.Width = _pnlSearchResults.Width;
                y += 60;
            }
        }

        private Panel BuildSearchRow(ContactItem c)
        {
            var pnl = new Panel { Height = 60, BackColor = Color.White, Cursor = Cursors.Hand };
            pnl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
            pnl.MouseLeave += (s, e) => pnl.BackColor = Color.White;

            var avatar = new AvatarControl { Size = new Size(40, 40), Location = new Point(10, 10), ShowOnline = c.IsOnline };
            avatar.SetName(c.Name);

            var lblName = new Label
            {
                Text = c.Name, Font = TG.FontSemiBold(9.5f), ForeColor = TG.TextName,
                AutoSize = false, Height = 20, Location = new Point(58, 10), BackColor = Color.Transparent,
            };
            var lblUser = new Label
            {
                Text = c.Username, Font = TG.FontRegular(8.5f), ForeColor = TG.TextBlue,
                AutoSize = false, Height = 16, Location = new Point(58, 30), BackColor = Color.Transparent,
            };

            // Status button
            Control statusCtrl;
            if (c.Status == FriendStatus.Friend)
            {
                statusCtrl = new Label
                {
                    Text = "✓ Bạn bè", Font = TG.FontRegular(8f),
                    ForeColor = Color.FromArgb(0x2E, 0x7D, 0x32),
                    BackColor = Color.FromArgb(0xE8, 0xF5, 0xE9),
                    AutoSize = false, Height = 24, TextAlign = ContentAlignment.MiddleCenter,
                    Padding = new Padding(6, 0, 6, 0),
                };
                ((Label)statusCtrl).BorderStyle = BorderStyle.FixedSingle;
            }
            else if (c.Status == FriendStatus.Pending)
            {
                statusCtrl = new Label
                {
                    Text = "Đã gửi", Font = TG.FontRegular(8f),
                    ForeColor = Color.FromArgb(0xE6, 0x5C, 0x00),
                    BackColor = Color.FromArgb(0xFF, 0xF3, 0xE0),
                    AutoSize = false, Height = 24, TextAlign = ContentAlignment.MiddleCenter,
                };
            }
            else
            {
                var btn = new TelegramButton { Text = "+ Kết bạn", Height = 28, Width = 80, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f) };
                btn.Click += (s, e) =>
                {
                    c.Status = FriendStatus.Pending;
                    pnl.Invalidate();
                };
                statusCtrl = btn;
            }

            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblUser, statusCtrl });
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 58, 59, pnl.Width, 59);
            pnl.Resize += (s, e) =>
            {
                lblName.Width = lblUser.Width = pnl.Width - 58 - 100;
                statusCtrl.Location = new Point(pnl.Width - 92, 16);
                if (statusCtrl is not TelegramButton) statusCtrl.Width = 80;
            };

            return pnl;
        }
    }
}
