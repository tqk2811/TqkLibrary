using System;

namespace TqkLibrary.ScrcpyDotNet
{
  public class ScrcpyException : Exception
  {
    public ScrcpyException(int error_code,string message) : base(message)
    {
      this.ErrorCode = error_code;
    }

    public int ErrorCode { get; }
  }
}
