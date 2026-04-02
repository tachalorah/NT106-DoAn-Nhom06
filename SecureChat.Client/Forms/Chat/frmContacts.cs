using System;
using System.Collections.Generic; //  cho phép dùng List<T>.
using System.Drawing; //  làm việc với màu sắc (Color), font, hình ảnh.
using System.Drawing.Drawing2D; // vẽ nâng cao như SmoothingMode, AntiAlias.
using System.Windows.Forms; // tạo giao diện desktop (Form, Panel, Label, Button...).

namespace SecureChat.Client
{
    // Dùng một List<ContactItem> để chứa cả bạn bè và nhóm
    // Tìm kiếm hoặc hiển thị danh sách tổng hợp dễ dàng hơn.
    public enum ContactType { Friend, Group }

    public enum FriendStatus { None, PendingIncoming, PendingOutgoing, Friend, Blocked }
    /*Trạng thái quan hệ bạn bè:
    None — chưa có quan hệ
    PendingIncoming — đang có lời mời gửi đến
    PendingOutgoing — đã gửi lời mời, chờ đối phương
    Friend — đã là bạn bè
    Blocked — đã bị chặn*/


    // Model dữ liệu đại diện cho một contact (bạn bè hoặc nhóm):
    public class ContactItem
    {
        public ContactType Type { get; set; } = ContactType.Friend;
        // Khi tạo 1 đối tượng ContactItem, mặc định là Friend
        // Khi tạo Group thì ghi đè lại

        public string DisplayName { get; set; } = string.Empty; // Tên hiển thị
        public string AvatarUrl { get; set; } = string.Empty; // URL ảnh đại diện
        public string LastSeenAt { get; set; } = string.Empty; // Thời gian hoạt động cuối

        public string UserId { get; set; } = string.Empty; // ID người dùng
        public string Username { get; set; } = string.Empty; // Tên đăng nhập (@username)
        public string Nickname { get; set; } = string.Empty; // Biệt danh (nếu có)
        public bool IsOnline { get; set; } // Đang online hay không: có chấm xanh cạnh Avatar
        public FriendStatus Status { get; set; } = FriendStatus.None; // Trạng thái bạn bè: 5 cái (None, PendingIncoming, PendingOutgoing, Friend, Blocked)

        public string ConversationId { get; set; } = string.Empty; // ID cuộc hội thoại (dùng cho Group)
        public int MemberCount { get; set; } // Số thành viên (dùng cho Group)
    }

    // Model cho một lời mời kết bạn:
    public class FriendRequestItem
    {
        public string RequestId { get; set; } = string.Empty; // ID người nhận
        public string SenderId { get; set; } = string.Empty; // ID người gửi
        public string RecipientId { get; set; } = string.Empty; // ID người nhận
        public string DisplayName { get; set; } = string.Empty; // Tên người gửi
        public string Username { get; set; } = string.Empty; // Username người gửi
        public string AvatarUrl { get; set; } = string.Empty; // Ảnh đại diện người gửi
        public string CreatedAt { get; set; } = string.Empty; // Thời điểm gửi
        public int MutualCount { get; set; } // Số bạn chung
        public bool IsIncoming { get; set; } // true = lời mời nhận được, false = mình đã gửi
    }

    public class frmContacts : Form
    {
        // Biến toàn cục của class (private readonly).
        // Điều này giúp các hàm khác trong class có thể truy cập trực tiếp vào hai cái bảng này bất cứ lúc nào
        // Ví dụ: để làm mới danh sách bạn bè mà không cần tìm lại nó nằm ở đâu.

        private readonly TabControl _tabs; // chứa 3 tab cha : Danh sách, Lời mời, và Tìm kiếm.
        private readonly TabPage _tabContacts, _tabRequests, _tabSearch; // 3 trang nội dung tương ứng với 3 tab cha.

        // Tab con trong "Danh sách" để chia Bạn bè / Nhóm.
        private readonly TabControl _contactSubTabs;
        private readonly Panel _pnlFriends, _pnlGroups; // Khác với tab cha Lời mời, tab con của Danh sách được khai báo toàn cục.
                                                        // Khi 1 người dùng được cập nhật hay thay đổi trạng thái thì không ảnh hưởng đến còn lại
                                                        // ----------------------- Thiếu tab con ""Người dùng đã bị chặn"" ----------------------------------------

        /*
        _______________________________________________________
        |                                                     |
        |   [Tab 1 ][Tab 2 ][Tab 3 ]                          | <-- Tabstrip
        |_____________________________________________________|
        |                                                     |
        |                                                     |
        |                  TAB PAGE                           |
        |                                                     |
        |                                                     |
        |                                                     |
        |_____________________________________________________|
        */

        // Tab con trong "Lời mời" để chia Đã nhận / Đã gửi.
        private readonly TabControl _requestSubTabs;
        // Panel cho 2 tab con được khai báo cục bộ
        // Trong lập trình WinForms, thay vì tìm cách chèn thêm 1 dòng vào giữa một cái Panel đang có sẵn, người ta thường chọn cách xóa sạch và vẽ lại:

        // Thanh tìm kiếm, vùng kết quả, và label gợi ý tìm kiếm.
        private readonly TelegramTextBox _tbSearch;
        private readonly Panel _pnlSearchResults;
        // Khác với Placeholder là "Bạn có thể gõ gì vào đây", Hint là giải thích "Tại sao vùng này đang trống"
        private readonly Label _lblSearchHint;

        // Đếm số những lời mời bạn đã nhận được (Incoming) (để hiển thị badge đỏ trên tab "Lời mời").
        private int _incomingCount = 0;

        private List<ContactItem> _friends = new List<ContactItem>();
        private List<ContactItem> _groups = new List<ContactItem>();
        private List<FriendRequestItem> _requests = new List<FriendRequestItem>();

        public frmContacts()
        {
            InitMockData();

            // TabPage chính vừa là cái "đầu tab" (tabstrip) vừa là vùng nội dung bên trong.
            // Chuỗi "  Danh sách  " truyền vào constructor chính là thuộc tính Text của TabPage, và WinForms tự dùng Text đó để vẽ chữ lên tabstrip.
            // WinForms không tự vẽ nữa — thay vào đó hàm DrawTabItem() tự lấy tab.Text ra để vẽ.
            _tabs = new TabControl();
            _tabContacts = new TabPage("  Danh sách  ") { BackColor = Color.White, UseVisualStyleBackColor = false };
            _tabRequests = new TabPage("  Lời mời    ") { BackColor = Color.White, UseVisualStyleBackColor = false };
            _tabSearch = new TabPage("  Tìm kiếm  ") { BackColor = Color.White, UseVisualStyleBackColor = false };

            _contactSubTabs = new TabControl();
            _pnlFriends = new Panel();
            _pnlGroups = new Panel();

            _requestSubTabs = new TabControl();

            _tbSearch = new TelegramTextBox();
            _pnlSearchResults = new Panel();
            _lblSearchHint = new Label();

            InitializeComponent();
        }

        /*Tạo dữ liệu giả cho demo:
        _friends — 4 người bạn(2 online, 2 offline).
        _groups — 2 nhóm chat.
        _requests — 3 lời mời đến + 1 lời mời đã gửi.
        _incomingCount — đếm số request IsIncoming = true(= 3).*/
        private void InitMockData()
        {
            _friends = new List<ContactItem>
            {
                new() { Type = ContactType.Friend, UserId = "usr-001", DisplayName = "Nguyễn Văn A", Username = "nguyenvana", Nickname = "", IsOnline = true, Status = FriendStatus.Friend, LastSeenAt = "2025-03-31T09:00:00Z" },
                new() { Type = ContactType.Friend, UserId = "usr-002", DisplayName = "Trần Thị B", Username = "tranthib", Nickname = "Bé B", IsOnline = true, Status = FriendStatus.Friend, LastSeenAt = "2025-03-31T08:55:00Z" },
                new() { Type = ContactType.Friend, UserId = "usr-003", DisplayName = "Lê Minh C", Username = "leminhc", IsOnline = false, Status = FriendStatus.Friend, LastSeenAt = "2025-03-31T08:00:00Z" },
                new() { Type = ContactType.Friend, UserId = "usr-004", DisplayName = "Phạm Minh Đức", Username = "phamminhduc", IsOnline = false, Status = FriendStatus.Friend, LastSeenAt = "2025-03-30T20:00:00Z" },
            };

            _groups = new List<ContactItem>
            {
                new() { Type = ContactType.Group, ConversationId = "conv-g01", DisplayName = "Nhóm NT106 Q22", MemberCount = 5, LastSeenAt = "2025-03-31T07:00:00Z" },
                new() { Type = ContactType.Group, ConversationId = "conv-g02", DisplayName = "Nhóm An Toàn TT", MemberCount = 8, LastSeenAt = "2025-03-30T18:00:00Z" },
            };

            _requests = new List<FriendRequestItem>
            {
                new() { RequestId = "req-001", SenderId = "usr-010", RecipientId = "usr-me", DisplayName = "Trần Thị H", Username = "tranthih", MutualCount = 2, IsIncoming = true, CreatedAt = "2025-03-30T10:00:00Z" },
                new() { RequestId = "req-002", SenderId = "usr-011", RecipientId = "usr-me", DisplayName = "Phạm Văn D", Username = "phamvand", MutualCount = 0, IsIncoming = true, CreatedAt = "2025-03-29T15:30:00Z" },
                new() { RequestId = "req-003", SenderId = "usr-012", RecipientId = "usr-me", DisplayName = "Lê Thị E", Username = "lethie", MutualCount = 5, IsIncoming = true, CreatedAt = "2025-03-28T08:00:00Z" },
                new() { RequestId = "req-004", SenderId = "usr-me", RecipientId = "usr-013", DisplayName = "Nguyễn Quốc K", Username = "nguyenquock", MutualCount = 1, IsIncoming = false, CreatedAt = "2025-03-31T06:00:00Z" },
            };

            _incomingCount = _requests.FindAll(r => r.IsIncoming).Count;
            // FindAll(r => r.IsIncoming): Duyệt qua toàn bộ danh sách _requests và lọc ra một danh sách con chỉ chứa các lời mời có IsIncoming == true.
            // Count: Đếm xem danh sách con đó có bao nhiêu phần tử và gán con số đó vào biến _incomingCount.
        }

        private void InitializeComponent()
        {
            Text = "Danh bạ";
            Size = new Size(440, 620);
            MinimumSize = new Size(360, 560);
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.White;
            MaximizeBox = false; // chặn nút phóng to
            Font = TG.FontRegular(9.5f);

            // 1. Header (đặt trước để WinForms tính docking đúng)
            var header = new TelegramHeader
            {
                Title = "Danh bạ",
                ShowBack = true,
                Dock = DockStyle.Top
            };
            header.BackClicked += (s, e) => Close();

            // 2. Tab cha

            // Cấu hình Kích thước và Vị trí
            _tabs.Padding = new Point(0, 0); // Loại bỏ khoảng cách đệm giữa các TabPage và nội dung bên trong
                                             // giúp giao diện khít sát và gọn gàng.
            _tabs.Dock = DockStyle.Fill; // Làm cho bộ Tab này lấp đầy toàn bộ diện tích của Form hoặc Panel chứa nó.
            _tabs.ItemSize = new Size(0, 32); // Thiết lập chiều cao của thanh tiêu đề Tabstrip là 32 pixel.
                                              // Số 0 ở đầu có nghĩa là chiều rộng sẽ được tự động tính toán.
            _tabs.SizeMode = TabSizeMode.FillToRight; // Các tiêu đề Tab sẽ tự động giãn ra để dàn đều theo chiều ngang,
                                                      // lấp đầy thanh menu phía trên thay vì chỉ co cụm ở bên trái.

            // Hình thức
            _tabs.Appearance = TabAppearance.FlatButtons; // Thay đổi kiểu hiển thị từ dạng "thẻ kẹp hồ sơ" truyền thống của Windows sang dạng nút phẳng.
                                                          // Khi kết hợp với OwnerDrawFixed, nó sẽ giúp bạn dễ dàng vẽ lại màu sắc theo ý muốn.
            _tabs.Font = TG.FontRegular(9.5f);

            // Vẽ
            _tabs.DrawMode = TabDrawMode.OwnerDrawFixed; // Bật chế độ tự vẽ
            _tabs.DrawItem += DrawTabItem; // Sự kiện kích hoạt hàm DrawTabItem.
                                           // Trong hàm đó, bạn sẽ code để tô màu nền xanh khi chọn Tab, vẽ số thông báo(badge) đỏ, hoặc đổi màu chữ.
            _tabs.Selected += (s, e) => _tabs.Invalidate(); // Khi người dùng bấm chọn một Tab khác, lệnh Invalidate() yêu cầu Tab đó phải vẽ lại ngay lập tức để cập nhật trạng thái "đang được chọn"

            // Thêm 3 trang
            _tabs.TabPages.AddRange(new[] { _tabContacts, _tabRequests, _tabSearch });

            // Khởi tạo chi tiết bên trong từng trang (ví dụ: tạo các Tab con, tạo Panel, đổ dữ liệu ban đầu).
            BuildContactsTab();
            BuildRequestsTab();
            BuildSearchTab();

            // 3. Panel Content (Fill) - PHẢI ADD TRƯỚC HEADER

            var pnlContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White
            };
            pnlContent.Controls.Add(_tabs);

            // Thứ tự Add quan trọng: Fill trước → Top sau
            Controls.Add(pnlContent);
            Controls.Add(header);
            // KHÔNG dùng BringToFront() nữa
        }

        // Hàm xử lý resize panel. Khi panel thay đổi kích thước khi tôi kéo to nhỏ form,
        // Tất cả các row bên trong cũng được cập nhật độ rộng theo
        // row là các đoạn chat trong tabpage để không bị khoảnh trắng lúc ta kéo
        private void Pnl_UpdateRowsWidth(object? sender, EventArgs e)
        {
            if (sender is Panel pnl) // kiểm tra và ép kiểu để sài các function của 1 panel
                                     // tránh gây lỗi "văng" ứng dụng (crash) nếu lỡ tay gán sự kiện này cho một cái nút hay cái nhãn.
            {
                pnl.SuspendLayout(); // tạm dừng bố cục lại, giúp hệ thống chỉ tính toán một lần duy nhất ở cuối nếu kéo lâu lần.
                int targetWidth = pnl.ClientSize.Width;
                // int targetWidth = pnl.Width;
                foreach (Control row in pnl.Controls)
                {
                    row.Width = targetWidth;
                }
                pnl.ResumeLayout();
            }
        }

        private void DrawTabItem(object? sender, DrawItemEventArgs e)
        {
            var tab = _tabs.TabPages[e.Index];
            bool selected = e.Index == _tabs.SelectedIndex;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.FillRectangle(Brushes.White, e.Bounds); // chỉnh màu trắng cho bên trong các tab cha thay vì màu xám

            if (selected)
            {
                var underline = new Rectangle(e.Bounds.Left, e.Bounds.Bottom - 2, e.Bounds.Width, 2);
                e.Graphics.FillRectangle(new SolidBrush(TG.Blue), underline); // gạch xanh cho tab cha đang chọn
            }

            Color fgColor = selected ? TG.Blue : TG.TextSecondary; // Nếu chọn thì chữ màu xanh, không chọn thì chữ màu xám phụ
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            e.Graphics.DrawString(tab.Text.Trim(), TG.FontSemiBold(9.5f), new SolidBrush(fgColor), e.Bounds, sf);

            if (e.Index == 1 && _incomingCount > 0) // Chỉ vẽ trên Tab thứ 2 (Tab Lời mời).
                                                    // Chỉ vẽ khi thực sự có lời mời đang chờ. Nếu không có ai kết bạn, cái chấm đỏ sẽ tự biến mất.
            {
                int bx = e.Bounds.Right - 22; // Lấy mép phải của Tab lùi vào 22 pixel.
                // int by = e.Bounds.Top + 6; // Cách mép trên của Tab xuống 6 pixel.
                int by = e.Bounds.Top;
                var badgeRect = new Rectangle(bx, by, 18, 18); // Tạo một khung hình vuông kích thước 18x18 pixel.
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(0xE2, 0x4B, 0x4A)), badgeRect); // Vẽ hình tròn màu đỏ hồng

                // sau khi vẽ xong số, hệ thống sẽ giải phóng bộ nhớ (Dispose) của đối tượng StringFormat ngay lập tức
                using var sf2 = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(_incomingCount.ToString(), TG.FontSemiBold(7.5f), Brushes.White, badgeRect, sf2);
            }
        }

        private void BuildContactsTab()
        {
            _contactSubTabs.Dock = DockStyle.Fill;
            _contactSubTabs.Appearance = TabAppearance.FlatButtons;
            _contactSubTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            _contactSubTabs.ItemSize = new Size(0, 30);           // ← auto chia đều 2 tab
            _contactSubTabs.SizeMode = TabSizeMode.Fixed;
            _contactSubTabs.Font = TG.FontRegular(9f);
            _contactSubTabs.DrawItem += (s, e) =>
            {
                var t = _contactSubTabs.TabPages[e.Index];
                bool sel = e.Index == _contactSubTabs.SelectedIndex;
                e.Graphics.FillRectangle(sel ? new SolidBrush(Color.FromArgb(0xE3, 0xF2, 0xFD)) : Brushes.White, e.Bounds);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(t.Text, TG.FontSemiBold(9f), new SolidBrush(sel ? TG.Blue : TG.TextSecondary), e.Bounds, sf);
            };

            var tpFriends = new TabPage("Bạn bè") { BackColor = Color.White, UseVisualStyleBackColor = false };
            var tpGroups = new TabPage("Nhóm") { BackColor = Color.White, UseVisualStyleBackColor = false };
            _contactSubTabs.TabPages.AddRange(new[] { tpFriends, tpGroups });

            _pnlFriends.Dock = DockStyle.Fill;
            _pnlFriends.AutoScroll = true;
            _pnlFriends.BackColor = Color.White;
            _pnlFriends.Resize += Pnl_UpdateRowsWidth;

            _pnlGroups.Dock = DockStyle.Fill;
            _pnlGroups.AutoScroll = true;
            _pnlGroups.BackColor = Color.White;
            _pnlGroups.Resize += Pnl_UpdateRowsWidth;

            BuildFriendList(_friends, _pnlFriends);
            BuildGroupList(_groups, _pnlGroups);

            tpFriends.Controls.Add(_pnlFriends);
            tpGroups.Controls.Add(_pnlGroups);
            _tabContacts.Controls.Add(_contactSubTabs);
        }

        private void BuildFriendList(List<ContactItem> friends, Panel pnl)
        {
            int y = 0;
            int initialWidth = pnl.ClientSize.Width > 0 ? pnl.ClientSize.Width : 360;
            foreach (var item in friends)
            {
                var row = BuildFriendRow(item, initialWidth);
                row.Location = new Point(0, y);
                pnl.Controls.Add(row);
                y += 62;
            }
        }

        private void BuildGroupList(List<ContactItem> groups, Panel pnl)
        {
            int y = 0;
            int initialWidth = pnl.ClientSize.Width > 0 ? pnl.ClientSize.Width : 360;
            foreach (var item in groups)
            {
                var row = BuildGroupRow(item, initialWidth);
                row.Location = new Point(0, y);
                pnl.Controls.Add(row);
                y += 62;
            }
        }

        private Panel BuildFriendRow(ContactItem c, int initialWidth)
        {
            var pnl = new Panel { Height = 62, Width = initialWidth, BackColor = Color.White, Cursor = Cursors.Hand };

            var avatar = new AvatarControl { Size = new Size(44, 44), Location = new Point(10, 9), ShowOnline = c.IsOnline };
            avatar.SetName(c.DisplayName);

            string displayText = string.IsNullOrWhiteSpace(c.Nickname) ? c.DisplayName : c.Nickname;
            var lblName = new Label
            {
                Text = displayText,
                Font = TG.FontSemiBold(9.5f),
                ForeColor = TG.TextName,
                AutoSize = false,
                Height = 20,
                Location = new Point(62, 12),
                Width = initialWidth - 112,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            var lblSub = new Label
            {
                Text = "@" + c.Username,
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false,
                Height = 18,
                Location = new Point(62, 32),
                Width = initialWidth - 112,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            var btnMsg = new TelegramButton
            {
                Text = "💬",
                Width = 36,
                Height = 28,
                Font = new Font("Segoe UI Emoji", 11f),
                Radius = TG.RadiusSmall,
                NormalColor = Color.Transparent,
                TextColor = TG.Blue,
                Location = new Point(initialWidth - 46, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            btnMsg.Click += (s, e) =>
            {
                var mainForm = Application.OpenForms["MainForm"] as frmMainChat ?? new frmMainChat();
                mainForm.Show();
            };

            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnMsg });
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 62, 61, pnl.Width, 61);

            foreach (Control ctrl in pnl.Controls)
            {
                ctrl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
                ctrl.MouseLeave += (s, e) =>
                {
                    if (!pnl.ClientRectangle.Contains(pnl.PointToClient(Control.MousePosition)))
                        pnl.BackColor = Color.White;
                };
            }
            pnl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
            pnl.MouseLeave += (s, e) => pnl.BackColor = Color.White;

            return pnl;
        }

        private Panel BuildGroupRow(ContactItem c, int initialWidth)
        {
            var pnl = new Panel { Height = 62, Width = initialWidth, BackColor = Color.White, Cursor = Cursors.Hand };

            var avatar = new AvatarControl { Size = new Size(44, 44), Location = new Point(10, 9), ShowOnline = false };
            avatar.SetName(c.DisplayName);

            var lblName = new Label
            {
                Text = c.DisplayName,
                Font = TG.FontSemiBold(9.5f),
                ForeColor = TG.TextName,
                AutoSize = false,
                Height = 20,
                Location = new Point(62, 12),
                Width = initialWidth - 112,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            var lblSub = new Label
            {
                Text = $"{c.MemberCount} thành viên",
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false,
                Height = 18,
                Location = new Point(62, 32),
                Width = initialWidth - 112,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            var btnMsg = new TelegramButton
            {
                Text = "💬",
                Width = 36,
                Height = 28,
                Font = new Font("Segoe UI Emoji", 11f),
                Radius = TG.RadiusSmall,
                NormalColor = Color.Transparent,
                TextColor = TG.Blue,
                Location = new Point(initialWidth - 46, 17),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            btnMsg.Click += (s, e) =>
            {
                var mainForm = Application.OpenForms["MainForm"] as frmMainChat ?? new frmMainChat();
                mainForm.Show();
            };

            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnMsg });
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 62, 61, pnl.Width, 61);

            foreach (Control ctrl in pnl.Controls)
            {
                ctrl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
                ctrl.MouseLeave += (s, e) =>
                {
                    if (!pnl.ClientRectangle.Contains(pnl.PointToClient(Control.MousePosition)))
                        pnl.BackColor = Color.White;
                };
            }
            pnl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
            pnl.MouseLeave += (s, e) => pnl.BackColor = Color.White;

            return pnl;
        }

        private void BuildRequestsTab()
        {
            _requestSubTabs.Dock = DockStyle.Fill;
            _requestSubTabs.Appearance = TabAppearance.FlatButtons;
            _requestSubTabs.DrawMode = TabDrawMode.OwnerDrawFixed;
            _requestSubTabs.ItemSize = new Size(180, 30);
            _requestSubTabs.SizeMode = TabSizeMode.Fixed;
            _requestSubTabs.Font = TG.FontRegular(9f);
            _requestSubTabs.DrawItem += (s, e) =>
            {
                var t = _requestSubTabs.TabPages[e.Index];
                bool sel = e.Index == _requestSubTabs.SelectedIndex;
                e.Graphics.FillRectangle(sel ? new SolidBrush(Color.FromArgb(0xE3, 0xF2, 0xFD)) : Brushes.White, e.Bounds);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(t.Text, TG.FontSemiBold(9f), new SolidBrush(sel ? TG.Blue : TG.TextSecondary), e.Bounds, sf);
            };

            int incomingCount = _requests.FindAll(r => r.IsIncoming).Count;
            int outgoingCount = _requests.FindAll(r => !r.IsIncoming).Count;

            var tpIncoming = new TabPage($"Đã nhận ({incomingCount})") { BackColor = Color.White, UseVisualStyleBackColor = false };
            var tpSent = new TabPage($"Đã gửi ({outgoingCount})") { BackColor = Color.White, UseVisualStyleBackColor = false };
            _requestSubTabs.TabPages.AddRange(new[] { tpIncoming, tpSent });

            var pnlIn = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            pnlIn.Resize += Pnl_UpdateRowsWidth;
            int y = 0;
            int initInWidth = pnlIn.ClientSize.Width > 0 ? pnlIn.ClientSize.Width : 360;
            foreach (var req in _requests.FindAll(r => r.IsIncoming))
            {
                var row = BuildRequestRow(req, true, initInWidth);
                row.Location = new Point(0, y);
                pnlIn.Controls.Add(row);
                y += 86;
            }
            tpIncoming.Controls.Add(pnlIn);

            var pnlSent = new Panel { Dock = DockStyle.Fill, AutoScroll = true, BackColor = Color.White };
            pnlSent.Resize += Pnl_UpdateRowsWidth;
            y = 0;
            int initSentWidth = pnlSent.ClientSize.Width > 0 ? pnlSent.ClientSize.Width : 360;
            foreach (var req in _requests.FindAll(r => !r.IsIncoming))
            {
                var row = BuildRequestRow(req, false, initSentWidth);
                row.Location = new Point(0, y);
                pnlSent.Controls.Add(row);
                y += 86;
            }
            tpSent.Controls.Add(pnlSent);

            _tabRequests.Controls.Add(_requestSubTabs);
        }

        private Panel BuildRequestRow(FriendRequestItem req, bool isIncoming, int initialWidth)
        {
            var pnl = new Panel { Height = 86, Width = initialWidth, BackColor = Color.White };

            var avatar = new AvatarControl { Size = new Size(44, 44), Location = new Point(12, 10) };
            avatar.SetName(req.DisplayName);

            var lblName = new Label
            {
                Text = req.DisplayName,
                Font = TG.FontSemiBold(9.5f),
                ForeColor = TG.TextName,
                AutoSize = false,
                Height = 20,
                Location = new Point(64, 12),
                Width = initialWidth - 76,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            var lblSub = new Label
            {
                Text = req.MutualCount > 0 ? $"Bạn bè chung: {req.MutualCount}" : "@" + req.Username,
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false,
                Height = 16,
                Location = new Point(64, 32),
                Width = initialWidth - 76,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            if (isIncoming)
            {
                int btnWidth = (initialWidth - 88) / 2;
                var btnAccept = new TelegramButton { Text = "Chấp nhận", Height = 28, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f), Location = new Point(64, 54), Width = btnWidth, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                var btnDecline = new TelegramButton { Text = "Từ chối", Height = 28, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f), IsOutlined = true, Location = new Point(64 + btnWidth + 8, 54), Width = btnWidth, Anchor = AnchorStyles.Top | AnchorStyles.Right };

                btnAccept.Click += (s, e) => { RemoveRequest(pnl, isAccepted: true); _incomingCount--; _tabs.Invalidate(); };
                btnDecline.Click += (s, e) => RemoveRequest(pnl, isAccepted: false);

                pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnAccept, btnDecline });

                pnl.Resize += (s, e) =>
                {
                    int bw = (pnl.Width - 88) / 2;
                    btnAccept.Width = bw;
                    btnDecline.Left = 64 + bw + 8;
                    btnDecline.Width = bw;
                };
            }
            else
            {
                var btnCancel = new TelegramButton { Text = "Hủy lời mời", Height = 28, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f), IsOutlined = true, Location = new Point(64, 54), Width = initialWidth - 76, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
                btnCancel.Click += (s, e) => RemoveRequest(pnl, false);

                pnl.Controls.AddRange(new Control[] { avatar, lblName, lblSub, btnCancel });
            }

            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 0, 85, pnl.Width, 85);
            return pnl;
        }

        private static void RemoveRequest(Panel row, bool isAccepted)
        {
            string msg = isAccepted ? "Đã chấp nhận lời mời kết bạn!" : "Đã từ chối lời mời.";
            MessageBox.Show(msg, "SecureChat", MessageBoxButtons.OK, MessageBoxIcon.Information);
            row.Visible = false;
        }

        private void BuildSearchTab()
        {
            var pnlSearch = new Panel { Height = 52, Dock = DockStyle.Top, BackColor = Color.White, Padding = new Padding(12, 8, 12, 6) };

            _tbSearch.Height = 36;
            _tbSearch.Dock = DockStyle.Fill;
            _tbSearch.SetPlaceholder("🔍  Tìm theo tên hoặc @username...");
            _tbSearch.TextChanged += (s, e) => DoSearch(_tbSearch.Text);
            pnlSearch.Controls.Add(_tbSearch);

            _lblSearchHint.Text = "Nhập tên hoặc username để tìm kiếm";
            _lblSearchHint.Font = TG.FontRegular(9.5f);
            _lblSearchHint.ForeColor = TG.TextHint;
            _lblSearchHint.AutoSize = false;
            _lblSearchHint.BackColor = Color.Transparent;
            _lblSearchHint.TextAlign = ContentAlignment.MiddleCenter;
            _lblSearchHint.Dock = DockStyle.Fill;

            _pnlSearchResults.Dock = DockStyle.Fill;
            _pnlSearchResults.AutoScroll = true;
            _pnlSearchResults.BackColor = Color.White;
            _pnlSearchResults.Resize += Pnl_UpdateRowsWidth;
            _pnlSearchResults.Controls.Add(_lblSearchHint);

            _tabSearch.Controls.AddRange(new Control[] { _pnlSearchResults, pnlSearch });
        }

        private void DoSearch(string query)
        {
            _pnlSearchResults.Controls.Clear();

            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                _pnlSearchResults.Controls.Add(_lblSearchHint);
                return;
            }

            var results = new List<ContactItem>
            {
                new() { Type = ContactType.Friend, UserId = "usr-001", DisplayName = "Nguyễn Văn A", Username = "nguyenvana", Status = FriendStatus.Friend, IsOnline = true },
                new() { Type = ContactType.Friend, UserId = "usr-020", DisplayName = "Nguyễn Thị H", Username = "nguyenthih", Status = FriendStatus.None, IsOnline = false },
                new() { Type = ContactType.Friend, UserId = "usr-013", DisplayName = "Nguyễn Minh K", Username = "nguyenquock", Status = FriendStatus.PendingOutgoing, IsOnline = false },
            };

            int y = 0;
            var lblHdr = new Label
            {
                Text = $"Kết quả cho \"{query}\"",
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false,
                Height = 24,
                BackColor = Color.Transparent,
                Padding = new Padding(12, 4, 0, 0),
                Location = new Point(0, y),
                Width = _pnlSearchResults.ClientSize.Width,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };
            _pnlSearchResults.Controls.Add(lblHdr);
            y += 26;

            int initialWidth = _pnlSearchResults.ClientSize.Width > 0 ? _pnlSearchResults.ClientSize.Width : 360;
            foreach (var r in results)
            {
                var row = BuildSearchRow(r, initialWidth);
                row.Location = new Point(0, y);
                _pnlSearchResults.Controls.Add(row);
                y += 60;
            }
        }

        private Panel BuildSearchRow(ContactItem c, int initialWidth)
        {
            var pnl = new Panel { Height = 60, Width = initialWidth, BackColor = Color.White, Cursor = Cursors.Hand };

            var avatar = new AvatarControl { Size = new Size(40, 40), Location = new Point(10, 10), ShowOnline = c.IsOnline };
            avatar.SetName(c.DisplayName);

            var lblName = new Label
            {
                Text = c.DisplayName,
                Font = TG.FontSemiBold(9.5f),
                ForeColor = TG.TextName,
                AutoSize = false,
                Height = 20,
                Location = new Point(58, 10),
                Width = initialWidth - 158,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            var lblUser = new Label
            {
                Text = "@" + c.Username,
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TextBlue,
                AutoSize = false,
                Height = 16,
                Location = new Point(58, 30),
                Width = initialWidth - 158,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.Transparent,
            };

            Control statusCtrl;
            int statusX = initialWidth - 92;

            switch (c.Status)
            {
                case FriendStatus.Friend:
                    statusCtrl = new Label { Text = "✓ Bạn bè", Font = TG.FontRegular(8f), ForeColor = Color.FromArgb(0x2E, 0x7D, 0x32), BackColor = Color.FromArgb(0xE8, 0xF5, 0xE9), AutoSize = false, Height = 24, Width = 80, Location = new Point(statusX, 16), Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter, Padding = new Padding(6, 0, 6, 0), BorderStyle = BorderStyle.FixedSingle };
                    break;
                case FriendStatus.PendingOutgoing:
                    statusCtrl = new Label { Text = "Đã gửi", Font = TG.FontRegular(8f), ForeColor = Color.FromArgb(0xE6, 0x5C, 0x00), BackColor = Color.FromArgb(0xFF, 0xF3, 0xE0), AutoSize = false, Height = 24, Width = 80, Location = new Point(statusX, 16), Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter };
                    break;
                case FriendStatus.PendingIncoming:
                    statusCtrl = new Label { Text = "Chờ duyệt", Font = TG.FontRegular(8f), ForeColor = Color.FromArgb(0x15, 0x65, 0xC0), BackColor = Color.FromArgb(0xE3, 0xF2, 0xFD), AutoSize = false, Height = 24, Width = 80, Location = new Point(statusX, 16), Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter };
                    break;
                case FriendStatus.Blocked:
                    statusCtrl = new Label { Text = "Đã chặn", Font = TG.FontRegular(8f), ForeColor = Color.FromArgb(0x75, 0x75, 0x75), BackColor = Color.FromArgb(0xF5, 0xF5, 0xF5), AutoSize = false, Height = 24, Width = 80, Location = new Point(statusX, 16), Anchor = AnchorStyles.Top | AnchorStyles.Right, TextAlign = ContentAlignment.MiddleCenter };
                    break;
                default:
                    var btn = new TelegramButton { Text = "+ Kết bạn", Height = 28, Width = 80, Radius = TG.RadiusSmall, Font = TG.FontRegular(8.5f), Location = new Point(statusX, 16), Anchor = AnchorStyles.Top | AnchorStyles.Right };
                    btn.Click += (s, e) => { c.Status = FriendStatus.PendingOutgoing; pnl.Invalidate(); };
                    statusCtrl = btn;
                    break;
            }

            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblUser, statusCtrl });
            pnl.Paint += (s, e) => e.Graphics.DrawLine(new Pen(TG.DividerLight), 58, 59, pnl.Width, 59);

            foreach (Control ctrl in pnl.Controls)
            {
                ctrl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
                ctrl.MouseLeave += (s, e) =>
                {
                    if (!pnl.ClientRectangle.Contains(pnl.PointToClient(Control.MousePosition)))
                        pnl.BackColor = Color.White;
                };
            }
            pnl.MouseEnter += (s, e) => pnl.BackColor = TG.SidebarHover;
            pnl.MouseLeave += (s, e) => pnl.BackColor = Color.White;

            return pnl;
        }
    }
}