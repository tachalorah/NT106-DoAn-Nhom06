using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SecureChat.Client
{
    /// Màn hình Đăng nhập, Đăng ký, Quên mật khẩu kiểu 


    public class frmLoginRegister : Form
    {
        // ── Controls: thành phần giao diện ──────────────────────────────

        private Panel _pnlLogo; // Vùng header xanh chứa icon + tên app + slogan
        private Panel _pnlCard; // Vùng trắng chứa các thông tin

        private Label _lblAppName, _lblTagline; // Tên app + slogan
        private Label _lblPhone, _lblPassword; // Nhãn chữ "Số điện thoại", "Mật khẩu"
        private Label _lblCountryCode; // Hiển thị "🇻🇳 +84"
        private LinkLabel _lnkForgot; // Link "Quên mật khẩu?"

        private TelegramTextBox _tbPhone, _tbPassword; // Ô nhập 
        private TelegramButton _btnLogin, _btnRegister; // Nút bấm "Đăng nhập" + "Tạo tài khoản mới" hoặc "Đã có tài khoản" + "Tạo tài khoảng"


        private Label _lblError; // Dòng thông báo lỗi cho đăng nhập và đăng ký


        // ── State: trạng thái đăng nhập, đăng ký ──────────────────────────────────

        private bool _isRegisterMode = false; // false = đang ở màn Đăng nhập, true  = đang ở màn Đăng ký

        private TelegramTextBox _tbDisplayName, _tbEmail, _tbConfirmPass;
        private Label _lblDisplayName, _lblEmail, _lblConfirmPass;

        // private = chỉ dùng trong class này, bên ngoài không truy cập được.
        // Tiền tố  _ là quy ước đặt tên cho field (biến của class).
        public frmLoginRegister()
        {
            InitializeComponent(); // Gọi hàm khởi tạo giao diện
        }

        private void InitializeComponent()
        {
            // Form settings
            Text = "SecureChat"; // Tiêu đề cửa sổ
            Size = new Size(420, 620); // Kích thước lúc mới mở (rộng, dài)
            MinimumSize = new Size(380, 560);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = TG.Blue; // Màu nền chủ đạo là màu xanh (lấy từ class TG)
            // FormBorderStyle = FormBorderStyle.FixedSingle; // Viền cố định, không kéo được
            FormBorderStyle = FormBorderStyle.Sizable; // Cho phép kéo thay đổi kích thước
            MaximizeBox = false; // chặn nút phóng to
            Font = TG.FontRegular(9.5f); // Font mặc định toàn form

            // ── Logo Panel (header xanh) ──────────────
            _pnlLogo = new Panel
            {
                BackColor = TG.Blue,
                Height = 160, // Cao 160 pixel
                Dock = DockStyle.Top, // Ghim lên đầu form, tự co theo chiều ngang 
            };

            // Plane ico
            var lblIcon = new Label
            {
                Text = "✈",
                Font = new Font("Segoe UI Emoji", 42f),
                ForeColor = Color.White,
                AutoSize = false, // Không tự co kích thước
                Size = new Size(80, 80),
                TextAlign = ContentAlignment.MiddleCenter, // Chữ căn giữa trong ô
                BackColor = Color.Transparent, // Nền trong suốt
            };

            // Thêm icon vào header xanh
            _pnlLogo.Controls.Add(lblIcon);

            // Căn giữa icon mỗi khi panel thay đổi kích thước
            _pnlLogo.Resize += (s, e) =>
            {
                // X = (chiều rộng panel - 80) / 2  → căn giữa ngang
                // Y = 20 → cách đỉnh 20px
                lblIcon.Location = new Point((_pnlLogo.Width - 80) / 2, 20);
            };

            _lblAppName = new Label
            {
                Text = "SecureChat", // Tên Ứng dụng dưới icon 
                Font = TG.FontTitle(16f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                Height = 30, // Label cao 30px
                TextAlign = ContentAlignment.MiddleCenter,
            };

            _lblTagline = new Label
            {
                Text = "Nhắn tin an toàn và mã hóa đầu cuối",
                Font = TG.FontRegular(9f),
                ForeColor = Color.FromArgb(200, 235, 255),
                BackColor = Color.Transparent,
                AutoSize = false,
                Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
            };

            _pnlLogo.Controls.AddRange(new Control[] { _lblAppName, _lblTagline }); // thay vì add từng cái, quăng hết cho mảng
            // Đặt vị trí 2 label mỗi khi panel resize:
            _pnlLogo.Resize += (s, e) =>
            {
                _lblAppName.SetBounds(0, 108, _pnlLogo.Width, 30); // y=108 (dưới icon)
                _lblTagline.SetBounds(0, 136, _pnlLogo.Width, 20); // y=136 (dưới tên)
            };
            // SetBounds(x, y, width, height) = đặt vị trí và kích thước cùng lúc.

            // ── Card trắng: dưới header xanh ──────────────────────────
            _pnlCard = new RoundedPanel // Panel tùy chỉnh có thể bo góc
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill, // Lấp đầy phần còn lại sau _pnlLogo: header xanh
                Padding = new Padding(28, 24, 28, 20), // Khoảng cách trong: trái, trên, phải, dưới
            };
            ((RoundedPanel)_pnlCard).Radius = 0; // Không bo góc (bằng 0)

            // Labels
            _lblPhone = MakeFieldLabel("Số điện thoại");
            _lblPassword = MakeFieldLabel("Mật khẩu");
            _lblDisplayName = MakeFieldLabel("Họ và tên");
            _lblEmail = MakeFieldLabel("Email");
            _lblConfirmPass = MakeFieldLabel("Xác nhận mật khẩu");

            // Tạo ô nhập sđt + mã nước
            var pnlPhone = new Panel { Height = 44, BackColor = Color.Transparent }; // tạo vùng trước
            var borderPhone = MakeInputBorderPanel(); // vẽ đường viền bo góc
            // tách bên trái 70 pixel cho mã vùng 
            _lblCountryCode = new Label
            {
                Text = "🇻🇳 +84",
                Font = TG.FontRegular(10f),
                ForeColor = TG.Blue,
                AutoSize = false,
                Width = 70,
                Dock = DockStyle.Left, // Ghim sang trái trong panel
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
                Cursor = Cursors.Hand, // Chuột thành hình bàn tay khi hover
            };
            // còn lại bên trái là nhập sđt
            _tbPhone = new TelegramTextBox
            {
                Height = 44,
                Dock = DockStyle.Fill,

            };
            _tbPhone.SetPlaceholder("9x xxx xxxx"); // Tạo chữ mờ 
            borderPhone.Controls.Add(_tbPhone);       // add textbox trước
            borderPhone.Controls.Add(_lblCountryCode); // add label sau
            pnlPhone.Controls.Add(borderPhone);
            borderPhone.Dock = DockStyle.Fill;


            _tbPassword = new TelegramTextBox { Height = 44 };
            _tbPassword.SetPlaceholder("Nhập mật khẩu...");
            _tbPassword.PasswordChar = '●';

            _tbDisplayName = new TelegramTextBox { Height = 44 };
            _tbDisplayName.SetPlaceholder("Nguyễn Văn A");

            _tbEmail = new TelegramTextBox { Height = 44 };
            _tbEmail.SetPlaceholder("email@example.com");

            _tbConfirmPass = new TelegramTextBox { Height = 44 };
            _tbConfirmPass.SetPlaceholder("Nhập lại mật khẩu...");
            _tbConfirmPass.PasswordChar = '●';

            // Error label
            _lblError = new Label
            {
                AutoSize = false,
                Height = 20,
                ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A),
                Font = TG.FontRegular(8.5f),
                BackColor = Color.Transparent,
                Visible = false,
            };

            // Buttons
            _btnLogin = new TelegramButton
            {
                Text = "ĐĂNG NHẬP",
                Height = 46,
                Font = TG.FontSemiBold(10.5f),
                Radius = TG.RadiusSmall, // Độ bo góc nhỏ
            };
            _btnLogin.Click += BtnLogin_Click; // Gắn sự kiện click

            _btnRegister = new TelegramButton
            {
                Text = "TẠO TÀI KHOẢN",
                Height = 46,
                Font = TG.FontSemiBold(10.5f),
                Radius = TG.RadiusSmall,
                IsOutlined = true, // Kiểu viền rỗng (outline)
                NormalColor = TG.Blue,
            };
            _btnRegister.Click += BtnRegister_Click;

            _lnkForgot = new LinkLabel
            {
                Text = "Quên mật khẩu?",
                LinkColor = TG.Blue,
                ActiveLinkColor = TG.BlueActive,
                Font = TG.FontRegular(9f),
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            _lnkForgot.LinkClicked += (s, e) =>
            {
                new frmForgot().ShowDialog(this); // Mở popup quên mật khẩu
            };

            // Thêm tất cả control vào _pnlCard:
            _pnlCard.Controls.AddRange(new Control[] {
                _lblDisplayName, _tbDisplayName,
                _lblEmail, _tbEmail,
                _lblPhone, pnlPhone,
                _lblPassword, _tbPassword,
                _lblConfirmPass, _tbConfirmPass,
                _lblError,
                _btnLogin, _btnRegister,
                _lnkForgot,
            });

            // Thêm _pnlCard và _pnlLogo vào Form:
            Controls.AddRange(new Control[] { _pnlCard, _pnlLogo });
            // _pnlLogo thêm sau → nằm trên cùng (z-order)

            Resize += (s, e) => DoLayout(); // Mỗi khi resize → tính lại layout
            Load += (s, e) => { DoLayout(); SetLoginMode(); }; // Khi form mở lần đầu
        }

        // Hàm tạo label chuẩn:
        private Label MakeFieldLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.Blue,
                AutoSize = false,
                Height = 18,
                BackColor = Color.Transparent,
            };
        }

        private Panel MakeInputBorderPanel()
        {
            var p = new Panel { Height = 44, BackColor = Color.Transparent };
            return p;
        }

        private void SetLoginMode()
        {
            _isRegisterMode = false;

            Text = "SecureChat – Đăng nhập";

            // Ẩn các field chỉ dùng cho đăng ký:
            _lblDisplayName.Visible = _tbDisplayName.Visible = false;
            _lblEmail.Visible = _tbEmail.Visible = false;
            _lblConfirmPass.Visible = _tbConfirmPass.Visible = false;

            _btnLogin.Text = "ĐĂNG NHẬP";
            _btnLogin.IsOutlined = false; // bỏ hightlight viền
            _btnRegister.Text = "TẠO TÀI KHOẢN MỚI";
            _btnRegister.IsOutlined = true;
            _lnkForgot.Visible = true;

            // ── Thu nhỏ lại khi Login ──
            this.ClientSize = new Size(420, 560); // thu nhỏ cửa sổ lại vì ít field hơn đăng ký

            DoLayout();
        }

        private void SetRegisterMode()
        {
            _isRegisterMode = true;
            Text = "SecureChat – Đăng ký";

            _lblDisplayName.Visible = _tbDisplayName.Visible = true;
            _lblEmail.Visible = _tbEmail.Visible = true;
            _lblConfirmPass.Visible = _tbConfirmPass.Visible = true;

            _btnLogin.Text = "TẠO TÀI KHOẢN";
            _btnLogin.IsOutlined = false;
            _btnRegister.Text = "← Đã có tài khoản";
            _btnRegister.IsOutlined = true;
            _lnkForgot.Visible = false;

            // ── Phóng to khi Register (thêm ~3 field × ~80px) ──
            this.ClientSize = new Size(420, 780);

            DoLayout();
        }

        //  sắp xếp các thành phần (nút, ô nhập liệu, nhãn) vào đúng vị trí mỗi khi giao diện thay đổi.
        private void DoLayout()
        {
            // Khởi tạo thông số cơ bản
            int pad = 28; // Khoảng cách từ lề trái/phải vào nội dung
            int w = _pnlCard.Width - pad * 2; // Chiều rộng thực tế của các ô (trừ đi 2 bên lề)
            int y = 20; // Điểm bắt đầu vẽ từ trên xuống (cách đỉnh 20px)

            // tránh việc phải viết lặp đi lặp lại code đặt vị trí.
            void Row(Control lbl, Control input, int inputH = 44, int gap = 4)
            {
                if (!lbl.Visible) return; // Nếu nhãn (label) đang ẩn thì không làm gì cả

                // 1. Đặt vị trí nhãn (lbl): X=pad, Y=y, Rộng=w, Cao=18
                lbl.SetBounds(pad, y, w, 18); y += 20 + gap;
                // Nhảy xuống một khoảng (18 cao + 2 lề + gap) để chuẩn bị vẽ ô nhập

                // 2. Đặt vị trí ô nhập (input): X=pad, Y=y, Rộng=w, Cao=inputH (mặc định 44)
                input.SetBounds(pad, y, w, inputH); y += inputH + 14;
                // Nhảy xuống thêm 14px nữa để chuẩn bị cho dòng tiếp theo
            }

            // Sắp xếp theo thứ tự (có/không tùy chế độ):
            if (_isRegisterMode) Row(_lblDisplayName, _tbDisplayName);
            Row(_lblPhone, _pnlCard.Controls[5] as Panel ?? new Panel()); // phone row
            if (_isRegisterMode) Row(_lblEmail, _tbEmail);
            Row(_lblPassword, _tbPassword);
            if (_isRegisterMode) Row(_lblConfirmPass, _tbConfirmPass);

            // Dòng lỗi
            _lblError.SetBounds(pad, y, w, 20); y += 22;


            _btnLogin.SetBounds(pad, y, w, 46); y += 54;
            _btnRegister.SetBounds(pad, y, w, 46); y += 54;
            _lnkForgot.Location = new Point(_pnlCard.Width / 2 - _lnkForgot.Width / 2, y);
        }

        private void BtnLogin_Click(object sender, EventArgs e)
        {
            if (_isRegisterMode)
            {
                if (string.IsNullOrWhiteSpace(_tbPhone.Text) || string.IsNullOrWhiteSpace(_tbPassword.Text))
                { ShowError("Vui lòng điền đầy đủ thông tin."); return; }
                if (_tbPassword.Text != _tbConfirmPass.Text)
                { ShowError("Mật khẩu xác nhận không khớp."); return; }

                var twoFA = new frmTwoFA();
                if (twoFA.ShowDialog(this) == DialogResult.OK)
                {
                    // ← Mở MainForm
                    var main = new frmMainChat();
                    main.Show();
                    this.Hide();
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(_tbPhone.Text) || string.IsNullOrWhiteSpace(_tbPassword.Text))
                { ShowError("Vui lòng nhập số điện thoại và mật khẩu."); return; }
                HideError();

                // ← Mở MainForm luôn (mock, chưa có backend)
                var main = new frmMainChat();
                main.Show();
                this.Hide();
            }
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (_isRegisterMode) SetLoginMode();
            else SetRegisterMode();
        }

        private void ShowError(string msg) { _lblError.Text = msg; _lblError.Visible = true; }
        private void HideError() { _lblError.Visible = false; }
    }
}
