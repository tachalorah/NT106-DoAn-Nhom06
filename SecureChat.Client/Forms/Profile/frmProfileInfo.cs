using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SecureChat.Client.Models;
using System.IO;

namespace SecureChat.Client.Forms.Profile
{
    public partial class frmProfileInfo : Form
    {
        private void InitializeComponent() { /* built in code */ }

        private static readonly Color C_BG = Color.FromArgb(0x14, 0x1D, 0x27);
        private static readonly Color C_TEXT = Color.FromArgb(0xF5, 0xF5, 0xF5);
        private static readonly Color C_SUB = Color.FromArgb(0x89, 0x9A, 0xB4);
        private static readonly Color C_ACCENT = Color.FromArgb(0x2A, 0xAB, 0xEE);
        private static readonly Color C_BORDER = Color.FromArgb(0x22, 0x2F, 0x3C);

        private readonly ProfileModel _profile;

        private PictureBox _avatar = null!;
        private Label _lblInitial = null!;
        private Label _lblName = null!;
        private Label _lblStatus = null!;
        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtUsername = null!;
        private DateTimePicker _dtBirthday = null!;
        private Label _lblError = null!;
        private Button _btnBack = null!;
        private Button _btnClose = null!;

        public frmProfileInfo(ProfileModel profile)
        {
            _profile = profile ?? throw new ArgumentNullException(nameof(profile));
            InitializeComponent();
            BuildUI();
            LoadProfile(profile);
            Resize += (_, __) => LayoutDynamic();
            LayoutDynamic();
        }

        private void BuildUI()
        {
            Text = "Info";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            HelpButton = false;
            ControlBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(540, 720);
            BackColor = C_BG;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;

            _btnBack = FlatIconButton("<< Back");
            _btnBack.Image = null;
            _btnBack.TextImageRelation = TextImageRelation.Overlay;
            _btnBack.Click += (_, __) => Close();

            _btnClose = FlatIconButton("X");
            _btnClose.Image = null;
            _btnClose.TextImageRelation = TextImageRelation.Overlay;
            _btnClose.Click += (_, __) => Close();

            _avatar = new PictureBox
            {
                Size = new Size(120, 120),
                BackColor = Color.FromArgb(0xFF, 0x6B, 0x81),
                SizeMode = PictureBoxSizeMode.Zoom,
            };
            _avatar.Paint += (_, __) => ClipCircle(_avatar);

            _lblInitial = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 30f),
                BackColor = Color.Transparent,
            };
            _avatar.Controls.Add(_lblInitial);

            _lblName = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI Semibold", 13f),
                ForeColor = C_TEXT,
                BackColor = Color.Transparent,
                Location = new Point((ClientSize.Width / 2) - 60, 170),
            };

            _lblStatus = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = C_ACCENT,
                BackColor = Color.Transparent,
                Location = new Point((ClientSize.Width / 2) - 20, 195),
            };

            int fieldTop = 240;
            var nameField = InputField("Name", fieldTop);
            fieldTop += 74;
            var phoneField = InputField("Phone number", fieldTop);
            fieldTop += 74;
            var userField = InputField("t.me/username", fieldTop);
            fieldTop += 74;

            _dtBirthday = new DateTimePicker
            {
                Location = new Point(26, fieldTop + 26),
                Size = new Size(ClientSize.Width - 52, 32),
                Font = new Font("Segoe UI", 10.5f),
                CalendarForeColor = C_TEXT,
                CalendarMonthBackground = Color.White,
                CalendarTitleBackColor = Color.White,
                CalendarTitleForeColor = C_TEXT,
                CalendarTrailingForeColor = Color.Gray,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "dd/MM/yyyy",
                ShowUpDown = false,
            };
            var lblBirth = new Label
            {
                Text = "Birthday",
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI Semibold", 10.5f),
                AutoSize = true,
                Location = new Point(26, fieldTop),
                BackColor = Color.Transparent,
            };
            fieldTop += 70;

            _lblError = new Label
            {
                ForeColor = Color.OrangeRed,
                Font = new Font("Segoe UI", 9.5f),
                AutoSize = false,
                Size = new Size(ClientSize.Width - 52, 40),
                Location = new Point(26, fieldTop),
                BackColor = Color.Transparent,
            };
            fieldTop += 50;

            var btnSave = new Button
            {
                Text = "Save",
                Size = new Size(110, 34),
                Location = new Point(ClientSize.Width - 26 - 110, fieldTop),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(0x2F, 0x6A, 0xC1),
                ForeColor = Color.White,
                Font = new Font("Segoe UI Semibold", 10.5f),
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (_, __) => SaveProfile();

            Controls.AddRange(new Control[]
            {
                _btnBack, _btnClose, _avatar, _lblName, _lblStatus,
                nameField.Label, nameField.TextBox, nameField.Underline,
                phoneField.Label, phoneField.TextBox, phoneField.Underline,
                userField.Label, userField.TextBox, userField.Underline,
                lblBirth, _dtBirthday, _lblError, btnSave,
            });

            _txtName = nameField.TextBox;
            _txtPhone = phoneField.TextBox;
            _txtUsername = userField.TextBox;
        }

        private (Label Label, TextBox TextBox, Panel Underline) InputField(string label, int top)
        {
            var lbl = new Label
            {
                Text = label,
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI Semibold", 10.5f),
                AutoSize = true,
                Location = new Point(26, top),
                BackColor = Color.Transparent,
            };

            var txt = new TextBox
            {
                Location = new Point(26, top + 24),
                Size = new Size(ClientSize.Width - 52, 28),
                Font = new Font("Segoe UI", 10.5f),
                ForeColor = C_TEXT,
                BackColor = C_BG,
                BorderStyle = BorderStyle.None,
            };

            var underline = new Panel
            {
                Location = new Point(txt.Left, txt.Bottom + 2),
                Size = new Size(txt.Width, 1),
                BackColor = C_BORDER,
            };

            txt.GotFocus += (_, __) => underline.BackColor = C_ACCENT;
            txt.LostFocus += (_, __) => underline.BackColor = C_BORDER;

            return (lbl, txt, underline);
        }

        private void LoadProfile(ProfileModel profile)
        {
            _txtName.Text = profile.FullName;
            _txtPhone.Text = profile.PhoneNumber;
            _txtUsername.Text = profile.Username;
            if (profile.Birthday.HasValue)
                _dtBirthday.Value = profile.Birthday.Value;
            else
                _dtBirthday.Value = DateTime.Today;

            _lblName.Text = profile.FullName;
            _lblStatus.Text = profile.StatusText;
            _lblInitial.Text = GetInitials(profile.FullName);
            LayoutDynamic();
        }

        private void LayoutDynamic()
        {
            int centerX = ClientSize.Width / 2;

            _btnClose.Location = new Point(ClientSize.Width - _btnClose.Width - 14, 12);
            _btnBack.Location = new Point(14, 12);

            _avatar.Location = new Point(centerX - _avatar.Width / 2, 52);
            _lblName.Location = new Point(centerX - (_lblName.PreferredWidth / 2), _avatar.Bottom + 12);
            _lblStatus.Location = new Point(centerX - (_lblStatus.PreferredWidth / 2), _lblName.Bottom + 2);
        }

        private void SaveProfile()
        {
            try
            {
                _lblError.Text = string.Empty;
                var name = _txtName.Text.Trim();
                var phone = _txtPhone.Text.Trim();
                var username = _txtUsername.Text.Trim().TrimStart('@');
                if (string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException("Name is required.");
                if (!string.IsNullOrEmpty(phone) && !Regex.IsMatch(phone, "^\\+?[0-9 ]{6,20}$"))
                    throw new InvalidOperationException("Phone number is not valid.");
                if (!string.IsNullOrEmpty(username) && !Regex.IsMatch(username, "^[a-zA-Z0-9_]{5,32}$"))
                    throw new InvalidOperationException("Username must be 5-32 chars [a-zA-Z0-9_].");

                _profile.FullName = name;
                _profile.PhoneNumber = phone;
                _profile.Username = username;
                _profile.Birthday = _dtBirthday.Value;

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                _lblError.Text = ex.Message;
            }
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
            var enumerator = System.Globalization.StringInfo.GetTextElementEnumerator(text);
            return enumerator.MoveNext() ? enumerator.GetTextElement() : string.Empty;
        }

        private static void ClipCircle(PictureBox pb)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, pb.Width, pb.Height);
            pb.Region = new Region(path);
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
                var path = Path.Combine(AppContext.BaseDirectory, "Resources", "Icons", "profile", fileName);
                if (!File.Exists(path)) return null;
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var img = Image.FromStream(fs);
                return new Bitmap(img);
            }
            catch
            {
                return null;
            }
        }
    }
}
