using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TqkLibrary.Net.Facebook;

namespace TqkLibrary.Test.Net
{
  [TestClass]
  public class FacebookGraphTest
  {
    [TestMethod]
    public void TestMethod1()
    {
      FacebookApi facebookApi = new FacebookApi();
      var user = facebookApi.UserInfo("EAAGNO4a7r2wBAFvzaBwon1c5LC6gdp2wIuN9vlnoKL7RrRpBNpRiJmxLgEbMgRvUWoARxvpNaplzRkZB4QRKQddfjzIRS7YXVicHnjVd5CDLsLMfHOvNkZCdgIZBDR6nEoiAMSQRyReQH1GoohEcqHFwERpipucYgTZC2uzKZAwIe4uTZAlbRx",
        "birthday,name,email,link").Result;
    }
  }
}
