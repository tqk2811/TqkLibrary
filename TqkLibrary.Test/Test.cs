using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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