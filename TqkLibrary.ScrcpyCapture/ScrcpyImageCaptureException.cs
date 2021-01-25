using System;

namespace TqkLibrary.ScrcpyCapture
{
  public class ScrcpyImageCaptureException : Exception
  {
    public ScrcpyImageCaptureException(int error_code,string message) : base(message)
    {
      this.ErrorCode = error_code;
    }

    public int ErrorCode { get; }
  }
}
