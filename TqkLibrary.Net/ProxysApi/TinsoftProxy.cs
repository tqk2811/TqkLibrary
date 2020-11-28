using Newtonsoft.Json;
using System;
using System.Net.Http;
namespace TqkLibrary.Net.ProxysApi
{
  public class TinSoftProxy
  {
    public bool success { get; set; }
    public string proxy { get; set; }
    public int? next_change { get; set; }
    public int? timeout { get; set; }
    public string description { get; set; }
  }

  public static class TinsoftProxyHelper
  {
    static readonly HttpClient httpClient = new HttpClient();
    static readonly string url = "http://proxy.tinsoftsv.com/api/changeProxy.php?key={0}&location={1}";
    public static TinSoftProxy Get(string Key,string location = "0")
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(url,Key, location)))
      {
        using (HttpResponseMessage httpResponseMessage = httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead).Result)
        {
          return JsonConvert.DeserializeObject<TinSoftProxy>(httpResponseMessage.Content.ReadAsStringAsync().Result);
        }
      }
    }
  }
}
