using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TqkLibrary.ScrcpyCapture
{
  class StreamSupport
  {
    Stream input;
    public StreamSupport(Stream input, int buf_size)
    {
      this.input = input;
      buffer = new byte[buf_size];
    }
    readonly byte[] buffer;
    public unsafe int read_packet(void* opaque, byte* buf, int buf_size)
    {
      int byte_read = input.Read(buffer, 0, buffer.Length);
      Marshal.Copy(buffer, 0, new IntPtr(buf), byte_read);
      return byte_read;
    }
  }
}
