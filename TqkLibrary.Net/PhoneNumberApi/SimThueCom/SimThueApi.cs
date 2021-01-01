using System;
using System.Threading.Tasks;

namespace TqkLibrary.Net.PhoneNumberApi.SimThueCom
{
  /// <summary>
  /// https://simthue.com/vi/api/index
  /// </summary>
  public sealed class SimThueApi : BaseApi
  {
    private const string EndPoint = "http://api.simthue.com";

    public SimThueApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<BalanceResult> GetBalance()
      => await RequestGet<BalanceResult>(string.Format(EndPoint + "/balance?key={0}", ApiKey)).ConfigureAwait(false);

    public async Task<ServicesResult> GetAvailableServices()
      => await RequestGet<ServicesResult>(string.Format(EndPoint + "/service?key={0}", ApiKey)).ConfigureAwait(false);

    public async Task<RequestResult> CreateRequest(ServiceResult serviceResult)
    {
      if (null == serviceResult) throw new ArgumentNullException(nameof(serviceResult));
      return await RequestGet<RequestResult>(string.Format(EndPoint + "/create?key={0}&service_id={1}", ApiKey, serviceResult.Id)).ConfigureAwait(false);
    }

    public async Task<CheckResult> CheckRequest(RequestResult createResult)
    {
      if (null == createResult) throw new ArgumentNullException(nameof(createResult));
      return await RequestGet<CheckResult>(string.Format(EndPoint + "/check?key={0}&id={1}", ApiKey, createResult.Id)).ConfigureAwait(false);
    }

    public async Task<RequestResult> CancelRequest(RequestResult createResult)
    {
      if (null == createResult) throw new ArgumentNullException(nameof(createResult));
      return await RequestGet<RequestResult>(string.Format(EndPoint + "/cancel?key={0}&id={1}", ApiKey, createResult.Id)).ConfigureAwait(false);
    }

    public async Task<RequestResult> FinishRequest(RequestResult createResult)
    {
      if (null == createResult) throw new ArgumentNullException(nameof(createResult));
      return await RequestGet<RequestResult>(string.Format(EndPoint + "/cancel?key={0}&id={1}", ApiKey, createResult.Id)).ConfigureAwait(false);
    }
  }
}