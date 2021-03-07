using System;
using System.Text;

namespace TqkLibrary.ScrcpyDotNet
{
  static class Extensions
  {
    public static void CheckError(this int error_code, string message)
    {
      if (error_code < 0) throw new ScrcpyException(error_code, message);
    }

    internal static unsafe string GetString(byte* text)
    {
      int length = 0;
      while(true) if (text[length++] == 0) break;
      string result = Encoding.UTF8.GetString(text, length - 1).Trim();
      Console.WriteLine(result);
      return result;
    }
  }
}
