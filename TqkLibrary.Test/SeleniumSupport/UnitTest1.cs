using Microsoft.VisualStudio.TestTools.UnitTesting;
using TqkLibrary.SeleniumSupport;

namespace TqkLibrary.Test.SeleniumSupport
{
  [TestClass]
  public class UnitTest1
  {
    [TestMethod]
    public void TestMethod1()
    {
      int a = ChromeDriverUpdater.Download("D:\\temp", 86).Result;
    }
  }
}
