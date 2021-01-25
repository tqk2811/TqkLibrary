namespace TqkLibrary.ScrcpyCapture
{
  static class Extensions
  {
    public static void CheckError(this int error_code, string message)
    {
      if (error_code < 0) throw new ScrcpyImageCaptureException(error_code, message);
    }
  }
}
