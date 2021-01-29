using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using TqkLibrary.ScrcpyDotNet.Util;
//using TqkLibrary.ScrcpyDotNet.Util;
using static FFmpeg.AutoGen.ffmpeg;
namespace TqkLibrary.ScrcpyDotNet
{
  //https://github.com/Genymobile/scrcpy/issues/673
  public sealed class Scrcpy
  {
    static readonly Random random = new Random();
    static string adbPath = "adb.exe";

    public string DeviceName { get; set; }
    public int Width { get; set; } = -1;
    public int Height { get; set; } = -1;
    public ScrcpyControl Control { get; }

    int reversePort = 34676;
    public readonly string deviceId;
    bool IsRunning = false;
    AutoResetEvent AutoResetEvent = new AutoResetEvent(false);
    stream scrcpyStream;
    public Scrcpy(string deviceId = null, string adbPath = null)
    {
      this.deviceId = deviceId;
      if (!string.IsNullOrEmpty(adbPath))
      {
        if (!File.Exists(adbPath)) throw new FileNotFoundException(adbPath);
        else Scrcpy.adbPath = adbPath;
      }
      Control = new ScrcpyControl(this);
    }

    public void Start()
    {
      if(!IsRunning)
      {
        AutoResetEvent.Reset();
        Task.Factory.StartNew(InitServerConnection, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        IsRunning = true;
      }
    }

    public bool WaitForConnect(int timeout = 60000)
    {
      return AutoResetEvent.WaitOne(timeout);
    }

    public void Stop()
    {
      Control._controlStream = null;
      IsRunning = false;
    }

    //public Bitmap GetLastedFrame()
    //{
    //  lock (_lock)
    //  {
    //    if (buffer_image == null) return null;
    //    MemoryStream memoryStream = new MemoryStream(buffer_image);
    //    return (Bitmap)Bitmap.FromStream(memoryStream);
    //  }
    //}

    void InitServerConnection()
    {
      TcpListener server = null;
      TcpClient client = null;
      NetworkStream stream = null;
      try
      {
        while(true)
        {
          try
          {
            reversePort = random.Next(10000, 55000);
            server = new TcpListener(IPAddress.Parse("127.0.0.1"), reversePort);
            server.Start();
            break;
          }
          catch (Exception)
          {

          }
        }        
        byte[] buffer = new byte[64];
        byte[] sizebuff = new byte[2];

        Task.Factory.StartNew(DeployServer, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        using(CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(60000))
        {
          using(cancellationTokenSource.Token.Register(() => server.Stop()))
          {
            client = server.AcceptTcpClient();
          }
          cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }
        stream = client.GetStream();
        BinaryReader br = new BinaryReader(stream);
        br.Read(buffer, 0, 64);
        DeviceName = Encoding.ASCII.GetString(buffer, 0, 64);

        br.Read(sizebuff, 0, sizebuff.Length);
        Width = BitConverter.ToInt16(sizebuff.Reverse().ToArray(), 0);

        br.Read(sizebuff, 0, sizebuff.Length);
        Height = BitConverter.ToInt16(sizebuff.Reverse().ToArray(), 0);

        Control._controlStream = stream;

        AutoResetEvent.Set();
        using (scrcpyStream = new stream(client, Width, Height)) scrcpyStream.RunStream();
      }
      finally
      {
        client?.Dispose();
        server.Stop();
      }
    }

    void DeployServer()
    {
      AdbCommand("reverse --remove localabstract:scrcpy");
      AdbCommand($"push scrcpy-server1.17.jar \"/data/local/tmp/scrcpy-server.jar\"");
      AdbCommand($"reverse localabstract:scrcpy tcp:{reversePort}");
      string version = "1.17";
      string loglevel = "info";
      int max_size_string = 0;
      int bit_rate_string = 8000000;
      int max_fps_string = 0;
      int lock_video_orientation_string = -1;
      bool tunnel_forward = false;
      string crop = "-";
      bool frame_meta = true;//required
      bool control = true;
      int display_id_string = 0;
      bool show_touches = true;
      bool stay_awake = true;
      string codec_options = "-";
      string encoder_name = "-";

      AdbCommand($"shell CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.genymobile.scrcpy.Server {version} {loglevel} {max_size_string} {bit_rate_string} {max_fps_string} {lock_video_orientation_string} {tunnel_forward} {crop} {frame_meta} {control} {display_id_string} {show_touches} {stay_awake} {codec_options} {encoder_name}");
    }

    string AdbCommand(string command)
    {
      if (string.IsNullOrEmpty(deviceId)) return ExecuteCommand(command);
      else return ExecuteCommand($"-s {deviceId} {command}");
    }
    static string ExecuteCommand(string command)
    {
      using Process process = new Process();
      process.StartInfo.FileName = adbPath;
      process.StartInfo.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
      process.StartInfo.Arguments = command;
      process.StartInfo.CreateNoWindow = true;
      process.StartInfo.UseShellExecute = false;
      process.StartInfo.RedirectStandardOutput = true;
      process.StartInfo.RedirectStandardError = true;
      process.StartInfo.RedirectStandardInput = true;
      process.Start();
      process.WaitForExit();

      string result = process.StandardOutput.ReadToEnd();
      string err = process.StandardError.ReadToEnd();
      return result;
    }

    public Bitmap GetScreenShot() => scrcpyStream?.GetScreenShot();
  }
}
