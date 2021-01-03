using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.SeleniumSupport
{
  public class FrameSwitch : IDisposable
  {
    private readonly ChromeDriver chromeDriver;

    internal FrameSwitch(ChromeDriver chromeDriver, IWebElement webElement)
    {
      this.chromeDriver = chromeDriver;
      chromeDriver.SwitchTo().Frame(webElement ?? throw new ArgumentNullException(nameof(webElement)));
    }

    public void Dispose()
    {
      chromeDriver.SwitchTo().ParentFrame();
    }
  }
}