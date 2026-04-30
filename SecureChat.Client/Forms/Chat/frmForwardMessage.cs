using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace SecureChat.Client.Forms.Chat
{
    public sealed class frmForwardMessage : Form
    {
        public string SelectedConversationId { get; private set; }

        // Nhận vào danh sách hội thoại từ frmMainChat
        public frmForwardMessage(List<(string Id, string Name, string Preview, string Time, int Unread, bool IsGroup)> convs)
        {
            Text = "Forward to...";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            BackColor = Color.White;
            Font = new Font("Segoe UI", 10f);
            ClientSize = new Size(320, 450);

            var pnlList = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Color.White
            };

            int y = 0;
            foreach (var c in convs)
            {
                var row = new Panel { Height = 56, Cursor = Cursors.Hand, Width = ClientSize.Width };

                // Dùng AvatarControl có sẵn của bạn
                var avatar = new AvatarControl { Size = new Size(40, 40), Location = new Point(16, 8) };
                avatar.SetName(c.Name);

                var lblName = new Label
                {
                    Text = c.Name,
                    Font = TG.FontSemiBold(10f),
                    Location = new Point(68, 18),
                    AutoSize = true,
                    ForeColor = TG.TextPrimary,
                    BackColor = Color.Transparent
                };

                row.Controls.AddRange(new Control[] { avatar, lblName });

                // Hiệu ứng hover giống Telegram
                row.MouseEnter += (s, e) => row.BackColor = TG.SidebarHover;
                row.MouseLeave += (s, e) => row.BackColor = Color.White;
                lblName.MouseEnter += (s, e) => row.BackColor = TG.SidebarHover;
                avatar.MouseEnter += (s, e) => row.BackColor = TG.SidebarHover;

                // Xử lý Click: Lấy ID và đóng Form
                Action onClick = () =>
                {
                    SelectedConversationId = c.Id;
                    DialogResult = DialogResult.OK;
                };

                row.Click += (s, e) => onClick();
                avatar.Click += (s, e) => onClick();
                lblName.Click += (s, e) => onClick();

                row.Location = new Point(0, y);
                pnlList.Controls.Add(row);
                y += 56;
            }

            Controls.Add(pnlList);
        }
    }
}