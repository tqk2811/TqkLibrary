using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace TqkLibrary.Net.ProxysApi.TinsoftProxyCom
{
  public sealed class TinsoftUserApi : BaseApi
  {
    public TinsoftUserApi(string UserApiKey) : base(UserApiKey)
    {
    }

    public async Task<UserKeyInfo> GetUserKeys()
      => await RequestGet<UserKeyInfo>(string.Format(TinsoftProxyApi.EndPoint + "/getUserKeys.php?key={0}", ApiKey));

    public async Task<UserInfo> GetUserInfo()
      => await RequestGet<UserInfo>(string.Format(TinsoftProxyApi.EndPoint + "/getUserInfo.php?key={0}", ApiKey));

    public async Task<OrderResult> OrderKeys(int quantity, DateTime dateTime, TinsoftVip tinsoftVip)
      => await RequestGet<OrderResult>(string.Format(
        TinsoftProxyApi.EndPoint + "/orderKeys.php?key={0}&quantity={1}&days={2}&vip={3}",
        ApiKey,
        quantity,
        dateTime.ToString("dd-MM-yyyy HH:mm:ss"),
        (int)tinsoftVip));

    public async Task<BaseResult> ExtendKey(DateTime dateTime, string proxyKey)
      => await RequestGet<BaseResult>(string.Format(
        TinsoftProxyApi.EndPoint + "/extendKey.php?key={0}&days={1}&proxy_key={2}",
        ApiKey,
        dateTime.ToString("dd-MM-yyyy HH:mm:ss"),
        proxyKey));
  }
}