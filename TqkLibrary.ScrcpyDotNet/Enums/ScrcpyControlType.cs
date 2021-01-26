namespace TqkLibrary.ScrcpyDotNet
{
  /// <summary>
  /// https://github.com/Genymobile/scrcpy/blob/ce43fad645d4eb30f322dbeb50d5197601564931/server/src/main/java/com/genymobile/scrcpy/ControlMessage.java#L8
  /// </summary>
  public enum ScrcpyControlType : byte
  {
    TYPE_INJECT_KEYCODE = 0,
    TYPE_INJECT_TEXT = 1,
    TYPE_INJECT_TOUCH_EVENT = 2,
    TYPE_INJECT_SCROLL_EVENT = 3,
    TYPE_BACK_OR_SCREEN_ON = 4,
    TYPE_EXPAND_NOTIFICATION_PANEL = 5,
    TYPE_COLLAPSE_NOTIFICATION_PANEL = 6,
    TYPE_GET_CLIPBOARD = 7,
    TYPE_SET_CLIPBOARD = 8,
    TYPE_SET_SCREEN_POWER_MODE = 9,
    TYPE_ROTATE_DEVICE = 10
  }
}
