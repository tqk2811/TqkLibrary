using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace TqkLibrary.Net.ImagesHostApi.ImagesHackCom
{
  public class ImagesHackResponse<T>
  {
    public bool success { get; set; }
    public int? process_time { get; set; }
    public T result { get; set; }
  }

  public class ImagesHackUploadResult
  {
    public long? max_filesize { get; set; }
    public long? space_limit { get; set; }
    public long? space_used { get; set; }
    public long? space_left { get; set; }
    public int? passed { get; set; }
    public int? failed { get; set; }
    public int? total { get; set; }
    public List<ImagesHackUploadImage> images { get; set; }
  }

  public class ImagesHackUploadImage
  {
    public string id { get; set; }
    public int? server { get; set; }
    public int? bucket { get; set; }
    public string filename { get; set; }
    public string direct_link { get; set; }
    public string original_filename { get; set; }
    public string title { get; set; }
    public ImageHackAlbum album { get; set; }
    public long? creation_date { get; set; }

    [JsonProperty("public")]
    public bool? IsPublic { get; set; }

    public bool? hidden { get; set; }
    public long? filesize { get; set; }
    public int? width { get; set; }
    public int? height { get; set; }
    public int? likes { get; set; }
    public bool? liked { get; set; }
    public bool? is_owner { get; set; }
    public ImageHackOwner owner { get; set; }
    public bool? adult_content { get; set; }
  }

  public class ImageHackAlbum
  {
    public string id { get; set; }
    public string title { get; set; }

    [JsonProperty("public")]
    public bool? IsPublic { get; set; }
  }

  public class ImageHackOwner
  {
    public string username { get; set; }
    public ImageHackAvatar avatar { get; set; }
    public string membership { get; set; }
    public bool? featured_photographer { get; set; }
  }

  public class ImageHackAvatar
  {
    public string id { get; set; }
    public string filename { get; set; }
    public int? server { get; set; }
    public bool? cropped { get; set; }
    public int? x_pos { get; set; }
    public int? y_pos { get; set; }
    public int? x_length { get; set; }
    public int? y_length { get; set; }
  }

  //https://docs.google.com/document/d/16M3qaw27vgwuwXqExo0aIC0nni42OOuWu_OGvpYl7dE/pub#h.jcrh03smytne
  public class ImagesHackApi : BaseApi
  {
    private const string EndPoint = "https://api.imageshack.com/v2/images";

    public ImagesHackApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<ImagesHackResponse<ImagesHackUploadResult>> UploadImage(Bitmap bitmap)
    {
      byte[] buffer_bitmap = null;
      using (MemoryStream memoryStream = new MemoryStream())
      {
        bitmap.Save(memoryStream, ImageFormat.Png);//hoac png
        memoryStream.Position = 0;
        buffer_bitmap = new byte[memoryStream.Length];
        memoryStream.Read(buffer_bitmap, 0, (int)memoryStream.Length);
      }

      using HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(EndPoint + $"?api_key={ApiKey}"));
      MultipartFormDataContent requestContent = new MultipartFormDataContent();
      ByteArrayContent imageContent_instructions = new ByteArrayContent(buffer_bitmap);
      imageContent_instructions.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");
      requestContent.Add(imageContent_instructions, "file", "file.png");
      httpRequestMessage.Content = requestContent;
      HttpResponseMessage httpResponseMessage = await NetExtensions.httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead);
      string result = await httpResponseMessage.EnsureSuccessStatusCode().Content.ReadAsStringAsync();
      Console.WriteLine(result);
      return JsonConvert.DeserializeObject<ImagesHackResponse<ImagesHackUploadResult>>(result);
    }
  }
}