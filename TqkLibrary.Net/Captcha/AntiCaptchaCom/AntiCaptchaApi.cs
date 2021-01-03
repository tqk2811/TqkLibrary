using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Net.Captcha.AntiCaptchaCom
{
  public sealed class AntiCaptchaApi : BaseApi
  {
    private const string EndPoint = "https://api.anti-captcha.com";

    /// <summary>
    ///
    /// </summary>
    /// <exception cref="System.ArgumentNullException"></exception>
    /// <param name="ApiKey">ApiKey</param>
    public AntiCaptchaApi(string ApiKey) : base(ApiKey)
    {
    }

    public async Task<AntiCaptchaTaskResponse> CreateTask(AntiCaptchaTask antiCaptchaTask, string languagePool = "en")
    {
      CreateTaskJson createTaskJson = new CreateTaskJson();
      createTaskJson.ClientKey = ApiKey;
      createTaskJson.Task = antiCaptchaTask;
      createTaskJson.LanguagePool = languagePool;

      return await RequestPost<AntiCaptchaTaskResponse>(EndPoint + "/createTask", new StringContent(JsonConvert.SerializeObject(createTaskJson, NetExtensions.JsonSerializerSettings), Encoding.UTF8, "application/json")).ConfigureAwait(false);
    }

    public async Task<AntiCaptchaTaskResultResponse> GetTaskResult(int taskId)
    {
      TaskResultJson taskResultJson = new TaskResultJson();
      taskResultJson.ClientKey = ApiKey;
      taskResultJson.TaskId = taskId;

      return await RequestPost<AntiCaptchaTaskResultResponse>(EndPoint + "/createTask", new StringContent(JsonConvert.SerializeObject(taskResultJson, NetExtensions.JsonSerializerSettings), Encoding.UTF8, "application/json")).ConfigureAwait(false);
    }

    private class CreateTaskJson
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

    private class TaskResultJson
    {
      [JsonProperty("clientKey")]
      public string ClientKey { get; set; }

      [JsonProperty("taskId")]
      public int TaskId { get; set; }
    }
  }
}