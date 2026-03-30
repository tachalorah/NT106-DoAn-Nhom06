using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureChat.Client
{
    /// <summary>
    /// Màn hình quên mật khẩu - 3 bước: Email → OTP → Mật khẩu mới
    /// </summary>
    public class frmForgot : Form
    {
        private int _step = 1; // 1=email, 2=otp, 3=newpass

        private Panel _pnlMain; // thêm vào chỗ khai báo field

        // Step indicators
        // Mảng 3 chấm tròn hiển thị tiến trình (Step 1 → 2 → 3). Mỗi chấm là một Panel tự vẽ.
        private Panel[] _stepDots = new Panel[3];
        // Tiêu đề lớn và mô tả nhỏ thay đổi theo từng bước.
        private Label   _lblStepTitle, _lblStepDesc;

        // Step 1
        private TelegramTextBox _tbEmail; // _tbEmail: ô nhập địa chỉ email
        private Label           _lblEmailHint; // _lblEmailHint: thông báo màu xanh lá "Link có hiệu lực 15 phút…" (ẩn ban đầu)

        // Step 2
        private Panel           _pnlOtp; // panel chứa 6 ô nhập OTP
        private TextBox[]       _otpBoxes = new TextBox[6]; // mảng 6 ô TextBox, mỗi ô 1 chữ số
        private System.Windows.Forms.Timer _timer; // đồng hồ đếm ngược 60 giây
        private int             _countdown = 60; // giá trị đếm ngược hiện tại
        private Label           _lblCountdown; // nhãn hiển thị "Gửi lại sau (Xs)"

        // Step 3: Hai ô nhập mật khẩu mới và xác nhận mật khẩu.
        private TelegramTextBox _tbNewPass, _tbConfirmPass;

        // Common
        private TelegramButton  _btnNext, _btnBack;
        private Label           _lblError;
        private Panel           _pnlContent;
        private TelegramHeader  _header;

        public frmForgot()
        {
            InitializeComponent();
            ShowStep(1);
        }

        private void InitializeComponent()
        {
            Text = "Đặt lại mật khẩu";
            Size = new Size(400, 520);
            MinimumSize = new Size(380, 490);
            StartPosition = FormStartPosition.CenterParent;
            // FormBorderStyle = FormBorderStyle.FixedSingle;
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = TG.FontRegular(9.5f);

            // Header
            _header = new TelegramHeader { Title = "Đặt lại mật khẩu" };
            _header.ShowBack = true;
            _header.BackClicked += (s, e) =>
            {
                if (_step > 1) ShowStep(_step - 1);
                else Close();
            };
            Controls.Add(_header);

            // Step indicator
            var pnlSteps = new Panel { Height = 48, BackColor = Color.White };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                var dot = new Panel { Size = new Size(28, 28), BackColor = Color.Transparent };
                dot.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    bool active = _step == idx + 1;
                    bool done   = _step > idx + 1;
                    Color bg = done ? TG.Blue : active ? TG.Blue : TG.Divider;
                    e.Graphics.FillEllipse(new SolidBrush(bg), 0, 0, 27, 27);
                    string txt = done ? "✓" : (idx + 1).ToString();
                    using var sf = new System.Drawing.StringFormat { Alignment = System.Drawing.StringAlignment.Center, LineAlignment = System.Drawing.StringAlignment.Center };
                    e.Graphics.DrawString(txt, TG.FontSemiBold(9f), System.Drawing.Brushes.White, new Rectangle(0, 0, 28, 28), sf);
                };
                _stepDots[i] = dot;
                pnlSteps.Controls.Add(dot);
            }
            // Connecting lines
            pnlSteps.Paint += (s, e) =>
            {
                int lineY = 24 + pnlSteps.Padding.Top;
                int[] xs = GetDotXs(pnlSteps.Width);
                e.Graphics.DrawLine(new System.Drawing.Pen(_step > 1 ? TG.Blue : TG.Divider, 2), xs[0] + 28, lineY, xs[1], lineY);
                e.Graphics.DrawLine(new System.Drawing.Pen(_step > 2 ? TG.Blue : TG.Divider, 2), xs[1] + 28, lineY, xs[2], lineY);
            };
            pnlSteps.Resize += (s, e) =>
            {
                int[] xs = GetDotXs(pnlSteps.Width);
                for (int i = 0; i < 3; i++) _stepDots[i].Location = new Point(xs[i], 10);
                pnlSteps.Invalidate();
            };

            // Step labels
            _lblStepTitle = new Label
            {
                AutoSize = false, Height = 26,
                Font = TG.FontSemiBold(12f),
                ForeColor = TG.TextPrimary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
            };
            _lblStepDesc = new Label
            {
                AutoSize = false, Height = 36,
                Font = TG.FontRegular(9f),
                ForeColor = TG.TextSecondary,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopCenter,
            };

            // Content panel
            _pnlContent = new Panel { BackColor = Color.Transparent };

            // ── Step 1: Email ─────────────────────────
            var lblEmail = new Label { Text = "Địa chỉ email đã đăng ký:", Font = TG.FontRegular(8.5f), ForeColor = TG.Blue, AutoSize = false, Height = 18, BackColor = Color.Transparent };
            _tbEmail = new TelegramTextBox { Height = 44 };
            _tbEmail.SetPlaceholder("user@example.com");
            _lblEmailHint = new Label
            {
                Text = "📧  Link đặt lại có hiệu lực trong 15 phút. Kiểm tra cả thư mục spam.",
                Font = TG.FontRegular(8.5f),
                ForeColor = Color.FromArgb(0x2E, 0x7D, 0x32),
                BackColor = Color.FromArgb(0xE8, 0xF5, 0xE9),
                AutoSize = false, Height = 52,
                Padding = new Padding(10, 8, 10, 0),
                Visible = false,
            };

            // ── Step 2: OTP ───────────────────────────
            _pnlOtp = new Panel { Height = 62, BackColor = Color.Transparent };
            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                var box = new TextBox
                {
                    MaxLength = 1,
                    Font = TG.FontTitle(16f),
                    ForeColor = TG.Blue,
                    TextAlign = HorizontalAlignment.Center,
                    BorderStyle = BorderStyle.FixedSingle,
                    Size = new Size(42, 50),
                    BackColor = Color.White,
                };
                box.TextChanged += (s, e) =>
                {
                    if (!string.IsNullOrEmpty(box.Text) && idx < 5) _otpBoxes[idx + 1].Focus();
                };
                box.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Back && string.IsNullOrEmpty(box.Text) && idx > 0) _otpBoxes[idx - 1].Focus();
                };
                _pnlOtp.Controls.Add(box);
                _otpBoxes[i] = box;
            }
            Action layoutOtp = () =>
            {
                if (_pnlOtp.Width == 0) return;
                int total = 6 * 42 + 5 * 6, startX = (_pnlOtp.Width - total) / 2;
                for (int i = 0; i < 6; i++) _otpBoxes[i].Location = new Point(startX + i * 48, 4);
            };

            _pnlOtp.Resize += (s, e) => layoutOtp();
            _pnlOtp.VisibleChanged += (s, e) => { if (_pnlOtp.Visible) layoutOtp(); };

            _lblCountdown = new Label
            {
                AutoSize = false, Height = 22, TextAlign = ContentAlignment.MiddleCenter,
                Font = TG.FontRegular(8.5f), ForeColor = TG.TextSecondary, BackColor = Color.Transparent,
            };
            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) => { _countdown--; UpdateCountdown(); if (_countdown <= 0) _timer.Stop(); };

            // ── Step 3: New Password ──────────────────
            var lblNew = new Label { Text = "Mật khẩu mới:", Font = TG.FontRegular(8.5f), ForeColor = TG.Blue, AutoSize = false, Height = 18, BackColor = Color.Transparent };
            _tbNewPass = new TelegramTextBox { Height = 44 };
            _tbNewPass.SetPlaceholder("Ít nhất 8 ký tự...");
            _tbNewPass.PasswordChar = '●';

            var lblConf = new Label { Text = "Xác nhận mật khẩu mới:", Font = TG.FontRegular(8.5f), ForeColor = TG.Blue, AutoSize = false, Height = 18, BackColor = Color.Transparent };
            _tbConfirmPass = new TelegramTextBox { Height = 44 };
            _tbConfirmPass.SetPlaceholder("Nhập lại mật khẩu...");
            _tbConfirmPass.PasswordChar = '●';

            // Error
            _lblError = new Label
            {
                AutoSize = false, Height = 20, TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A),
                Font = TG.FontRegular(8.5f), BackColor = Color.Transparent, Visible = false,
            };

            // Buttons
            _btnNext = new TelegramButton { Text = "TIẾP THEO", Height = 46, Font = TG.FontSemiBold(10.5f), Radius = TG.RadiusSmall };
            _btnNext.Click += BtnNext_Click;

            _pnlContent.Controls.AddRange(new Control[] {
                // step1
                lblEmail, _tbEmail, _lblEmailHint,
                // step2
                _pnlOtp, _lblCountdown,
                // step3
                lblNew, _tbNewPass, lblConf, _tbConfirmPass,
            });

            _pnlMain = new Panel { BackColor = Color.White, Padding = new Padding(28, 12, 28, 20) };
            _pnlMain.Controls.AddRange(new Control[] { _lblStepTitle, _lblStepDesc, pnlSteps, _pnlContent, _lblError, _btnNext });
            _pnlMain.Dock = DockStyle.Fill;
            _pnlMain.Resize += (s, e) => DoLayout(_pnlMain);

            Controls.AddRange(new Control[] { _pnlMain, _header });
        }

        private int[] GetDotXs(int panelWidth)
        {
            int center = panelWidth / 2;
            return new[] { center - 60, center - 14, center + 32 };
        }

        private void ShowStep(int step)
        {
            _step = step;
            HideError();

            // Refresh step dots
            foreach (var d in _stepDots) d.Invalidate();
            if (_stepDots[0].Parent != null) _stepDots[0].Parent.Invalidate();

            switch (step)
            {
                case 1:
                    _header.Title = "Đặt lại mật khẩu";
                    _lblStepTitle.Text = "Nhập email của bạn";
                    _lblStepDesc.Text = "Chúng tôi sẽ gửi link xác nhận để đặt lại mật khẩu.";
                    SetStep1Visible(true); SetStep2Visible(false); SetStep3Visible(false);
                    _btnNext.Text = "GỬI LINK ĐẶT LẠI";
                    break;
                case 2:
                    _header.Title = "Nhập mã xác nhận";
                    _lblStepTitle.Text = "Kiểm tra email của bạn";
                    _lblStepDesc.Text = $"Mã 6 chữ số đã được gửi đến\n{_tbEmail.Text}";
                    SetStep1Visible(false); SetStep2Visible(true); SetStep3Visible(false);
                    _btnNext.Text = "XÁC NHẬN";
                    _countdown = 60; _timer.Start(); UpdateCountdown();
                    _otpBoxes[0].Focus();
                    break;
                case 3:
                    _header.Title = "Mật khẩu mới";
                    _lblStepTitle.Text = "Đặt mật khẩu mới";
                    _lblStepDesc.Text = "Mật khẩu mới phải có ít nhất 8 ký tự.";
                    SetStep1Visible(false); SetStep2Visible(false); SetStep3Visible(true);
                    _btnNext.Text = "ĐẶT LẠI MẬT KHẨU";
                    _tbNewPass.Text = "";
                    _tbConfirmPass.Text = "";
                    break;
            }
            DoLayout(_pnlMain); // ← thêm dòng này vào cuối
        }

        private void SetStep1Visible(bool v)
        {
            _pnlContent.Controls[0].Visible = v; // lblEmail
            _pnlContent.Controls[1].Visible = v; // tbEmail
            _lblEmailHint.Visible = false;
        }
        private void SetStep2Visible(bool v)
        {
            _pnlContent.Controls[3].Visible = v; // pnlOtp
            _pnlContent.Controls[4].Visible = v; // countdown
        }
        private void SetStep3Visible(bool v)
        {
            _pnlContent.Controls[5].Visible = v;
            _pnlContent.Controls[6].Visible = v;
            _pnlContent.Controls[7].Visible = v;
            _pnlContent.Controls[8].Visible = v;
        }

        private void DoLayout(Panel pnlMain)
        {
            int pad = 28, w = pnlMain.Width - pad * 2, y = 12;
            var pnlSteps = pnlMain.Controls[2] as Panel;

            _lblStepTitle.SetBounds(0, y, pnlMain.Width, 26); y += 30;
            _lblStepDesc.SetBounds(10, y, pnlMain.Width - 20, 36); y += 44;
            pnlSteps?.SetBounds(0, y, pnlMain.Width, 48); y += 56;

            _pnlContent.SetBounds(pad, y, w, 160);
            int cy = 0;

            if (_step == 1)
            {
                _pnlContent.Controls[0].SetBounds(0, cy, w, 18); cy += 22;
                _pnlContent.Controls[1].SetBounds(0, cy, w, 44); cy += 52;
                if (_lblEmailHint.Visible) { _lblEmailHint.SetBounds(0, cy, w, 52); cy += 58; }
            }
            else if (_step == 2)
            {
                _pnlContent.Controls[3].SetBounds(0, cy, w, 62); cy += 70;
                _pnlContent.Controls[4].SetBounds(0, cy, w, 22); cy += 28;
                int total = 6 * 42 + 5 * 6, startX = (w - total) / 2;
                for (int i = 0; i < 6; i++) _otpBoxes[i].Location = new Point(startX + i * 48, 4);
            }
            else
            {
                _pnlContent.Controls[5].SetBounds(0, cy, w, 18); cy += 22;
                _pnlContent.Controls[6].SetBounds(0, cy, w, 44); cy += 52;
                _pnlContent.Controls[7].SetBounds(0, cy, w, 18); cy += 22;
                _pnlContent.Controls[8].SetBounds(0, cy, w, 44); cy += 48;
            }

            _pnlContent.Height = cy;
            y += _pnlContent.Height + 12;
            _lblError.SetBounds(0, y, pnlMain.Width, 20); y += 24;
            _btnNext.SetBounds(pad, y, w, 46);
        }

        private void BtnNext_Click(object sender, EventArgs e)
        {
            HideError();
            switch (_step)
            {
                case 1:
                    if (string.IsNullOrWhiteSpace(_tbEmail.Text)) { ShowError("Vui lòng nhập địa chỉ email."); return; }
                    if (!_tbEmail.Text.Contains("@")) { ShowError("Email không hợp lệ."); return; }
                    _lblEmailHint.Visible = true;
                    ShowStep(2);
                    break;
                case 2:
                    string otp = "";
                    foreach (var b in _otpBoxes) otp += b.Text;
                    if (otp.Length < 6) { ShowError("Vui lòng nhập đủ 6 chữ số."); return; }
                    ShowStep(3);
                    break;
                case 3:
                    if (_tbNewPass.Text.Length < 8) { ShowError("Mật khẩu phải có ít nhất 8 ký tự."); return; }
                    if (_tbNewPass.Text != _tbConfirmPass.Text) { ShowError("Mật khẩu xác nhận không khớp."); return; }
                    MessageBox.Show("Đặt lại mật khẩu thành công!\nVui lòng đăng nhập lại.", "SecureChat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                    break;
            }
        }

        private void UpdateCountdown()
        {
            _lblCountdown.Text = _countdown > 0
                ? $"Không nhận được mã? Gửi lại sau ({_countdown}s)"
                : "Không nhận được mã? Nhấn Gửi lại";
        }

        private void ShowError(string msg) { _lblError.Text = msg; _lblError.Visible = true; }
        private void HideError() { _lblError.Visible = false; }
        protected override void OnFormClosed(FormClosedEventArgs e) { _timer?.Stop(); base.OnFormClosed(e); }
    }
}
