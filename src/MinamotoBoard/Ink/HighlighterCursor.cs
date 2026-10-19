using System;
using System.IO;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;

namespace MinamotoBoard.Ink
{
    internal static class HighlighterCursor
    {
        public static Cursor CreateEraser(double diameter, double dpiX, double dpiY)
        {
            double size = Math.Max(28, diameter + 4);
            double scale = Math.Min(1, 254 / (size * Math.Max(dpiX, dpiY)));
            int width = (int)Math.Ceiling(size * dpiX * scale) + 2;
            int height = (int)Math.Ceiling(size * dpiY * scale) + 2;
            return Encode(width, height, delegate(int x, int y)
            {
                double px = (x - width / 2.0) / (dpiX * scale), py = (y - height / 2.0) / (dpiY * scale);
                double distance = Math.Sqrt(px * px + py * py);
                Color pixel = Colors.Transparent;
                if (Math.Abs(distance - diameter / 2) <= 0.9)
                    pixel = ((int)Math.Floor((Math.Atan2(py, px) + Math.PI) * Math.Max(4, diameter / 2) / 4) % 2 == 0) ? Colors.White : Color.FromRgb(55, 65, 70);
                // A small tilted two-tone eraser distinguishes the tool from a pen nib.
                double u = px * 0.819 - py * 0.574, v = px * 0.574 + py * 0.819;
                if (Math.Abs(u) <= 10 && Math.Abs(v) <= 6)
                    pixel = Math.Abs(u) > 9 || Math.Abs(v) > 5 || Math.Abs(u) < 0.6
                        ? Color.FromRgb(55, 65, 70) : u < 0 ? Color.FromRgb(244, 163, 177) : Colors.White;
                return pixel;
            });
        }

        public static Cursor Create(DrawingAttributes attributes, double dpiX, double dpiY)
        {
            double pixelWidth = attributes.Width * dpiX, pixelHeight = attributes.Height * dpiY;
            double scale = Math.Min(1, 254 / Math.Max(pixelWidth, pixelHeight));
            int nibWidth = Math.Max(1, (int)Math.Round(pixelWidth * scale));
            int nibHeight = Math.Max(1, (int)Math.Round(pixelHeight * scale));
            int width = nibWidth + 2, height = nibHeight + 2;
            Color color = attributes.Color;
            return Encode(width, height, delegate(int x, int y)
            {
                bool outside = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                if (attributes.StylusTip == StylusTip.Ellipse)
                {
                    double nx = (x + 0.5 - width / 2.0) / (nibWidth / 2.0), ny = (y + 0.5 - height / 2.0) / (nibHeight / 2.0);
                    outside = outside || nx * nx + ny * ny > 1;
                }
                return outside ? Colors.Transparent : color;
            });
        }

        private static Cursor Encode(int width, int height, Func<int, int, Color> pixelAt)
        {
            int maskStride = ((width + 31) / 32) * 4;
            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // CUR directory with a 32-bit DIB; the hotspot is the nib's center.
                writer.Write((ushort)0); writer.Write((ushort)2); writer.Write((ushort)1);
                writer.Write((byte)width); writer.Write((byte)height); writer.Write((ushort)0);
                writer.Write((ushort)(width / 2)); writer.Write((ushort)(height / 2));
                writer.Write(40 + width * height * 4 + maskStride * height); writer.Write(22);
                writer.Write(40); writer.Write(width); writer.Write(height * 2);
                writer.Write((ushort)1); writer.Write((ushort)32);
                writer.Write(0); writer.Write(width * height * 4 + maskStride * height);
                writer.Write(0); writer.Write(0); writer.Write(0); writer.Write(0);
                byte[] mask = new byte[maskStride * height];
                for (int y = height - 1; y >= 0; y--)
                    for (int x = 0; x < width; x++)
                    {
                        Color color = pixelAt(x, y);
                        bool transparent = color.A == 0;
                        writer.Write(transparent ? (byte)0 : color.B);
                        writer.Write(transparent ? (byte)0 : color.G);
                        writer.Write(transparent ? (byte)0 : color.R);
                        writer.Write(transparent ? (byte)0 : color.A);
                        if (transparent) mask[(height - 1 - y) * maskStride + x / 8] |= (byte)(0x80 >> (x % 8));
                    }
                writer.Write(mask); writer.Flush(); stream.Position = 0;
                // Dimensions already reflect this monitor's DPI.
                return new Cursor(stream, false);
            }
        }
    }
}
