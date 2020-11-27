using System.Net.Http;
namespace TqkLibrary.Net.ProxysApi
{
  public static class TinsoftProxy
  {
    static readonly HttpClient httpClient = new HttpClient();
    static readonly string url = "http://proxy.tinsoftsv.com/api/changeProxy.php?key={0}&location=0";
    public static string Get(string Key)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(url,Key)))
      {
        using (HttpResponseMessage httpResponseMessage = httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead).Result)
        {
          return httpResponseMessage.Content.ReadAsStringAsync().Result;
        }
      }
    }
  }
}
