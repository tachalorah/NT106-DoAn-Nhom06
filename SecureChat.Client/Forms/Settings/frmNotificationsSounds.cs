using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace SecureChat.Client.Forms.Settings
{
    public class frmNotificationsSounds : Form
    {
        private static readonly Color C_BG = Color.FromArgb(0x14, 0x1D, 0x27);
        private static readonly Color C_TEXT = Color.FromArgb(0xF5, 0xF5, 0xF5);
        private static readonly Color C_SUB = Color.FromArgb(0x89, 0x9A, 0xB4);
        private static readonly Color C_ACCENT = Color.FromArgb(0x2A, 0xAB, 0xEE);
        private static readonly Color C_SEPARATOR = Color.FromArgb(0x22, 0x2F, 0x3C);

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
            Font = new Font("Segoe UI", 10.5f);
            DoubleBuffered = true;

            var btnBack = FlatButton("<< Back");
            btnBack.Location = new Point(12, 12);
            btnBack.Click += (_, __) => Close();

            var btnClose = FlatButton("X");
            btnClose.Location = new Point(ClientSize.Width - btnClose.Width - 12, 12);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (_, __) => Close();

            var lblTitle = new Label
            {
                Text = "Notifications and Sounds",
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI Semibold", 12.5f),
                AutoSize = true,
                Location = new Point(18, 48),
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
                Font = new Font("Segoe UI Semibold", 11f),
                AutoSize = true,
                Location = new Point(18, y + 12),
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
                Image = LoadIcon(iconFile),
                BackColor = Color.Transparent,
            };
            var lbl = new Label
            {
                Text = text,
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 10.5f),
                AutoSize = true,
                Location = new Point(leftPad + 26, 14),
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
                var sep = new Panel { Location = new Point(leftPad, y), Size = new Size(parent.Width - leftPad - rightPad, 1), BackColor = C_SEPARATOR, Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top };
                parent.Controls.Add(sep);
                y += 10;
            }
        }

        private void AddVolume(Control parent, ref int y)
        {
            var leftPad = parent.Padding.Left;
            var rightPad = parent.Padding.Right;
            int volumeY = y; // Capture y in a local variable for use in the lambda

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
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 10.5f),
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            void PositionVal()
            {
                lblVal.Location = new Point(parent.Width - rightPad - lblVal.Width, volumeY + 8);
            }
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
                FlatStyle = FlatStyle.Flat,
            };
            chk.FlatAppearance.BorderSize = 0;
            chk.Paint += (s, e) => DrawToggle(chk, e.Graphics);
            return chk;
        }

        private void DrawToggle(CheckBox chk, Graphics g)
        {
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, chk.Width - 1, chk.Height - 1);
            int r = rect.Height / 2;
            var track = chk.Checked ? C_ACCENT : Color.FromArgb(0x55, 0x65, 0x78);
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
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                TabStop = false,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
                Padding = new Padding(6, 2, 6, 2),
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 255, 255, 255);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 255, 255, 255);
            return b;
        }

        private static Image? LoadIcon(string fileName)
        {
            try
            {
                var searchPaths = new[]
                {
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "profile", fileName),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "settings", fileName),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "menu", fileName),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "limits", fileName),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", fileName)
                };
                foreach (var path in searchPaths)
                {
                    if (File.Exists(path))
                    {
                        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                        using var img = Image.FromStream(fs);
                        return new Bitmap(img);
                    }
                }
                return null;
            }
            catch
            {
                return null;
            }
        }
    }
}
