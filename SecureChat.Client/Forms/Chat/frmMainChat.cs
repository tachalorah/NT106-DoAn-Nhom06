using System;
using System.Collections.Generic;   // List<>, Dictionary<>
using System.Drawing;               // Color, Point, Size, Image, Bitmap
using System.Drawing.Drawing2D;     // SmoothingMode, LinearGradientBrush
using System.Drawing.Imaging;       // PixelFormat, ColorMatrix, ImageAttributes
using System.IO;                    // File, Path, MemoryStream, FileSystemWatcher
using System.Reflection;
using System.Threading.Tasks;       // Task.Delay (dùng cho wallpaper reload)
using System.Windows.Forms;         // Form, Panel, Button, Label, ...

namespace SecureChat.Client
{
    public class frmMainChat : Form
    {
        // ── Các panel layout ──────────────────────────
        private Panel _pnlSidebar = null!; // Panel trái rộng 280px chứa danh sách hội thoại
        private Panel _pnlChat; // Panel phải chiếm phần còn lại, hiển thị tin nhắn
        private Panel _pnlSettingsMenu;  // Menu trượt từ trái ra, đè lên chat area
        private bool _settingsVisible = false; // Trạng thái menu đang hiện hay ẩn, mặc định là ẩn
        private System.Windows.Forms.Timer _slideTimer; // Timer tạo hiệu ứng trượt (animation)
        private int _settingsTargetX;  // Tọa độ X đích khi animate menu

        // ── Sidebar controls ───────────────────────
        private Button _btnHamburger; // nút ☰ mở menu
        private TelegramTextBox _tbSearch; // ô tìm kiếm
        private Panel _pnlConvList; // danh sách cuộc trò chuyện

        // ── Chat area controls ─────────────────────

        private Panel _pnlChatHeader; // thanh header trên cùng khu chat

        //private Panel _pnlMessages; // vùng hiển thị bong bóng tin nhắn
        private ChatPanel _pnlMessages;

        private Panel _pnlInputBar; // thanh nhập tin nhắn bên dưới
        private TelegramTextBox _tbMessage; // TextBox gõ tin nhắn
        private Label _lblChatName, _lblChatStatus; // tên và trạng thái người nhận
        private AvatarControl _chatAvatar; //  avatar tròn người nhận

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
        private string _activeConvId = "1"; // // ID cuộc trò chuyện đang mở, mặc định là mở cuộc trò chuyện này

        private readonly List<(string Id, string Name, string Preview, string Time, int Unread, bool IsGroup)> _convs = new()
        {
            /*
            ("1", "Telegram",    "Quack Cyber added Sim 18a3",     "10:10 PM", 0,  false),
            ("2", "dk test",     "Quack Cyber: Hello hello hello!", "12:51 PM", 100,  false),
            ("3", "Tuấn Thành",  "Sure",                           "10 Mar",   0,  false),
            */

            // Đổi IsGroup từ false → true cho "dk test", và thêm conv nhóm mới
            ("1", "Telegram",    "Quack Cyber added Sim 18a3",     "10:10 PM", 0,   false),
            ("2", "NT106 Nhóm 6","Hang Hieu: tui vừa làm với ô đó","8:30 AM",  100,   true),   // ← nhóm
            ("3", "Tuấn Thành",  "Sure",                           "10 Mar",   0,   false),
         };

        /*
        private readonly List<(string Text, bool Out, string Time)> _msgs = new()
        {
            ("Hello hello hello!",                                                                      false, "12:51 PM"),
            ("Tuấn Thành you've been removed from the group chat",                                      false, "1:01 PM"),
            ("Quack Cyber added Sim 18a3",                                                              false, "10:10 PM"),
            ("Tuấn Thành, you've been wonderful friends for so long. I could never imagine you doing this to me.", true, "10:15 PM"),
            ("Search it",                                                                                true,  "10:16 PM"),
        };
        */

        // Key = convId, Value = danh sách tin nhắn của conversation đó
        private readonly Dictionary<string, List<(string Text, bool Out, string Time, string Sender)>> _allMsgs = new()
        {
            ["1"] = new()
    {
        ("Hello hello hello!",                                                                       false, "12:51 PM", "Hang Hieu"),
        ("Tuấn Thành you've been removed from the group chat",                                       false, "1:01 PM",  "Bot"),
        ("Quack Cyber added Sim 18a3",                                                               false, "10:10 PM", "Bot"),
        ("Tuấn Thành, you've been wonderful friends for so long. I could never imagine you doing this to me.", true, "10:15 PM", ""),
        ("Search it",                                                                                 true,  "10:16 PM", ""),
    },
            ["2"] = new()   // group chat NT106
    {
        ("ê mấy ông ơi nộp bài chưa",          false, "8:28 AM", "Hang Hieu"),
        ("chưa ông ơi",                          false, "8:29 AM", "Tuấn Thành"),
        ("add new contact á",                    false, "8:29 AM", "Hang Hieu"),
        ("tui vừa làm với ô đó",                 false, "8:30 AM", "Hang Hieu"),
        ("nó tự chấp nhận kb luôn mà",           true,  "8:30 AM", ""),
        ("af af",                                false, "8:31 AM", "Hang Hieu"),
        ("oke chút nữa tui push lên",            true,  "8:32 AM", ""),
        ("ừ nhanh lên nha còn test",             false, "8:33 AM", "Tuấn Thành"),
    },
            ["3"] = new()
    {
        ("Hey, lâu rồi không gặp!",              false, "10 Mar", "Tuấn Thành"),
        ("Ừ, dạo này bận lắm",                   true,  "10 Mar", ""),
        ("Sure",                                  false, "10 Mar", "Tuấn Thành"),
    },
        };

        // Tin nhắn của conversation đang active (để SendMessage biết thêm vào đâu)
        private List<(string Text, bool Out, string Time, string Sender)> _currentMsgs =>
            _allMsgs.TryGetValue(_activeConvId, out var list) ? list : new();

        private readonly Dictionary<string, bool> _settingsToggles = new(); // công tắc Night mode

        public frmMainChat()
        {
            InitUI();

            // 1. Các thiết lập DoubleBuffer hiện tại của bạn
            this.DoubleBuffered = true;

            // 1. Kích hoạt cho Panel chính (Nơi chứa hình nền và tin nhắn)
            EnableDoubleBuffering(_pnlMessages);

            // 2. Ép vẽ lại toàn bộ khi cuộn để xóa sạch sọc ngang
            // _pnlMessages.Scroll += (s, e) => _pnlMessages.Invalidate(false); // false = không xóa nền cũ trước khi vẽ lại
            // _pnlMessages.MouseWheel += (s, e) => _pnlMessages.Invalidate(false);

        }

        protected override void OnResize(EventArgs e)
        {
            if (_pnlMessages != null)
            {
                _pnlMessages.SuspendLayout();
                base.OnResize(e);
                _pnlMessages.ResumeLayout(false); // false = không force layout ngay
                BuildMessages();                  // rebuild bubble theo width mới
            }
            else
            {
                base.OnResize(e);
            }
        }

        // Hàm tiện ích dùng Reflection để ép bật DoubleBuffered cho Panel
        private void EnableDoubleBuffering(Control control)
        {
            var type = typeof(Control);
            // Bật bộ đệm kép
            type.InvokeMember("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic, null, control, new object[] { true });

            // Ép Control vẽ tất cả trong một luồng Paint, tránh vẽ nền riêng lẻ gây nhấp nháy
            var method = type.GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.NonPublic);
            if (method != null)
            {
                method.Invoke(control, new object[] { ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true });
            }
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
            // this.MaximizeBox = false; // Vô hiệu hóa nút phóng to
            FormBorderStyle = FormBorderStyle.FixedSingle;

            BackColor = Color.White;
            Font = TG.FontRegular(9.5f);

            BuildSidebar();
            BuildChatArea();
            BuildSettingsMenu();

            // Thứ tự add: sidebar → chat → settings (settings ở trên cùng)
            Controls.Add(_pnlChat);
            Controls.Add(_pnlSidebar);
            Controls.Add(_pnlSettingsMenu);  // add cuối = hiện trên cùng

            Resize += (s, e) =>
            {
                AdjustLayout();
                UpdateCachedBackground(); // Thêm dòng này để ảnh nền co giãn theo Form
            };
            AdjustLayout();

            LoadConversation("1"); // tải cuộc trò chuyện đầu tiên
                                   // inside InitUI(), after LoadConversation("1");
            SetupWallpaperWatcher(); // 1. Bắt đầu theo dõi thư mục ảnh

            // 2. Nạp hình nền lần đầu tiên
            // Nên để sau AdjustLayout để _pnlChat và _pnlMessages đã có kích thước chuẩn
            UpdateCachedBackground();
        }

        private void AdjustLayout()
        {
            // Khai báo các hằng số kích thước
            int sw = 280;                        // Sidebar Width
            int smw = 260;                       // Settings Menu Width

            // Thiết lập vị trí và kích thước cho Sidebar và Khung Chat
            // .SetBounds(x, y, rộng, cao)
            // ClientSize.Height: Chiều cao kéo dài bằng toàn bộ chiều cao vùng làm việc của cửa sổ.
            _pnlSidebar.SetBounds(0, 0, sw, ClientSize.Height);
            _pnlChat.SetBounds(sw, 0, ClientSize.Width - sw, ClientSize.Height);

            // Xử lý hiệu ứng trượt (Slide) của Menu Cài đặt
            // Settings menu: slide overlay từ bên TRÁI
            int visibleX = 0; // Khai báo tọa độ X khi menu cài đặt hiển thị là 0 (nằm sát mép trái màn hình).
            int hiddenX = -smw; // Khai báo tọa độ X khi menu cài đặt ẩn đi là -260 (đẩy toàn bộ menu ra ngoài phạm vi nhìn thấy về phía bên trái).
            // Kiểm tra nếu biến trạng thái _settingsVisible đang là false (người dùng không mở menu cài đặt).
            if (!_settingsVisible)
                _pnlSettingsMenu.SetBounds(hiddenX, 0, smw, ClientSize.Height);
            else
                _pnlSettingsMenu.SetBounds(visibleX, 0, smw, ClientSize.Height);
        }

        // ════════════════════════════════════════════
        //  SIDEBAR: Một thanh tiêu đề trên cùng (Header) chứa nút menu và nút chỉnh sửa, một ô tìm kiếm (Search), và danh sách các cuộc hội thoại (Conversation list).
        // ════════════════════════════════════════════
        private void BuildSidebar()
        {
            _pnlSidebar = new Panel { BackColor = Color.White };

            // ── Header xanh ──────────────────────────
            var pnlHeader = new Panel { Height = 52, BackColor = TG.TitleBarBg, Dock = DockStyle.Top };
            // Gắn chặt panel này vào mép trên cùng của sidebar.

            _btnHamburger = new Button
            {
                Text = "☰",
                FlatStyle = FlatStyle.Flat, // Đặt kiểu hiển thị phẳng, không có hiệu ứng nổi 3D của Windows cổ điển.
                Font = TG.FontRegular(14f),
                ForeColor = Color.White,
                Size = new Size(48, 52),
                Location = new Point(0, 0), // Đặt nút ở góc trên cùng bên trái của header.
                BackColor = Color.Transparent, // Nền trong suốt để lộ màu xanh của header.
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter, // Căn giữa biểu tượng "☰".
            };

            _btnHamburger.FlatAppearance.BorderSize = 0; // Loại bỏ đường viền bao quanh nút khi vẽ giao diện phẳng.
            _btnHamburger.Click += (s, e) => ToggleSettingsMenu();

            var lblTitle = new Label
            {
                Text = "SecureChat",
                Font = TG.FontSemiBold(11f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Location = new Point(50, 0), // Đặt cách mép trái 50px (để không đè lên nút Hamburger rộng 48px).
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

            // pnlHeader.Controls.AddRange(new Control[] { _btnHamburger, lblTitle, btnEdit });
            pnlHeader.Controls.AddRange(new Control[] { _btnHamburger, lblTitle });

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
            // Xóa bỏ toàn bộ các đối tượng đó để chuẩn bị vẽ lại từ đầu(tránh việc danh sách mới bị đè lên danh sách cũ).
            _pnlConvList.Controls.Clear();

            // Nó dùng để xác định vị trí đặt hàng tiếp theo, giúp các hàng không bị đè lên nhau.
            int y = 0;

            // Duyệt qua từng cuộc trò chuyện trong danh sách dữ liệu.
            foreach (var c in _convs)
            {
                var row = BuildConvRow(c.Id, c.Name, c.Preview, c.Time, c.Unread, c.IsGroup);
                EnableDoubleBuffering(row); // <--- QUAN TRỌNG: 
                row.Location = new Point(0, y);
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                row.Width = _pnlConvList.ClientSize.Width;
                _pnlConvList.Controls.Add(row);
                y += 68;
            }
        }

        private Panel BuildConvRow(string id, string name, string preview, string time, int unread, bool isGroup)
        {
            // Tạo một khung chứa có chiều cao cố định là 68px, màu nền trắng và đổi con trỏ chuột thành hình bàn tay (Cursors.Hand) khi rê vào.
            // Thuộc tính Tag được gán bằng id của cuộc trò chuyện để dễ dàng nhận diện.
            var pnl = new Panel { Height = 68, BackColor = Color.White, Tag = id, Cursor = Cursors.Hand };

            // Kiểm tra xem dòng chat này có đang được người dùng chọn (active) hay không bằng cách so sánh Tag với biến toàn cục _activeConvId.
            bool isActive() => (string)pnl.Tag == _activeConvId;

            // Avatar
            var avatar = new AvatarControl { Size = new Size(48, 48), Location = new Point(10, 10) };
            EnableDoubleBuffering(avatar); // <--- QUAN TRỌNG: Control tự vẽ rất cần cái này
            avatar.SetName(name);
            avatar.ShowOnline = false; // nếu có thì có chấm xanh nhỏ ở dưới phải avatar

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
            // Hiển thị nội dung tin nhắn mới nhất.
            // Thuộc tính AutoEllipsis = true rất quan trọng: nó sẽ tự động thêm dấu ba chấm ... nếu tin nhắn quá dài vượt quá chiều rộng của nhãn.
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

            // Hiển thị thời gian nhắn tin ở góc trên bên phải.
            // Sử dụng Anchor để giữ khoảng cách cố định với mép phải khi co dãn giao diện.
            var lblTime = new Label
            {
                Text = time,
                Font = TG.FontRegular(7.5f),
                ForeColor = TG.TextTime,
                AutoSize = true,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            // Nhãn hiển thị số lượng tin nhắn chưa đọc.
            // Nó chỉ hiện lên khi unread > 0.
            var lblUnread = new Label
            {
                // Nếu > 99 thì cho rộng ra một chút (ví dụ 28px) để chứa vừa dấu "+"
                Size = unread > 99 ? new Size(30, 25) : new Size(22, 22),
                AutoSize = false,
                BackColor = Color.Transparent,


                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = TG.FontSemiBold(8f),
                Visible = unread > 0,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            // Đoạn code này tự tay vẽ (custom draw) một hình tròn màu xanh (TG.Blue) đè lên nhãn và viết số lượng tin nhắn vào giữa.
            // Nếu số lượng lớn hơn 99, nó tự động đổi thành "99+".
            lblUnread.Paint += (s, e) =>
            {
                if (!lblUnread.Visible) return;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(TG.Blue);

                // Vẽ hình nền tròn hoặc bầu dục nếu số dài
                e.Graphics.FillEllipse(brush, 0, 0, lblUnread.Width - 1, lblUnread.Height - 1);

                // LOGIC QUAN TRỌNG: Kiểm tra lại biến unread ở đây
                string txt = unread > 99 ? "99+" : unread.ToString();

                using var fore = new SolidBrush(Color.White);
                var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(txt, lblUnread.Font, fore, lblUnread.ClientRectangle, sf);
            };


            // Add controls
            pnl.Controls.AddRange(new Control[] { avatar, lblName, lblPreview, lblTime, lblUnread });

            // Click behavior
            // Định nghĩa hành động khi người dùng bấm vào dòng chat: Cập nhật ID đang kích hoạt, vẽ lại danh sách và tải nội dung tin nhắn của cuộc trò chuyện đó.
            Action doClick = () => { _activeConvId = id; BuildConvList(); LoadConversation(id); };
            pnl.Click += (s, e) => doClick();
            avatar.Click += (s, e) => doClick();

            // Khi rê chuột vào/ra khỏi tấm nền, nếu dòng chat này không ở trạng thái được chọn (!isActive()), nền sẽ đổi sang màu hover (TG.SidebarHover) và ngược lại.
            pnl.MouseEnter += (s, e) => { if (!isActive()) pnl.BackColor = TG.SidebarHover; };
            pnl.MouseLeave += (s, e) => { if (!isActive()) pnl.BackColor = Color.White; };

            // Resize child layout when row width changes
            pnl.Resize += (s, e) =>
            {
                lblName.Width = Math.Max(0, pnl.Width - 66 - 70); // 70px là trừ hao cho phần hiển thị thời gian

                // NẾU CÓ TIN NHẮN CHƯA ĐỌC: Trừ đi độ rộng của Badge (khoảng 35-40px tính cả lề)
                // NẾU KHÔNG CÓ: Chỉ trừ lề phải 12px
                int previewRightMargin = (unread > 0) ? 40 : 12;
                lblPreview.Width = Math.Max(0, pnl.Width - 66 - previewRightMargin);

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

                lblUnread.BringToFront(); // đảm bảo 99+ không bị đè
            };

            // Truyền sự kiện (Event Propagation)/hover for child controls
            // Trong WinForms, khi bạn click vào một Label nằm bên trong Panel, sự kiện Click của Panel sẽ không tự kích hoạt.
            // Vòng lặp này duyệt qua tất cả các control con để gán đè sự kiện Click và Hover. Mục đích là giúp người dùng click hay rê chuột vào bất cứ điểm nào trên dòng chat(dù là vào chữ hay vào khoảng trống) thì cả dòng chat vẫn phản hồi đồng bộ.
            foreach (Control c in pnl.Controls)
            {
                if (c == avatar) continue; // avatar wired already
                c.Click += (s, e) => doClick();
                c.MouseEnter += (s, e) => { if (!isActive()) pnl.BackColor = TG.SidebarHover; };
                c.MouseLeave += (s, e) => { if (!isActive()) pnl.BackColor = Color.White; };
            }

            pnl.Width = _pnlConvList?.ClientSize.Width ?? pnl.Width;
            // Ép Panel tính toán lại vị trí các control dựa trên các logic resize vừa viết ở trên trước khi trả về kết quả.
            pnl.PerformLayout();

            return pnl;
        }

        // ════════════════════════════════════════════
        //  CHAT AREA
        // ════════════════════════════════════════════

        // Mới thêm
        private Button _btnToggleSidebar;
        private Panel _pnlRightSidebar;
        private bool _isSidebarOpen = false; // Biến phụ để theo dõi trạng thái

        private void BuildChatArea()
        {
            // chứa toàn bộ Header, danh sách tin nhắn và thanh nhập liệu.
            _pnlChat = new Panel { BackColor = Color.White };

            // ── Chat Header ───────────────────────────
            _pnlChatHeader = new Panel { Height = 52, BackColor = Color.White, Dock = DockStyle.Top };
            // vẽ một đường kẻ ngang màu xám/nhạt (TG.Divider) ở dưới cùng để ngăn cách header với vùng tin nhắn.
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
                Height = 20,
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

            // Khởi tạo nút và gán vào biến đã khai báo ở trên
            _btnToggleSidebar = MakeChatHeaderBtn("⏪");

            // _pnlChatHeader.Controls.AddRange(new Control[] { _chatAvatar, _lblChatName, _lblChatStatus, btnSearch, btnVideo, btnMore });
            // Thêm vào AddRange (nhớ thêm _btnToggleSidebar)
            _pnlChatHeader.Controls.AddRange(new Control[] { _chatAvatar, _lblChatName, _lblChatStatus, btnSearch, btnVideo, _btnToggleSidebar, btnMore });

            _btnToggleSidebar.Click += (s, e) =>
            {
                /*
                if (_pnlRightSidebar.Visible)
                {
                    _pnlRightSidebar.Visible = false;
                    _btnToggleSidebar.Text = "⏪"; // Đóng rồi thì hiện mũi tên hướng trái để mở lại
                }
                else
                {
                    _pnlRightSidebar.Visible = true;
                    _btnToggleSidebar.Text = "⏩"; // Mở rồi thì hiện mũi tên hướng phải để đóng
                }
                */
            };

            /* _pnlChatHeader.Resize += (s, e) =>
            {

                _lblChatName.Width = _pnlChatHeader.Width - 56 - 130;
                _lblChatStatus.Width = _pnlChatHeader.Width - 56 - 130;
                btnMore.Location = new Point(_pnlChatHeader.Width - 42, 8);
                btnVideo.Location = new Point(_pnlChatHeader.Width - 84, 8);
                btnSearch.Location = new Point(_pnlChatHeader.Width - 126, 8);
            };*/
            _pnlChatHeader.Resize += (s, e) =>
            {
                // Khoảng cách dành cho 4 nút (42px mỗi nút) = 168px
                _lblChatName.Width = _pnlChatHeader.Width - 56 - 170;
                _lblChatStatus.Width = _pnlChatHeader.Width - 56 - 170;

                int w = _pnlChatHeader.Width;
                btnMore.Location = new Point(w - 42, 8);             // Cách mép 42
                _btnToggleSidebar.Location = new Point(w - 84, 8);   // Cách mép 84 (Nút của bạn ở đây)
                btnVideo.Location = new Point(w - 126, 8);           // Cách mép 126
                btnSearch.Location = new Point(w - 168, 8);          // Cách mép 168
            };


            // ── Messages area  - Vùng hiển thị tin nhắn ─────────────────────────
            _pnlMessages = new ChatPanel
            {
                Dock = DockStyle.Fill, // chiếm trọn phần diện tích còn lại
                AutoScroll = true,
                Padding = new Padding(12, 8, 12, 8),
                BackColor = Color.FromArgb(0xDB, 0xE8, 0xD5), // Thêm dòng này
            };


            typeof(Panel).InvokeMember("DoubleBuffered",
            System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, _pnlMessages, new object[] { true });
            // _pnlMessages.Paint += PaintChatBackground;
            // Click vào vùng chat → đóng settings menu
            _pnlMessages.Click += (s, e) => { if (_settingsVisible) HideSettingsMenu(); };

            // ── Input bar ─────────────────────────────
            BuildInputBar();

            _pnlChat.Controls.Add(_pnlMessages);
            _pnlChat.Controls.Add(_pnlInputBar);
            _pnlChat.Controls.Add(_pnlChatHeader);

            // _pnlMessages.Resize += (s, e) => _pnlMessages.Invalidate(); // bỏ vì đã có PaintChatBackground tự xử lý.
            _pnlMessages.Resize += (s, e) => UpdateCachedBackground();
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

            _currentMsgs.Add((pollText, true, DateTime.Now.ToString("h:mm tt"), ""));
            BuildMessages();
        }

        private void ClearHistory()
        {
            using var dlg = new SecureChat.Client.Forms.Chat.frmClearHistory(_lblChatName.Text);
            if (dlg.ShowDialog(this) != DialogResult.OK || !dlg.DeleteConfirmed)
                return;

            _currentMsgs.Clear();
            BuildMessages();
        }

        private void DeleteAndLeave()
        {
            var members = new[] { "Duck Cyber", "Sim 18a3", "Tuấn Thành", "Hoang Hieu" };
            using var dlg = new SecureChat.Client.Forms.Chat.frmLeaveGroup(_lblChatName.Text, "Duck Cyber", members);
            if (dlg.ShowDialog(this) != DialogResult.OK || !dlg.LeaveConfirmed)
                return;

            _currentMsgs.Clear();
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

        // Tự động theo dõi một thư mục ảnh.
        // Khi thêm, xóa hoặc sửa ảnh trong đó, chương trình sẽ biết để cập nhật hình nền ngay lập tức.
        private void SetupWallpaperWatcher()
        {
            try
            {
                // Tạo đường dẫn đến thư mục chứa ảnh. Nó kết hợp: Nơi phần mềm đang chạy (StartupPath) + thư mục Resources + thư mục Images.
                string dir = Path.Combine(Application.StartupPath, "Resources", "Images");

                // Kiểm tra xem thư mục đó có tồn tại không. Nếu không thấy thư mục, thoát luôn (không theo dõi gì cả).
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

        // Xử lý khi có sự kiện thay đổi file xảy ra.
        // Mục tiêu của nó là lọc đúng file ảnh cần thiết và cập nhật lại giao diện một cách an toàn.
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

        // vai trò "dọn dẹp" dữ liệu cũ để chuẩn bị cho việc hiển thị hình ảnh mới
        private void ReloadWallpaper()
        {
            _wallpaperLoaded = false;
            _wallpaper?.Dispose();
            _wallpaper = null;

            // Cập nhật lại bộ đệm ảnh thay vì chỉ Invalidate
            UpdateCachedBackground();

            // _pnlMessages?.Invalidate();
        }

        // Tự tạo ra một hình nền mặc định (màu chuyển sắc - gradient) trong trường hợp chương trình không tìm thấy bất kỳ file ảnh nào trong thư mục.
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

        /*
        private void PaintChatBackground(object sender, PaintEventArgs e)
        {
            var panel = sender as Panel;

            // THÊM DÒNG NÀY — xóa vùng clip trước khi vẽ
            e.Graphics.Clear(Color.FromArgb(0xDB, 0xE8, 0xD5)); // màu fallback gradient

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
        */

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

            var userAvatar = new AvatarControl { Size = new Size(56, 56), Location = new Point(14, 52) };
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

        // --- Modified methods and new helpers: replace the existing BuildMessages and BuildBubble implementations
        //     and add the helper methods shown below into the frmMainChat class.

        private void BuildMessages()
        {
            // 1. CHÈN VÀO ĐÂY: Tạm dừng vẽ để tránh "nháy" khi xóa/thêm hàng loạt control
            _pnlMessages.SuspendLayout();

            // BẮT BUỘC: Reset thanh cuộn về top trước khi thao tác
            _pnlMessages.AutoScrollPosition = new Point(0, 0);

            _pnlMessages.Controls.Clear();

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
                Padding = new Padding(8, 2, 8, 2),
            };

            sep.Location = new Point(0, y);
            sep.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            sep.Width = Math.Max(0, _pnlMessages.ClientSize.Width - _pnlMessages.Padding.Horizontal);

            _pnlMessages.Controls.Add(sep);
            y += sep.Height + 4;

            var bubbles = new List<Control>();

            // Iterate with index to support actions that modify a specific message
            for (int i = 0; i < _currentMsgs.Count; i++)
            {
                var msg = _currentMsgs[i];
                bool isGroup = _convs.Find(c => c.Id == _activeConvId).IsGroup;
                var bubble = BuildBubble(msg.Text, msg.Out, msg.Time, msg.Sender, isGroup, i);

                // Make bubble respect panel resizing
                bubble.Location = new Point(0, y);
                bubble.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                bubble.Width = Math.Max(0, _pnlMessages.ClientSize.Width - _pnlMessages.Padding.Horizontal);
                _pnlMessages.Controls.Add(bubble);
                bubbles.Add(bubble);
                y += bubble.Height + 4;
            }

            _pnlMessages.ResumeLayout(true);

            if (bubbles.Count > 0)
                _pnlMessages.ScrollControlIntoView(bubbles[^1]);
        }

        // Updated BuildBubble signature: added messageIndex to identify which message the context menu acts on
        private Panel BuildBubble(string text, bool isOut, string time,
                           string sender = "", bool isGroup = false, int messageIndex = -1)
        {
            var pnl = new Panel { BackColor = Color.Transparent };

            int pad = 12; // Padding (khoảng cách lề) bên trong bubble.

            const int avatarAreaW = 44;
            int leftOffset = (!isOut && isGroup) ? avatarAreaW : 10;

            int maxW = 360; // // Mặc định rộng 360px
            if (_pnlMessages != null && _pnlMessages.ClientSize.Width > 0)
                maxW = Math.Max(220, (int)(_pnlMessages.ClientSize.Width * 0.66f) - _pnlMessages.Padding.Horizontal);

            SizeF sz;
            using (var g = _pnlMessages.CreateGraphics())
            {
                sz = g.MeasureString(text, TG.FontRegular(9.5f), maxW - pad * 2);
            }

            int statusHeight = 16;
            int senderHeight = (!isOut && isGroup && !string.IsNullOrEmpty(sender)) ? 19 : 0;
            int bw = Math.Min(maxW, Math.Max((int)sz.Width + pad * 2 + 10, 100));
            int bh = (int)sz.Height + pad * 2 + statusHeight + senderHeight;
            pnl.Height = bh + 16;

            pnl.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

                int x = isOut ? pnl.ClientSize.Width - bw - 10 : leftOffset;
                int y = 4;

                Color bg = isOut ? TG.MsgOutBg : TG.MsgInBg;

                // Shadow
                using var shadowBrush = new SolidBrush(Color.FromArgb(20, 0, 0, 0));
                var shadowRect = new Rectangle(x + 1, y + 2, bw, bh);
                using var shadowPath = RoundedPanel.GetRoundedPath(shadowRect, TG.RadiusBubble);
                e.Graphics.FillPath(shadowBrush, shadowPath);

                // Bubble
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

                // Sender name (chỉ group, tin nhận)
                bool showSender = !isOut && isGroup && !string.IsNullOrEmpty(sender);
                if (showSender)
                {
                    using var senderBrush = new SolidBrush(SenderNameColor(sender));
                    e.Graphics.DrawString(sender, TG.FontSemiBold(8f), senderBrush, x + pad, y + pad);
                }

                // Text tin nhắn - bắt đầu dưới tên người gửi
                float textY = y + pad + (showSender ? 19 : 0);
                var textRect = new RectangleF(x + pad, textY, bw - pad * 2, sz.Height);
                e.Graphics.DrawString(text, TG.FontRegular(9.5f), new SolidBrush(TG.TextPrimary), textRect);

                // Time
                var timeSz = e.Graphics.MeasureString(time, TG.FontRegular(7.5f));
                float tx = x + bw - timeSz.Width - pad - (isOut ? 26 : 0);
                float ty = y + bh - timeSz.Height - 6;
                e.Graphics.DrawString(time, TG.FontRegular(7.5f), new SolidBrush(TG.TextTime), tx, ty);

                if (isOut)
                {
                    float tickX = x + bw - pad - 22;
                    using var tickFont = new Font("Segoe UI Symbol", 8f, FontStyle.Bold);
                    e.Graphics.DrawString("✓✓", tickFont, new SolidBrush(TG.Blue), tickX, ty - 1);
                }
            };

            // Context menu: align with Telegram-like items and wire proper logic
            var ctx = new ContextMenuStrip
            {
                ShowImageMargin = false,
                Font = new Font("Segoe UI", 10f),
                Renderer = new ToolStripProfessionalRenderer(new ChatMenuColorTable())
            };

            // Common actions
            var miReply = new ToolStripMenuItem("↩  Reply");
            miReply.Click += (_, __) => OnReplyMessage(messageIndex);
            ctx.Items.Add(miReply);

            var miForward = new ToolStripMenuItem("↗  Forward");
            miForward.Click += (_, __) => OnForwardMessage(messageIndex);
            ctx.Items.Add(miForward);

            var miCopy = new ToolStripMenuItem("📋  Copy");
            miCopy.Click += (_, __) => OnCopyMessage(messageIndex);
            ctx.Items.Add(miCopy);

            // Edit (only for outgoing messages)
            if (isOut)
            {
                var miEdit = new ToolStripMenuItem("✎  Edit");
                miEdit.Click += (_, __) => OnEditMessage(messageIndex);
                ctx.Items.Add(miEdit);
            }

            var miPin = new ToolStripMenuItem("📌  Pin");
            miPin.Click += (_, __) => OnPinMessage(messageIndex);
            ctx.Items.Add(miPin);

            // Delete submenu: Delete for me (always) and Delete for everyone (only if outgoing)
            ctx.Items.Add(new ToolStripSeparator());
            var miDelete = new ToolStripMenuItem("🗑  Delete");
            var miDeleteForMe = new ToolStripMenuItem("Delete for me");
            miDeleteForMe.Click += (_, __) => OnDeleteMessage(messageIndex, deleteForEveryone: false);
            miDelete.DropDownItems.Add(miDeleteForMe);

            if (isOut)
            {
                var miDeleteForEveryone = new ToolStripMenuItem("Delete for everyone");
                miDeleteForEveryone.ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A);
                miDeleteForEveryone.Click += (_, __) => OnDeleteMessage(messageIndex, deleteForEveryone: true);
                miDelete.DropDownItems.Add(miDeleteForEveryone);
            }
            else
            {
                // For incoming messages Telegram sometimes provides "Report" or "Report spam" — skip for now.
            }

            ctx.Items.Add(miDelete);

            pnl.ContextMenuStrip = ctx;

            // Avatar for group messages (unchanged)
            if (!isOut && isGroup)
            {
                var av = new AvatarControl
                {
                    Size = new Size(32, 32),
                    Location = new Point(6, 4)
                };
                av.SetName(sender);
                pnl.Controls.Add(av);
            }

            return pnl;
        }

        // --- Helper action implementations ---
        private void OnReplyMessage(int index)
        {
            if (index < 0 || index >= _currentMsgs.Count) return;
            var msg = _currentMsgs[index];
            string sender = string.IsNullOrEmpty(msg.Sender) ? "You" : msg.Sender;
            // Insert a simple quoted reply into the message box and focus it
            _tbMessage.Text = $"> {sender}: {msg.Text}\r\n";
            _tbMessage.Focus();
            // FIX: Use the underlying TextBox's SelectionStart property
            if (_tbMessage.Controls.Count > 0 && _tbMessage.Controls[0] is TextBox tb)
                tb.SelectionStart = tb.Text.Length;
        }

        private void OnForwardMessage(int index)
        {
            if (index < 0 || index >= _currentMsgs.Count) return;
            var msg = _currentMsgs[index];
            // For now, copy to clipboard and inform user — a real implementation should open contact chooser
            try
            {
                Clipboard.SetText(msg.Text ?? string.Empty);
                MessageBox.Show(this, "Message copied to clipboard. Use Paste to forward or implement Forward UI.", "Forward", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch
            {
                MessageBox.Show(this, "Unable to copy message to clipboard.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnCopyMessage(int index)
        {
            if (index < 0 || index >= _currentMsgs.Count) return;
            var msg = _currentMsgs[index];
            try
            {
                Clipboard.SetText(msg.Text ?? string.Empty);
            }
            catch
            {
                MessageBox.Show(this, "Unable to copy message to clipboard.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnEditMessage(int index)
        {
            if (index < 0 || index >= _currentMsgs.Count) return;
            var msg = _currentMsgs[index];
            if (!msg.Out) return; // safety

            if (TryShowEditDialog(msg.Text, out var newText))
            {
                var t = _currentMsgs[index];
                _currentMsgs[index] = (newText, t.Out, t.Time, t.Sender);
                BuildMessages();
            }
        }

        private void OnPinMessage(int index)
        {
            if (index < 0 || index >= _currentMsgs.Count) return;
            // No pin store currently — just notify. In a real app, persist pin to conversation state.
            MessageBox.Show(this, "Message pinned (UI only). Implement persistent pinning in conversation state.", "Pin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void OnDeleteMessage(int index, bool deleteForEveryone)
        {
            if (index < 0 || index >= _currentMsgs.Count) return;

            var msg = _currentMsgs[index];
            string prompt = deleteForEveryone
                ? "Are you sure you want to delete this message for everyone?"
                : "Are you sure you want to delete this message for you?";
            var res = MessageBox.Show(this, prompt, "Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (res != DialogResult.Yes) return;

            // Local removal
            _currentMsgs.RemoveAt(index);

            // If deleteForEveryone, in a real app we would notify server/hub to remove it for all clients.
            if (deleteForEveryone)
            {
                // TODO: SignalR call: await _chatService.DeleteMessageForEveryone(conversationId, messageId);
                MessageBox.Show(this, "Requested deletion for everyone (simulated). Implement server-side delete via API/SignalR.", "Delete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            BuildMessages();
        }

        // Simple modal edit dialog (returns true if user clicked OK)
        private bool TryShowEditDialog(string currentText, out string newText)
        {
            newText = currentText;
            using var dlg = new Form()
            {
                Text = "Edit message",
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false,
                Size = new Size(480, 200)
            };

            var tb = new TextBox()
            {
                Multiline = true,
                Text = currentText,
                Dock = DockStyle.Top,
                Height = 100,
                Font = TG.FontRegular(9.5f)
            };

            var btnOk = new Button() { Text = "OK", DialogResult = DialogResult.OK, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            var btnCancel = new Button() { Text = "Cancel", DialogResult = DialogResult.Cancel, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };

            btnOk.Location = new Point(dlg.ClientSize.Width - 180, tb.Bottom + 20);
            btnCancel.Location = new Point(dlg.ClientSize.Width - 90, tb.Bottom + 20);

            dlg.Controls.Add(tb);
            dlg.Controls.Add(btnOk);
            dlg.Controls.Add(btnCancel);

            dlg.AcceptButton = btnOk;
            dlg.CancelButton = btnCancel;

            var dr = dlg.ShowDialog(this);
            if (dr == DialogResult.OK)
            {
                newText = tb.Text;
                return true;
            }
            return false;
        }

        // ════════════════════════════════════════════
        //  SEND MESSAGE
        // ════════════════════════════════════════════
        private void SendMessage()
        {
            if (string.IsNullOrWhiteSpace(_tbMessage.Text)) return;
            string text = _tbMessage.Text.Trim();
            _tbMessage.Text = "";

            // _msgs.Add((text, true, DateTime.Now.ToString("h:mm tt")));
            _currentMsgs.Add((text, true, DateTime.Now.ToString("h:mm tt"), ""));

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

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                // Bật WS_EX_COMPOSITED (0x02000000)
                // Ép toàn bộ control trên form phải vẽ bằng Double Buffer
                cp.ExStyle |= 0x02000000;
                return cp;
            }
        }
        private static readonly Color[] _senderPalette =
{
    Color.FromArgb(0xE5, 0x55, 0x45),
    Color.FromArgb(0xF4, 0x8C, 0x4A),
    Color.FromArgb(0x70, 0xBB, 0x4D),
    Color.FromArgb(0x20, 0x9E, 0xD9),
    Color.FromArgb(0x9B, 0x59, 0xB6),
    Color.FromArgb(0xE9, 0x67, 0xA8),
    Color.FromArgb(0x17, 0xA5, 0x89),
};

        private static Color SenderNameColor(string name)
        {
            if (string.IsNullOrEmpty(name)) return _senderPalette[0];
            int hash = 0;
            foreach (char c in name) hash = hash * 31 + c;
            return _senderPalette[Math.Abs(hash) % _senderPalette.Length];
        }

        internal class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                SetStyle(ControlStyles.OptimizedDoubleBuffer |
                         ControlStyles.AllPaintingInWmPaint, true); // Bỏ SupportsTransparentBackColor
                UpdateStyles();
            }
        }

        private Bitmap _cachedBackground;

        private void UpdateCachedBackground()
        {
            // Kiểm tra nếu Panel chưa có kích thước hoặc bị ẩn thì không làm gì cả
            if (_pnlMessages.Width <= 0 || _pnlMessages.Height <= 0) return;

            // 1. Tạo một Bitmap mới khớp hoàn toàn với kích thước hiện tại của Panel
            Bitmap newBmp = new Bitmap(_pnlMessages.Width, _pnlMessages.Height);

            using (Graphics g = Graphics.FromImage(newBmp))
            {
                // 2. Tô nền bằng màu mặc định trước (phòng trường hợp ảnh không phủ hết hoặc lỗi)
                g.Clear(Color.FromArgb(0xDB, 0xE8, 0xD5));

                // 3. Lấy ảnh wallpaper (sử dụng hàm LoadWallpaper bạn đã viết)
                var img = LoadWallpaper();
                if (img != null)
                {
                    // Sử dụng lại logic "Center Crop" chuyên nghiệp của bạn
                    float scaleX = (float)_pnlMessages.Width / img.Width;
                    float scaleY = (float)_pnlMessages.Height / img.Height;
                    float scale = Math.Max(scaleX, scaleY);

                    int drawW = (int)(img.Width * scale);
                    int drawH = (int)(img.Height * scale);

                    int offsetX = (_pnlMessages.Width - drawW) / 2;
                    int offsetY = (_pnlMessages.Height - drawH) / 2;

                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;

                    g.DrawImage(img, offsetX, offsetY, drawW, drawH);
                }
            }

            // 4. Cập nhật BackgroundImage cho Panel
            // Giải phóng ảnh cũ để tránh tràn bộ nhớ (Memory Leak)
            var oldBmp = _pnlMessages.BackgroundImage;
            // Xóa 2 dòng code này đi:
            // _pnlMessages.BackgroundImage = newBmp;
            // _pnlMessages.BackgroundImageLayout = ImageLayout.None;

            // Thay bằng 2 dòng này:
            _pnlMessages.CachedWallpaper = newBmp;
            _pnlMessages.Invalidate(); // Ra lệnh cho Panel vẽ lại nền lập tức

            if (oldBmp != null) oldBmp.Dispose();
        }

    }

    public class ChatPanel : Panel
    {
        public Bitmap CachedWallpaper { get; set; }

        public ChatPanel()
        {
            this.SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.UserPaint |
                ControlStyles.ResizeRedraw, true);
            this.UpdateStyles();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (CachedWallpaper != null)
            {
                var state = e.Graphics.Save();
                e.Graphics.ResetTransform();
                e.Graphics.DrawImage(CachedWallpaper, 0, 0);
                e.Graphics.Restore(state);
            }
            else
            {
                var state = e.Graphics.Save();
                e.Graphics.ResetTransform();
                e.Graphics.Clear(this.BackColor);
                e.Graphics.Restore(state);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_ERASEBKGND = 0x0014;
            const int WM_VSCROLL = 0x0115;
            const int WM_HSCROLL = 0x0114;
            const int WM_MOUSEWHEEL = 0x020A;

            if (m.Msg == WM_ERASEBKGND)
            {
                m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);

            if (m.Msg == WM_VSCROLL || m.Msg == WM_HSCROLL || m.Msg == WM_MOUSEWHEEL)
            {
                this.Invalidate();
            }
        }
    }
}