using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

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