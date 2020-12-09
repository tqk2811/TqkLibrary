using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace TqkLibrary.Net.Captcha
{
  /// <summary>
  /// https://anti-captcha.com/apidoc/recaptcha
  /// </summary>
  public sealed class AntiCaptchaTask
  {
    [JsonProperty("type")]
    public string Type { get; set; }

    [JsonProperty("websiteURL")]
    public string WebsiteUrl { get; set; }

    [JsonProperty("websiteKey")]
    public string WebsiteKey { get; set; }

    [JsonProperty("proxyType")]
    public string ProxyType { get; set; }

    [JsonProperty("proxyAddress")]
    public string ProxyAddress { get; set; }

    [JsonProperty("proxyPort")]
    public int? ProxyPort { get; set; }

    [JsonProperty("proxyLogin")]
    public string ProxyLogin { get; set; }

    [JsonProperty("proxyPassword")]
    public string ProxyPassword { get; set; }

    [JsonProperty("userAgent")]
    public string UserAgent { get; set; }

    [JsonProperty("cookies")]
    public string Cookies { get; set; }
  }

  public sealed class AntiCaptchaTaskResponse
  {
    [JsonProperty("errorId")]
    public int? ErrorId { get; set; }

    [JsonProperty("errorCode")]
    public string ErrorCode { get; set; }

    [JsonProperty("errorDescription")]
    public string ErrorDescription { get; set; }

    [JsonProperty("taskId")]
    public int? TaskId { get; set; }
  }

  public sealed class AntiCaptchaTaskResultResponse
  {
    [JsonProperty("errorId")]
    public int? ErrorId { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("solution")]
    public AntiCaptchaTaskSolutionResultResponse Solution { get; set; }

    [JsonProperty("cost")]
    public double? cost { get; set; }

    [JsonProperty("ip")]
    public string Ip { get; set; }

    [JsonProperty("createTime")]
    public long? CreateTime { get; set; }

    [JsonProperty("endTime")]
    public long? EndTime { get; set; }

    [JsonProperty("solveCount")]
    public int? SolveCount { get; set; }

    public bool IsComplete()
    {
      return Status == null || Status.Equals("ready");
    }
  }

  public sealed class AntiCaptchaTaskSolutionResultResponse
  {
    [JsonProperty("text")]
    public string Text { get; set; }

    [JsonProperty("url")]
    public string Url { get; set; }

    public string gRecaptchaResponse { get; set; }
  }




  public sealed class AntiCaptcha
  {
    static readonly HttpClient httpClient = new HttpClient();
    const string EndPoint = "https://api.anti-captcha.com";
    readonly string ApiKey;

    /// <summary>
    /// 
    /// </summary>
    /// <exception cref="System.ArgumentNullException"></exception>
    /// <param name="ApiKey">ApiKey</param>
    public AntiCaptcha(string ApiKey)
    {
      if (string.IsNullOrEmpty(ApiKey)) throw new ArgumentNullException(nameof(ApiKey));
      this.ApiKey = ApiKey;
    }

    public async Task<AntiCaptchaTaskResponse> CreateTask(AntiCaptchaTask antiCaptchaTask,string languagePool = "en")
    {
      CreateTaskJson createTaskJson = new CreateTaskJson();
      createTaskJson.ClientKey = ApiKey;
      createTaskJson.Task = antiCaptchaTask;
      createTaskJson.LanguagePool = languagePool;
      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, EndPoint + "/createTask"))
      {
        httpRequestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(createTaskJson,NetExtensions.JsonSerializerSettings), Encoding.UTF8, "application/json");
        using(HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage,HttpCompletionOption.ResponseContentRead))
        {
          return JsonConvert.DeserializeObject<AntiCaptchaTaskResponse>(await httpResponseMessage.Content.ReadAsStringAsync());
        }
      }
    }

    public async Task<AntiCaptchaTaskResultResponse> GetTaskResult(int taskId)
    {
      TaskResultJson taskResultJson = new TaskResultJson();
      taskResultJson.ClientKey = ApiKey;
      taskResultJson.TaskId = taskId;

      using (HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, EndPoint + "/getTaskResult"))
      {
        httpRequestMessage.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        httpRequestMessage.Content = new StringContent(JsonConvert.SerializeObject(taskResultJson, NetExtensions.JsonSerializerSettings), Encoding.UTF8, "application/json");
        using (HttpResponseMessage httpResponseMessage = await httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseContentRead))
        {
          return JsonConvert.DeserializeObject<AntiCaptchaTaskResultResponse>(await httpResponseMessage.Content.ReadAsStringAsync());
        }
      }
    }


    class CreateTaskJson
    {
      [JsonProperty("clientKey")]
      public string ClientKey { get; set; }

      [JsonProperty("task")]
      public AntiCaptchaTask Task { get; set; }

      [JsonProperty("softId")]
      public int SoftId { get; set; } = 0;

      [JsonProperty("languagePool")]
      public string LanguagePool { get; set; } = "en";
    }

    class TaskResultJson
    {
      [JsonProperty("clientKey")]
      public string ClientKey { get; set; }

      [JsonProperty("taskId")]
      public int TaskId { get; set; }
    }
  }
}
