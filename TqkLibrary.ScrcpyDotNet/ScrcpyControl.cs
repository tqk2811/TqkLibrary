﻿using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
namespace TqkLibrary.ScrcpyDotNet
{
  public sealed class ScrcpyControl
  {
    readonly object lock_send = new object();
    public readonly Scrcpy scrcpy;
    internal NetworkStream _controlStream = null;

    internal ScrcpyControl(Scrcpy scrcpy)
    {
      this.scrcpy = scrcpy;
    }

    public void SendControl(ScrcpyControlMessage scrcpyControlMessage)
    {
      if (_controlStream == null) throw new ScrcpyException(0, "Control Stream is null");
      byte[] buffer = scrcpyControlMessage?.GetCommand();
      if (buffer != null)
      {
        lock (lock_send)
        {
          _controlStream.Write(buffer, 0, buffer.Length);
          _controlStream.Flush();
        }
#if DEBUG
        Console.WriteLine("Control:" + BitConverter.ToString(buffer).Replace("-", ""));
#endif
      }
    }

    public void SendControlBuffer(byte[] buffer)
    {
      lock (_controlStream)
      {
        _controlStream?.Write(buffer, 0, buffer.Length);
        _controlStream?.Flush();
      }
    }

    public void SendControlBuffer(byte[] buffer, int index, int size)
    {
      lock (_controlStream)
      {
        _controlStream?.Write(buffer, index, size);
        _controlStream?.Flush();
      }
    }

    public void Tap(double x, double y, int releaseDelay = 100)
    {
      Tap((int)x * scrcpy.Width, (int)y * scrcpy.Height, releaseDelay);
    }
    public void Tap(int x, int y, int releaseDelay = 100)
    {
      long pointerId = random.Next(int.MinValue, int.MaxValue);
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
        AndroidMotionEventAction.ACTION_DOWN,
        pointerId,
        new Rectangle() { X = x, Y = y, Width = scrcpy.Width, Height = scrcpy.Height },
        1f,
        AndroidMotionEventButton.BUTTON_PRIMARY));

      Thread.Sleep(releaseDelay);

      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
        AndroidMotionEventAction.ACTION_UP,
       pointerId,
       new Rectangle() { X = x, Y = y, Width = scrcpy.Width, Height = scrcpy.Height },
       1f,
       AndroidMotionEventButton.BUTTON_PRIMARY));
    }

    public void Key(AndroidKeyCode androidKeyCode, uint repeat = 1, int releaseDelay = 100)
    {
      SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_DOWN, androidKeyCode, repeat, AndroidKeyEventMeta.META_NONE));
      Thread.Sleep(releaseDelay);
      SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_UP, androidKeyCode, repeat, AndroidKeyEventMeta.META_NONE));
    }

    public void WriteText(string text) => SendControl(ScrcpyControlMessage.CreateInjectText(text));

    public void Scroll(double x, double y, int hScroll, int vScroll)
    {
      Scroll((int)x * scrcpy.Width, (int)y * scrcpy.Height, hScroll, vScroll);
    }

    public void Scroll(int x, int y, int hScroll, int vScroll)
    {
      SendControl(ScrcpyControlMessage.CreateInjectScrollEvent(new Rectangle() { X = x, Y = y, Width = scrcpy.Width, Height = scrcpy.Height }, hScroll, vScroll));
    }

    static readonly Random random = new Random();

    public void Swipe(double x1, double y1, double x2, double y2, int duration = 100, int delay = 5)
    {
      Swipe((int)x1 * scrcpy.Width, (int)y1 * scrcpy.Height, (int)x2 * scrcpy.Width, (int)y2 * scrcpy.Height, duration, delay);
    }

    public void Swipe(int x1, int y1, int x2, int y2, int duration = 100, int delay = 5)
    {
      long pointerId = random.Next(int.MinValue, int.MaxValue);
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
        AndroidMotionEventAction.ACTION_DOWN,
        pointerId,
        new Rectangle() { X = x1, Y = y1, Width = scrcpy.Width, Height = scrcpy.Height }));
      int times = duration / delay;
      int x = (x2 - x1) / times;
      int y = (y2 - y1) / times;
      for (int i = 0; i < times; i++)
      {
        SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
          AndroidMotionEventAction.ACTION_MOVE,
          pointerId,
          new Rectangle() { X = x1 + x * i, Y = y1 + y * i, Width = scrcpy.Width, Height = scrcpy.Height }));
        Thread.Sleep(delay);
      }
      SendControl(ScrcpyControlMessage.CreateInjectTouchEvent(
       AndroidMotionEventAction.ACTION_UP,
       pointerId,
       new Rectangle() { X = x2, Y = y2, Width = scrcpy.Width, Height = scrcpy.Height }));
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
