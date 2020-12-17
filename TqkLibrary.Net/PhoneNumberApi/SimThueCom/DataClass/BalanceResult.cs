using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TqkLibrary.Net.PhoneNumberApi.SimThueCom
{
  public class BalanceResult : BaseResult
  {
    [JsonProperty("balance")]
    public double? Balance { get; set; }
  }
}