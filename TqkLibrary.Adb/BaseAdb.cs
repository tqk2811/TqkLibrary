using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace TqkLibrary.Adb
{
  public delegate void AdbLog(string log);

  public class BaseAdb : IDisposable
  {
    private static string _AdbPath = "adb.exe";

    public static string AdbPath
    {
      get { return _AdbPath; }
      set
      {
        if (File.Exists(value)) _AdbPath = value;
        else throw new FileNotFoundException(value);
      }
    }

    private const string tap = "shell input tap {0} {1}";
    private const string swipe = "shell input swipe {0} {1} {2} {3} {4}";

    public int TimeoutDefault { get; set; } = 30000;
    private readonly string adbPath;
    private readonly Random rd = new Random();
    protected CancellationTokenSource TokenSource;

    public CancellationToken CancellationToken { get { return TokenSource.Token; } }

    public readonly string DeviceId;

    public event AdbLog LogCommand;

    public BaseAdb(string deviceId = null, string adbPath = null)
    {
      this.DeviceId = deviceId;
      this.adbPath = adbPath;
      if (File.Exists(adbPath)) _AdbPath = adbPath;
      TokenSource = new CancellationTokenSource();
    }

    #region Static

    public static void KillServer(int timeout = 30000) => ExecuteCommand("adb kill-server", timeout);

    public static void StartServer(int timeout = 30000) => ExecuteCommand("adb start-server", timeout);

    public static List<string> GetDevices()
    {
      List<string> ListDevices = new List<string>();
      string input = ExecuteCommandCmd("devices");
      string pattern = @"(?<=List of devices attached)([^\n]*\n+)+";
      MatchCollection matchCollection = Regex.Matches(input, pattern, RegexOptions.Singleline);
      if (matchCollection.Count > 0)
      {
        string AllDevices = matchCollection[0].Groups[0].Value;
        string[] lines = Regex.Split(AllDevices, "\r\n");

        foreach (var device in lines)
        {
          if (!string.IsNullOrEmpty(device) && device != " ")
          {
            string devices = device.Trim().Replace("device", "");
            ListDevices.Add(devices.Trim());
          }
        }
      }
      return ListDevices;
    }

    public static IEnumerable<string> GetDevicesOnline() => GetDevices().Where(x => !x.EndsWith("\toffline"));

    public static string ExecuteCommand(string command, int timeout = 30000, string adbPath = null)
      => ExecuteCommand(command, CancellationToken.None, timeout, adbPath);

    public static string ExecuteCommand(string command, CancellationToken cancellationToken, int timeout = 30000, string adbPath = null)
    {
      using (Process process = new Process())
      {
        process.StartInfo.FileName = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
        process.StartInfo.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        process.StartInfo.Arguments = command;
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;
        process.Start();
        using CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout);
        using (cancellationTokenSource.Token.Register(() => process.Kill()))
        {
          using (cancellationToken.Register(() => process.Kill()))
          {
            process.WaitForExit();
          }
        }
        cancellationToken.ThrowIfCancellationRequested();
        if (cancellationTokenSource.IsCancellationRequested) throw new AdbTimeoutException();

        string result = process.StandardOutput.ReadToEnd();
        string err = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(err))
        {
          if (err.Trim().StartsWith("Warning:")) throw new AdbException(err, result);
          else
          {
            Console.WriteLine($"AdbCommand:" + command);
            Console.WriteLine($"\t" + err);
          }
        }
        return result;
      }
    }

    public static string ExecuteCommandCmd(string command, int timeout = 30000, string adbPath = null)
      => ExecuteCommandCmd(command, CancellationToken.None, timeout, adbPath);

    public static string ExecuteCommandCmd(string command, CancellationToken cancellationToken, int timeout = 30000, string adbPath = null)
    {
      using (Process process = new Process())
      {
        process.StartInfo.FileName = "cmd.exe";
        process.StartInfo.CreateNoWindow = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.RedirectStandardInput = true;

        process.Start();

        process.StandardInput.WriteLine($"\"{(string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath)}\" {command}");
        process.StandardInput.Flush();
        process.StandardInput.Close();

        process.WaitForExit();
        string result = process.StandardOutput.ReadToEnd();
        string err = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(err))
        {
          if (err.Trim().StartsWith("Warning:")) throw new AdbException(err, result);
          else
          {
            Console.WriteLine($"AdbCommand:" + command);
            Console.WriteLine($"\t" + err);
          }
        }
        return result;
      }
    }

    #endregion

    public void Stop() => TokenSource.Cancel();

    public void Delay(int min, int max) => Task.Delay(rd.Next(min, max), CancellationToken).Wait();

    public void Dispose() => TokenSource.Dispose();

    public string AdbCommand(string command)
      => AdbCommand(command, TimeoutDefault);

    public string AdbCommand(string command, int timeout)
    {
      CancellationToken.ThrowIfCancellationRequested();
      string adbLocation = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
      string commands = string.IsNullOrEmpty(DeviceId) ? command : $"-s {DeviceId} {command}";
      LogCommand?.Invoke(commands);
      return ExecuteCommand(commands, CancellationToken, timeout, adbLocation);
    }

    public string AdbCommandCmd(string command)
      => AdbCommandCmd(command, TimeoutDefault);

    public string AdbCommandCmd(string command, int timeout)
    {
      CancellationToken.ThrowIfCancellationRequested();
      string adbLocation = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
      string commands = string.IsNullOrEmpty(DeviceId) ? command : $"-s {DeviceId} {command}";
      LogCommand?.Invoke(commands);
      return ExecuteCommandCmd(commands, CancellationToken, timeout, adbLocation);
    }

    //public void WaitForDevice() => AdbCommand("wait-for-device");
    public void Root() => AdbCommand("root");

    public void UnRoot() => AdbCommand("unroot");

    public void WaitForDevice(int timeout = 120000) => AdbCommand("wait-for-device", timeout);

    public void Shutdown() => AdbCommand("shell reboot -p");

    public void Reboot() => AdbCommand("shell reboot");

    public void RebootRecovery() => AdbCommand("shell reboot-recovery");

    public void RebootBootLoader() => AdbCommand("shell reboot-bootloader");

    public void FastBoot() => AdbCommand("shell fastboot");

    public void PushFile(string pcPath, string androidPath) => AdbCommand($"push \"{pcPath}\" \"{androidPath}\"");

    public void PushFile(string pcPath, string androidPath, int timeout) => AdbCommand($"push \"{pcPath}\" \"{androidPath}\"", timeout);

    public void PullFile(string androidPath, string pcPath) => AdbCommand($"pull \"{androidPath}\" \"{pcPath}\"");

    public void PullFile(string androidPath, string pcPath, int timeout) => AdbCommand($"pull \"{androidPath}\" \"{pcPath}\"", timeout);

    public void DeleteFile(string androidPath) => AdbCommand($"shell rm \"{androidPath}\"");

    public void InstallApk(string pcPath) => AdbCommand($"install \"{pcPath}\"");

    public void InstallApk(string pcPath, int timeout = 30000) => AdbCommand($"install \"{pcPath}\"", timeout);

    public void UpdateApk(string pcPath) => AdbCommand($"install -r \"{pcPath}\"");

    public void UpdateApk(string pcPath, int timeout = 30000) => AdbCommand($"install -r \"{pcPath}\"", timeout);

    /// <summary>
    /// Example: com.google.android.gms/.accountsettings.mg.ui.main.MainActivity<br/>
    /// OpenApk("com.google.android.gms",".accountsettings.mg.ui.main.MainActivity");
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="activityName"></param>
    //https://developer.android.com/studio/command-line/adb#IntentSpec
    public void OpenApk(string packageName, string activityName) => AdbCommand($"shell am start -n {packageName}/{activityName}");

    public void DisableApk(string packageName) => AdbCommand($"shell pm disable {packageName}");

    public void EnableApk(string packageName) => AdbCommand($"shell pm enable {packageName}");

    public void UnInstallApk(string packageName) => AdbCommand($"uninstall {packageName}");

    public void ForceStopApk(string packageName) => AdbCommand($"shell am force-stop {packageName}");

    public void ClearApk(string packageName) => AdbCommand($"shell pm clear {packageName}");

    public void SetProxy(string proxy, int timeout) => AdbCommand($"shell settings put global http_proxy {proxy}", timeout);

    public void SetProxy(string proxy) => AdbCommand($"shell settings put global http_proxy {proxy}", TimeoutDefault);

    public void ClearProxy() => AdbCommand("shell settings put global http_proxy :0");

    public Bitmap ScreenShot(string FilePath = null, bool deleteInAndroid = true)
    {
      bool IsDelete = false;
      if (string.IsNullOrEmpty(FilePath))
      {
        FilePath = (string.IsNullOrEmpty(DeviceId) ? Guid.NewGuid().ToString() : DeviceId.Replace(":", "_")) + ".png";
        IsDelete = true;
      }
      string androidPath = $"/sdcard/{Guid.NewGuid()}.png";
      AdbCommand($"shell screencap -p \"{androidPath}\"");
      PullFile(androidPath, FilePath);
      if (deleteInAndroid) DeleteFile(androidPath);
      if (File.Exists(FilePath))
      {
        try
        {
          byte[] buff = File.ReadAllBytes(FilePath);
          MemoryStream memoryStream = new MemoryStream(buff);
          return (Bitmap)Bitmap.FromStream(memoryStream);
        }
        finally
        {
          if (IsDelete) try { File.Delete(FilePath); } catch (Exception) { }
        }
      }
      throw new FileNotFoundException(FilePath);
    }

    Point? point = null;
    public Point GetScreenResolution()
    {
      if (point == null)
      {
        Regex regex = new Regex("(?<=mCurrentDisplayRect=Rect\\().*?(?=\\))", RegexOptions.Multiline);
        string result = AdbCommandCmd("shell dumpsys display | Find \"mCurrentDisplayRect\"");//
        Match match = regex.Match(result);
        if (match.Success)
        {
          result = match.Value;

          result = result.Substring(result.IndexOf("- ") + 2);
          string[] temp = result.Split(',');

          int x = Convert.ToInt32(temp[0].Trim());
          int y = Convert.ToInt32(temp[1].Trim());
          point = new Point(x, y);
          return point.Value;
        }
        throw new AdbException(result, null);
      }
      else return point.Value;
    }

    public void Tap(int x, int y, int count = 1)
    {
      for (int i = 0; i < count; i++) AdbCommand(string.Format(tap, x, y));
    }

    public void TapByPercent(double x, double y, int count = 1)
    {
      var resolution = GetScreenResolution();
      int X = (int)(x * resolution.X);
      int Y = (int)(y * resolution.Y);
      Tap(X, Y, count);
    }

    public void Swipe(int x1, int y1, int x2, int y2, int duration = 100) => AdbCommand(string.Format(swipe, x1, y1, x2, y2, duration));

    public void SwipeByPercent(double x1, double y1, double x2, double y2, int duration = 100)
    {
      var resolution = GetScreenResolution();

      int X1 = (int)(x1 * resolution.X);
      int Y1 = (int)(y1 * resolution.Y);
      int X2 = (int)(x2 * resolution.X);
      int Y2 = (int)(y2 * resolution.Y);

      Swipe(X1, Y1, X2, Y2, duration);
    }

    public void LongPress(int x, int y, int duration = 100) => AdbCommand(string.Format(swipe, x, y, x, y, duration));

    public void Key(ADBKeyEvent key) => AdbCommand(string.Format("shell input keyevent {0}", key));

    public void Key(int keyCode) => AdbCommand(string.Format("shell input keyevent {0}", keyCode));

    public void InputText(string text) => AdbCommand(string.Format("shell input text \"{0}\"",
        text.Replace(" ", "%s").Replace("&", "\\&").Replace("<", "\\<").Replace(">", "\\>").Replace("?", "\\?").Replace(":", "\\:").Replace("{", "\\{").Replace("}", "\\}").Replace("[", "\\[").Replace("]", "\\]").Replace("|", "\\|")));

    public void PlanModeON()
    {
      AdbCommand("settings put global airplane_mode_on 1");
      AdbCommand("am broadcast -a android.intent.action.AIRPLANE_MODE");
    }

    public void PlanModeOFF()
    {
      AdbCommand("settings put global airplane_mode_on 0");
      AdbCommand("am broadcast -a android.intent.action.AIRPLANE_MODE");
    }
  }
}