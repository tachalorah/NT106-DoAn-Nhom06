using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace SecureChat.Client.Forms.Chat
{
    public sealed class frmCreatePoll : Form
    {
        private const int MaxOptions = 12;

        private readonly TextBox _txtQuestion;
        private readonly TextBox _txtDescription;
        private readonly FlowLayoutPanel _optionsPanel;
        private readonly Label _lblRemaining;
        private readonly List<OptionRow> _optionRows = new();

        public string PollQuestion { get; private set; } = string.Empty;
        public string PollDescription { get; private set; } = string.Empty;
        public List<string> PollOptions { get; private set; } = new();

        public frmCreatePoll()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            DoubleBuffered = true;

            Text = "New poll";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ControlBox = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(470, 620);
            Opacity = 0;
            Shown += (_, __) => StartFadeIn();

            var lblHeader = new Label
            {
                Text = "New poll",
                Font = new Font("Segoe UI Semibold", 15f),
                ForeColor = Color.FromArgb(0x1F, 0x2D, 0x3D),
                Location = new Point(20, 14),
                Size = new Size(240, 32)
            };

            var lblQuestion = new Label
            {
                Text = "Question",
                Font = new Font("Segoe UI Semibold", 12f),
                ForeColor = Color.FromArgb(0x17, 0x7E, 0xC4),
                Location = new Point(20, 58),
                Size = new Size(220, 26)
            };

            var pnlQuestion = BuildInputRow(20, 88, 430, "Ask a question", out _txtQuestion);
            AttachEmojiPicker((Control)pnlQuestion.Tag!, _txtQuestion);

            var pnlDescription = BuildInputRow(20, 138, 430, "Add Description (optional)", out _txtDescription);
            AttachEmojiPicker((Control)pnlDescription.Tag!, _txtDescription);

            var dividerTop = BuildDivider(0, 194);

            var lblOptions = new Label
            {
                Text = "Poll options",
                Font = new Font("Segoe UI Semibold", 12f),
                ForeColor = Color.FromArgb(0x17, 0x7E, 0xC4),
                Location = new Point(20, 206),
                Size = new Size(220, 26)
            };

            _optionsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 236),
                Size = new Size(430, 262),
                AutoScroll = true,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.White
            };
            EnableDoubleBuffer(_optionsPanel);

            _lblRemaining = new Label
            {
                Font = new Font("Segoe UI", 10f),
                ForeColor = Color.FromArgb(0x7D, 0x8B, 0x98),
                Location = new Point(20, 512),
                Size = new Size(430, 24)
            };

            var btnCancel = BuildBottomButton("Cancel", Color.FromArgb(0x2A, 0xAB, 0xEE));
            btnCancel.Location = new Point(274, 568);
            btnCancel.Click += (_, __) => DialogResult = DialogResult.Cancel;

            var btnCreate = BuildBottomButton("Create", Color.FromArgb(0x2A, 0xAB, 0xEE), bold: true);
            btnCreate.Location = new Point(364, 568);
            btnCreate.Click += OnCreateClick;

            Controls.AddRange(new Control[]
            {
                lblHeader,
                lblQuestion,
                pnlQuestion,
                pnlDescription,
                dividerTop,
                lblOptions,
                _optionsPanel,
                _lblRemaining,
                btnCancel,
                btnCreate
            });

            AddOptionRow();
            UpdateRemainingLabel();
        }

        private void StartFadeIn()
        {
            var timer = new System.Windows.Forms.Timer { Interval = 12 };
            timer.Tick += (_, __) =>
            {
                Opacity = Math.Min(1, Opacity + 0.12);
                if (Opacity >= 1)
                {
                    timer.Stop();
                    timer.Dispose();
                }
            };
            timer.Start();
        }

        private void OnCreateClick(object? sender, EventArgs e)
        {
            var question = _txtQuestion.Text.Trim();
            var options = _optionRows
                .Select(x => x.TextBox.Text.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(question))
            {
                MessageBox.Show(this, "Please enter a question.", "New poll", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtQuestion.Focus();
                return;
            }

            if (options.Count < 2)
            {
                MessageBox.Show(this, "Please enter at least 2 options.", "New poll", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            PollQuestion = question;
            PollDescription = _txtDescription.Text.Trim();
            PollOptions = options;
            DialogResult = DialogResult.OK;
        }

        private Panel BuildInputRow(int x, int y, int width, string placeholder, out TextBox textBox)
        {
            var panel = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(width, 44),
                BackColor = Color.White,
                Cursor = Cursors.IBeam
            };
            EnableDoubleBuffer(panel);

            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(0xDB, 0xE2, 0xEA));
                e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };

            var tb = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(0x1F, 0x2D, 0x3D),
                Location = new Point(0, 10),
                Size = new Size(width - 46, 24),
                PlaceholderText = placeholder
            };
            panel.Click += (_, __) => tb.Focus();

            var btnEmoji = new EmojiIconButton
            {
                Size = new Size(34, 30),
                Location = new Point(width - 36, 6)
            };

            panel.Controls.Add(tb);
            panel.Controls.Add(btnEmoji);
            btnEmoji.BringToFront();
            panel.Tag = btnEmoji;

            textBox = tb;
            return panel;
        }

        private void AddOptionRow(string text = "")
        {
            if (_optionRows.Count >= MaxOptions)
                return;

            _optionsPanel.SuspendLayout();
            var row = BuildOptionRow(text);
            _optionRows.Add(row);
            _optionsPanel.Controls.Add(row.Panel);
            _optionsPanel.ResumeLayout();

            UpdateOptionRowGlyphs();
            UpdateRemainingLabel();
        }

        private OptionRow BuildOptionRow(string text)
        {
            var panel = new Panel
            {
                Size = new Size(420, 40),
                Margin = new Padding(0, 2, 0, 2),
                BackColor = Color.White,
                Cursor = Cursors.IBeam
            };
            EnableDoubleBuffer(panel);

            panel.Paint += (_, e) =>
            {
                using var pen = new Pen(Color.FromArgb(0xDB, 0xE2, 0xEA));
                e.Graphics.DrawLine(pen, 0, panel.Height - 1, panel.Width, panel.Height - 1);
            };

            var glyph = new OptionGlyphControl
            {
                Location = new Point(0, 4),
                Size = new Size(30, 30)
            };

            var txtOption = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = new Font("Segoe UI", 12f),
                ForeColor = Color.FromArgb(0x1F, 0x2D, 0x3D),
                Location = new Point(36, 10),
                Size = new Size(344, 24),
                Text = text,
                PlaceholderText = "Add an option..."
            };
            panel.Click += (_, __) => txtOption.Focus();

            var btnEmoji = new EmojiIconButton
            {
                Size = new Size(32, 28),
                Location = new Point(386, 5)
            };

            txtOption.TextChanged += (_, __) =>
            {
                EnsureTrailingEmptyOption(txtOption);
                UpdateOptionRowGlyphs();
                UpdateRemainingLabel();
            };

            AttachEmojiPicker(btnEmoji, txtOption);

            panel.Controls.Add(glyph);
            panel.Controls.Add(txtOption);
            panel.Controls.Add(btnEmoji);
            btnEmoji.BringToFront();

            return new OptionRow(panel, glyph, txtOption);
        }

        private void EnsureTrailingEmptyOption(TextBox source)
        {
            if (string.IsNullOrWhiteSpace(source.Text))
                return;

            var last = _optionRows.LastOrDefault();
            if (last is null || !ReferenceEquals(last.TextBox, source))
                return;

            if (_optionRows.Count >= MaxOptions)
                return;

            AddOptionRow();
        }

        private void UpdateOptionRowGlyphs()
        {
            for (var i = 0; i < _optionRows.Count; i++)
            {
                var row = _optionRows[i];
                var isLast = i == _optionRows.Count - 1;
                var isEmpty = string.IsNullOrWhiteSpace(row.TextBox.Text);
                row.Glyph.IsAddMode = isLast && isEmpty;
                row.Glyph.Invalidate();
            }
        }

        private void UpdateRemainingLabel()
        {
            var used = _optionRows.Count(x => !string.IsNullOrWhiteSpace(x.TextBox.Text));
            var left = Math.Max(0, MaxOptions - used);

            _lblRemaining.Text = left == 1
                ? "You can add 1 more option."
                : $"You can add {left} more options.";
        }

        private Panel BuildDivider(int x, int y)
        {
            return new Panel
            {
                Location = new Point(x, y),
                Size = new Size(ClientSize.Width, 1),
                BackColor = Color.FromArgb(0xE6, 0xEC, 0xF2)
            };
        }

        private void AttachEmojiPicker(Control button, TextBox target)
        {
            button.Click += (_, __) =>
            {
                var menu = BuildEmojiMenu(emoji => InsertEmoji(target, emoji));
                menu.Show(button, new Point(0, button.Height));
            };
        }

        private static ContextMenuStrip BuildEmojiMenu(Action<string> onEmojiSelect)
        {
            var menu = new ContextMenuStrip
            {
                ShowImageMargin = false,
                Font = new Font("Segoe UI Emoji", 11f)
            };

            var emojis = new[]
            {
                char.ConvertFromUtf32(0x1F600),
                char.ConvertFromUtf32(0x1F604),
                char.ConvertFromUtf32(0x1F602),
                char.ConvertFromUtf32(0x1F60A),
                char.ConvertFromUtf32(0x1F60D),
                char.ConvertFromUtf32(0x1F914),
                char.ConvertFromUtf32(0x1F44D),
                char.ConvertFromUtf32(0x1F64F),
                char.ConvertFromUtf32(0x1F525),
                char.ConvertFromUtf32(0x2764),
                char.ConvertFromUtf32(0x1F389),
                char.ConvertFromUtf32(0x2705)
            };

            foreach (var emoji in emojis)
            {
                var item = new ToolStripMenuItem(emoji);
                item.Click += (_, __) => onEmojiSelect(emoji);
                menu.Items.Add(item);
            }

            return menu;
        }

        private static void InsertEmoji(TextBox target, string emoji)
        {
            var start = target.SelectionStart;
            target.Text = target.Text.Insert(start, emoji);
            target.SelectionStart = start + emoji.Length;
            target.Focus();
        }

        private static Button BuildBottomButton(string text, Color color, bool bold = false)
        {
            var btn = new Button
            {
                Text = text,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.Transparent,
                ForeColor = color,
                Font = new Font("Segoe UI", 11f, bold ? FontStyle.Bold : FontStyle.Regular),
                Size = new Size(90, 34),
                Cursor = Cursors.Hand,
                TextAlign = ContentAlignment.MiddleCenter
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private static void EnableDoubleBuffer(Control control)
        {
            typeof(Control).GetProperty("DoubleBuffered", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                ?.SetValue(control, true, null);
        }

        private sealed record OptionRow(Panel Panel, OptionGlyphControl Glyph, TextBox TextBox);

        private sealed class EmojiIconButton : Control
        {
            public EmojiIconButton()
            {
                SetStyle(
    ControlStyles.UserPaint |
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.OptimizedDoubleBuffer |
    ControlStyles.SupportsTransparentBackColor,
    true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                var border = Color.FromArgb(0x9C, 0xA8, 0xB5);
                using var pen = new Pen(border, 1.5f);
                var faceRect = new Rectangle(3, 3, Width - 7, Height - 7);
                e.Graphics.DrawEllipse(pen, faceRect);

                using var eyeBrush = new SolidBrush(border);
                e.Graphics.FillEllipse(eyeBrush, Width / 2 - 7, Height / 2 - 4, 2, 2);
                e.Graphics.FillEllipse(eyeBrush, Width / 2 + 4, Height / 2 - 4, 2, 2);

                using var smilePen = new Pen(border, 1.4f);
                e.Graphics.DrawArc(smilePen, Width / 2 - 8, Height / 2 - 2, 16, 10, 18, 144);
            }
        }

        private sealed class OptionGlyphControl : Control
        {
            public bool IsAddMode { get; set; }

            public OptionGlyphControl()
            {
                SetStyle(
    ControlStyles.UserPaint |
    ControlStyles.AllPaintingInWmPaint |
    ControlStyles.OptimizedDoubleBuffer |
    ControlStyles.SupportsTransparentBackColor,
    true);
                BackColor = Color.Transparent;
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

                if (IsAddMode)
                {
                    var blue = Color.FromArgb(0x2A, 0xAB, 0xEE);
                    using var brush = new SolidBrush(blue);
                    e.Graphics.FillEllipse(brush, 2, 2, 24, 24);

                    using var pen = new Pen(Color.White, 2f);
                    e.Graphics.DrawLine(pen, 14, 8, 14, 20);
                    e.Graphics.DrawLine(pen, 8, 14, 20, 14);
                    return;
                }

                using var linePen = new Pen(Color.FromArgb(0x9A, 0xA7, 0xB4), 1.6f);
                e.Graphics.DrawLine(linePen, 6, 10, 22, 10);
                e.Graphics.DrawLine(linePen, 6, 14, 22, 14);
                e.Graphics.DrawLine(linePen, 6, 18, 22, 18);
            }
        }
    }
}
