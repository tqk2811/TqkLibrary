using System.Net.Http;
using System.Threading.Tasks;

namespace TqkLibrary.AspNetCoreLibrary.Common
{
  public static class SocialApiWrap
  {
    private static readonly HttpClient httpClient = new HttpClient();

    public static async Task<string> GoogleGetEmail(string access_token)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v1/userinfo?alt=json"))
      {
        httpRequestMessage.Headers.Add("Authorization", "Bearer " + access_token);
        using (var respone = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await respone.Content.ReadAsStringAsync();
        }
      }
    }

    public static async Task<string> FacebookGetEmail(string access_token, string user_id)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "https://graph.facebook.com/v8.0/" + user_id + "?fields=email"))
      {
        httpRequestMessage.Headers.Add("Authorization", "Bearer " + access_token);
        using (var respone = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await respone.Content.ReadAsStringAsync();
        }
      }
    }
  }
}