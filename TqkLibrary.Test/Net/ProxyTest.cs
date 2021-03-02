using Microsoft.VisualStudio.TestTools.UnitTesting;
using TqkLibrary.Net.ProxysApi.TinsoftProxyCom;
namespace TqkLibrary.Test.Net
{
  [TestClass]
  public class ProxyTest
  {
    [TestMethod]
    public void Tinsoft()
    {
      TinsoftProxyApi tinsoftProxyApi = new TinsoftProxyApi("None");
      var locations = tinsoftProxyApi.GetLocations().Result;
      Assert.AreNotEqual(locations.data.Count, 0);
    }
  }
}
