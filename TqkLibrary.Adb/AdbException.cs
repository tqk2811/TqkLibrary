using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Adb
{
  public class AdbException : Exception
  {
    public AdbException(string messsage, string StandardOutput) : base(messsage)
    {
      this.StandardOutput = StandardOutput;
    }

    public string StandardOutput { get; }
  }
}