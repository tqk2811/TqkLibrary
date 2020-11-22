﻿using PInvoke;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using TqkLibrary.Window.Api;

namespace TqkLibrary.Window.IPC
{
  public static class COPYDATA
  {
    public static Win32ErrorCode EnableWM_CopyData(IntPtr windowHandle)
    {
      CHANGEFILTERSTRUCT changeFilter = new CHANGEFILTERSTRUCT();
      changeFilter.size = (uint)Marshal.SizeOf(changeFilter);
      changeFilter.info = 0;
      if (!MyUser32.ChangeWindowMessageFilterEx(windowHandle, User32.WindowMessage.WM_COPYDATA, ChangeWindowMessageFilterExAction.Allow, ref changeFilter)) return Kernel32.GetLastError();
      else return Win32ErrorCode.ERROR_SUCCESS;
    }

    public static void Init_WndProc(System.Windows.Window window, HwndSourceHook hwndSourceHook)
    {
      HwndSource source = PresentationSource.FromVisual(window) as HwndSource;
      source.AddHook(hwndSourceHook);
    }
  }
}
