using System.Drawing;
using System.Windows.Forms;

namespace SecureChat.Client.Forms.Settings
{
    internal static class SettingsGlyphIcons
    {
        public static Image Create(string key, int size = 24)
        {
            var glyph = key switch
            {
                "my_account.png" or "profile_manage.png" => "\uE77B",
                "notifications.png" => "\uE7F4",
                "privacy.png" or "lock.png" or "mini_lock.png" or "info_rights_lock.png" => "\uE72E",
                "chat.png" or "mode_messages.png" or "stories_to_chats.png" or "show_in_chat.png" => "\uE8BD",
                "folders.png" => "\uE8B7",
                "storage_local.png" => "\uE7F1",
                "downloads_arrow.png" => "\uE896",
                "advanced.png" or "settings.png" => "\uE713",
                "devices.png" or "volume_mute.png" => "\uE767",
                "language.png" => "\uE909",
                "title_back.png" => "\uE72B",
                "moon.png" => "\uE708",
                "font.png" => "\uE8D2",
                "input_autodelete.png" => "\uE823",
                "account_check.png" => "\uE77B",
                "info_block.png" => "\uEA39",
                "upload_chat_photo.png" => "\uE722",
                "saved_messages.png" => "\uE734",
                _ => "\uE10C"
            };

            var bmp = new Bitmap(size, size);
            using var g = Graphics.FromImage(bmp);
            g.Clear(Color.Transparent);

            TextRenderer.DrawText(
                g,
                glyph,
                new Font("Segoe MDL2 Assets", 14f, FontStyle.Regular),
                new Rectangle(0, 0, size, size),
                Color.FromArgb(0x5F, 0x63, 0x68),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);

            return bmp;
        }
    }
}