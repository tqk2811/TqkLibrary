using System;
using System.Runtime.InteropServices;
//using TqkLibrary.ScrcpyDotNet.Util;

namespace TqkLibrary.ScrcpyDotNet
{
  static class Extensions
  {
    public static void CheckError(this int error_code, string message)
    {
      if (error_code < 0) throw new ScrcpyException(error_code, message);
    }

    [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
    public extern static IntPtr AddDllDirectory(string NewDirectory);

    static bool IsLoad = false;
    public static void LoadDll()
    {
      if (!IsLoad)
      {
        string dllPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        if (System.Environment.Is64BitOperatingSystem) dllPath += "\\x64";
        else dllPath += "\\x86";
        if (AddDllDirectory(dllPath) == IntPtr.Zero) throw new Exception("Unable to call Kernel32->AddDllDirectory");
        IsLoad = !IsLoad;
      }
    }
  }
}
