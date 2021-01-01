using System;

namespace TqkLibrary.Net.Captcha
{
  internal class CapMonster
  {
    private readonly string ApiKey;

    public CapMonster(string ApiKey)
    {
      if (string.IsNullOrEmpty(ApiKey)) throw new ArgumentNullException(nameof(ApiKey));
      this.ApiKey = ApiKey;
    }
  }
}