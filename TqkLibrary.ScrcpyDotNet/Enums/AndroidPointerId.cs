using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.ScrcpyDotNet
{
  public enum AndroidPointerId : ulong
  {
    POINTER_ID_MOUSE = ulong.MaxValue,
    POINTER_ID_VIRTUAL_FINGER = ulong.MaxValue - 1,
  }
}
