using Newtonsoft.Json;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace TqkLibrary.Net.Captcha
{
  public enum TwoCaptchaState
  {
    NotReady,
    Error,
    Success
  }


  public class TwoCaptchaResponse
  {
    public int status { get; set; }
    public string request { get; set; }
    public override string ToString()
    {
      return $"request: {status}, request: {request}";
    }

    public TwoCaptchaState CheckState()
    {
      if (status == 1) return TwoCaptchaState.Success;
      if (request.Contains("CAPCHA_NOT_READY")) return TwoCaptchaState.NotReady;
      else return TwoCaptchaState.Error;
    }

    public static void Wait(CancellationToken cancellationToken,int delay = 5000,int step = 100)
    {
      int timeloop = delay / step;
      while (timeloop-- != 0)
      {
        Task.Delay(step).Wait();
        cancellationToken.ThrowIfCancellationRequested();
      }
    }
  }


  public static class TwoCaptcha
  {
    static readonly HttpClient httpClient = new HttpClient();


    //https://2captcha.com/2captcha-api#solving_recaptchav2_old
    //có thể dùng TwoCaptcha.ReCaptchaV2_old(....).Result  - Khuyến cáo không dùng .Result ở main thread
    public static async Task<string> ReCaptchaV2_old(Bitmap bitmap, Bitmap imginstructions,string ApiKey, int? recaptcharows = null,int? recaptchacols = null)
    {
      byte[] buffer_bitmap = null;
      using (MemoryStream memoryStream = new MemoryStream())
      {
        bitmap.Save(memoryStream, ImageFormat.Jpeg);//hoac png
        memoryStream.Position = 0;
        buffer_bitmap = new byte[memoryStream.Length];
        memoryStream.Read(buffer_bitmap, 0, (int)memoryStream.Length);
      }

      byte[] buffer_instructions = null;
      using (MemoryStream memoryStream = new MemoryStream())
      {
        imginstructions.Save(memoryStream, ImageFormat.Jpeg);//hoac png
        memoryStream.Position = 0;
        buffer_instructions = new byte[memoryStream.Length];
        memoryStream.Read(buffer_instructions, 0, (int)memoryStream.Length);
      }

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["key"] = ApiKey;
      parameters["recaptcha"] = "1";
      parameters["method"] = "post";
      //if(!string.IsNullOrEmpty(textinstructions)) parameters["textinstructions"] = textinstructions;
      if (recaptcharows != null) parameters["recaptcharows"] = recaptcharows.Value.ToString();
      if (recaptchacols != null) parameters["recaptchacols"] = recaptchacols.Value.ToString();
      Uri uri = new Uri("https://2captcha.com/in.php?" + parameters.ToString());

      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
      {
        MultipartFormDataContent requestContent = new MultipartFormDataContent();

        ByteArrayContent imageContent_bitmap = new ByteArrayContent(buffer_bitmap);
        imageContent_bitmap.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        requestContent.Add(imageContent_bitmap, "file", "file.jpg");

        ByteArrayContent imageContent_instructions = new ByteArrayContent(buffer_instructions);
        imageContent_instructions.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        requestContent.Add(imageContent_instructions, "imginstructions", "imginstructions.jpg");

        httpRequestMessage.Content = requestContent;
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }

    public static async Task<TwoCaptchaResponse> GetResponseJson(string id,string ApiKey)
    {
      if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["key"] = ApiKey;
      parameters["id"] = id;
      parameters["action"] = "get";
      parameters["json"] = "1";
      Uri uri = new Uri("https://2captcha.com/res.php?" + parameters.ToString());

      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
      {
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return JsonConvert.DeserializeObject<TwoCaptchaResponse>(await httpResponseMessage.Content.ReadAsStringAsync());
        }
      }
    }

    public static async Task<string> GetResponse(string id, string ApiKey)
    {
      if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["key"] = ApiKey;
      parameters["id"] = id;
      parameters["action"] = "get";
      Uri uri = new Uri("https://2captcha.com/res.php?" + parameters.ToString());

      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
      {
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }


    public static async Task<string> Nomal(Bitmap bitmap,string ApiKey)
    {
      byte[] buffer_bitmap = null;
      using (MemoryStream memoryStream = new MemoryStream())
      {
        bitmap.Save(memoryStream, ImageFormat.Jpeg);//hoac png
        memoryStream.Position = 0;
        buffer_bitmap = new byte[memoryStream.Length];
        memoryStream.Read(buffer_bitmap, 0, (int)memoryStream.Length);
      }
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["key"] = ApiKey;
      parameters["method"] = "post";
      Uri uri = new Uri("https://2captcha.com/in.php?" + parameters.ToString());
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri))
      {
        MultipartFormDataContent requestContent = new MultipartFormDataContent();

        ByteArrayContent imageContent_bitmap = new ByteArrayContent(buffer_bitmap);
        imageContent_bitmap.Headers.ContentType = MediaTypeHeaderValue.Parse("image/jpeg");
        requestContent.Add(imageContent_bitmap, "file", "file.jpg");

        httpRequestMessage.Content = requestContent;
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }


    public static async Task<string> SolveRecaptchaV2(string ApiKey, string googleKey, string pageUrl)
    {
      var parameters = HttpUtility.ParseQueryString(string.Empty);
      parameters["key"] = ApiKey;
      parameters["googlekey"] = googleKey;
      parameters["method"] = "userrecaptcha";
      parameters["pageurl"] = pageUrl;
      Uri uri = new Uri("https://2captcha.com/in.php?" + parameters.ToString());

      using(HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
      {
        using(HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }


    //public static bool SolveRecaptchaV2_(string ApiKey, string googleKey, string pageUrl, out string result)
    //{
    //  string requestUrl = string.Concat(new string[]
    //  {
    //            "http://2captcha.com/in.php?key=",
    //            ApiKey,
    //            "&method=userrecaptcha&googlekey=",
    //            googleKey,
    //            "&pageurl=",
    //            pageUrl
    //  });
    //  bool result2;
    //  try
    //  {
    //    WebRequest req = WebRequest.Create(requestUrl);
    //    using (WebResponse resp = req.GetResponse())
    //    {
    //      using (StreamReader read = new StreamReader(resp.GetResponseStream()))
    //      {
    //        string response = read.ReadToEnd();
    //        bool flag = response.Length < 3;
    //        if (flag)
    //        {
    //          result = response;
    //          result2 = false;
    //          return result2;
    //        }
    //        bool flag2 = response.Substring(0, 3) == "OK|";
    //        if (flag2)
    //        {
    //          string captchaID = response.Remove(0, 3);
    //          for (int i = 0; i < 24; i++)
    //          {
    //            WebRequest getAnswer = WebRequest.Create("http://2captcha.com/res.php?key=" + ApiKey + "&action=get&id=" + captchaID);
    //            using (WebResponse answerResp = getAnswer.GetResponse())
    //            {
    //              using (StreamReader answerStream = new StreamReader(answerResp.GetResponseStream()))
    //              {
    //                string answerResponse = answerStream.ReadToEnd();
    //                bool flag3 = answerResponse.Length < 3;
    //                if (flag3)
    //                {
    //                  result = answerResponse;
    //                  result2 = false;
    //                  return result2;
    //                }
    //                bool flag4 = answerResponse.Substring(0, 3) == "OK|";
    //                if (flag4)
    //                {
    //                  result = answerResponse.Remove(0, 3);
    //                  result2 = true;
    //                  return result2;
    //                }
    //                bool flag5 = answerResponse != "CAPCHA_NOT_READY";
    //                if (flag5)
    //                {
    //                  result = answerResponse;
    //                  result2 = false;
    //                  return result2;
    //                }
    //              }
    //            }
    //            Thread.Sleep(5000);
    //          }
    //          result = "Timeout";
    //          result2 = false;
    //          return result2;
    //        }
    //        result = response;
    //        result2 = false;
    //        return result2;
    //      }
    //    }
    //  }
    //  catch
    //  {
    //  }
    //  result = "Unknown error";
    //  result2 = false;
    //  return result2;
    //}
  }
}