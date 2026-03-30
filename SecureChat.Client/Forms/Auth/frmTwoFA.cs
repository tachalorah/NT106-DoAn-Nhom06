using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SecureChat.Client
{
    /// <summary>
    /// Màn hình nhập mã 2FA (OTP 5 ký tự kiểu Telegram)
    /// </summary>
    public class frmTwoFA : Form
    {
        private TextBox[]    _otpBoxes = new TextBox[5];
        private TelegramButton _btnConfirm;
        private Label        _lblTitle, _lblDesc, _lblResend, _lblTimer;
        private System.Windows.Forms.Timer _timer;
        private int          _countdown = 60;
        private Label        _lblError;

        public frmTwoFA()
        {
            InitializeComponent();
            StartCountdown();
        }

        private void InitializeComponent()
        {
            Text = "Xác minh 2 bước";
            Size = new Size(380, 480);
            MinimumSize = new Size(360, 450);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            BackColor = Color.White;
            Font = TG.FontRegular(9.5f);

            // ── Header xanh ──────────────────────────
            var header = new Panel { Height = 140, BackColor = TG.Blue, Dock = DockStyle.Top };

            var lblIcon = new Label
            {
                Text = "🔐",
                Font = new Font("Segoe UI Emoji", 36f),
                ForeColor = Color.White,
                AutoSize = false, Size = new Size(60, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            var lblH = new Label
            {
                Text = "Xác minh 2 bước",
                Font = TG.FontSemiBold(13f),
                ForeColor = Color.White,
                AutoSize = false, Height = 28,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            var lblSub = new Label
            {
                Text = "Mã xác nhận đã được gửi đến email của bạn",
                Font = TG.FontRegular(8.5f),
                ForeColor = Color.FromArgb(200, 235, 255),
                AutoSize = false, Height = 20,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            header.Controls.AddRange(new Control[] { lblIcon, lblH, lblSub });
            header.Resize += (s, e) =>
            {
                lblIcon.Location = new Point((header.Width - 60) / 2, 14);
                lblH.SetBounds(0, 82, header.Width, 28);
                lblSub.SetBounds(0, 110, header.Width, 20);
            };

            // ── Body ──────────────────────────────────
            _lblDesc = new Label
            {
                Text = "Nhập 5 chữ số trong mã xác nhận:",
                Font = TG.FontRegular(9.5f),
                ForeColor = TG.TextSecondary,
                AutoSize = false, Height = 24,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };

            // OTP boxes
            var pnlOtp = new Panel { Height = 58, BackColor = Color.Transparent };
            for (int i = 0; i < 5; i++)
            {
                int idx = i;
                var box = new TextBox
                {
                    MaxLength = 1,
                    Font = TG.FontTitle(18f),
                    ForeColor = TG.Blue,
                    TextAlign = HorizontalAlignment.Center,
                    BackColor = Color.White,
                    BorderStyle = BorderStyle.None,
                    Size = new Size(48, 56),
                    Tag = idx,
                };
                // Border painted by panel
                var wrap = new Panel
                {
                    Size = new Size(50, 58),
                    BackColor = Color.White,
                };
                wrap.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    bool focused = box.Focused;
                    bool filled = !string.IsNullOrEmpty(box.Text);
                    Color border = focused ? TG.Blue : filled ? TG.Blue : TG.Divider;
                    float bw = focused ? 2f : 1f;
                    var r = new Rectangle(0, 0, wrap.Width - 1, wrap.Height - 1);
                    using var path = RoundedPanel.GetRoundedPath(r, TG.RadiusSmall);
                    e.Graphics.FillPath(Brushes.White, path);
                    e.Graphics.DrawPath(new Pen(border, bw), path);
                };
                wrap.Controls.Add(box);
                box.Location = new Point(1, (58 - box.Height) / 2);

                // Auto advance
                box.TextChanged += (s, e) =>
                {
                    wrap.Invalidate();
                    if (!string.IsNullOrEmpty(box.Text) && idx < 4)
                        _otpBoxes[idx + 1].Focus();
                    if (!string.IsNullOrEmpty(box.Text) && idx == 4)
                        _btnConfirm.Focus();
                };
                box.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Back && string.IsNullOrEmpty(box.Text) && idx > 0)
                        _otpBoxes[idx - 1].Focus();
                };
                box.GotFocus  += (s, e) => wrap.Invalidate();
                box.LostFocus += (s, e) => wrap.Invalidate();

                pnlOtp.Controls.Add(wrap);
                _otpBoxes[i] = box;
            }
            // Layout OTP
            pnlOtp.Resize += (s, e) =>
            {
                int total = 5 * 50 + 4 * 8;
                int startX = (pnlOtp.Width - total) / 2;
                for (int i = 0; i < pnlOtp.Controls.Count; i++)
                    pnlOtp.Controls[i].Location = new Point(startX + i * 58, 0);
            };

            // Error
            _lblError = new Label
            {
                AutoSize = false, Height = 20,
                ForeColor = Color.FromArgb(0xE2, 0x4B, 0x4A),
                Font = TG.FontRegular(8.5f),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
            };

            // Button
            _btnConfirm = new TelegramButton
            {
                Text = "XÁC NHẬN",
                Height = 46,
                Font = TG.FontSemiBold(10.5f),
                Radius = TG.RadiusSmall,
            };
            _btnConfirm.Click += BtnConfirm_Click;

            // Resend
            _lblTimer = new Label
            {
                AutoSize = true,
                Font = TG.FontRegular(9f),
                ForeColor = TG.TextSecondary,
                BackColor = Color.Transparent,
            };
            _lblResend = new Label
            {
                Text = "Không nhận được mã?",
                Font = TG.FontRegular(9f),
                ForeColor = TG.TextSecondary,
                AutoSize = true,
                BackColor = Color.Transparent,
            };
            var lnkResend = new LinkLabel
            {
                Text = "Gửi lại",
                LinkColor = TG.Blue,
                Font = TG.FontRegular(9f),
                AutoSize = true,
                BackColor = Color.Transparent,
                Enabled = false,
            };
            lnkResend.LinkClicked += (s, e) => { _countdown = 60; lnkResend.Enabled = false; StartCountdown(); };

            // Layout all
            var pnlBody = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(28, 16, 28, 16) };
            pnlBody.Controls.AddRange(new Control[] { _lblDesc, pnlOtp, _lblError, _btnConfirm, _lblResend, lnkResend, _lblTimer });

            pnlBody.Resize += (s, e) =>
            {
                int pad = 28, w = pnlBody.Width - pad * 2, y = 16;
                _lblDesc.SetBounds(0, y, pnlBody.Width, 24); y += 32;
                pnlOtp.SetBounds(pad, y, w, 58); y += 68;
                _lblError.SetBounds(0, y, pnlBody.Width, 20); y += 24;
                _btnConfirm.SetBounds(pad, y, w, 46); y += 58;
                _lblResend.Location = new Point(pad, y);
                lnkResend.Location  = new Point(pad + _lblResend.Width + 4, y);
                _lblTimer.Location  = new Point(pad + _lblResend.Width + lnkResend.Width + 8, y);
            };

            _timer = new System.Windows.Forms.Timer { Interval = 1000 };
            _timer.Tick += (s, e) =>
            {
                _countdown--;
                _lblTimer.Text = $"({_countdown}s)";
                if (_countdown <= 0) { _timer.Stop(); lnkResend.Enabled = true; _lblTimer.Text = ""; }
            };

            Controls.AddRange(new Control[] { pnlBody, header });
        }

        private void StartCountdown()
        {
            _lblTimer.Text = $"({_countdown}s)";
            _timer.Start();
        }

        private string GetOtpCode()
        {
            string code = "";
            foreach (var box in _otpBoxes) code += box.Text;
            return code;
        }

        private void BtnConfirm_Click(object sender, EventArgs e)
        {
            string code = GetOtpCode();
            if (code.Length < 5) { ShowError("Vui lòng nhập đủ 5 chữ số."); return; }
            HideError();
            // Mock: code đúng = "12345"
            if (code == "12345")
            {
                MessageBox.Show("Xác minh thành công!", "SecureChat", MessageBoxButtons.OK, MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else ShowError("Mã xác nhận không đúng. Vui lòng thử lại.");
        }

        private void ShowError(string msg) { _lblError.Text = msg; _lblError.Visible = true; }
        private void HideError() { _lblError.Visible = false; }

        protected override void OnFormClosed(FormClosedEventArgs e) { _timer?.Stop(); base.OnFormClosed(e); }
    }
}
