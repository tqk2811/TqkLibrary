using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TqkLibrary.WpfUi
{
  public static class WpfUiExtensions
  {
    public static string ExeFolderPath { get; } = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

    [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DeleteObject([In] IntPtr hObject);

    public static System.Windows.Media.ImageSource ToImageSource(this System.Drawing.Bitmap src)
    {
      if (null == src) throw new ArgumentNullException(nameof(src));
      var handle = src.GetHbitmap();
      try
      {
        return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, System.Windows.Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
      }
      finally { DeleteObject(handle); }
    }
  }
}