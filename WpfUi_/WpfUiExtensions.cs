﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;

namespace WpfUi
{
  public static class WpfUiExtensions
  {
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
