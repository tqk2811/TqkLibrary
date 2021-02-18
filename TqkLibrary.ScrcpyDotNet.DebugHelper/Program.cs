using System;
using System.Diagnostics;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;

namespace TqkLibrary.ScrcpyDotNet.DebugHelper
{
  class Program
  {
    static AutoResetEvent AutoResetEvent_Exit = new AutoResetEvent(false);
    static Process parent;
    static Scrcpy scrcpy;
    static NamedPipeServerStream namedPipeServerStream;
    static byte[] buffer = new byte[256 * 1024];
    static void Main(string[] args)
    {
      AutoResetEvent_Exit.Reset();
      if (args.Length == 0) return;
      string deviceId = args.First();
      parent = Process.GetCurrentProcess().Parent();
      parent.Exited += Parent_Exited;
      scrcpy = new Scrcpy(deviceId);
      scrcpy.Start();
      if (!scrcpy.WaitForFirstFrame())
      {
        Console.Error.WriteLine("WaitForFirstFrame Timeout");
        return;
      }

      string pipe = "ScrcpyDebugHelper_" + Guid.NewGuid();
      namedPipeServerStream = new NamedPipeServerStream(pipe, PipeDirection.InOut, 1);
      namedPipeServerStream.BeginWaitForConnection(BeginWaitForConnectionCallback, null);


      AutoResetEvent_Exit.WaitOne();
    }

    private static void Parent_Exited(object sender, EventArgs e)
    {
      AutoResetEvent_Exit.Set();
    }

    private static void BeginWaitForConnectionCallback(IAsyncResult asyncResult)
    {
      namedPipeServerStream.EndWaitForConnection(asyncResult);
      namedPipeServerStream.BeginRead(buffer, 0, buffer.Length, BeginReadCallback, null);
    }

    private static void BeginReadCallback(IAsyncResult asyncResult)
    {
      int byte_read = namedPipeServerStream.EndRead(asyncResult);
      string command = Encoding.UTF8.GetString(buffer, 0, byte_read);
      if (command.StartsWith("Screenshot", StringComparison.CurrentCultureIgnoreCase))
      {
        byte[] screenshot = scrcpy.GetScreenShotByteArray();
        if (screenshot == null || screenshot.Length == 0)
        {
          screenshot = new byte[1] { 0 };
        }

        namedPipeServerStream.Write(screenshot, 0, screenshot.Length);
        namedPipeServerStream.Flush();
        namedPipeServerStream.WaitForPipeDrain();
      }
      else if (command.StartsWith("Control", StringComparison.CurrentCultureIgnoreCase))
      {
        scrcpy.Control.SendControlBuffer(buffer, 7, byte_read - 7);
      }
      else if (command.StartsWith("Quit", StringComparison.CurrentCultureIgnoreCase))
      {
        AutoResetEvent_Exit.Set();
      }
    }
  }
}
