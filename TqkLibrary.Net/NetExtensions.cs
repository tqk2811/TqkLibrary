using Newtonsoft.Json;
using System.Net.Http;
using System.Threading.Tasks;

namespace TqkLibrary.Net
{
  public static class NetExtensions
  {
    internal static HttpClient httpClient = new HttpClient();

    internal static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
    {
      NullValueHandling = NullValueHandling.Ignore
    };

    public static async Task<MyIp> GetCurrentIpAdreess()
    {
      using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://api.myip.com");
      using HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
      return JsonConvert.DeserializeObject<MyIp>(await httpResponseMessage.EnsureSuccessStatusCode().Content.ReadAsStringAsync().ConfigureAwait(false));
    }
  }
}