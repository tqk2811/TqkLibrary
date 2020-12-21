using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TqkLibrary.Net
{
  public abstract class BaseApi
  {
    protected readonly string ApiKey;

    internal BaseApi(string ApiKey)
    {
      if (string.IsNullOrEmpty(ApiKey)) throw new ArgumentNullException(nameof(ApiKey));
      this.ApiKey = ApiKey;
    }

    protected async Task<T> RequestGet<T>(string Url)
    {
      using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, Url);
      httpRequestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
      using HttpResponseMessage httpResponseMessage = await NetExtensions.httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead).ConfigureAwait(false);
      return JsonConvert.DeserializeObject<T>(await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false));
    }
  }
}