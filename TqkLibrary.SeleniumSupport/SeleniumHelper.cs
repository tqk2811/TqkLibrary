using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System.Collections.ObjectModel;

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
  }
}