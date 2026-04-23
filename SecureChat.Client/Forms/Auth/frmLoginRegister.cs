using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Net.Mail;
using SecureChat.Client.Services;
using SecureChat.Client.Security;
using SecureChat.DTOs;

namespace SecureChat.Client
{
    public class frmLoginRegister : Form
    {
        // ── Controls ──────────────────────────────────────────────────
        private Panel _pnlLogo;
        private Panel _pnlCard;
        private Label _lblAppName, _lblTagline;
        private Label _lblEmail, _lblPassword, _lblDisplayName, _lblConfirmPass;
        private TelegramTextBox _tbEmail, _tbPassword, _tbDisplayName, _tbConfirmPass;
        private TelegramButton _btnLogin, _btnRegister;
        private Label _lblError;
        private LinkLabel _lnkForgot;

        // ── State ─────────────────────────────────────────────────────
        private bool _isRegisterMode = false;

        public frmLoginRegister()
        {
            // Bật DoubleBuffered để giảm flickering khi resize hoặc đổi mode
            this.DoubleBuffered = true;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            // Form settings
            Text = "SecureChat";
            Size = new Size(420, 560);
            MinimumSize = new Size(380, 500);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = TG.Blue;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            Font = TG.FontRegular(9.5f);

            // ── Logo Panel ───────────────────────────────────────────
            _pnlLogo = new Panel { BackColor = TG.Blue, Height = 160, Dock = DockStyle.Top };

            var lblIcon = new Label
            {
                Text = "✈",
                Font = new Font("Segoe UI Emoji", 42f),
                ForeColor = Color.White,
                Size = new Size(80, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _pnlLogo.Controls.Add(lblIcon);

            _lblAppName = new Label
            {
                Text = "SecureChat",
                Font = TG.FontTitle(16f),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 30,
            };
            _lblTagline = new Label
            {
                Text = "Nhắn tin an toàn và mã hóa đầu cuối",
                Font = TG.FontRegular(9f),
                ForeColor = Color.FromArgb(200, 235, 255),
                TextAlign = ContentAlignment.MiddleCenter,
                Height = 20,
            };
            _pnlLogo.Controls.AddRange(new Control[] { _lblAppName, _lblTagline });

            _pnlLogo.Resize += (s, e) =>
            {
                lblIcon.Location = new Point((_pnlLogo.Width - 80) / 2, 20);
                _lblAppName.SetBounds(0, 108, _pnlLogo.Width, 30);
                _lblTagline.SetBounds(0, 136, _pnlLogo.Width, 20);
            };

            // ── Card trắng ──────────────────────────────────────────
            _pnlCard = new RoundedPanel
            {
                BackColor = Color.White,
                Dock = DockStyle.Fill,
                Padding = new Padding(28, 24, 28, 20),
            };

            // Khởi tạo các ô nhập liệu
            _lblDisplayName = MakeFieldLabel("Họ và tên");
            _tbDisplayName = new TelegramTextBox { Height = 44 };
            _tbDisplayName.SetPlaceholder("Nguyễn Văn A");

            _lblEmail = MakeFieldLabel("Địa chỉ Email");
            _tbEmail = new TelegramTextBox { Height = 44 };
            _tbEmail.SetPlaceholder("example@email.com");

            _lblPassword = MakeFieldLabel("Mật khẩu");
            _tbPassword = new TelegramTextBox { Height = 44, PasswordChar = '●' };
            _tbPassword.SetPlaceholder("Nhập mật khẩu...");

            _lblConfirmPass = MakeFieldLabel("Xác nhận mật khẩu");
            _tbConfirmPass = new TelegramTextBox { Height = 44, PasswordChar = '●' };
            _tbConfirmPass.SetPlaceholder("Nhập lại mật khẩu...");

            _lblError = new Label
            {
                ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A),
                Font = TG.FontRegular(8.5f),
                Visible = false,
                Height = 20
            };

            _btnLogin = new TelegramButton
            {
                Text = "ĐĂNG NHẬP",
                Height = 46,
                Font = TG.FontSemiBold(10.5f),
                Radius = TG.RadiusSmall
            };
            _btnLogin.Click += BtnLogin_Click;

            _btnRegister = new TelegramButton
            {
                Text = "TẠO TÀI KHOẢN",
                Height = 46,
                Font = TG.FontSemiBold(10.5f),
                Radius = TG.RadiusSmall,
                IsOutlined = true,
                NormalColor = TG.Blue
            };
            _btnRegister.Click += BtnRegister_Click;

            _lnkForgot = new LinkLabel
            {
                Text = "Quên mật khẩu?",
                LinkColor = TG.Blue,
                Font = TG.FontRegular(9f),
                AutoSize = true
            };

            _pnlCard.Controls.AddRange(new Control[] {
                _lblDisplayName, _tbDisplayName,
                _lblEmail, _tbEmail,
                _lblPassword, _tbPassword,
                _lblConfirmPass, _tbConfirmPass,
                _lblError, _btnLogin, _btnRegister, _lnkForgot
            });

            Controls.AddRange(new Control[] { _pnlCard, _pnlLogo });

            this.Resize += (s, e) => DoLayout();
            this.Load += (s, e) => SetLoginMode();
        }

        private Label MakeFieldLabel(string text) => new Label
        {
            Text = text,
            Font = TG.FontRegular(8.5f),
            ForeColor = TG.Blue,
            Height = 18,
            BackColor = Color.Transparent
        };

        private void SetLoginMode()
        {
            _isRegisterMode = false;
            Text = "SecureChat – Đăng nhập";
            _lblDisplayName.Visible = _tbDisplayName.Visible = false;
            _lblConfirmPass.Visible = _tbConfirmPass.Visible = false;

            _btnLogin.Text = "ĐĂNG NHẬP";
            _btnRegister.Text = "TẠO TÀI KHOẢN MỚI";
            _lnkForgot.Visible = true;

            this.ClientSize = new Size(420, 560);
            DoLayout();
        }

        private void SetRegisterMode()
        {
            _isRegisterMode = true;
            Text = "SecureChat – Đăng ký";
            _lblDisplayName.Visible = _tbDisplayName.Visible = true;
            _lblConfirmPass.Visible = _tbConfirmPass.Visible = true;

            _btnLogin.Text = "TẠO TÀI KHOẢN";
            _btnRegister.Text = "← Đã có tài khoản";
            _lnkForgot.Visible = false;

            this.ClientSize = new Size(420, 720);
            DoLayout();
        }

        private void DoLayout()
        {
            int pad = 28, y = 20;
            int w = _pnlCard.Width - pad * 2;

            void Row(Control lbl, Control input)
            {
                if (!lbl.Visible) return;
                lbl.SetBounds(pad, y, w, 18); y += 22;
                input.SetBounds(pad, y, w, 44); y += 58;
            }

            Row(_lblDisplayName, _tbDisplayName);
            Row(_lblEmail, _tbEmail);
            Row(_lblPassword, _tbPassword);
            Row(_lblConfirmPass, _tbConfirmPass);

            _lblError.SetBounds(pad, y, w, 20); y += 25;
            _btnLogin.SetBounds(pad, y, w, 46); y += 54;
            _btnRegister.SetBounds(pad, y, w, 46); y += 54;
            _lnkForgot.Location = new Point((_pnlCard.Width - _lnkForgot.Width) / 2, y);
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            SetLoading(true);
            HideError();

            try
            {
                if (_isRegisterMode) await HandleRegistration();
                else await HandleLogin();
            }
            catch (Exception ex)
            {
                ShowError("Lỗi kết nối: " + ex.Message);
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task HandleRegistration()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(_tbEmail.Text) || string.IsNullOrWhiteSpace(_tbPassword.Text) ||
                string.IsNullOrWhiteSpace(_tbDisplayName.Text))
            {
                ShowError("Vui lòng nhập đầy đủ thông tin.");
                return;
            }
            if (!IsValidEmail(_tbEmail.Text))
            {
                ShowError("Định dạng email không hợp lệ.");
                return;
            }
            if (_tbPassword.Text != _tbConfirmPass.Text)
            {
                ShowError("Mật khẩu xác nhận không khớp.");
                return;
            }

            // Cryptography (chạy Task.Run để không lag UI)
            var keys = await Task.Run(() => RSAKeyManager.GenerateRSAKeys());
            string hashedPass = await Task.Run(() => Argon2Hasher.HashPassword(_tbPassword.Text));

            var req = new RegisterRequest(
                Username: _tbEmail.Text.Split('@')[0], // Lấy phần trước @ làm username tạm
                DisplayName: _tbDisplayName.Text,
                Email: _tbEmail.Text,
                HashedPassword: hashedPass,
                HashedBKey: "TBD",
                KeySalt: hashedPass.Split(':')[0],
                PublicKey: keys.PublicKey
            );

            var (ok, _, err) = await ApiClient.Instance.PostAsync<RegisterRequest, object>("api/auth/register", req);
            if (ok)
            {
                MessageBox.Show("Đăng ký thành công!", "Thông báo");
                SetLoginMode();
            }
            else ShowError(err);
        }

        private async Task HandleLogin()
        {
            if (string.IsNullOrWhiteSpace(_tbEmail.Text)) { ShowError("Vui lòng nhập Email."); return; }

            // LƯU Ý: Trong thực tế, bạn cần lấy Salt từ Server trước khi Hash ở bước Login
            string hashedPass = await Task.Run(() => Argon2Hasher.HashPassword(_tbPassword.Text));

            var req = new LoginRequest(_tbEmail.Text, hashedPass, Environment.MachineName);
            var (ok, res, err) = await ApiClient.Instance.PostAsync<LoginRequest, AuthResponse>("api/auth/login", req);

            if (ok && res != null)
            {
                ApiClient.Instance.SetAccessToken(res.AccessToken);
                new frmMainChat().Show();
                this.Hide();
            }
            else ShowError(err);
        }

        private void BtnRegister_Click(object sender, EventArgs e)
        {
            if (_isRegisterMode) SetLoginMode();
            else SetRegisterMode();
        }

        private void SetLoading(bool loading)
        {
            _btnLogin.Enabled = _btnRegister.Enabled = !loading;
            _btnLogin.Text = loading ? "ĐANG XỬ LÝ..." : (_isRegisterMode ? "TẠO TÀI KHOẢN" : "ĐĂNG NHẬP");
        }

        private bool IsValidEmail(string email)
        {
            try { return new MailAddress(email).Address == email; }
            catch { return false; }
        }

        private void ShowError(string msg) { _lblError.Text = msg; _lblError.Visible = true; }
        private void HideError() => _lblError.Visible = false;
    }
}