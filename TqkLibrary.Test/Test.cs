using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TqkLibrary.Test
{
  [TestClass]
  public class Test
  {
    [TestMethod]
    public void TestExtension()
    {
      string result = LibExtensions.convertToUnSign3("dasda:dsádas");
      Console.WriteLine(result);
      return;
    }
  }
}