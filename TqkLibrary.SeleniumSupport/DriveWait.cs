using OpenQA.Selenium;
using System;
using System.Collections.ObjectModel;

namespace TqkLibrary.SeleniumSupport
{
  public class DriveWait
  {
    public TimeSpan TimeOut;
    readonly IWebDriver drive;
    public int Interval { get; set; } = 100;
    public DriveWait(IWebDriver drive, TimeSpan timeout,int interval = 100)
    {
      this.drive = drive ?? throw new ArgumentNullException(nameof(drive));
      this.TimeOut = timeout;
      this.Interval = interval;
    }



    //public ReadOnlyCollection<IWebElement> WaitUntil(Func<By, ReadOnlyCollection<IWebElement>> func)
    //{
    //  DateTime endTime = DateTime.Now.Add(TimeOut);

    //  while (DateTime.Now < endTime)
    //  {
    //    var result = drive.FindElements(by);
    //    if (result.Count > 0) return result;
    //  }
    //  return null;
    //}
  }

  public static class ExpectedConditions
  {

  }
}
