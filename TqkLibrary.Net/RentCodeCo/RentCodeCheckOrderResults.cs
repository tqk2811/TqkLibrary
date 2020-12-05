using Newtonsoft.Json;
using System.Collections.Generic;

namespace TqkLibrary.Net.RentCodeCo
{
  public sealed class RentCodeCheckOrderResults
  {
    [JsonProperty("phoneNumber")]
    public string PhoneNumber { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }

    [JsonProperty("messages")]
    public List<RentCodeSmsMessage> Messages { get; set; }

    public override string ToString()
    {
      return $"Success: {Success}, PhoneNumber: {PhoneNumber}, Message: {Message}";
    }
  }
}
