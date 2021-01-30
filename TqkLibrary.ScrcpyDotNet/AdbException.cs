using System;
//using TqkLibrary.ScrcpyDotNet.Util;
namespace TqkLibrary.ScrcpyDotNet
{
  public sealed class AdbException : Exception
  {
    internal AdbException(string args, string result, string error)
    {
      this.Arguments = args;
      this.ResultMessage = result;
      this.ErrorMessage = error;
    }

    public string Arguments { get; }
    public string ResultMessage { get; }
    public string ErrorMessage { get; }
  }
}
