using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SecureChat.Client
{
    // ─────────────────────────────────────────────
    // ROUNDED PANEL - panel với bo góc
    // ─────────────────────────────────────────────
    public class RoundedPanel : Panel
    {
        public int Radius { get; set; } = TG.RadiusMedium;
        public Color BorderColor { get; set; } = Color.Transparent;
        public int BorderWidth { get; set; } = 0;

        public RoundedPanel()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = GetRoundedPath(rect, Radius);
            using var brush = new SolidBrush(BackColor);
            e.Graphics.FillPath(brush, path);
            if (BorderWidth > 0 && BorderColor != Color.Transparent)
            {
                using var pen = new Pen(BorderColor, BorderWidth);
                e.Graphics.DrawPath(pen, path);
            }
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            // Thêm dòng này
            if (radius <= 0)
            {
                var p = new GraphicsPath();
                p.AddRectangle(rect);
                return p;
            }

            var path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    // ─────────────────────────────────────────────
    // TELEGRAM BUTTON - nút xanh bo góc
    // ─────────────────────────────────────────────
    public class TelegramButton : Button
    {
        private bool _isHovered, _isPressed;
        public int Radius { get; set; } = TG.RadiusLarge;
        public Color NormalColor { get; set; } = TG.Blue;
        public Color HoverColor { get; set; } = TG.BlueHover;
        public Color PressColor { get; set; } = TG.BlueActive;
        public Color TextColor { get; set; } = Color.White;
        public bool IsOutlined { get; set; } = false;

        public TelegramButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
            DoubleBuffered = true;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand;
            Font = TG.FontSemiBold(10f);
            ForeColor = Color.White;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _isPressed = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _isPressed = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPanel.GetRoundedPath(rect, Radius);

            Color bg = _isPressed ? PressColor : _isHovered ? HoverColor : NormalColor;

            if (IsOutlined)
            {
                e.Graphics.FillPath(new SolidBrush(Color.White), path);
                e.Graphics.DrawPath(new Pen(NormalColor, 1.5f), path);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(Text, Font, new SolidBrush(NormalColor), rect, sf);
            }
            else
            {
                e.Graphics.FillPath(new SolidBrush(bg), path);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                e.Graphics.DrawString(Text, Font, new SolidBrush(TextColor), rect, sf);
            }
        }
    }

    // ─────────────────────────────────────────────
    // AVATAR CONTROL - hình tròn chữ cái đầu
    // ─────────────────────────────────────────────
    public class AvatarControl : Control
    {
        public string DisplayName { get; set; } = "";
        public Image Photo { get; set; } = null;
        public bool ShowOnline { get; set; } = false;
        private Color _avatarColor;

        public AvatarControl()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            DoubleBuffered = true;
            BackColor = Color.Transparent;
        }

        public void SetName(string name)
        {
            DisplayName = name;
            _avatarColor = TG.GetAvatarColor(name);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            int size = Math.Min(Width, Height);
            var rect = new Rectangle(0, 0, size - 1, size - 1);

            if (Photo != null)
            {
                using var path = new GraphicsPath();
                path.AddEllipse(rect);
                e.Graphics.SetClip(path);
                e.Graphics.DrawImage(Photo, rect);
                e.Graphics.ResetClip();
            }
            else
            {
                e.Graphics.FillEllipse(new SolidBrush(_avatarColor == default ? TG.Blue : _avatarColor), rect);
                string initials = GetInitials(DisplayName);
                using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                float fontSize = size * 0.35f;
                e.Graphics.DrawString(initials, TG.FontSemiBold(fontSize), Brushes.White, rect, sf);
            }

            // Online dot
            if (ShowOnline)
            {
                int dotSize = Math.Max(8, size / 5);
                int dotX = size - dotSize;
                int dotY = size - dotSize;
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(0xFF, 0xFF, 0xFF)), dotX - 1, dotY - 1, dotSize + 2, dotSize + 2);
                e.Graphics.FillEllipse(new SolidBrush(Color.FromArgb(0x4D, 0xD9, 0x64)), dotX, dotY, dotSize, dotSize);
            }
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";
            var parts = name.Trim().Split(' ');
            if (parts.Length >= 2) return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return name.Length >= 2 ? name.Substring(0, 2).ToUpper() : name.ToUpper();
        }
    }

    // ─────────────────────────────────────────────
    // TELEGRAM TEXTBOX - input có bo góc + placeholder
    // ─────────────────────────────────────────────
    public class TelegramTextBox : Panel
    {
        private TextBox _tb;
        private string _placeholder;
        private bool _isFocused;
        private Label _placeholderLabel;

        public string Text { get => _tb.Text; set => _tb.Text = value; }
        public char PasswordChar { get => _tb.PasswordChar; set => _tb.PasswordChar = value; }
        public bool Multiline { get => _tb.Multiline; set => _tb.Multiline = value; }
        public event EventHandler TextChanged { add => _tb.TextChanged += value; remove => _tb.TextChanged -= value; }

        public TelegramTextBox()
        {
            Height = 44;
            BackColor = TG.InputBg;
            DoubleBuffered = true;
            Padding = new Padding(12, 0, 12, 0);

            _tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = TG.FontRegular(10f),
                ForeColor = TG.TextPrimary,
                BackColor = TG.InputBg,
                Dock = DockStyle.Fill,
                TabStop = true,
            };

            _placeholderLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Font = TG.FontRegular(10f),
                ForeColor = TG.TextHint,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(12, 0, 0, 0),
                Cursor = Cursors.IBeam,
            };
            _placeholderLabel.Click += (s, e) => _tb.Focus();

            Controls.Add(_tb);
            Controls.Add(_placeholderLabel);
            _placeholderLabel.BringToFront();

            _tb.GotFocus += (s, e) => { _isFocused = true; Invalidate(); UpdatePlaceholder(); };
            _tb.LostFocus += (s, e) => { _isFocused = false; Invalidate(); UpdatePlaceholder(); };
            _tb.TextChanged += (s, e) => UpdatePlaceholder();

            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw, true);
        }

        public void SetPlaceholder(string text)
        {
            _placeholder = text;
            _placeholderLabel.Text = text;
        }

        private void UpdatePlaceholder()
        {
            _placeholderLabel.Visible = string.IsNullOrEmpty(_tb.Text);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = RoundedPanel.GetRoundedPath(rect, TG.RadiusSmall);
            e.Graphics.FillPath(new SolidBrush(TG.InputBg), path);
            Color borderColor = _isFocused ? TG.InputFocused : TG.InputBorder;
            float borderWidth = _isFocused ? 2f : 1f;
            e.Graphics.DrawPath(new Pen(borderColor, borderWidth), path);
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            int pad = 12;

            // Thêm dòng này để tránh null crash
            if (_tb == null) return;

            int tbHeight = _tb.PreferredHeight;
            _tb.SetBounds(pad, (Height - tbHeight) / 2, Width - pad * 2, tbHeight);
            _placeholderLabel.SetBounds(0, 0, Width, Height);
        }
    }

    // ─────────────────────────────────────────────
    // TELEGRAM HEADER - thanh trên cùng màu xanh
    // ─────────────────────────────────────────────
    public class TelegramHeader : Panel
    {
        private Label _lblTitle;
        private Label _lblSubtitle;
        private Button _btnBack;
        private AvatarControl _avatar;
        private Panel _rightPanel;

        public string Title { get => _lblTitle.Text; set => _lblTitle.Text = value; }
        public string Subtitle { get => _lblSubtitle.Text; set { _lblSubtitle.Text = value; _lblSubtitle.Visible = !string.IsNullOrEmpty(value); } }
        public bool ShowBack
        {
            get => _btnBack.Visible;
            set
            {
                _btnBack.Visible = value;
                PerformLayout(); // ← thay OnResize(null) bằng cái này
            }
        }
        public event EventHandler BackClicked { add => _btnBack.Click += value; remove => _btnBack.Click -= value; }

        public TelegramHeader()
        {
            Height = 52;
            BackColor = TG.TitleBarBg;
            Dock = DockStyle.Top;

            _btnBack = new Button
            {
                Text = "←",
                Font = TG.FontRegular(14f),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 52),
                Location = new Point(0, 0),
                Cursor = Cursors.Hand,
                Visible = false,
                TextAlign = ContentAlignment.MiddleCenter,
                BackColor = Color.Transparent,
            };
            _btnBack.FlatAppearance.BorderSize = 0;

            _avatar = new AvatarControl { Size = new Size(36, 36), Location = new Point(50, 8), Visible = false };

            _lblTitle = new Label
            {
                AutoSize = false,
                Height = 28,
                Font = TG.FontSemiBold(11f),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.BottomLeft,
            };
            _lblSubtitle = new Label
            {
                AutoSize = false,
                Height = 18,
                Font = TG.FontRegular(8.5f),
                ForeColor = TG.TitleBarSub,
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.TopLeft,
                Visible = false,
            };

            _rightPanel = new Panel { BackColor = Color.Transparent, Width = 80 };

            Controls.AddRange(new Control[] { _btnBack, _avatar, _lblTitle, _lblSubtitle, _rightPanel });
        }

        protected override void OnLayout(LayoutEventArgs e)
        {
            base.OnLayout(e);
            if (_btnBack == null || _lblTitle == null) return;

            int leftX = _btnBack.Visible ? 44 : 12;
            if (_avatar != null && _avatar.Visible)
            {
                _avatar.Location = new Point(leftX, 8);
                leftX += 44;
            }
            _lblTitle.SetBounds(leftX,
                (_lblSubtitle != null && _lblSubtitle.Visible) ? 6 : 12,
                Width - leftX - 90, 28);
            if (_lblSubtitle != null)
                _lblSubtitle.SetBounds(leftX, 30, Width - leftX - 90, 18);
            if (_rightPanel != null)
                _rightPanel.SetBounds(Width - 90, 0, 90, Height);
        }

        public void SetAvatar(string name) { _avatar.SetName(name); _avatar.Visible = true; OnResize(null); }
        public void AddRightButton(string text, EventHandler onClick)
        {
            var btn = new Button
            {
                Text = text,
                Font = TG.FontRegular(14f),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(36, 36),
                Location = new Point(_rightPanel.Width - 40, 8),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent,
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Click += onClick;
            _rightPanel.Controls.Add(btn);
        }
    }

    // ─────────────────────────────────────────────
    // UNREAD BADGE - chấm tròn số tin chưa đọc
    // ─────────────────────────────────────────────
    public class UnreadBadge : Control
    {
        private int _count;
        public int Count { get => _count; set { _count = value; Invalidate(); Visible = value > 0; } }
        public bool IsMuted { get; set; } = false;

        public UnreadBadge()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.ResizeRedraw |
                     ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
            Size = new Size(22, 22);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            string text = _count > 99 ? "99+" : _count.ToString();
            using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            int w = Math.Max(22, text.Length * 8 + 8);
            var rect = new Rectangle(Width - w, 0, w, 20);
            Color bg = IsMuted ? TG.BadgeMuted : TG.BadgeBg;
            e.Graphics.FillEllipse(new SolidBrush(bg), rect);
            e.Graphics.DrawString(text, TG.FontSemiBold(7.5f), Brushes.White, rect, sf);
        }
    }
}
