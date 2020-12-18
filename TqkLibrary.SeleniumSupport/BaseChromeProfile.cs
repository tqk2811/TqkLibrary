using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

  public delegate void RunningStateChange(bool change);

  public abstract class BaseChromeProfile
  {
    protected static readonly Random rd = new Random();
    protected readonly ChromeDriverService service;

    protected ChromeDriver chromeDriver { get; private set; }
    protected CancellationTokenSource tokenSource { get; private set; }
    protected Process process { get; private set; }

    public event RunningStateChange StateChange;

    public bool IsOpenChrome
    {
      get { return chromeDriver != null || process != null; }
    }

    protected BaseChromeProfile(string ChromeDrivePath, bool HideCommandPromptWindow = true)
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
        StateChange?.Invoke(IsOpenChrome);
        return true;
      }
      return false;
    }

    protected virtual bool OpenChrome(CancellationTokenSource cancellationTokenSource, Func<ChromeOptions> chromeOptions)
    {
      if (!IsOpenChrome)
      {
        tokenSource = cancellationTokenSource;
        return OpenChrome(chromeOptions.Invoke());
      }
      return false;
    }

    protected virtual bool OpenChromeWithoutSelenium(string Arguments, string ChromePath = null)
    {
      if (!IsOpenChrome)
      {
        process = new Process();
        if (!string.IsNullOrEmpty(ChromePath)) process.StartInfo.FileName = ChromePath;
        else
        {
          string chrome64 = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
          string chrome86 = "C:\\Program Files (x86)\\Google\\Chrome\\Application\\chrome.exe";
          if (File.Exists(chrome64)) process.StartInfo.FileName = chrome64;
          else if (File.Exists(chrome86)) process.StartInfo.FileName = chrome86;
          else throw new FileNotFoundException("chrome.exe");
        }
        process.StartInfo.Arguments = Arguments;
        process.Start();
        StateChange?.Invoke(IsOpenChrome);
        return true;
      }
      return false;
    }

    protected virtual bool CloseChrome()
    {
      if (IsOpenChrome)
      {
        if (process?.HasExited == false) process?.Kill();
        process?.Dispose();
        process = null;
        chromeDriver?.Quit();
        chromeDriver = null;
        tokenSource?.Dispose();
        tokenSource = null;
        StateChange?.Invoke(IsOpenChrome);
        return true;
      }
      return false;
    }

    protected virtual void Stop() => tokenSource?.Cancel();

    protected virtual void Delay(int min, int max)
    {
      int time = rd.Next(min, max) / 100;
      for (int i = 0; i < time; i++)
      {
        Task.Delay(100).Wait();
        tokenSource?.Token.ThrowIfCancellationRequested();
      }
    }

    public virtual void SaveHtml(string path)
    {
      if (IsOpenChrome)
      {
        using StreamWriter streamWriter = new StreamWriter(path, false);
        streamWriter.Write(chromeDriver.PageSource);
        streamWriter.Flush();
      }
    }

    protected virtual ReadOnlyCollection<IWebElement> WaitUntilAll(IWebElement parent, By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
    {
      if (IsOpenChrome)
      {
        using CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
        while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
        {
          Delay(delay, delay);
          var eles = parent.FindElements(by);
          if (eles.Count > 0)
          {
            switch (waitFlag)
            {
              case ElementsIs.Exists: return eles;

              case ElementsIs.Visible:
                if (eles.All(x => x.Displayed)) return eles;
                break;

              case ElementsIs.Clickable:
                if (eles.All(x => x.Displayed && x.Enabled)) return eles;
                break;

              case ElementsIs.Selected:
                if (eles.All(x => x.Selected)) return eles;
                break;
            }
          }
          else
          {
            switch (waitFlag)
            {
              case ElementsIs.NotExists:
                return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
            }
          }
        }
      }
      return null;
    }

    protected virtual ReadOnlyCollection<IWebElement> WaitUntilAny(IWebElement parent, By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
    {
      if (IsOpenChrome)
      {
        CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
        while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
        {
          Delay(delay, delay);
          var eles = parent.FindElements(by);
          if (eles.Count > 0)
          {
            switch (waitFlag)
            {
              case ElementsIs.Exists: return eles;

              case ElementsIs.Visible:
                if (eles.Any(x => x.Displayed)) return eles;
                break;

              case ElementsIs.Clickable:
                if (eles.Any(x => x.Displayed && x.Enabled)) return eles;
                break;

              case ElementsIs.Selected:
                if (eles.Any(x => x.Selected)) return eles;
                break;
            }
          }
          else
          {
            switch (waitFlag)
            {
              case ElementsIs.NotExists:
                return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
            }
          }
        }
      }
      return null;
    }

    protected virtual ReadOnlyCollection<IWebElement> WaitUntilAll(By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
    {
      if (IsOpenChrome)
      {
        using CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
        while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
        {
          Delay(delay, delay);
          var eles = chromeDriver.FindElements(by);
          if (eles.Count > 0)
          {
            switch (waitFlag)
            {
              case ElementsIs.Exists: return eles;

              case ElementsIs.Visible:
                if (eles.All(x => x.Displayed)) return eles;
                break;

              case ElementsIs.Clickable:
                if (eles.All(x => x.Displayed && x.Enabled)) return eles;
                break;

              case ElementsIs.Selected:
                if (eles.All(x => x.Selected)) return eles;
                break;
            }
          }
          else
          {
            switch (waitFlag)
            {
              case ElementsIs.NotExists:
                return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
            }
          }
        }
      }
      return null;
    }

    protected virtual ReadOnlyCollection<IWebElement> WaitUntilAny(By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
    {
      if (IsOpenChrome)
      {
        CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
        while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
        {
          Delay(delay, delay);
          var eles = chromeDriver.FindElements(by);
          if (eles.Count > 0)
          {
            switch (waitFlag)
            {
              case ElementsIs.Exists: return eles;

              case ElementsIs.Visible:
                if (eles.Any(x => x.Displayed)) return eles;
                break;

              case ElementsIs.Clickable:
                if (eles.Any(x => x.Displayed && x.Enabled)) return eles;
                break;

              case ElementsIs.Selected:
                if (eles.Any(x => x.Selected)) return eles;
                break;
            }
          }
          else
          {
            switch (waitFlag)
            {
              case ElementsIs.NotExists:
                return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
            }
          }
        }
      }
      return null;
    }
  }
}