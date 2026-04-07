using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace SecureChat.Client.Forms.Settings
{
    internal static class UiLocalization
    {
        private sealed class TextState
        {
            public string BaseText { get; set; } = string.Empty;
        }

        private static readonly ConditionalWeakTable<Control, TextState> BaseTexts = new();

        private static readonly Dictionary<string, string> Vi = new(StringComparer.Ordinal)
        {
            ["Settings"] = "Cài ??t",
            ["My Account"] = "Tài kho?n c?a tôi",
            ["Notifications and Sounds"] = "Thông báo và âm thanh",
            ["Privacy and Security"] = "Quy?n riêng t? và b?o m?t",
            ["Chat Settings"] = "Cài ??t trò chuy?n",
            ["Advanced"] = "Nâng cao",
            ["Speakers and Camera"] = "Loa và camera",
            ["Language"] = "Ngôn ng?",
            ["Unknown User"] = "Ng??i dùng ch?a xác ??nh",
            ["Add username"] = "Thêm tên ng??i dùng",

            ["Data and storage"] = "D? li?u và l?u tr?",
            ["Download path"] = "???ng d?n t?i xu?ng",
            ["Downloads"] = "T?i xu?ng",
            ["Ask download path for each file"] = "H?i ???ng d?n khi t?i t?ng t?p",
            ["Window title bar"] = "Thanh tiêu ?? c?a s?",
            ["Show chat name"] = "Hi?n th? tên chat",
            ["Total unread count"] = "T?ng s? ch?a ??c",
            ["Use system window frame"] = "Dùng khung c?a s? h? th?ng",
            ["System integration"] = "Tích h?p h? th?ng",
            ["Show taskbar icon"] = "Hi?n bi?u t??ng taskbar",
            ["Use monochrome icon"] = "Dùng bi?u t??ng ??n s?c",
            ["Default folder"] = "Th? m?c m?c ??nh",
            ["Temp folder"] = "Th? m?c t?m",
            ["Custom folder"] = "Th? m?c tùy ch?nh",
            ["Choose download path"] = "Ch?n ???ng d?n t?i xu?ng",
            ["Default app folder"] = "Th? m?c m?c ??nh c?a ?ng d?ng",
            ["Temp folder, cleared on logout or uninstall"] = "Th? m?c t?m, xóa khi ??ng xu?t ho?c g? cài ??t",
            ["Custom folder, cleared only manually"] = "Th? m?c tùy ch?nh, ch? xóa th? công",
            ["Cancel"] = "H?y",
            ["Save"] = "L?u",
            ["OK"] = "OK",
            ["No files here yet"] = "Ch?a có t?p nào",

            ["Speakers and headphones"] = "Loa và tai nghe",
            ["Output device"] = "Thi?t b? ??u ra",
            ["Microphone"] = "Micrô",
            ["Input device"] = "Thi?t b? ??u vào",
            ["Calls and video chats"] = "Cu?c g?i và video chat",
            ["Use the same devices for calls"] = "Dùng cùng thi?t b? cho cu?c g?i",
            ["Camera"] = "Camera",
            ["Other settings"] = "Cài ??t khác",
            ["Accept calls on this device"] = "Nh?n cu?c g?i trên thi?t b? này",
            ["Camera preview unavailable"] = "Không th? xem tr??c camera",

            ["Show Translate Button"] = "Hi?n nút D?ch",
            ["Do Not Translate"] = "Không d?ch",
            ["The 'Translate' button will appear in the context menu of messages containing text."] = "Nút 'D?ch' s? xu?t hi?n trong menu ng? c?nh c?a tin nh?n có v?n b?n.",
            ["Search"] = "Tìm ki?m",
            ["None"] = "Không có",
            ["languages"] = "ngôn ng?",
            ["1 language"] = "1 ngôn ng?"
        };

        private static string CurrentCode => LanguagePrefs.CurrentLanguageCode;

        public static void ApplyToForm(Control root)
        {
            ApplyRecursive(root);
        }

        public static void ApplyToOpenForms()
        {
            foreach (Form form in Application.OpenForms)
            {
                ApplyToForm(form);
                form.Refresh();
            }
        }

        private static void ApplyRecursive(Control c)
        {
            if (!BaseTexts.TryGetValue(c, out var state))
            {
                state = new TextState { BaseText = c.Text ?? string.Empty };
                BaseTexts.Add(c, state);
            }

            if (!string.IsNullOrWhiteSpace(state.BaseText))
            {
                c.Text = Translate(state.BaseText);
            }

            foreach (Control child in c.Controls)
            {
                ApplyRecursive(child);
            }
        }

        private static string Translate(string baseText)
        {
            if (string.Equals(CurrentCode, "vi", StringComparison.OrdinalIgnoreCase)
                && Vi.TryGetValue(baseText, out var vi))
            {
                return vi;
            }

            return baseText;
        }
    }
}
