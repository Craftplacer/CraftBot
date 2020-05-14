using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace CraftBot.Extensions
{
    public static class GraphicsExtensions
    {
        public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var diameter = radius * 2;
            var size = new Size(diameter, diameter);
            var arc = new Rectangle(bounds.Location, size);
            var path = new GraphicsPath();

            if (radius == 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            // top left arc
            path.AddArc(arc, 180, 90);

            // top right arc
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);

            // bottom right arc
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);

            // bottom left arc
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);

            path.CloseFigure();
            return path;
        }

        public static void SetHighQuality(this Graphics graphics)
        {
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            //graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
        }

        public static void DrawRoundedRectangle(this Graphics graphics, Pen pen, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            if (pen == null)
                throw new ArgumentNullException(nameof(pen));

            using (var path = RoundedRect(bounds, cornerRadius))
            {
                graphics.DrawPath(pen, path);
            }
        }

        public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int cornerRadius)
        {
            if (graphics == null)
                throw new ArgumentNullException(nameof(graphics));

            if (brush == null)
                throw new ArgumentNullException(nameof(brush));

            using (var path = RoundedRect(bounds, cornerRadius))
            {
                graphics.FillPath(brush, path);
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(this Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.SetHighQuality();

                using (var attributes = new ImageAttributes())
                {
                    attributes.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
                }
            }

            return destImage;
        }

        /// <summary>
        /// Clips the <paramref name="sourceImage"/> to the shape of an circle
        /// </summary>
        /// <returns>The clipped image</returns>
        public static Bitmap ClipToCircle(this Bitmap sourceImage)
        {
            var destinationImage = new Bitmap(sourceImage.Width * 2, sourceImage.Height * 2, PixelFormat.Format32bppArgb);
            var center = new PointF(sourceImage.Width, sourceImage.Height);
            var radius = sourceImage.Width;

            using var graphics = Graphics.FromImage(destinationImage);
            graphics.SetHighQuality();

            var path = new GraphicsPath();
            var r = new RectangleF(center.X - radius, center.Y - radius, radius * 2, radius * 2);
            path.AddEllipse(r);
            graphics.SetClip(path);
            graphics.DrawImage(sourceImage, new Rectangle(Point.Empty, new Size(sourceImage.Width * 2, sourceImage.Height * 2)));
            return destinationImage;
        }
    }
}