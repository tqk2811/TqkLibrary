using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TqkLibrary.SeleniumSupport
{
  public class ChromeAutoException : Exception
  {
    public ChromeAutoException(string Message) : base(Message)
    {

    }
  }
  public abstract class BaseChromeProfile
  {
    protected static readonly Random rd = new Random();
    protected readonly ChromeDriverService service;

    protected ChromeDriver chromeDriver;
    protected CancellationTokenSource tokenSource;

    public bool IsOpenChrome
    {
      get { return chromeDriver != null; }
    }


    protected BaseChromeProfile(string ChromeDrivePath,bool HideCommandPromptWindow = true)
    {
      if (string.IsNullOrEmpty(ChromeDrivePath)) throw new ArgumentNullException(nameof(ChromeDrivePath));
      service = ChromeDriverService.CreateDefaultService(ChromeDrivePath);
      service.HideCommandPromptWindow = HideCommandPromptWindow;
    }

    protected virtual bool OpenChrome(ChromeOptions chromeOptions)
    {
      if (!IsOpenChrome)
      {
        tokenSource = new CancellationTokenSource();
        chromeDriver = new ChromeDriver(service, chromeOptions);
        return true;
      }
      return false;
    }

    protected virtual bool CloseChrome()
    {
      if (IsOpenChrome)
      {
        chromeDriver.Quit();
        chromeDriver = null;
        tokenSource.Dispose();
        tokenSource = null;
        return true;
      }
      return false;
    }

    protected virtual void Delay(int min,int max)
    {
      int time = rd.Next(min, max) / 100;
      for (int i = 0; i < time; i++)
      {
        Task.Delay(100).Wait();
        tokenSource?.Token.ThrowIfCancellationRequested();
      }
    }

    public virtual void Stop()
    {
      tokenSource?.Cancel();
    }
  }
}
