using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TqkLibrary.ScrcpyDotNet
{
  class FfmpegBufferReaderSupport
  {
    Stream input;
    public FfmpegBufferReaderSupport(Stream input, int buf_size)
    {
      this.input = input;
    }
    public unsafe int read_packet(void* opaque, byte* buf, int buf_size)
    {
      byte[] buffer = new byte[buf_size];
      int byte_read = input.Read(buffer, 0, buf_size);
      Marshal.Copy(buffer, 0, new IntPtr(buf), byte_read);
      return byte_read;
    }
  }
}
