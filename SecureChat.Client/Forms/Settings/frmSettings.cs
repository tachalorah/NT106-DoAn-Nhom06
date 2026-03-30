using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Windows.Forms;
using SecureChat.Client.Forms.Profile;
using SecureChat.Client.Models;

namespace SecureChat.Client.Forms.Settings
{
    public class frmSettings : Form
    {
        private static readonly Color C_BG = Color.FromArgb(0x17, 0x21, 0x2B);
        private static readonly Color C_HOVER = Color.FromArgb(0x20, 0x2B, 0x36);
        private static readonly Color C_TEXT = Color.White;
        private static readonly Color C_SUB = Color.Gray;
        private static readonly Color C_ACCENT = Color.FromArgb(0x2A, 0xAB, 0xEE);
        private static readonly int ITEM_HEIGHT = 52;

        private readonly ProfileModel _profile;
        private Panel _root = null!;
        private Label _lblName = null!;
        private Label _lblPhone = null!;
        private LinkLabel _linkUsername = null!;
        private Panel _avatarPanel = null!;

        public frmSettings(ProfileModel profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            InitializeComponent();
            BuildUI();
        }

        private void InitializeComponent() { }

        private void BuildUI()
        {
            Text = "Settings";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            HelpButton = false;
            ControlBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(520, 740);
            BackColor = C_BG;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;

            _root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = C_BG,
                AutoScroll = true,
                Padding = new Padding(0, 0, 0, 12)
            };
            Controls.Add(_root);

            int y = 0;
            y = BuildHeader(y);
            y += 8;

            AddMenuItem(ref y, "My Account", "my_account.png", OpenProfile);
            AddMenuItem(ref y, "Notifications and Sounds", "notifications.png", OpenNotifications);
            AddMenuItem(ref y, "Privacy and Security", "privacy.png", ShowPending);
            AddMenuItem(ref y, "Chat Settings", "chat.png", ShowPending);
            AddMenuItem(ref y, "Folders", "folders.png", ShowPending);
            AddMenuItem(ref y, "Advanced", "advanced.png", ShowPending);
            AddMenuItem(ref y, "Speakers and Camera", "devices.png", ShowPending);
            AddMenuItem(ref y, "Language", "language.png", ShowPending, true, "English");
        }

        private int BuildHeader(int y)
        {
            var header = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ClientSize.Width, 120),
                BackColor = C_BG,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            var btnClose = FlatIconButton("X");
            btnClose.Location = new Point(header.Width - btnClose.Width - 12, 12);
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.Click += (_, __) => Close();

            // no menu / ellipsis
            _avatarPanel = new Panel
            {
                Size = new Size(64, 64),
                Location = new Point(16, 40),
                BackColor = Color.FromArgb(0xEE, 0x68, 0x53)
            };
            _avatarPanel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var path = new GraphicsPath();
                path.AddEllipse(0, 0, _avatarPanel.Width, _avatarPanel.Height);
                _avatarPanel.Region = new Region(path);
                using var f = new Font("Segoe UI Semibold", 18f);
                var initials = GetInitials(_profile.FullName);
                var sz = e.Graphics.MeasureString(initials, f);
                e.Graphics.DrawString(initials, f, Brushes.White, (_avatarPanel.Width - sz.Width) / 2, (_avatarPanel.Height - sz.Height) / 2);
            };

            _lblName = new Label
            {
                Font = new Font("Segoe UI Semibold", 12f),
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(16 + 64 + 12, 36),
                BackColor = Color.Transparent
            };
            _lblPhone = new Label
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = C_SUB,
                AutoSize = true,
                Location = new Point(_lblName.Left, 58),
                BackColor = Color.Transparent
            };
            _linkUsername = new LinkLabel
            {
                Font = new Font("Segoe UI", 10f),
                LinkColor = C_ACCENT,
                ActiveLinkColor = C_ACCENT,
                VisitedLinkColor = C_ACCENT,
                AutoSize = true,
                Location = new Point(_lblName.Left, 76),
                BackColor = Color.Transparent
            };

            header.Controls.AddRange(new Control[] { btnClose, _avatarPanel, _lblName, _lblPhone, _linkUsername });
            _root.Controls.Add(header);
            RefreshHeader();
            return header.Bottom;
        }

        private void AddMenuItem(ref int y, string text, string iconFile, Action onClick, bool showExtraText = false, string extraText = "")
        {
            var panel = new Panel
            {
                Location = new Point(0, y),
                Size = new Size(ClientSize.Width, ITEM_HEIGHT),
                BackColor = C_BG,
                Cursor = Cursors.Hand,
                Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top
            };

            panel.MouseEnter += (_, __) => panel.BackColor = C_HOVER;
            panel.MouseLeave += (_, __) => panel.BackColor = C_BG;
            panel.Click += (_, __) => onClick();

            var icon = new PictureBox
            {
                Size = new Size(24, 24),
                Location = new Point(16, (ITEM_HEIGHT - 24) / 2),
                SizeMode = PictureBoxSizeMode.Zoom,
                Image = LoadIcon(iconFile),
                BackColor = Color.Transparent
            };
            icon.MouseEnter += (_, __) => panel.BackColor = C_HOVER;
            icon.MouseLeave += (_, __) => panel.BackColor = C_BG;
            icon.Click += (_, __) => onClick();

            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = C_TEXT,
                AutoSize = true,
                Location = new Point(52, (ITEM_HEIGHT - 20) / 2),
                BackColor = Color.Transparent
            };
            lbl.MouseEnter += (_, __) => panel.BackColor = C_HOVER;
            lbl.MouseLeave += (_, __) => panel.BackColor = C_BG;
            lbl.Click += (_, __) => onClick();

            panel.Controls.Add(icon);
            panel.Controls.Add(lbl);

            if (showExtraText)
            {
                var trailing = new Label
                {
                    Text = extraText,
                    Font = new Font("Segoe UI", 10f),
                    ForeColor = C_ACCENT,
                    AutoSize = true,
                    BackColor = Color.Transparent
                };
                trailing.MouseEnter += (_, __) => panel.BackColor = C_HOVER;
                trailing.MouseLeave += (_, __) => panel.BackColor = C_BG;
                trailing.Click += (_, __) => onClick();
                panel.Controls.Add(trailing);
                panel.Resize += (_, __) => trailing.Location = new Point(panel.Width - trailing.Width - 16, (ITEM_HEIGHT - trailing.Height) / 2);
                trailing.Location = new Point(panel.Width - trailing.Width - 16, (ITEM_HEIGHT - trailing.Height) / 2);
            }

            var sep = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(34, 46, 58)
            };
            panel.Controls.Add(sep);

            _root.Controls.Add(panel);
            y += ITEM_HEIGHT;
        }

        private void OpenProfile()
        {
            using var dlg = new frmProfileInfo(_profile);
            dlg.StartPosition = FormStartPosition.CenterParent;
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                RefreshHeader();
            }
        }

        private void OpenNotifications()
        {
            using var dlg = new frmNotificationsSounds();
            dlg.StartPosition = FormStartPosition.CenterParent;
            dlg.ShowDialog(this);
        }

        private void ShowPending()
        {
            MessageBox.Show(this, "Feature coming soon", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private static Button FlatIconButton(string text)
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
                Padding = new Padding(6, 2, 6, 2)
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 255, 255, 255);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 255, 255, 255);
            return b;
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "?";
            var parts = name.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                var first = GetFirstGrapheme(parts[0]);
                var second = parts[0].Length > first.Length ? GetFirstGrapheme(parts[0].Substring(first.Length)) : string.Empty;
                return (first + second).ToUpperInvariant();
            }
            var firstWord = GetFirstGrapheme(parts[0]);
            var lastWord = GetFirstGrapheme(parts[^1]);
            return (firstWord + lastWord).ToUpperInvariant();
        }

        private static string GetFirstGrapheme(string text)
        {
            var e = System.Globalization.StringInfo.GetTextElementEnumerator(text);
            return e.MoveNext() ? e.GetTextElement() : string.Empty;
        }

        private static Image? LoadIcon(string fileName)
        {
            try
            {
                string mapped = fileName switch
                {
                    "my_account.png" => "profile_manage.png",
                    "notifications.png" => "notifications.png",
                    "privacy.png" => "lock.png",
                    "chat.png" => "chat.png",
                    "folders.png" => "folders.png",
                    "advanced.png" => "settings.png",
                    "devices.png" => "devices.png",
                    "language.png" => "language.png",
                    _ => fileName
                };

                var searchPaths = new[]
                {
                    Path.Combine(Application.StartupPath, "Resources", "Icon", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icon", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "profile", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "settings", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "menu", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "limits", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", mapped),
                    Path.Combine(AppContext.BaseDirectory, "Resources", mapped)
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
            }
            catch { }
            return null;
        }

        private void RefreshHeader()
        {
            _lblName.Text = _profile.FullName;
            _lblPhone.Text = _profile.PhoneNumber;
            _linkUsername.Text = string.IsNullOrWhiteSpace(_profile.Username) ? "Add username" : _profile.Username;
            _avatarPanel.Invalidate();
        }
    }
}
