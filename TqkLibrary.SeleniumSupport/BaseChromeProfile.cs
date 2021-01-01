using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TqkLibrary.SeleniumSupport
{
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

    protected BaseChromeProfile() : this(null)
    {
    }

    protected BaseChromeProfile(string ChromeDrivePath, bool HideCommandPromptWindow = true)
    {
      if (string.IsNullOrEmpty(ChromeDrivePath))
      {
        ChromeDrivePath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) + "\\ChromeDriver";
      }
      service = ChromeDriverService.CreateDefaultService(ChromeDrivePath);
      service.HideCommandPromptWindow = HideCommandPromptWindow;
    }

    /// <summary>
    /// <strong>AddArguments:</strong>
    /// <para>--disable-notifications<br/>
    /// --disable-web-security<br/>
    /// --disable-blink-features<br/>
    /// --disable-blink-features=AutomationControlled<br/>
    /// --disable-infobars<br/>
    /// --ignore-certificate-errors<br/>
    /// --ignore-certificate-errors<br/>
    /// --allow-running-insecure-content</para>
    ///
    /// <strong>AddExcludedArgument:</strong>
    /// <para>enable-automation</para>
    ///
    /// <strong>AddAdditionalCapability:</strong>
    /// <para>useAutomationExtension false</para>
    ///
    /// <strong>AddUserProfilePreference:</strong>
    /// <para>credentials_enable_service false<br/>
    /// profile.password_manager_enabled false</para>
    /// </summary>
    /// <returns></returns>
    protected virtual ChromeOptions DefaultChromeOptions()
    {
      ChromeOptions options = new ChromeOptions();
      options.AddArgument("--disable-notifications");
      options.AddArgument("--disable-web-security");
      options.AddArgument("--disable-blink-features");
      options.AddArgument("--disable-blink-features=AutomationControlled");
      options.AddArgument("--disable-infobars");
      options.AddArgument("--ignore-certificate-errors");
      options.AddArgument("--allow-running-insecure-content");
      options.AddAdditionalCapability("useAutomationExtension", false);
      options.AddExcludedArgument("enable-automation");
      //options.c
      //disable ask password
      options.AddUserProfilePreference("credentials_enable_service", false);
      options.AddUserProfilePreference("profile.password_manager_enabled", false);
      return options;
    }

    public virtual bool OpenChrome(ChromeOptions chromeOptions)
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

    public virtual bool OpenChrome(CancellationTokenSource cancellationTokenSource, Func<ChromeOptions> chromeOptions)
    {
      if (!IsOpenChrome)
      {
        tokenSource = cancellationTokenSource;
        return OpenChrome(chromeOptions.Invoke());
      }
      return false;
    }

    public virtual bool OpenChromeWithoutSelenium(string Arguments, string ChromePath = null)
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

    public virtual bool CloseChrome()
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

    public virtual void Stop() => tokenSource?.Cancel();

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

    protected void SwitchToFrame(By by) => chromeDriver.SwitchTo().Frame(WaitUntil(by, ElementsExists, true).First());

    protected ReadOnlyCollection<IWebElement> FindElements(By by) => chromeDriver.FindElements(by);

    #region WaitUntil

    #region Func

    protected bool ElementsExists(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.Count > 0) return true;
      return false;
    }

    //protected bool ElementsNotExists(ReadOnlyCollection<IWebElement> webElements) => !ElementsExists(webElements);

    protected bool AllElementsVisible(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.All(x => x.Displayed) == true) return true;
      return false;
    }

    protected bool AnyElementsVisible(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.Any(x => x.Displayed) == true) return true;
      return false;
    }

    protected bool AllElementsClickable(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.All(x => x.Displayed && x.Enabled) == true) return true;
      return false;
    }

    protected bool AnyElementsClickable(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.Any(x => x.Displayed && x.Enabled) == true) return true;
      return false;
    }

    protected bool AllElementsSelected(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.All(x => x.Selected) == true) return true;
      return false;
    }

    protected bool AnyElementsSelected(ReadOnlyCollection<IWebElement> webElements)
    {
      if (webElements?.Any(x => x.Selected) == true) return true;
      return false;
    }

    #endregion Func

    protected ReadOnlyCollection<IWebElement> WaitUntil(By by, Func<ReadOnlyCollection<IWebElement>, bool> func, bool isThrow = true, int delay = 500, int timeout = 10000)
    => WaitUntil_(chromeDriver, by, func, isThrow, delay, timeout);

    protected ReadOnlyCollection<IWebElement> WaitUntil(IWebElement webElement, By by, Func<ReadOnlyCollection<IWebElement>, bool> func, bool isThrow = true, int delay = 500, int timeout = 10000)
    => WaitUntil_(webElement, by, func, isThrow, delay, timeout);

    private ReadOnlyCollection<IWebElement> WaitUntil_(ISearchContext searchContext, By by, Func<ReadOnlyCollection<IWebElement>, bool> func,
      bool isThrow = true, int delay = 200, int timeout = 10000)
    {
      using CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
      while (!timeoutToken.IsCancellationRequested)
      {
        var eles = searchContext.FindElements(by);
        if (func(eles)) return eles;
        Task.Delay(delay).Wait();
        tokenSource?.Token.ThrowIfCancellationRequested();
      }
      if (isThrow) throw new ChromeAutoException(by.ToString());
      return null;
    }

    #endregion WaitUntil

    #region JSDropFile

    private const string JsDropFile = @"var target = arguments[0],
offsetX = arguments[1],
offsetY = arguments[2],
document = target.ownerDocument || document,
window = document.defaultView || window;

var input = document.createElement('INPUT');
input.type = 'file';
input.style.display = 'none';
input.onchange = function () {
  var rect = target.getBoundingClientRect(),
    x = rect.left + (offsetX || (rect.width >> 1)),
    y = rect.top + (offsetY || (rect.height >> 1)),
    dataTransfer = { files: this.files };

  ['dragenter', 'dragover', 'drop'].forEach(function (name) {
    var evt = document.createEvent('MouseEvent');
    evt.initMouseEvent(name, !0, !0, window, 0, 0, 0, x, y, !1, !1, !1, !1, 0, null);
    evt.dataTransfer = dataTransfer;
    target.dispatchEvent(evt);
  });
  setTimeout(function () { document.body.removeChild(input); }, 25);
}
document.body.appendChild(input);
return input;";

    protected void DropFile(string file, IWebElement webElement, int offsetX, int offsetY)
    {
      IWebElement input = (IWebElement)chromeDriver.ExecuteScript(JsDropFile, webElement, offsetX, offsetY);
      input.SendKeys(file);
    }

    #endregion JSDropFile

    #region JsClick

    protected void JsDoubleClick(IWebElement webElement) => chromeDriver.ExecuteScript(@"var evt = document.createEvent('MouseEvents');
evt.initMouseEvent('dblclick',true, true, window, 0, 0, 0, 0, 0, false, false, false, false, 0,null);
arguments[0].dispatchEvent(evt);", webElement);

    protected void JsClick(IWebElement webElement) => chromeDriver.ExecuteScript("arguments[0].click();", webElement);

    #endregion JsClick

    protected void JsScrollIntoView(IWebElement webElement) => chromeDriver.ExecuteScript("arguments[0].scrollIntoView();", webElement);

    protected void JsSetInputText(IWebElement webElement, string text) => chromeDriver.ExecuteScript($"arguments[0].value = \"{text}\";", webElement);
  }
}

//protected virtual ReadOnlyCollection<IWebElement> WaitUntilAll(IWebElement parent, By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
//{
//  if (IsOpenChrome)
//  {
//    using CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
//    while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
//    {
//      Delay(delay, delay);
//      var eles = parent.FindElements(by);
//      if (eles.Count > 0)
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.Exists: return eles;

//          case ElementsIs.Visible:
//            if (eles.All(x => x.Displayed)) return eles;
//            break;

//          case ElementsIs.Clickable:
//            if (eles.All(x => x.Displayed && x.Enabled)) return eles;
//            break;

//          case ElementsIs.Selected:
//            if (eles.All(x => x.Selected)) return eles;
//            break;
//        }
//      }
//      else
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.NotExists:
//            return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
//        }
//      }
//    }
//  }
//  return null;
//}

//protected virtual ReadOnlyCollection<IWebElement> WaitUntilAny(IWebElement parent, By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
//{
//  if (IsOpenChrome)
//  {
//    CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
//    while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
//    {
//      Delay(delay, delay);
//      var eles = parent.FindElements(by);
//      if (eles.Count > 0)
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.Exists: return eles;

//          case ElementsIs.Visible:
//            if (eles.Any(x => x.Displayed)) return eles;
//            break;

//          case ElementsIs.Clickable:
//            if (eles.Any(x => x.Displayed && x.Enabled)) return eles;
//            break;

//          case ElementsIs.Selected:
//            if (eles.Any(x => x.Selected)) return eles;
//            break;
//        }
//      }
//      else
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.NotExists:
//            return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
//        }
//      }
//    }
//  }
//  return null;
//}

//protected virtual ReadOnlyCollection<IWebElement> WaitUntilAll(By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
//{
//  if (IsOpenChrome)
//  {
//    using CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
//    while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
//    {
//      Delay(delay, delay);
//      var eles = chromeDriver.FindElements(by);
//      if (eles.Count > 0)
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.Exists: return eles;

//          case ElementsIs.Visible:
//            if (eles.All(x => x.Displayed)) return eles;
//            break;

//          case ElementsIs.Clickable:
//            if (eles.All(x => x.Displayed && x.Enabled)) return eles;
//            break;

//          case ElementsIs.Selected:
//            if (eles.All(x => x.Selected)) return eles;
//            break;
//        }
//      }
//      else
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.NotExists:
//            return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
//        }
//      }
//    }
//  }
//  return null;
//}

//protected virtual ReadOnlyCollection<IWebElement> WaitUntilAny(By by, ElementsIs waitFlag = ElementsIs.Exists, int delay = 500, int timeout = 10000, CancellationTokenSource tokenSource = null)
//{
//  if (IsOpenChrome)
//  {
//    CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
//    while (!timeoutToken.IsCancellationRequested && tokenSource?.IsCancellationRequested != true)
//    {
//      Delay(delay, delay);
//      var eles = chromeDriver.FindElements(by);
//      if (eles.Count > 0)
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.Exists: return eles;

//          case ElementsIs.Visible:
//            if (eles.Any(x => x.Displayed)) return eles;
//            break;

//          case ElementsIs.Clickable:
//            if (eles.Any(x => x.Displayed && x.Enabled)) return eles;
//            break;

//          case ElementsIs.Selected:
//            if (eles.Any(x => x.Selected)) return eles;
//            break;
//        }
//      }
//      else
//      {
//        switch (waitFlag)
//        {
//          case ElementsIs.NotExists:
//            return new ReadOnlyCollection<IWebElement>(new List<IWebElement>());
//        }
//      }
//    }
//  }
//  return null;
//}