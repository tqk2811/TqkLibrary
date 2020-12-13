using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;

namespace TqkLibrary.Net.RentCodeCo
{

  public sealed partial class RentCode
  {
    static readonly HttpClient httpClient = new HttpClient();
    const string EndPoint = "https://api.rentcode.net/api/v2/";
    readonly string ApiKey;
    public RentCode(string ApiKey)
    {
      if (string.IsNullOrEmpty(ApiKey)) throw new ArgumentNullException(nameof(ApiKey));
      this.ApiKey = ApiKey;
    }


    public async Task<RentCodeResult> Request(
      int? MaximumSms = null,
      bool? AllowVoiceSms = null,
      NetworkProvider networkProvider = NetworkProvider.None, 
      ServiceProviderId serviceProviderId = ServiceProviderId.Facebook)
    {
      if (serviceProviderId == ServiceProviderId.None) throw new RentCodeException("serviceProviderId is required");

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["apiKey"] = ApiKey;
      parameters["ServiceProviderId"] = ((int)serviceProviderId).ToString();
      if(networkProvider != NetworkProvider.None) parameters["NetworkProvider"] = ((int)networkProvider).ToString();
      if(MaximumSms != null) parameters["MaximumSms"] = MaximumSms.Value.ToString();
      if (AllowVoiceSms != null) parameters["AllowVoiceSms"] = AllowVoiceSms.Value.ToString();

      string url = EndPoint + "order/request?" + parameters.ToString();
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url))
      {
        httpRequestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        //httpRequestMessage.Headers.Referrer = new Uri(EndPoint);
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          string result = await httpResponseMessage.Content.ReadAsStringAsync();
          //Console.WriteLine("RentCode Request:" + result);
          return JsonConvert.DeserializeObject<RentCodeResult>(result);
        }
      }
    }

    public async Task<RentCodeResult> RequestHolding(
      int Duration = 300,
      int Unit = 1,
      NetworkProvider networkProvider = NetworkProvider.None)
    {
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["apiKey"] = ApiKey;
      parameters["Duration"] = Duration.ToString();
      parameters["Unit"] = Unit.ToString();
      if (networkProvider != NetworkProvider.None) parameters["NetworkProvider"] = ((int)networkProvider).ToString();

      string url = EndPoint + $"order/request-holding?apiKey={ApiKey}&Duration={Duration}&Unit=1&NetworkProvider={(int)networkProvider}";
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url))
      {
        httpRequestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseContentRead))
        {
          return JsonConvert.DeserializeObject<RentCodeResult>(await httpResponseMessage.Content.ReadAsStringAsync());
        }
      }
    }

    public async Task<RentCodeCheckOrderResults> Check(RentCodeResult rentCodeResult)
    {
      string url = EndPoint + $"order/{rentCodeResult.Id}/check?apiKey={ApiKey}";
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url))
      {
        httpRequestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          string result = await httpResponseMessage.Content.ReadAsStringAsync();
          //Console.WriteLine("RentCode Check:" + result);
          return JsonConvert.DeserializeObject<RentCodeCheckOrderResults>(result);
        }
      }
    }
  }
}
