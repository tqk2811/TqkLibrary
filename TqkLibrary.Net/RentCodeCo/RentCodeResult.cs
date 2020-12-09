﻿using Newtonsoft.Json;

namespace TqkLibrary.Net.RentCodeCo
{
  public sealed class RentCodeResult
  {
    [JsonProperty("id")]
    public int Id { get; set; }

    [JsonProperty("success")]
    public bool Success { get; set; }

    [JsonProperty("message")]
    public string Message { get; set; }
  }
}