using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.ScrcpyDotNet
{
  public class ScrcpyControl
  {
    public readonly Scrcpy Scrcpy;
    internal Stream _controlStream = null;

    internal ScrcpyControl(Scrcpy Scrcpy)
    {
      this.Scrcpy = Scrcpy;
    }

    public void SendControl(ScrcpyControlMessage scrcpyControlMessage)
    {
      byte[] buffer = scrcpyControlMessage.GetCommand();
      _controlStream.Write(buffer, 0, buffer.Length);
      _controlStream.Flush();
    }
  }
}
