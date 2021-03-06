﻿using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections.Generic;
using System.Drawing;

namespace TqkLibrary.Media.Images
{
  public class OpenCvHelper
  {
    public static Point? FindOutPoint(Bitmap mainBitmap, Bitmap subBitmap, double percent = 0.9)
    {
      if (subBitmap == null || mainBitmap == null)
        return null;
      if (subBitmap.Width > mainBitmap.Width || subBitmap.Height > mainBitmap.Height)
        return null;

      using Image<Bgr, byte> source = mainBitmap.ToImage<Bgr, byte>();// new Image<Bgr, byte>(mainBitmap);
      using Image<Bgr, byte> template = subBitmap.ToImage<Bgr, byte>();
      Point? resPoint = null;

      using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
      {
        double[] minValues, maxValues;
        System.Drawing.Point[] minLocations, maxLocations;
        result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

        double currentMax = 0;
        for (int i = 0; i < maxValues.Length; i++)
        {
          if (maxValues[i] > percent && maxValues[i] > currentMax)
          {
            currentMax = maxValues[i];
            resPoint = maxLocations[i];
          }
        }
      }
      return resPoint;
    }

    public static Point? FindOutPoint(Bitmap mainBitmap, Bitmap subBitmap, Rectangle crop, double percent = 0.9)
    {
      if (subBitmap == null || mainBitmap == null)
        return null;
      if (subBitmap.Width > mainBitmap.Width || subBitmap.Height > mainBitmap.Height)
        return null;

      using Bitmap bm_crop = mainBitmap.CropImage(crop);
      Point? point = FindOutPoint(bm_crop, subBitmap, percent);
      if (point != null)
      {
        Point subpoint = point.Value;
        subpoint.X += crop.X;
        subpoint.Y += crop.Y;
        point = subpoint;
      }
      return point;
    }

    public static Point? FindOutPoint(Bitmap mainBitmap, double percent = 0.9, params Bitmap[] subBitmaps)
    {
      foreach (var subBitmap in subBitmaps)
      {
        Point? point = FindOutPoint(mainBitmap, subBitmap, percent);
        if (point != null) return point;
      }
      return null;
    }

    public static Point? FindOutPoint(Bitmap mainBitmap, Rectangle crop, double percent = 0.9, params Bitmap[] subBitmaps)
    {
      using Bitmap bm_crop = mainBitmap.CropImage(crop);
      return FindOutPoint(bm_crop, percent, subBitmaps);
    }

    public static List<Point> FindOutPoints(Bitmap mainBitmap, Bitmap subBitmap, double percent = 0.9)
    {
      List<Point> resPoint = new List<Point>();
      if (subBitmap == null || mainBitmap == null)
        return resPoint;
      if (subBitmap.Width > mainBitmap.Width || subBitmap.Height > mainBitmap.Height)
        return resPoint;

      using Image<Bgr, byte> source = mainBitmap.ToImage<Bgr, byte>();
      using Image<Bgr, byte> template = subBitmap.ToImage<Bgr, byte>();
      while (true)
      {
        using (Image<Gray, float> result = source.MatchTemplate(template, Emgu.CV.CvEnum.TemplateMatchingType.CcoeffNormed))
        {
          double[] minValues, maxValues;
          Point[] minLocations, maxLocations;
          result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);

          if (maxValues[0] > percent)
          {
            Rectangle match = new Rectangle(maxLocations[0], template.Size);
            source.Draw(match, new Bgr(Color.Blue), -1);
            resPoint.Add(maxLocations[0]);
          }
          else break;
        }
      }
      return resPoint;
    }

    public static List<Point> FindOutPoints(Bitmap mainBitmap, Bitmap subBitmap, Rectangle crop, double percent = 0.9)
    {
      using Bitmap bm_crop = mainBitmap.CropImage(crop);
      List<Point> points = FindOutPoints(bm_crop, subBitmap, percent);
      for (int i = 0; i < points.Count; i++)
      {
        Point temp = points[i];
        temp.X += crop.X;
        temp.Y += crop.Y;
        points[i] = temp;
      }
      return points;
    }

    public static Bitmap CropNonTransparent(Bitmap bitmap)
    {
      using Image<Bgra, byte> imageIn = bitmap.ToImage<Bgra, byte>();
      using Mat mat = new Mat(/*imageIn.Size, Emgu.CV.CvEnum.DepthType.Cv8U, 1*/);
      CvInvoke.FindNonZero(imageIn, mat);
      return mat.ToBitmap();
    }
  }
}
