using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TqkLibrary.Net.ProxysApi.TinsoftProxyCom
{
  /// <summary>
  /// http://proxy.tinsoftsv.com/api/document_vi.php
  /// </summary>
  public sealed class TinsoftProxyApi : BaseApi
  {
    internal const string EndPoint = "http://proxy.tinsoftsv.com/api";

    public TinsoftProxyApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<ProxyResult> ChangeProxy(int location = 0)
      => await RequestGet<ProxyResult>(string.Format(EndPoint + "/changeProxy.php?key={0}&location={1}", ApiKey, location)).ConfigureAwait(false);

    public async Task<KeyInfo> GetKeyInfo()
      => await RequestGet<KeyInfo>(string.Format(EndPoint + "/getKeyInfo.php?key={0}", ApiKey)).ConfigureAwait(false);

    public async Task<KeyInfo> DeleteKey()
      => await RequestGet<KeyInfo>(string.Format(EndPoint + "/deleteKey.php?key={0}", ApiKey)).ConfigureAwait(false);
  }
}