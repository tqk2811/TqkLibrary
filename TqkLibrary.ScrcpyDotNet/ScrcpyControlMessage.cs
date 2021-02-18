using System;
using System.Drawing;
using System.Linq;
using System.Text;
namespace TqkLibrary.ScrcpyDotNet
{
  /// <summary>
  /// https://github.com/Genymobile/scrcpy/blob/master/server/src/main/java/com/genymobile/scrcpy/ControlMessage.java
  /// </summary>
  public sealed class ScrcpyControlMessage
  {
    private ScrcpyControlType ControlType { get; set; }
    private string Text { get; set; }
    private AndroidKeyEventMeta MetaState { get; set; } // KeyEvent.META_*
    private AndroidMotionEventAction MotionEventAction { get; set; }//MotionEvent.ACTION_
    private AndroidKeyEventAction KeyEventAction { get; set; }//KeyEvent.ACTION_*
    private AndroidKeyCode Keycode { get; set; } // KeyEvent.KEYCODE_*
    private AndroidMotionEventButton Buttons { get; set; } // MotionEvent.BUTTON_*
    private long PointerId { get; set; }
    private float Pressure { get; set; }
    private Rectangle Position { get; set; }
    private int hScroll { get; set; }
    private int vScroll { get; set; }
    private bool Paste { get; set; }
    private uint Repeat { get; set; }
    private ScrcpyScreenPowerMode PowerMode { get; set; }

    private ScrcpyControlMessage() { }

    public static ScrcpyControlMessage CreateInjectKeycode(AndroidKeyEventAction action, AndroidKeyCode keycode, uint repeat = 1, AndroidKeyEventMeta metaState = AndroidKeyEventMeta.META_META_ON)
    {
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = ScrcpyControlType.TYPE_INJECT_KEYCODE;
      msg.KeyEventAction = action;
      msg.Keycode = keycode;
      msg.Repeat = repeat;
      msg.MetaState = metaState;
      return msg;
    }

    public static ScrcpyControlMessage CreateInjectText(string text)
    {
      if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = ScrcpyControlType.TYPE_INJECT_TEXT;
      msg.Text = text;
      return msg;
    }

    public static ScrcpyControlMessage CreateInjectTouchEvent(AndroidMotionEventAction action, long pointerId, Rectangle position, float pressure = 1f, AndroidMotionEventButton buttons = AndroidMotionEventButton.BUTTON_PRIMARY)
    {
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = ScrcpyControlType.TYPE_INJECT_TOUCH_EVENT;
      msg.MotionEventAction = action;
      msg.PointerId = pointerId;
      msg.Pressure = pressure;
      msg.Position = position;
      msg.Buttons = buttons;
      return msg;
    }

    public static ScrcpyControlMessage CreateInjectScrollEvent(Rectangle position, int hScroll, int vScroll)
    {
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = ScrcpyControlType.TYPE_INJECT_SCROLL_EVENT;
      msg.Position = position;
      msg.hScroll = hScroll;
      msg.vScroll = vScroll;
      return msg;
    }

    public static ScrcpyControlMessage CreateSetClipboard(string text, bool paste)
    {
      if (string.IsNullOrEmpty(text)) throw new ArgumentNullException(nameof(text));
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = ScrcpyControlType.TYPE_SET_CLIPBOARD;
      msg.Text = text;
      msg.Paste = paste;
      return msg;
    }

    public static ScrcpyControlMessage CreateSetScreenPowerMode(ScrcpyScreenPowerMode powerMode)
    {
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = ScrcpyControlType.TYPE_SET_SCREEN_POWER_MODE;
      msg.PowerMode = powerMode;
      return msg;
    }

    public static ScrcpyControlMessage BackOrScreenOn() => CreateEmpty(ScrcpyControlType.TYPE_BACK_OR_SCREEN_ON);

    public static ScrcpyControlMessage ExpandNotificationPanel() => CreateEmpty(ScrcpyControlType.TYPE_EXPAND_NOTIFICATION_PANEL);

    public static ScrcpyControlMessage CollapseNotificationPanel() => CreateEmpty(ScrcpyControlType.TYPE_COLLAPSE_NOTIFICATION_PANEL);

    internal static ScrcpyControlMessage GetClipboard() => CreateEmpty(ScrcpyControlType.TYPE_GET_CLIPBOARD);

    public static ScrcpyControlMessage RotateDevice() => CreateEmpty(ScrcpyControlType.TYPE_ROTATE_DEVICE);

    static ScrcpyControlMessage CreateEmpty(ScrcpyControlType type)
    {
      ScrcpyControlMessage msg = new ScrcpyControlMessage();
      msg.ControlType = type;
      return msg;
    }


    UInt16 to_fixed_point_16(float f)
    {
      UInt32 u = (UInt32)f * 2 << 15;
      if (u >= 0xffff)
      {
        u = 0xffff;
      }
      return (UInt16)u;
    }
    /// <summary>
    /// https://github.com/Genymobile/scrcpy/blob/master/server/src/main/java/com/genymobile/scrcpy/ControlMessageReader.java
    /// https://github.com/Genymobile/scrcpy/blob/master/app/src/control_msg.c
    /// </summary>
    /// <returns></returns>
    internal byte[] GetCommand()
    {
      byte[] buffer = null;
      switch (ControlType)
      {
        case ScrcpyControlType.TYPE_INJECT_KEYCODE:
          {
            buffer = new byte[14];
            buffer[0] = (byte)ControlType;
            buffer[1] = (byte)KeyEventAction;
            Array.Copy(BitConverter.GetBytes((uint)Keycode).Reverse().ToArray(), 0, buffer, 2, 4);//2-5
            Array.Copy(BitConverter.GetBytes(Repeat).Reverse().ToArray(), 0, buffer, 6, 4);//6-9
            Array.Copy(BitConverter.GetBytes((uint)MetaState).Reverse().ToArray(), 0, buffer, 10, 4);//10-13
          }
          break;

        case ScrcpyControlType.TYPE_INJECT_TEXT:
          {
            byte[] utf8_text = Encoding.UTF8.GetBytes(Text);
            buffer = new byte[5 + utf8_text.Length];
            buffer[0] = (byte)ControlType;
            Array.Copy(BitConverter.GetBytes(utf8_text.Length).Reverse().ToArray(), 0, buffer, 1, 4);//1-4
            Array.Copy(utf8_text, 0, buffer, 5, utf8_text.Length);//5-....
          }
          break;

        case ScrcpyControlType.TYPE_INJECT_TOUCH_EVENT:
          {
            buffer = new byte[28];
            buffer[0] = (byte)ControlType;
            buffer[1] = (byte)MotionEventAction;
            Array.Copy(BitConverter.GetBytes((ulong)PointerId).Reverse().ToArray(), 0, buffer, 2, 8);//2-9
            Array.Copy(BitConverter.GetBytes(Position.X).Reverse().ToArray(), 0, buffer, 10, 4);//10-13
            Array.Copy(BitConverter.GetBytes(Position.Y).Reverse().ToArray(), 0, buffer, 14, 4);//14-17
            Array.Copy(BitConverter.GetBytes((UInt16)Position.Width).Reverse().ToArray(), 0, buffer, 18, 2);//18-19
            Array.Copy(BitConverter.GetBytes((UInt16)Position.Height).Reverse().ToArray(), 0, buffer, 20, 2);//20-21
            Array.Copy(BitConverter.GetBytes(to_fixed_point_16(Pressure)).Reverse().ToArray(), 0, buffer, 22, 2);//22-23
            Array.Copy(BitConverter.GetBytes((int)Buttons).Reverse().ToArray(), 0, buffer, 24, 4);//24-27
          }
          break;

        case ScrcpyControlType.TYPE_INJECT_SCROLL_EVENT:
          {
            buffer = new byte[21];
            buffer[0] = (byte)ControlType;
            Array.Copy(BitConverter.GetBytes(Position.X).Reverse().ToArray(), 0, buffer, 1, 4);//1-4
            Array.Copy(BitConverter.GetBytes(Position.Y).Reverse().ToArray(), 0, buffer, 5, 4);//5-8
            Array.Copy(BitConverter.GetBytes((UInt16)Position.Width).Reverse().ToArray(), 0, buffer, 9, 2);//9-10
            Array.Copy(BitConverter.GetBytes((UInt16)Position.Height).Reverse().ToArray(), 0, buffer, 11, 2);//11-12
            Array.Copy(BitConverter.GetBytes(hScroll).Reverse().ToArray(), 0, buffer, 13, 4);//13-16
            Array.Copy(BitConverter.GetBytes(vScroll).Reverse().ToArray(), 0, buffer, 17, 4);//17-20
          }
          break;

        case ScrcpyControlType.TYPE_SET_CLIPBOARD:
          {
            byte[] utf8_text = Encoding.UTF8.GetBytes(Text);
            buffer = new byte[6 + utf8_text.Length];
            buffer[0] = (byte)ControlType;
            Array.Copy(BitConverter.GetBytes(Paste), 0, buffer, 1, 1);//1
            Array.Copy(BitConverter.GetBytes(utf8_text.Length).Reverse().ToArray(), 0, buffer, 2, 4);//2-5
            Array.Copy(utf8_text, 0, buffer, 6, utf8_text.Length);//6-....
          }
          break;

        case ScrcpyControlType.TYPE_SET_SCREEN_POWER_MODE:
          {
            buffer = new byte[2];
            buffer[0] = (byte)ControlType;
            buffer[1] = (byte)PowerMode;
          }
          break;

        case ScrcpyControlType.TYPE_BACK_OR_SCREEN_ON:
        case ScrcpyControlType.TYPE_EXPAND_NOTIFICATION_PANEL:
        case ScrcpyControlType.TYPE_COLLAPSE_NOTIFICATION_PANEL:
        case ScrcpyControlType.TYPE_GET_CLIPBOARD:
        case ScrcpyControlType.TYPE_ROTATE_DEVICE:
          {
            buffer = new byte[1];
            buffer[0] = (byte)ControlType;
          }
          break;
      }
      return buffer;
    }

    //const int CONTROL_MSG_MAX_SIZE = 1 << 18;//256k
  }
}
