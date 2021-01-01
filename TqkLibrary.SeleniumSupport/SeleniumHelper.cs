using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.ObjectModel;

namespace TqkLibrary.SeleniumSupport
{
  public static class SeleniumHelper
  {
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
  }
}