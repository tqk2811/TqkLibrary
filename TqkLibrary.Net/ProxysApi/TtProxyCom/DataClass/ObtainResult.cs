using System.Collections.Generic;
using Newtonsoft.Json;

namespace TqkLibrary.Net.ProxysApi.TtProxyCom
{
  public class ObtainResult
  {
    [JsonProperty("todayObtain")]
    public int? TodayObtain { get; set; }

    [JsonProperty("ipLeft")]
    public int? IpLeft { get; set; }

    [JsonProperty("trafficLeft")]
    public long TrafficLeft { get; set; }

    [JsonProperty("proxies")]
    public List<string> Proxies { get; set; }
  }
}
