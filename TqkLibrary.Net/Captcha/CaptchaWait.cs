using System.Threading;
using System.Threading.Tasks;

namespace TqkLibrary.Net.Captcha
{
  public static class CaptchaWait
  {
    public static void Wait(CancellationToken cancellationToken, int delay = 5000, int step = 100)
    {
      int timeloop = delay / step;
      while (timeloop-- != 0)
      {
        Task.Delay(step).Wait();
        cancellationToken.ThrowIfCancellationRequested();
      }
    }
  }
}
