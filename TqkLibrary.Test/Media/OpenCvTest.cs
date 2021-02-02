using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using TqkLibrary.Media.Images;
namespace TqkLibrary.Test.Media
{
  [TestClass]
  public class OpenCvTest
  {
    [TestMethod]
    public void CropNonTransparent()
    {
      Bitmap bitmap = (Bitmap)Bitmap.FromFile("D:\\test.png");
      OpenCvHelper.CropNonTransparent(bitmap).Save("D:\\test2.png",ImageFormat.Png);
    }
  }
}
