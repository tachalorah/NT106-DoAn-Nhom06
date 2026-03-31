using SecureChat.Client.Components.Group;
using System.Drawing.Drawing2D;

namespace SecureChat.Client.Forms.Chat
{
    public partial class frmGroupInfo : Form
    {
        private void InitializeComponent()
        {
            // Built fully in code
        }

        private const int FORM_WIDTH = 520;
        private const int FORM_HEIGHT = 660;
        private const int HEADER_HEIGHT = 180;
        private const int SECTION_PAD = 16;
        private const int NOTIFY_HEIGHT = 56;
        private const int MEMBERS_HEADER_HEIGHT = 40;
        private const int BOTTOM_HEIGHT = 56;
        private int ListTop => HEADER_HEIGHT + NOTIFY_HEIGHT + MEMBERS_HEADER_HEIGHT + 12;

        private const string GLYPH_MORE = "\uE712";
        private const string GLYPH_BELL = "\uE7ED";
        private const string GLYPH_PEOPLE = "\uE716";
        private const string GLYPH_ADD = "\uE710";

        private static readonly Color C_BG = Color.FromArgb(0x14, 0x1D, 0x27);
        private static readonly Color C_TEXT = Color.FromArgb(0xF5, 0xF5, 0xF5);
        private static readonly Color C_SUBTEXT = Color.FromArgb(0x89, 0x9A, 0xB4);
        private static readonly Color C_ACCENT = Color.FromArgb(0x2A, 0xAB, 0xEE);
        private static readonly Color C_SEPARATOR = Color.FromArgb(0x22, 0x2F, 0x3C);

        private Label _lblName;
        private Label _lblCount;
        private Label _lblMembersTitle;
        private PictureBox _pbAvatar;
        private Panel _pnlList;
        private CheckBox _chkNotify;
        private Button _btnAdd;

        public event Action? AddMemberRequested;
        public event Action<bool>? NotificationsChanged;

        public frmGroupInfo()
        {
            InitializeComponent();
            BuildUI();
            LoadSample();
        }

        private void BuildUI()
        {
            Text = "Group Info";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            HelpButton = false;
            ControlBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(FORM_WIDTH, FORM_HEIGHT);
            BackColor = C_BG;
            Font = new Font("Segoe UI", 10f);
            DoubleBuffered = true;
            Padding = new Padding(0, 0, 0, 0);

            BuildHeader();
            BuildNotify();
            BuildMembers();
            BuildBottom();
        }

        private void BuildHeader()
        {
            var pnl = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(FORM_WIDTH, HEADER_HEIGHT),
                BackColor = C_BG,
            };

            var lblTitle = new Label
            {
                Text = "Group Info",
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI Semibold", 12.5f),
                AutoSize = true,
                Location = new Point(SECTION_PAD, SECTION_PAD),
                BackColor = Color.Transparent,
            };

            var btnMore = FlatIconButton(GLYPH_MORE, "Segoe MDL2 Assets", 14f);
            btnMore.Location = new Point(FORM_WIDTH - SECTION_PAD - btnMore.Width - 32, SECTION_PAD - 2);

            // Close button
            var btnClose = FlatIconButton("×", "Segoe UI", 12.5f);
            btnClose.Location = new Point(FORM_WIDTH - SECTION_PAD - btnClose.Width, SECTION_PAD - 2);
            btnClose.Click += (_, __) => Close();

            _pbAvatar = new PictureBox
            {
                Size = new Size(84, 84),
                Location = new Point(SECTION_PAD, 56),
                BackColor = Color.FromArgb(0xFF, 0x6B, 0x81),
                SizeMode = PictureBoxSizeMode.Zoom,
            };
            _pbAvatar.SizeChanged += (_, __) => ClipCircle(_pbAvatar);
            _pbAvatar.Paint += (_, __) => ClipCircle(_pbAvatar);

            _lblName = new Label
            {
                Text = "Group Name",
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI Semibold", 13f),
                AutoSize = true,
                Location = new Point(SECTION_PAD + 96, 62),
                BackColor = Color.Transparent,
            };

            _lblCount = new Label
            {
                Text = "0 members",
                ForeColor = C_SUBTEXT,
                Font = new Font("Segoe UI", 10.5f),
                AutoSize = true,
                Location = new Point(SECTION_PAD + 96, 92),
                BackColor = Color.Transparent,
            };

            var sep = Separator(HEADER_HEIGHT - 6);

            pnl.Controls.AddRange(new Control[] { lblTitle, btnMore, btnClose, _pbAvatar, _lblName, _lblCount, sep });
            Controls.Add(pnl);
        }

        private void BuildNotify()
        {
            Controls.Add(Separator(HEADER_HEIGHT - 2));

            var pnl = new Panel
            {
                Location = new Point(0, HEADER_HEIGHT),
                Size = new Size(FORM_WIDTH, NOTIFY_HEIGHT),
                BackColor = C_BG,
            };

            var icon = new Label
            {
                Text = GLYPH_BELL,
                ForeColor = C_TEXT,
                AutoSize = false,
                Size = new Size(32, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(SECTION_PAD + 2, 12),
                Font = new Font("Segoe MDL2 Assets", 16f),
                BackColor = Color.Transparent,
            };

            var lbl = new Label
            {
                Text = "Notifications",
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI", 11f),
                AutoSize = true,
                Location = new Point(SECTION_PAD + 42, 16),
                BackColor = Color.Transparent,
            };

            _chkNotify = new CheckBox
            {
                Appearance = Appearance.Button,
                AutoSize = false,
                Size = new Size(50, 26),
                Location = new Point(FORM_WIDTH - SECTION_PAD - 50, 14),
                BackColor = Color.Transparent,
                Checked = true,
            };
            _chkNotify.Paint += TogglePaint;
            _chkNotify.CheckedChanged += (_, __) =>
            {
                _chkNotify.Invalidate();
                NotificationsChanged?.Invoke(_chkNotify.Checked);
            };

            pnl.Controls.AddRange(new Control[] { icon, lbl, _chkNotify });
            Controls.Add(pnl);

            Controls.Add(Separator(HEADER_HEIGHT + NOTIFY_HEIGHT - 2));
        }

        private void BuildMembers()
        {
            _lblMembersTitle = new Label
            {
                Text = "MEMBERS",
                ForeColor = C_TEXT,
                Font = new Font("Segoe UI Semibold", 10.5f),
                AutoSize = true,
                Location = new Point(SECTION_PAD + 36, HEADER_HEIGHT + NOTIFY_HEIGHT + 10),
                BackColor = Color.Transparent,
            };

            var icon = new Label
            {
                Text = GLYPH_PEOPLE,
                AutoSize = false,
                Size = new Size(24, 24),
                ForeColor = C_TEXT,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(SECTION_PAD + 4, HEADER_HEIGHT + NOTIFY_HEIGHT + 8),
                Font = new Font("Segoe MDL2 Assets", 14f),
                BackColor = Color.Transparent,
            };

            _btnAdd = FlatIconButton(GLYPH_ADD, "Segoe MDL2 Assets", 14f);
            _btnAdd.Location = new Point(FORM_WIDTH - SECTION_PAD - _btnAdd.Width - 6, HEADER_HEIGHT + NOTIFY_HEIGHT + 6);
            _btnAdd.Click += (_, __) => AddMemberRequested?.Invoke();

            Controls.AddRange(new Control[] { icon, _lblMembersTitle, _btnAdd });
            Controls.Add(Separator(HEADER_HEIGHT + NOTIFY_HEIGHT + MEMBERS_HEADER_HEIGHT));

            _pnlList = new Panel
            {
                Location = new Point(0, ListTop),
                Size = new Size(FORM_WIDTH, FORM_HEIGHT - ListTop - BOTTOM_HEIGHT),
                AutoScroll = true,
                BackColor = C_BG,
                Padding = new Padding(0, 0, 0, 8),
            };
            _pnlList.SizeChanged += (_, __) => LayoutMemberItems();

            Controls.Add(_pnlList);
        }

        private void BuildBottom()
        {
            var pnl = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = BOTTOM_HEIGHT,
                BackColor = C_BG,
            };
            var sep = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = C_SEPARATOR };
            pnl.Controls.Add(sep);
            Controls.Add(pnl);
        }

        private void LoadSample()
        {
            var members = new List<MemberModel>
            {
                new("Ho\u00E0ng Hi\u1EBFu", "online", "owner", null, Color.FromArgb(0xE5,0x7E,0x25)),
                new("Phi V\u00E2n", "last seen a long time ago", string.Empty, null, Color.FromArgb(0x5A,0xC7,0x67)),
                new("Ph\u00FAc", "last seen a long time ago", string.Empty, null, Color.FromArgb(0xFF,0x6B,0x81)),
            };
            LoadGroup("hello", null, members);
        }

        public void LoadGroup(string name, Image? avatar, IReadOnlyList<MemberModel> members, bool notifyOn = true)
        {
            _lblName.Text = name;
            _lblCount.Text = $"{members.Count} members";
            _lblMembersTitle.Text = $"{members.Count} MEMBERS";
            _pbAvatar.Image = avatar;
            _pbAvatar.BackColor = avatar == null ? Color.FromArgb(0xFF, 0x6B, 0x81) : Color.Transparent;
            _chkNotify.Checked = notifyOn;

            _pnlList.SuspendLayout();
            _pnlList.Controls.Clear();
            int y = 0;
            foreach (var m in members)
            {
                var item = new ucGroupMemberItem
                {
                    Dock = DockStyle.None,
                    Margin = Padding.Empty,
                    Location = new Point(SECTION_PAD, y),
                };
                item.DisplayName = m.Name;
                item.Status = m.Status;
                item.Role = m.Role;
                item.AvatarImage = m.Avatar;
                item.AvatarColor = m.AvatarColor;
                item.SetInitial(m.Name.Length > 0 ? m.Name.Substring(0, 1).ToUpperInvariant() : "?");

                _pnlList.Controls.Add(item);
                y += item.Height;
            }
            _pnlList.AutoScrollMinSize = new Size(0, y);
            _pnlList.ResumeLayout();
            LayoutMemberItems();
        }

        private void LayoutMemberItems()
        {
            int scrollbar = SystemInformation.VerticalScrollBarWidth;
            int available = _pnlList.ClientSize.Width - (SECTION_PAD * 2) - scrollbar;
            foreach (Control c in _pnlList.Controls)
            {
                if (c is ucGroupMemberItem item)
                {
                    item.Width = available;
                    item.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                }
            }
        }

        private static Panel Separator(int top)
        {
            return new Panel
            {
                Location = new Point(0, top),
                Size = new Size(FORM_WIDTH, 1),
                BackColor = C_SEPARATOR,
            };
        }

        private static Button FlatIconButton(string text, string fontFamily = "Segoe UI", float size = 12f)
        {
            var b = new Button
            {
                Text = text,
                Size = new Size(32, 30),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = C_TEXT,
                Font = new Font(fontFamily, size, FontStyle.Bold),
                TabStop = false,
                Cursor = Cursors.Hand,
                UseCompatibleTextRendering = true,
            };
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(18, 255, 255, 255);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(30, 255, 255, 255);
            return b;
        }

        private static void ClipCircle(PictureBox pb)
        {
            using var path = new GraphicsPath();
            path.AddEllipse(0, 0, pb.Width, pb.Height);
            pb.Region = new Region(path);
        }

        private void TogglePaint(object sender, PaintEventArgs e)
        {
            var chk = (CheckBox)sender;
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, chk.Width - 1, chk.Height - 1);
            int r = rect.Height / 2;
            using var track = new SolidBrush(chk.Checked ? C_ACCENT : Color.FromArgb(0x55, 0x65, 0x78));
            using var thumb = new SolidBrush(Color.White);
            g.FillEllipse(track, rect.Left, rect.Top, rect.Height, rect.Height);
            g.FillEllipse(track, rect.Right - rect.Height, rect.Top, rect.Height, rect.Height);
            g.FillRectangle(track, rect.Left + r, rect.Top, rect.Width - rect.Height, rect.Height);

            int thumbX = chk.Checked ? rect.Right - rect.Height + 2 : rect.Left + 2;
            g.FillEllipse(thumb, thumbX, rect.Top + 2, rect.Height - 4, rect.Height - 4);
        }
    }

    public record MemberModel(string Name, string Status, string Role, Image? Avatar, Color AvatarColor);
}
