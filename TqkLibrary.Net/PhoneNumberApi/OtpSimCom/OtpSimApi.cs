using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web;

namespace TqkLibrary.Net.PhoneNumberApi.OtpSimCom
{
  public sealed class OtpSimApi : BaseApi
  {
    private const string EndPoint = "http://otpsim.com/api";

    public OtpSimApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<BaseResult<List<DataNetwork>>> GetNetworks()
      => await RequestGet<BaseResult<List<DataNetwork>>>(string.Format(EndPoint + "/networks?token={0}", ApiKey)).ConfigureAwait(false);

    public async Task<BaseResult<List<DataService>>> GetServices()
      => await RequestGet<BaseResult<List<DataService>>>(string.Format(EndPoint + "/networks?token={0}", ApiKey)).ConfigureAwait(false);

    public async Task<BaseResult<PhoneRequestResult>> PhonesRequest(
      DataService dataService,
      List<DataNetwork> dataNetworks = null,
      List<string> prefixs = null,
      List<string> exceptPrefixs = null)
    {
      if (null == dataService) throw new ArgumentNullException(nameof(dataService));

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["token"] = ApiKey;
      parameters["service"] = dataService.Id.ToString();
      if (null != dataNetworks) parameters["network"] = string.Join(",", dataNetworks);
      if (null != prefixs) parameters["prefix"] = string.Join(",", prefixs);
      if (null != exceptPrefixs) parameters["exceptPrefix"] = string.Join(",", exceptPrefixs);

      return await RequestGet<BaseResult<PhoneRequestResult>>(EndPoint + "/phones/request?" + parameters.ToString()).ConfigureAwait(false);
    }

    public async Task<BaseResult<PhoneRequestResult>> PhonesRequest(DataService dataService, string numberBuyBack)
    {
      if (null == dataService) throw new ArgumentNullException(nameof(dataService));
      if (string.IsNullOrEmpty(numberBuyBack)) throw new ArgumentNullException(nameof(numberBuyBack));

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["token"] = ApiKey;
      parameters["service"] = dataService.Id.ToString();
      parameters["numberBuyBack"] = numberBuyBack;

      return await RequestGet<BaseResult<PhoneRequestResult>>(EndPoint + "/phones/request?" + parameters.ToString()).ConfigureAwait(false);
    }

    public async Task<BaseResult<PhoneData>> GetPhoneMessage(PhoneRequestResult phoneRequestResult)
      => await RequestGet<BaseResult<PhoneData>>($"{EndPoint}/sessions/{phoneRequestResult.Session}?token={ApiKey}").ConfigureAwait(false);

    public async Task<BaseResult<RefundData>> CancelGetPhoneMessage(PhoneRequestResult phoneRequestResult)
      => await RequestGet<BaseResult<RefundData>>($"{EndPoint}/sessions/cancel?session={phoneRequestResult.Session}&token={ApiKey}").ConfigureAwait(false);

    public async Task<BaseResult<string>> ReportMessage(PhoneRequestResult phoneRequestResult)
      => await RequestGet<BaseResult<string>>($"{EndPoint}/sessions/report?session={phoneRequestResult.Session}&token={ApiKey}").ConfigureAwait(false);

    public async Task<BaseResult<BalanceData>> UserBalance()
       => await RequestGet<BaseResult<BalanceData>>($"{EndPoint}users/balance?token={ApiKey}").ConfigureAwait(false);
  }
}