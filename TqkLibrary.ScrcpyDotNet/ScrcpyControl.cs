using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TqkLibrary.ScrcpyDotNet
{
  public sealed class ScrcpyControl
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
      if(buffer != null)
      {
        _controlStream?.Write(buffer, 0, buffer.Length);
        _controlStream?.Flush();
#if DEBUG
        Console.WriteLine("Control:" + BitConverter.ToString(buffer).Replace("-", ""));
#endif
      }
    }

    public void Tap(int x,int y)
    {
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(AndroidMotionEventAction.ACTION_DOWN, 
        AndroidPointerId.POINTER_ID_VIRTUAL_FINGER, 
        new Rectangle() { X = x, Y = y, Width = Scrcpy.Width, Height = Scrcpy.Height }, 
        20f, 
        AndroidMotionEventButton.BUTTON_PRIMARY));
      Thread.Sleep(100);
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(AndroidMotionEventAction.ACTION_UP,
       AndroidPointerId.POINTER_ID_VIRTUAL_FINGER,
       new Rectangle() { X = x, Y = y, Width = Scrcpy.Width, Height = Scrcpy.Height },
       20f,
       AndroidMotionEventButton.BUTTON_PRIMARY));
    }

    public void Key(AndroidKeyCode androidKeyCode,int repeat = 1, int releaseDelay = 100)
    {
      SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_DOWN, androidKeyCode, repeat));
      Thread.Sleep(releaseDelay); 
      SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_UP, androidKeyCode, repeat));
    }

  }
}
