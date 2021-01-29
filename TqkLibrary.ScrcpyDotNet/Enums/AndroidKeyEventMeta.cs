using System;

namespace TqkLibrary.ScrcpyDotNet
{
  /// <summary>
  /// https://developer.android.com/reference/android/view/KeyEvent#META_ALT_LEFT_ON
  /// </summary>
  [Flags]
  public enum AndroidKeyEventMeta : int
  {
    META_SHIFT_ON = 1,
    META_ALT_ON = 2,
    META_SYM_ON = 4,
    META_FUNCTION_ON = 8,
    META_ALT_LEFT_ON = 16,
    META_ALT_RIGHT_ON = 32,
    META_ALT_MASK = 50,
    META_SHIFT_LEFT_ON = 64,
    META_SHIFT_RIGHT_ON = 128,
    META_SHIFT_MASK = 193,
    META_CTRL_ON = 4096,
    META_CTRL_LEFT_ON = 8192,
    META_CTRL_RIGHT_ON = 16384,
    META_CTRL_MASK = 28672,
    META_META_ON = 65536,
    META_META_LEFT_ON = 131072,
    META_META_RIGHT_ON = 262144,
    META_META_MASK = 458752,
    META_CAPS_LOCK_ON = 1048576,
    META_NUM_LOCK_ON = 2097152,
    META_SCROLL_LOCK_ON = 4194304,
  }
}
