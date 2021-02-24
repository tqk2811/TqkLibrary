using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace TqkLibrary.Media.Images
{
  public static class ImageUtil
  {
    private static readonly int TransparentARGB = Color.Transparent.ToArgb();

    public static Bitmap Resize(this Bitmap source, int newWidth, int newHeight)
    {
      var newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
      newImage.MakeTransparent();
      using (Graphics graphics = Graphics.FromImage(newImage))
      {
        source.SetResolution(graphics.DpiX, graphics.DpiY);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        graphics.DrawImage(source, 0, 0, newWidth, newHeight);
        return newImage;
      }
    }

    public static Bitmap Resize(this Bitmap source, double percent)
    {
      if (percent >= 1 || percent <= 0) throw new ArgumentException(nameof(percent));
      int newWidth = (int)(source.Width * percent);
      int newHeight = (int)(source.Height * percent);
      var newImage = new Bitmap(newWidth, newHeight, PixelFormat.Format32bppArgb);
      newImage.MakeTransparent();
      using (Graphics graphics = Graphics.FromImage(newImage))
      {
        source.SetResolution(graphics.DpiX, graphics.DpiY);

        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        graphics.DrawImage(source, 0, 0, newWidth, newHeight);
        return newImage;
      }
    }

    public static Bitmap CropImageByPercent(this Bitmap source, double start_x, double start_y, double width, double height)
      => CropImage(source, new Rectangle()
      {
        Location = new Point()
        {
          X = (int)(source.Width * start_x),
          Y = (int)(source.Height * start_y),
        },
        Size = new Size()
        {
          Width = (int)(source.Width * width),
          Height = (int)(source.Height * height),
        }
      });

    public static Bitmap CropImage(this Bitmap source, Rectangle rect)
    {
      var newImage = new Bitmap(rect.Width, rect.Height, PixelFormat.Format24bppRgb);
      newImage.MakeTransparent();
      using (Graphics graphics = Graphics.FromImage(newImage))
      {
        source.SetResolution(graphics.DpiX, graphics.DpiY);
        graphics.DrawImage(source, 0, 0, rect, GraphicsUnit.Pixel);
      }
      return newImage;
    }

    public static Bitmap CropImage(this Bitmap source, Size size, Point location) => source.CropImage(new Rectangle() { Size = size, Location = location });

    public static void DrawChild(this Bitmap target, Bitmap child, Point pos)
    {
      //var newImage = new Bitmap(target.Width, target.Height, PixelFormat.Format24bppRgb);
      //newImage.MakeTransparent();
      //using (Graphics graphics = Graphics.FromImage(newImage))
      //{
      //  target.SetResolution(graphics.DpiX, graphics.DpiY);
      //  graphics.DrawImage(target, 0, 0, target.Width, target.Height);
      //  graphics.DrawImage(child, pos.X, pos.Y, child.Width, child.Height);
      //}
      //return newImage;
      using (Graphics graphics = Graphics.FromImage(target))
      {
        graphics.DrawChild(child, pos);
      }
    }

    public static void DrawText(this Bitmap target, Point point, string text, Font font, Color color, int opacity = 128)
    {
      using (Graphics graphics = Graphics.FromImage(target))
      {
        graphics.DrawText(point, text, font, color, opacity);
      }
    }

    public static Bitmap RedrawCropToCenterWidth(this Bitmap source, Rectangle rectangle)
    {
      Bitmap result = new Bitmap(source.Width, source.Height);
      result.MakeTransparent();
      using (Bitmap crop = source.Clone(rectangle, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
      {
        using (Graphics graphics = Graphics.FromImage(result))
        {
          int center = source.Height / 2;
          int Y_start = center - rectangle.Height / 2;
          graphics.DrawImage(crop, 0, Y_start, source.Width, crop.Height);
        }
      }
      return result;
    }

    public static Point GetCenter(this Rectangle rectangle)
    {
      return new Point(rectangle.X + (rectangle.Width / 2), rectangle.Y + (rectangle.Height / 2));
    }

    public static Point ImageCenterImage(Size source, Size child)
    {
      var center_source = new Point(source.Width / 2, source.Height / 2);
      var center_child = new Point(child.Width / 2, child.Height / 2);
      return new Point(center_source.X - center_child.X, center_source.Y - center_child.Y);
    }

    public static Bitmap ToBitMap(this byte[] buffer)
    {
      MemoryStream memoryStream = new MemoryStream(buffer);
      return (Bitmap)Bitmap.FromStream(memoryStream);
    }

    #region Graphics Extension

    public static void DrawText(this Graphics graphics, Point point, string text, Font font, Color color, int opacity = 128)
    {
      graphics.DrawString(text, font, new SolidBrush(Color.FromArgb(opacity, color)), point);
    }

    public static void DrawChild(this Graphics graphics, Bitmap child, Point pos)
    {
      graphics.DrawImage(child, pos.X, pos.Y, child.Width, child.Height);
    }

    #endregion Graphics Extension

    public static byte[] GetBytes(this Bitmap bitmap)
    {
      using (MemoryStream ms = new MemoryStream())
      {
        bitmap.Save(ms, ImageFormat.Jpeg);
        byte[] buffer = new byte[ms.Length];
        ms.Seek(0, SeekOrigin.Begin);
        ms.Read(buffer, 0, buffer.Length);
        return buffer;
      }
    }
  }
}