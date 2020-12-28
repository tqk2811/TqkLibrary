using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace TqkLibrary.SeleniumSupport
{
  public static class SeleniumHelper
  {
    public static void JsClick(this IWebElement webElement, ChromeDriver drive)
    {
      drive.ExecuteScript("arguments[0].click();", webElement);
    }

    public static void JsClick(this ChromeDriver drive, IWebElement webElement)
    {
      drive.ExecuteScript("arguments[0].click();", webElement);
    }

    public static void JsScrollIntoView(this IWebElement webElement, ChromeDriver drive)
    {
      drive.ExecuteScript("arguments[0].scrollIntoView();", webElement);
    }

    public static void JsScrollIntoView(this ChromeDriver drive, IWebElement webElement)
    {
      drive.ExecuteScript("arguments[0].scrollIntoView();", webElement);
    }

    public static void JsSetInputText(this IWebElement webElement, ChromeDriver drive, string text)
    {
      drive.ExecuteScript($"arguments[0].value = \"{text}\";", webElement);
    }

    public static void JsSetInputText(this ChromeDriver drive, IWebElement webElement, string text)
    {
      drive.ExecuteScript($"arguments[0].value = \"{text}\";", webElement);
    }

    public static ReadOnlyCollection<IWebElement> ThrowIfNull(this ReadOnlyCollection<IWebElement> readOnlyCollection, string throwText)
    {
      if (null == readOnlyCollection) throw new ChromeAutoException(throwText);
      return readOnlyCollection;
    }

    public static ReadOnlyCollection<IWebElement> ThrowIfNullOrCountZero(this ReadOnlyCollection<IWebElement> readOnlyCollection, string throwText)
    {
      if (null == readOnlyCollection || readOnlyCollection.Count == 0) throw new ChromeAutoException(throwText);
      return readOnlyCollection;
    }

    public static ChromeOptions AddProfilePath(this ChromeOptions chromeOptions, string ProfilePath)
    {
      if (string.IsNullOrEmpty(ProfilePath)) throw new ArgumentNullException(nameof(ProfilePath));
      chromeOptions.AddArgument("--user-data-dir=" + ProfilePath);
      return chromeOptions;
    }

    public static ReadOnlyCollection<IWebElement> WaitUntil(this ISearchContext searchContext, By by, Func<ReadOnlyCollection<IWebElement>, bool> func,
      bool isThrow = false, int delay = 200, int timeout = 10000, CancellationTokenSource tokenSource = null)
    {
      using CancellationTokenSource timeoutToken = new CancellationTokenSource(timeout);
      while (!timeoutToken.IsCancellationRequested)
      {
        Task.Delay(delay).Wait();
        tokenSource?.Token.ThrowIfCancellationRequested();
        var eles = searchContext.FindElements(by);
        if (func(eles)) return eles;
      }
      if (isThrow) throw new ChromeAutoException(by.ToString());
      return null;
    }
  }
}