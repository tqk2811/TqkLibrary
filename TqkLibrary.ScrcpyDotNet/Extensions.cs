using static FFmpeg.AutoGen.ffmpeg;
using System.IO;
using System.Linq;
using FFmpeg.AutoGen;
using System.Net.Sockets;
using System;
//using TqkLibrary.ScrcpyDotNet.Util;

namespace TqkLibrary.ScrcpyDotNet
{
  static class Extensions
  {
    public static void CheckError(this int error_code, string message)
    {
      if (error_code < 0) throw new ScrcpyException(error_code, message);
    }

    

    
  }
}
