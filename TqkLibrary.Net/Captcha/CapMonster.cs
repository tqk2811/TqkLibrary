using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Net.Captcha
{
  internal class CapMonster
  {
    readonly string ApiKey;
    public CapMonster(string ApiKey)
    {
      if (string.IsNullOrEmpty(ApiKey)) throw new ArgumentNullException(nameof(ApiKey));
      this.ApiKey = ApiKey;
    }
  }
}
