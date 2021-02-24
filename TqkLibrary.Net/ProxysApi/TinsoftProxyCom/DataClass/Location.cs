using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Net.ProxysApi.TinsoftProxyCom
{
  public class Location
  {
    public int? location { get; set; }
    public string name { get; set; }

    public override string ToString() => $"location: {location}, name: {name}";
  }
}
