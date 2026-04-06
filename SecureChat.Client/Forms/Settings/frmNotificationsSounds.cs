using System;
using System.Drawing;
using System.Windows.Forms;

namespace SecureChat.Client.Forms.Settings
{
    public class frmNotificationsSounds : Form
    {
        private static readonly Color C_BG = Color.White;
        private static readonly Color C_TEXT = Color.FromArgb(0x1F, 0x2D, 0x3D);
        private static readonly Color C_SUB = Color.FromArgb(0x7A, 0x8A, 0x99);
        private static readonly Color C_ACCENT = Color.FromArgb(0x33, 0x99, 0xFF);
        private static readonly Color C_SEPARATOR = Color.FromArgb(0xE8, 0xEC, 0xF1);

        private TrackBar _volume = null!;

        public frmNotificationsSounds()
        {
            InitializeComponent();
            BuildUI();
        }

        private void InitializeComponent() { }

        private void BuildUI()
        {
            Text = "Notifications and Sounds";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            HelpButton = false;
            ControlBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 740);
            BackColor = C_BG;
            Font = TG.FontRegular(10.5f);
            DoubleBuffered = true;

            var btnBack = FlatButton("←");
            btnBack.Location = new Point(12, 12);
            btnBack.Click += (_, __) => Close();

            var btnClose = FlatButton("✕");
            btnClose.Location = new Point(ClientSize.Width - btnClose.Width - 12, 12);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (_, __) => Close();

            var lblTitle = new Label
            {
                Text = "Notifications and Sounds",
                ForeColor = C_TEXT,
                Font = TG.FontSemiBold(12.5f),
                AutoSize = true,
                Location = new Point(18, 48)
            };

            var container = new Panel
            {
                Location = new Point(12, 80),
                Size = new Size(ClientSize.Width - 24, ClientSize.Height - 92),
                AutoScroll = true,
                BackColor = C_BG,
                Padding = new Padding(12, 0, 12, 12),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            int y = 0;
            AddSectionHeader(container, "Global settings", ref y);
            AddToggle(container, "Desktop notifications", "notifications.png", ref y);
            AddToggle(container, "Flash the taskbar icon", "notifications.png", ref y);
            AddToggle(container, "Allow sound", "volume_mute.png", ref y);

            AddSectionHeader(container, "Volume", ref y);
            AddVolume(container, ref y);

            AddSectionHeader(container, "Notifications for chats", ref y);
            AddToggle(container, "Private chats", "mode_messages.png", ref y, true);
            AddToggle(container, "Groups", "stories_to_chats.png", ref y, true);
            AddToggle(container, "Channels", "show_in_chat.png", ref y, true);

            AddSectionHeader(container, "Events", ref y);
            AddToggle(container, "Contact joined", "upload_chat_photo.png", ref y, true);
            AddToggle(container, "Pinned messages", "saved_messages.png", ref y, true);

            Controls.AddRange(new Control[] { btnBack, btnClose, lblTitle, container });
        }

        private void AddSectionHeader(Control parent, string text, ref int y)
        {
            var lbl = new Label
            {
                Text = text,
                ForeColor = C_TEXT,
                Font = TG.FontSemiBold(11f),
                AutoSize = true,
                Location = new Point(18, y + 12)
            };
            parent.Controls.Add(lbl);
            y += 32;
        }

        private void AddToggle(Control parent, string text, string iconFile, ref int y, bool withDivider = false)
        {
            var leftPad = parent.Padding.Left;
            var rightPad = parent.Padding.Right;
            var panel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(parent.Width - leftPad - rightPad, 48),
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            var icon = new PictureBox
            {
                Size = new Size(22, 22),
                Location = new Point(leftPad, 13),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = SettingsGlyphIcons.Create(iconFile, 22),
                BackColor = Color.Transparent
            };

            var lbl = new Label
            {
                Text = text,
                ForeColor = C_TEXT,
                Font = TG.FontRegular(10.5f),
                AutoSize = true,
                Location = new Point(leftPad + 30, 14)
            };

            var toggle = CreateToggle();
            toggle.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            panel.Resize += (_, __) => toggle.Location = new Point(panel.Width - rightPad - toggle.Width, (panel.Height - toggle.Height) / 2);
            toggle.Location = new Point(panel.Width - rightPad - toggle.Width, (panel.Height - toggle.Height) / 2);

            panel.Controls.AddRange(new Control[] { icon, lbl, toggle });
            parent.Controls.Add(panel);
            y += panel.Height;

            if (withDivider)
            {
                var sep = new Panel
                {
                    Location = new Point(leftPad, y),
                    Size = new Size(parent.Width - leftPad - rightPad, 1),
                    BackColor = C_SEPARATOR,
                    Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
                };
                parent.Controls.Add(sep);
                y += 10;
            }
        }

        private void AddVolume(Control parent, ref int y)
        {
            var leftPad = parent.Padding.Left;
            var rightPad = parent.Padding.Right;
            int volumeY = y;

            _volume = new TrackBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = 100,
                TickStyle = TickStyle.None,
                SmallChange = 1,
                LargeChange = 5,
                BackColor = C_BG,
                Size = new Size(parent.Width - leftPad - rightPad - 80, 30),
                Location = new Point(leftPad, volumeY + 6),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var lblVal = new Label
            {
                Text = "100%",
                ForeColor = C_SUB,
                Font = TG.FontRegular(10.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };

            void PositionVal() => lblVal.Location = new Point(parent.Width - rightPad - lblVal.Width, volumeY + 8);

            PositionVal();
            parent.Resize += (_, __) => PositionVal();
            _volume.ValueChanged += (_, __) => { lblVal.Text = _volume.Value + "%"; PositionVal(); };

            parent.Controls.Add(_volume);
            parent.Controls.Add(lblVal);
            y += 42;
        }

        private CheckBox CreateToggle()
        {
            var chk = new CheckBox
            {
                Appearance = Appearance.Button,
                AutoSize = false,
                Size = new Size(44, 22),
                BackColor = Color.Transparent,
                Checked = true,
                FlatStyle = FlatStyle.Flat
            };
            chk.FlatAppearance.BorderSize = 0;
            chk.Paint += (_, e) => DrawToggle(chk, e.Graphics);
            return chk;
        }

        private void DrawToggle(CheckBox chk, Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, chk.Width - 1, chk.Height - 1);
            int r = rect.Height / 2;
            var track = chk.Checked ? C_ACCENT : Color.FromArgb(0xC7, 0xD2, 0xDE);

            using var trackBrush = new SolidBrush(track);
            using var thumbBrush = new SolidBrush(Color.White);

            g.FillEllipse(trackBrush, rect.Left, rect.Top, rect.Height, rect.Height);
            g.FillEllipse(trackBrush, rect.Right - rect.Height, rect.Top, rect.Height, rect.Height);
            g.FillRectangle(trackBrush, rect.Left + r, rect.Top, rect.Width - rect.Height, rect.Height);

            int thumbX = chk.Checked ? rect.Right - rect.Height + 2 : rect.Left + 2;
            g.FillEllipse(thumbBrush, thumbX, rect.Top + 2, rect.Height - 4, rect.Height - 4);
        }

        private static Button FlatButton(string text)
        {
            var b = new Button
            {
                Text = text,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = C_SUB,
                Font = TG.FontSemiBold(11f),
                TabStop = false,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                Padding = new Padding(6, 2, 6, 2)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = TG.SidebarHover;
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(0xEA, 0xEA, 0xEA);
            return b;
        }
    }
}
