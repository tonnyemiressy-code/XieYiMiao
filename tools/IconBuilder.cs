using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class IconBuilder
{
    [STAThread]
    private static void Main(string[] args)
    {
        string output = args.Length == 0 ? "app.ico" : args[0];
        string spritePath = args.Length >= 2 ? args[1] : "";
        using (Bitmap bitmap = new Bitmap(64, 64, PixelFormat.Format32bppArgb))
        using (Graphics graphics = Graphics.FromImage(bitmap))
        {
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.Clear(Color.Transparent);
            using (SolidBrush plate = new SolidBrush(Color.FromArgb(255, 235, 214)))
            {
                graphics.FillEllipse(plate, 2, 2, 60, 60);
            }

            if (File.Exists(spritePath))
            {
                using (Bitmap sheet = new Bitmap(spritePath))
                {
                    Rectangle cell = new Rectangle(
                        0,
                        0,
                        sheet.Width / 4,
                        sheet.Height / 2);
                    Rectangle bounds = AlphaBounds(sheet, cell);
                    const int maxWidth = 48;
                    const int maxHeight = 54;
                    double ratio = bounds.Width / (double)bounds.Height;
                    int drawWidth;
                    int drawHeight;
                    if (ratio >= maxWidth / (double)maxHeight)
                    {
                        drawWidth = maxWidth;
                        drawHeight = Math.Max(1, (int)Math.Round(maxWidth / ratio));
                    }
                    else
                    {
                        drawHeight = maxHeight;
                        drawWidth = Math.Max(1, (int)Math.Round(maxHeight * ratio));
                    }
                    Rectangle destination = new Rectangle(
                        (64 - drawWidth) / 2,
                        5 + (maxHeight - drawHeight) / 2,
                        drawWidth,
                        drawHeight);
                    graphics.DrawImage(
                        sheet,
                        destination,
                        bounds,
                        GraphicsUnit.Pixel);
                }
            }

            using (MemoryStream png = new MemoryStream())
            {
                bitmap.Save(png, ImageFormat.Png);
                byte[] bytes = png.ToArray();
                using (BinaryWriter writer = new BinaryWriter(File.Create(output)))
                {
                    writer.Write((ushort)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)1);
                    writer.Write((byte)64);
                    writer.Write((byte)64);
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(bytes.Length);
                    writer.Write(22);
                    writer.Write(bytes);
                }
            }
        }
    }

    private static Rectangle AlphaBounds(Bitmap bitmap, Rectangle cell)
    {
        int minX = cell.Right;
        int minY = cell.Bottom;
        int maxX = cell.Left;
        int maxY = cell.Top;
        for (int y = cell.Top; y < cell.Bottom; y++)
        {
            for (int x = cell.Left; x < cell.Right; x++)
            {
                if (bitmap.GetPixel(x, y).A <= 8)
                {
                    continue;
                }
                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }
        if (maxX <= minX || maxY <= minY)
        {
            return cell;
        }
        return Rectangle.FromLTRB(
            Math.Max(cell.Left, minX - 4),
            Math.Max(cell.Top, minY - 4),
            Math.Min(cell.Right, maxX + 5),
            Math.Min(cell.Bottom, maxY + 5));
    }
}
