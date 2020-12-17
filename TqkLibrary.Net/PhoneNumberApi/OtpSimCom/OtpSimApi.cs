using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace TqkLibrary.Net.PhoneNumberApi.OtpSimCom
{
  public sealed class OtpSimApi : BaseApi
  {
    private const string EndPoint = "http://otpsim.com/api";

    public OtpSimApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<BaseResultData<List<DataNetwork>>> GetNetworks()
      => await RequestGet<BaseResultData<List<DataNetwork>>>(string.Format(EndPoint + "/networks?token={0}", ApiKey));

    public async Task<BaseResultData<List<DataService>>> GetServices()
      => await RequestGet<BaseResultData<List<DataService>>>(string.Format(EndPoint + "/networks?token={0}", ApiKey));

    public async Task<BaseResultData<PhoneRequestResult>> PhonesRequest(
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

      return await RequestGet<BaseResultData<PhoneRequestResult>>(EndPoint + "/phones/request?" + parameters.ToString());
    }

    public async Task<BaseResultData<PhoneRequestResult>> PhonesRequest(DataService dataService, string numberBuyBack)
    {
      if (null == dataService) throw new ArgumentNullException(nameof(dataService));
      if (string.IsNullOrEmpty(numberBuyBack)) throw new ArgumentNullException(nameof(numberBuyBack));

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["token"] = ApiKey;
      parameters["service"] = dataService.Id.ToString();
      parameters["numberBuyBack"] = numberBuyBack;

      return await RequestGet<BaseResultData<PhoneRequestResult>>(EndPoint + "/phones/request?" + parameters.ToString());
    }

    public async Task<BaseResultData<PhoneData>> GetPhoneMessage(PhoneRequestResult phoneRequestResult)
      => await RequestGet<BaseResultData<PhoneData>>($"{EndPoint}/sessions/{phoneRequestResult.Session}?token={ApiKey}");

    public async Task<BaseResultData<RefundData>> CancelGetPhoneMessage(PhoneRequestResult phoneRequestResult)
      => await RequestGet<BaseResultData<RefundData>>($"{EndPoint}/sessions/cancel?session={phoneRequestResult.Session}&token={ApiKey}");

    public async Task<BaseResultData<string>> ReportMessage(PhoneRequestResult phoneRequestResult)
      => await RequestGet<BaseResultData<string>>($"{EndPoint}/sessions/report?session={phoneRequestResult.Session}&token={ApiKey}");

    public async Task<BaseResultData<BalanceData>> UserBalance()
       => await RequestGet<BaseResultData<BalanceData>>($"{EndPoint}users/balance?token={ApiKey}");
  }
}