using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TqkLibrary.Adb
{
  public delegate void AdbLog(string log);

  public class BaseAdb
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

    private readonly string adbPath;
    public readonly string DeviceId;

    public event AdbLog LogEvent;

    public BaseAdb(string deviceName = null, string adbPath = null)
    {
      this.DeviceId = deviceName;
      this.adbPath = adbPath;
      if (File.Exists(adbPath)) _AdbPath = adbPath;
    }

    public string AdbCommand(string command)
    {
      string adbLocation = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
      string commands = string.IsNullOrEmpty(DeviceId) ? command : $"-s {DeviceId} {command}";
      LogEvent?.Invoke(commands);
      return ExecuteCommand(commands, adbLocation);
    }

    private static string ExecuteCommand(string command, string adbPath = null)
    {
      using (Process process = new Process())
      {
        process.StartInfo.FileName = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
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
        if (!string.IsNullOrEmpty(err)) throw new AdbException(err, result);
        return result;
      }
    }

    private static string ExecuteCommandCmd(string command, string adbPath = null)
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
        if (!string.IsNullOrEmpty(err)) throw new AdbException(err, result);
        return result;
      }
    }

    public void WaitForDevice() => AdbCommand("wait-for-device");

    public static void KillServer() => ExecuteCommand("adb kill-server");

    public static void StartServer() => ExecuteCommand("adb start-server");

    public void Shutdown() => AdbCommand("shell reboot -p");

    public void Reboot() => AdbCommand("shell reboot");

    public void RebootRecovery() => AdbCommand("shell reboot-recovery");

    public void RebootBootLoader() => AdbCommand("shell reboot-bootloader");

    public void FastBoot() => AdbCommand("shell fastboot");

    public void PushFile(string pcPath, string androidPath) => AdbCommand($"push \"{pcPath}\" \"{androidPath}\"");

    public void PullFile(string androidPath, string pcPath) => AdbCommand($"pull \"{androidPath}\" \"{pcPath}\"");

    public void DeleteFile(string androidPath) => AdbCommand($"shell rm \"{androidPath}\"");

    public void InstallApk(string androidPath) => AdbCommand($"install \"{androidPath}\"");

    public void UpdateApk(string androidPath) => AdbCommand($"install -r \"{androidPath}\"");

    /// <summary>
    /// Example: com.google.android.gms/.accountsettings.mg.ui.main.MainActivity
    /// OpenApk("com.google.android.gms",".accountsettings.mg.ui.main.MainActivity");
    /// </summary>
    /// <param name="appName"></param>
    /// <param name="ActivityName"></param>
    public void OpenApk(string appName, string ActivityName) => AdbCommand($"shell am start -n {appName}/{ActivityName}");

    public void DisableApk(string appName) => AdbCommand($"shell pm disable {appName}");

    public void EnableApk(string appName) => AdbCommand($"shell pm enable {appName}");

    public void UnInstallApk(string packageName) => AdbCommand($"uninstall {packageName}");

    public void ForceStopApk(string appName) => AdbCommand($"shell am force-stop {appName}");

    public void ClearApk(string appName) => AdbCommand($"shell pm clear {appName}");

    public void SetProxy(string proxy) => AdbCommand($"shell settings put global http_proxy {proxy}");

    public void ClearProxy() => AdbCommand("shell settings put global http_proxy :0");

    public Bitmap ScreenShot(string FilePath = null)
    {
      bool IsDelete = false;
      if (string.IsNullOrEmpty(FilePath))
      {
        FilePath = (string.IsNullOrEmpty(DeviceId) ? Guid.NewGuid().ToString() : DeviceId) + ".png";
        IsDelete = true;
      }
      const string androidPath = "/sdcard/screen.png";
      AdbCommand($"shell screencap -p \"{androidPath}\"");
      PullFile(androidPath, FilePath);
      DeleteFile(androidPath);
      using FileStream fs = new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
      Bitmap result = (Bitmap)Bitmap.FromStream(fs);
      if (IsDelete) try { File.Delete(FilePath); } catch (Exception) { }
      return result;
    }

    public Point GetScreenResolution()
    {
      Regex regex = new Regex("(?<=mCurrentDisplayRect=Rect\\().*?(?=\\))", RegexOptions.Multiline);
      string result = ExecuteCommandCmd("shell dumpsys display | Find \"mCurrentDisplayRect\"", adbPath);//
      Match match = regex.Match(result);
      if (match.Success)
      {
        result = match.Value;

        result = result.Substring(result.IndexOf("- ") + 2);
        string[] temp = result.Split(',');

        int x = Convert.ToInt32(temp[0].Trim());
        int y = Convert.ToInt32(temp[1].Trim());

        return new Point(x, y);
      }
      throw new AdbException(result, null);
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
  }
}