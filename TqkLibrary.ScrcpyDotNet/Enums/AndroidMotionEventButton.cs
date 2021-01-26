using System;

namespace TqkLibrary.ScrcpyDotNet
{
  /// <summary>
  /// https://developer.android.com/reference/android/view/MotionEvent#ACTION_BUTTON_PRESS
  /// </summary>
  [Flags]
  public enum AndroidMotionEventButton : int
  {
    BUTTON_PRIMARY = 1,
    BUTTON_SECONDARY = 2,
    BUTTON_TERTIARY = 4,
    BUTTON_BACK = 8,
    BUTTON_FORWARD = 16,
    BUTTON_STYLUS_PRIMARY = 32,
    BUTTON_STYLUS_SECONDARY = 64,
  }
}
