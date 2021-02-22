using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TqkLibrary.ScrcpyDotNet.Util;
//using TqkLibrary.ScrcpyDotNet.Util;
namespace TqkLibrary.ScrcpyDotNet
{
  public delegate void OnException(Exception ex);
  //https://github.com/Genymobile/scrcpy/issues/673
  public sealed class Scrcpy
  {
    static readonly Random random = new Random();
    static string adbPath = "adb.exe";

    public event OnException OnException;

    public bool ShowTouches { get; set; } = true;
    public bool StayAwake { get; set; } = true;

    public string DeviceName { get; private set; }
    public int Width { get; private set; } = -1;
    public int Height { get; private set; } = -1;
    public ScrcpyControl Control { get; }

    int reversePort = 34676;
    public readonly string deviceId;

    bool _isRunning = false;
    public bool IsRunning
    {
      get { return _isRunning; }
      private set
      {
        if (value)
        {
          _isRunning = value;
        }
        else
        {
          if (scrcpyStream != null)
          {
            scrcpyStream.IsRunning = value;
            _isRunning = value;
          }
        }
      }
    }

    AutoResetEvent AutoResetEvent_Connect = new AutoResetEvent(false);
    AutoResetEvent AutoResetEvent_FirstFrame = new AutoResetEvent(false);
    stream_in scrcpyStream;
    int ImageBufferLength = 1024 * 1024;
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

    public void Start(int ImageBufferLength = 1024 * 1024)
    {
      if (!IsRunning)
      {
        this.ImageBufferLength = ImageBufferLength;
        AutoResetEvent_Connect.Reset();
        AutoResetEvent_FirstFrame.Reset();
        Task.Factory.StartNew(InitServerConnection, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(TaskContinue);
        IsRunning = true;
      }
    }

    public bool WaitForConnect(int timeout = 10000) => AutoResetEvent_Connect.WaitOne(timeout);

    public bool WaitForFirstFrame(int timeout = 10000) => AutoResetEvent_FirstFrame.WaitOne(timeout);

    public void Stop()
    {
      IsRunning = false;
    }

    public Bitmap GetScreenShot() => scrcpyStream?.GetScreenShot();

    public byte[] GetScreenShotByteArray() => scrcpyStream?.GetScreenShotByteArray();

#if TestVideo

    public string InitVideoH264Stream() => scrcpyStream?.InitVideoH264Stream();

    public void StopStream() => scrcpyStream?.StopStream();

#endif

    void InitServerConnection()
    {
      TcpListener server = null;
      TcpClient video_client = null;
      TcpClient control_client = null;
      try
      {
        while (true)
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

        Task.Factory.StartNew(DeployServer, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(TaskContinue);
        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(60000))
        {
          using (cancellationTokenSource.Token.Register(() => server.Stop()))
          {
            video_client = server.AcceptTcpClient();
            control_client = server.AcceptTcpClient();
          }
          cancellationTokenSource.Token.ThrowIfCancellationRequested();
        }

        NetworkStream stream = video_client.GetStream();
        stream.Read(buffer, 0, 64);
        DeviceName = Encoding.ASCII.GetString(buffer, 0, 64);

        stream.Read(sizebuff, 0, sizebuff.Length);
        Width = BitConverter.ToInt16(sizebuff.Reverse().ToArray(), 0);

        stream.Read(sizebuff, 0, sizebuff.Length);
        Height = BitConverter.ToInt16(sizebuff.Reverse().ToArray(), 0);

        Control._controlStream = control_client.GetStream();

        AutoResetEvent_Connect.Set();
        using (scrcpyStream = new stream_in(video_client, Width, Height, ImageBufferLength))
        {
          scrcpyStream.stopCallback += ScrcpyStream_stopCallback;
          scrcpyStream.firstFrameTrigger += () => AutoResetEvent_FirstFrame.Set();
          scrcpyStream.IsRunning = IsRunning;
          scrcpyStream.RunStream();
        }
      }
      finally
      {
        _isRunning = false;
        scrcpyStream = null;
        Control._controlStream = null;
        control_client?.Dispose();
        video_client?.Dispose();
        server?.Stop();
        AdbCommand("reverse --remove localabstract:scrcpy");
      }
    }

    private void ScrcpyStream_stopCallback(bool byUser)
    {
      //_isRunning = false;
    }

    void DeployServer()
    {
      try
      {
        AdbCommand("reverse --remove localabstract:scrcpy");
      }
      catch (Exception) { }
      AdbCommand($"push scrcpy-server1.17.jar \"/data/local/tmp/scrcpy_server_tqk.jar\"");
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
      string codec_options = "-";
      string encoder_name = "-";

      AdbCommand($"shell CLASSPATH=/data/local/tmp/scrcpy_server_tqk.jar app_process / com.genymobile.scrcpy.Server {version} {loglevel} {max_size_string} {bit_rate_string} {max_fps_string} {lock_video_orientation_string} {tunnel_forward} {crop} {frame_meta} {control} {display_id_string} {ShowTouches} {StayAwake} {codec_options} {encoder_name}");
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
      if (!string.IsNullOrEmpty(err)) throw new AdbException(command, result, err);
      return result;
    }

    void TaskContinue(Task task)
    {
      IsRunning = false;
      if (task.IsFaulted) OnException?.Invoke(task.Exception);
    }
  }
}
