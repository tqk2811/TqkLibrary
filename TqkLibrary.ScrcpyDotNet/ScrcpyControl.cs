using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace TqkLibrary.ScrcpyDotNet
{
  public sealed class ScrcpyControl
  {
    public readonly Scrcpy Scrcpy;
    internal NetworkStream _controlStream = null;

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

    public void Tap(int x,int y,int releaseDelay = 100)
    {
      long pointerId = random.Next(int.MinValue, int.MaxValue);
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
        AndroidMotionEventAction.ACTION_DOWN,
        pointerId,
        new Rectangle() { X = x, Y = y, Width = Scrcpy.Width, Height = Scrcpy.Height },
        1f,
        AndroidMotionEventButton.BUTTON_PRIMARY));

      Thread.Sleep(releaseDelay);

      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
        AndroidMotionEventAction.ACTION_UP,
       pointerId,
       new Rectangle() { X = x, Y = y, Width = Scrcpy.Width, Height = Scrcpy.Height },
       1f,
       AndroidMotionEventButton.BUTTON_PRIMARY));
    }

    public void Key(AndroidKeyCode androidKeyCode,uint repeat = 1, int releaseDelay = 100)
    {
      SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_DOWN, androidKeyCode, repeat, AndroidKeyEventMeta.META_NONE));
      Thread.Sleep(releaseDelay);
      SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_UP, androidKeyCode, repeat, AndroidKeyEventMeta.META_NONE));
    }

    public void WriteText(string text) => SendControl(ScrcpyControlMessage.CreateInjectText(text));

    public void Scroll(int x, int y, int hScroll, int vScroll)
    {
      SendControl(ScrcpyControlMessage.CreateInjectScrollEvent(new Rectangle() { X = x, Y = y, Width = Scrcpy.Width, Height = Scrcpy.Height }, hScroll, vScroll));
    }

    static readonly Random random = new Random();
    public void Swipe(int x1,int y1, int x2,int y2,int duration = 100,int delay = 5)
    {
      long pointerId = random.Next(int.MinValue, int.MaxValue);
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
        AndroidMotionEventAction.ACTION_DOWN,
        pointerId,
        new Rectangle() { X = x1, Y = y1, Width = Scrcpy.Width, Height = Scrcpy.Height}));
      int times = duration / delay; 
      int x = (x2 - x1) / times;
      int y = (y2 - y1) / times;
      for (int i = 0; i < times; i++)
      {        
        SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
          AndroidMotionEventAction.ACTION_MOVE,
          pointerId,
          new Rectangle() { X = x1 + x * i, Y = y1 + y * i, Width = Scrcpy.Width, Height = Scrcpy.Height }));
        Thread.Sleep(delay);
      }
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
       AndroidMotionEventAction.ACTION_UP,
       pointerId,
       new Rectangle() { X = x1, Y = y1, Width = Scrcpy.Width, Height = Scrcpy.Height }));
    }

#if DEBUG
    public string GetClipboard()
    {
      SendControl(ScrcpyControlMessage.GetClipboard());
      BinaryReader binaryReader = new BinaryReader(_controlStream);
      uint size = binaryReader.ReadUInt32();
      StreamReader streamReader = new StreamReader(_controlStream);
      return streamReader.ReadToEnd();
    }
#endif
  }
}
