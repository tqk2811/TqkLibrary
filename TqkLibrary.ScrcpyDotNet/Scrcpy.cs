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

    public uint ReadPacketTryAgainTimes { get; set; } = 3;
    public bool ShowTouches { get; set; } = true;
    public bool StayAwake { get; set; } = true;
    public Orientation Orientation { get; set; } = Orientation.Auto;
    public bool IsControl { get; set; } = true;
    /// <summary>
    /// 0 is unlimit
    /// </summary>
    public int MaxFps { get; set; } = 0;


    public string DeviceName { get; private set; }
    public int Width { get; private set; } = -1;
    public int Height { get; private set; } = -1;
    public ScrcpyControl Control { get; }

    public bool IsRunning { get; internal set; }

    public readonly string deviceId;

    int reversePort = 34676;
    AutoResetEvent AutoResetEvent_FirstFrame = new AutoResetEvent(false);

    MediaStreamIn scrcpyStream;
    int ImageBufferLength = 1024 * 1024;
    TcpListener server = null;
    TcpClient video_client = null;
    TcpClient control_client = null;

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

    public Task Start(int ImageBufferLength = 1024 * 1024)
    {
      if (!IsRunning)
      {
        this.ImageBufferLength = ImageBufferLength;
        AutoResetEvent_FirstFrame.Reset(); 
        IsRunning = true;
        return Task.Factory.StartNew(InitServerConnection, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)/*.ContinueWith(TaskContinue)*/;
      }
      else return Task.FromResult(0);
    }

    public bool WaitForFirstFrame(int timeout = 10000) => AutoResetEvent_FirstFrame.WaitOne(timeout);

    public void Stop()
    {
      IsRunning = false;
    }

    public Bitmap GetScreenShot() => scrcpyStream?.GetScreenShot();

    public byte[] GetScreenShotByteArray() => scrcpyStream?.GetScreenShotByteArray();

    public string InitVideoStream() => scrcpyStream?.InitVideoStream();
    public void StopStream() => scrcpyStream?.StopStream();

    void InitServerConnection()
    {
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

        Task task_DeployServer = Task.Factory.StartNew(DeployServer, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(10000))
        {
          using (cancellationTokenSource.Token.Register(() => server.Stop()))
          {
            video_client = server.AcceptTcpClient();
            if(IsControl) control_client = server.AcceptTcpClient();
          }
          if (cancellationTokenSource.IsCancellationRequested)
          {
            task_DeployServer.Wait();//throw exception if crash
            throw new ScrcpyException(0, "No connection from scrcpy server");
          }
        }

        NetworkStream stream = video_client.GetStream();

        stream.Read(buffer, 0, 64);
        DeviceName = Encoding.ASCII.GetString(buffer, 0, 64);

        stream.Read(sizebuff, 0, sizebuff.Length);
        Width = BitConverter.ToInt16(sizebuff.Reverse().ToArray(), 0);

        stream.Read(sizebuff, 0, sizebuff.Length);
        Height = BitConverter.ToInt16(sizebuff.Reverse().ToArray(), 0);

        if (IsControl) Control._controlStream = control_client.GetStream();

        scrcpyStream = new MediaStreamIn(this, video_client, Width, Height, ImageBufferLength);
        scrcpyStream.firstFrameTrigger += () => AutoResetEvent_FirstFrame.Set();
        scrcpyStream.resolutionChange += ScrcpyStream_resolutionChange;

        Task.Factory.StartNew(scrcpyStream.RunStream, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).ContinueWith(TaskContinue);
      }
      catch(Exception)
      {
        StopByError(false);
        throw;
      }
    }

    private void ScrcpyStream_resolutionChange(int width, int height)
    {
      this.Width = width;
      this.Height = height;
    }

    private void StopByError(bool byUser)
    {
      try
      {
        scrcpyStream?.Dispose();
        scrcpyStream = null;

        Control._controlStream = null;

        control_client?.Dispose();
        control_client = null;

        video_client?.Dispose();
        video_client = null;

        server?.Stop();
        server = null;

        try { AdbCommand("reverse --remove localabstract:scrcpy"); } catch (Exception) { }
      }
      finally
      {
        IsRunning = false;
      }
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
      bool tunnel_forward = false;
      string crop = "-";
      bool frame_meta = true;//required
      int display_id_string = 0;
      string codec_options = "-";
      string encoder_name = "-";

      AdbCommand("shell CLASSPATH=/data/local/tmp/scrcpy_server_tqk.jar app_process / com.genymobile.scrcpy.Server " + 
        $"{version} {loglevel} {max_size_string} {bit_rate_string} {MaxFps} {(int)Orientation} {tunnel_forward} {crop} {frame_meta} {IsControl} {display_id_string} {ShowTouches} {StayAwake} {codec_options} {encoder_name}");
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
      StopByError(false);
      if (task.IsFaulted)
      {
        OnException?.Invoke(task.Exception);
      }
    }
  }
}
