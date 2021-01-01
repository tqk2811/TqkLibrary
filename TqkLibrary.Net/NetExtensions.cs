using Newtonsoft.Json;
using System.Net.Http;

namespace TqkLibrary.Net
{
  internal static class NetExtensions
  {
    internal static HttpClient httpClient = new HttpClient();

    internal static JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings()
    {
      NullValueHandling = NullValueHandling.Ignore
    };
  }
}