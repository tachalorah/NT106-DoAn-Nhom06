using System.Drawing.Drawing2D;

namespace SecureChat.Client.Helpers
{
    public static class ImageHelper
    {
        public static Image GetCircularImage(Image img)
        {
            Bitmap bmp = new Bitmap(img.Width, img.Height);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                GraphicsPath path = new GraphicsPath();
                path.AddEllipse(0, 0, img.Width, img.Height);
                g.SetClip(path);
                g.DrawImage(img, 0, 0);
            }
            return bmp;
        }

        public static Image CreateDefaultAvatar(string name, int size, Color bgColor)
        {
            Bitmap bmp = new Bitmap(size, size);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.FillEllipse(new SolidBrush(bgColor), 0, 0, size, size);
                string initials = string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpper();
                Font font = new Font("Segoe UI", size / 2.5f, FontStyle.Bold);
                SizeF textSize = g.MeasureString(initials, font);
                g.DrawString(initials, font, Brushes.White, (size - textSize.Width) / 2, (size - textSize.Height) / 2);
            }
            return bmp;
        }
    }
}