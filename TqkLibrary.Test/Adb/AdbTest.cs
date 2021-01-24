using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TqkLibrary.Adb;
namespace TqkLibrary.Test.Adb
{
  [TestClass]
  public class AdbTest
  {
    [TestMethod]
    [ExpectedException(typeof(AdbTimeoutException))]
    public void TestTimeout()
    {
      BaseAdb baseAdb = new BaseAdb();
      baseAdb.TimeoutDefault = 1;
      baseAdb.Swipe(10, 10, 200, 200, 1000);
    }
  }
}
