﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TqkLibrary.Net.PhoneNumberApi.ChoThueSimCodeCom
{
  /// <summary>
  /// https://chothuesimcode.com/account/api
  /// </summary>
  public sealed class ChoThueSimCodeApi : BaseApi
  {
    private const string EndPoint = "https://chothuesimcode.com/api?";

    public ChoThueSimCodeApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<BaseResult<ResponseCode, AccountInfo>> GetAccountInfo()
      => await RequestGet<BaseResult<ResponseCode, AccountInfo>>(string.Format(EndPoint + "act=account&apik={0}", ApiKey)).ConfigureAwait(false);

    public async Task<BaseResult<ResponseCode, AppInfo>> GetAppRunning()
      => await RequestGet<BaseResult<ResponseCode, AppInfo>>(string.Format(EndPoint + "act=app&apik={0}", ApiKey)).ConfigureAwait(false);

    public async Task<BaseResult<ResponseCodeGetPhoneNumber, PhoneNumberResult>> GetPhoneNumber(int appId, Carrier carrier = Carrier.None)
    {
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["act"] = "number";
      parameters["apik"] = ApiKey;
      parameters["appId"] = appId.ToString();
      if (carrier != Carrier.None) parameters["carrier"] = carrier.ToString();
      return await RequestGet<BaseResult<ResponseCodeGetPhoneNumber, PhoneNumberResult>>(EndPoint + parameters.ToString()).ConfigureAwait(false);
    }

    public async Task<BaseResult<ResponseCodeGetPhoneNumber, PhoneNumberResult>> GetPhoneNumber(int appId, string number)
    {
      if (string.IsNullOrEmpty(number)) throw new ArgumentNullException(nameof(number));
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["act"] = "number";
      parameters["apik"] = ApiKey;
      parameters["appId"] = appId.ToString();
      parameters["number"] = number;
      return await RequestGet<BaseResult<ResponseCodeGetPhoneNumber, PhoneNumberResult>>(EndPoint + parameters.ToString()).ConfigureAwait(false);
    }

    public async Task<BaseResult<ResponseCodeMessage, MessageResult>> GetMessage(PhoneNumberResult phoneNumberResult)
    {
      if (null == phoneNumberResult) throw new ArgumentNullException(nameof(phoneNumberResult));
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["act"] = "code";
      parameters["apik"] = ApiKey;
      parameters["id"] = phoneNumberResult.Id.ToString();
      return await RequestGet<BaseResult<ResponseCodeMessage, MessageResult>>(EndPoint + parameters.ToString()).ConfigureAwait(false);
    }

    public async Task<BaseResult<ResponseCodeCancelMessage, RefundInfo>> CancelGetMessage(PhoneNumberResult phoneNumberResult)
    {
      if (null == phoneNumberResult) throw new ArgumentNullException(nameof(phoneNumberResult));
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["act"] = "expired";
      parameters["apik"] = ApiKey;
      parameters["id"] = phoneNumberResult.Id.ToString();
      return await RequestGet<BaseResult<ResponseCodeCancelMessage, RefundInfo>>(EndPoint + parameters.ToString()).ConfigureAwait(false);
    }
  }
}