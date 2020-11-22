using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace TqkLibrary.Facebook
{
  public class FacebookApi
  {
    readonly static HttpClient httpClient = new HttpClient();

    public static async Task<FacebookToken> GetAccessToken(string code,string AppId,string AppSecret, string redirect_uri)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get,
        $"https://graph.facebook.com/v8.0/oauth/access_token?client_id={AppId}&redirect_uri={redirect_uri}&client_secret={AppSecret}&code={code}"))
      {
        using(HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseContentRead))
        {
          return new FacebookToken(JsonConvert.DeserializeObject<FacebookToken_>(await httpResponseMessage.Content.ReadAsStringAsync()));
        }
      }
    }


    public static async Task<FacebookUser> GetCurrentUser(string access_token)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://graph.facebook.com/me?access_token={access_token}"))
      {
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return JsonConvert.DeserializeObject<FacebookUser>(await httpResponseMessage.Content.ReadAsStringAsync());
        }
      }
    }

    public static async Task<DataPages> ListAllPages(string access_token)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"https://graph.facebook.com/me/accounts?access_token={access_token}"))
      {
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return JsonConvert.DeserializeObject<DataPages>(await httpResponseMessage.Content.ReadAsStringAsync());
        }
      }
    }


    public static async Task<string> PagePostContent(string access_token, string content,string link = null,
      bool published = true,DateTime? ScheduleTime = null)
    {
      var dict = new Dictionary<string, string>();
      dict.Add("message", content);
      dict.Add("access_token", access_token);
      dict.Add("published", published.ToString());
      if (!published && ScheduleTime != null)
      {
        dict.Add("scheduled_publish_time", new DateTimeOffset(ScheduleTime.Value).ToUnixTimeSeconds().ToString());//4.6.2
      }
      if (!string.IsNullOrEmpty(link)) dict.Add("link", link);

      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, 
        $"https://graph.facebook.com/v8.0/me/feed"))
      {
        httpRequestMessage.Content = new FormUrlEncodedContent(dict);
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }


    public static async Task<string> UploadingPhoto(string access_token, string photo_url, bool published)
    {
      var dict = new Dictionary<string, string>();
      dict.Add("url", photo_url);
      dict.Add("access_token", access_token);
      dict.Add("published", published.ToString());
      if (!published) dict.Add("temporary", true.ToString());
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
        $"https://graph.facebook.com/v8.0/me/photos"))//$"https://graph.facebook.com/v8.0/{page_id}/photos
      {
        httpRequestMessage.Content = new FormUrlEncodedContent(dict);
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }

    public static async Task<string> UploadingPhoto(string access_token, byte[] image,bool published)
    {
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
        $"https://graph.facebook.com/v8.0/me/photos"))
      {
        MultipartFormDataContent form = new MultipartFormDataContent();
        form.Add(new StringContent(access_token), "access_token");
        form.Add(new StringContent(published.ToString()), "published");
        if (!published) form.Add(new StringContent(true.ToString()), "temporary");
        form.Add(new ByteArrayContent(image), "image", "image.jpg");

        httpRequestMessage.Content = form;
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }

    public static async Task<string> PublishingMultiPhoto(string access_token, string message,IEnumerable<string> imgsId,
      bool published = true,DateTime? time = null)
    {
      var dict = new Dictionary<string, string>();
      dict.Add("message", message);
      dict.Add("access_token", access_token);
      dict.Add("published", published ? "1" : "0");
      if(!published && time != null)
      {
        dict.Add("scheduled_publish_time", new DateTimeOffset(time.Value).ToUnixTimeSeconds().ToString());
        dict.Add("unpublished_content_type", "SCHEDULED");
      }
      
      int i = 0;
      foreach (var id in imgsId) dict.Add("attached_media[" + i++ + "]", "{\"media_fbid\":\"" + id + "\"}");
      
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post,
        $"https://graph.facebook.com/v8.0/me/feed"))
      {
        httpRequestMessage.Content = new FormUrlEncodedContent(dict);
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return await httpResponseMessage.Content.ReadAsStringAsync();
        }
      }
    }

  }
}
