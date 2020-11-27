using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace TqkLibrary.Captcha
{
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

    //tra ve json
    public static async Task<string> GetResponse(string id,string ApiKey)
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
  }
}