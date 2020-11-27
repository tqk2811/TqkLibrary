using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

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

    public static void JsWriteText(this IWebElement webElement, ChromeDriver drive, string text)
    {
      drive.ExecuteScript($"arguments[0].value = \"{text}\";", webElement);
    }
    public static void JsWriteText(this ChromeDriver drive, IWebElement webElement, string text)
    {
      drive.ExecuteScript($"arguments[0].value = \"{text}\";", webElement);
    }
  }
}
