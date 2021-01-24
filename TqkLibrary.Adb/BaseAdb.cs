﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

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

    public int TimeoutDefault { get; set; } = 10000;
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

    public void Stop() => TokenSource.Cancel();

    public void Delay(int min, int max)
    {
      int time = rd.Next(min, max) / 100;
      for (int i = 0; i < time; i++)
      {
        Task.Delay(100).Wait();
        CancellationToken.ThrowIfCancellationRequested();
      }
    }

    public void Dispose() => TokenSource.Dispose();

    public string AdbCommand(string command,int? timeout = null)
    {
      CancellationToken.ThrowIfCancellationRequested();
      string adbLocation = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
      string commands = string.IsNullOrEmpty(DeviceId) ? command : $"-s {DeviceId} {command}";
      LogCommand?.Invoke(commands);
      return ExecuteCommand(commands, adbLocation,timeout == null ? TimeoutDefault : timeout.Value);
    }

    public string AdbCommandCmd(string command)
    {
      CancellationToken.ThrowIfCancellationRequested();
      string adbLocation = string.IsNullOrEmpty(adbPath) ? AdbPath : adbPath;
      string commands = string.IsNullOrEmpty(DeviceId) ? command : $"-s {DeviceId} {command}";
      LogCommand?.Invoke(commands);
      return ExecuteCommandCmd(commands, adbLocation);
    }

    public static string ExecuteCommand(string command, string adbPath = null,int timeout = 10000)
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
          process.WaitForExit();
        }
        if (cancellationTokenSource.IsCancellationRequested) throw new AdbTimeoutException();

        string result = process.StandardOutput.ReadToEnd();
        string err = process.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(err)) throw new AdbException(err, result);
        return result;
      }
    }

    public static string ExecuteCommandCmd(string command, string adbPath = null)
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

    public void WaitForDevice(int timeout = 300000) => AdbCommand("wait-for-device",timeout);

    public static void KillServer() => ExecuteCommand("adb kill-server");

    public static void StartServer() => ExecuteCommand("adb start-server");

    public void Shutdown() => AdbCommand("shell reboot -p");

    public void Reboot() => AdbCommand("shell reboot");

    public void RebootRecovery() => AdbCommand("shell reboot-recovery");

    public void RebootBootLoader() => AdbCommand("shell reboot-bootloader");

    public void FastBoot() => AdbCommand("shell fastboot");

    public void PushFile(string pcPath, string androidPath,int timeout = 30000) => AdbCommand($"push \"{pcPath}\" \"{androidPath}\"",timeout);

    public void PullFile(string androidPath, string pcPath, int timeout = 30000) => AdbCommand($"pull \"{androidPath}\" \"{pcPath}\"", timeout);

    public void DeleteFile(string androidPath) => AdbCommand($"shell rm \"{androidPath}\"");

    public void InstallApk(string pcPath, int timeout = 30000) => AdbCommand($"install \"{pcPath}\"", timeout);

    public void UpdateApk(string pcPath, int timeout = 30000) => AdbCommand($"install -r \"{pcPath}\"", timeout);

    /// <summary>
    /// Example: com.google.android.gms/.accountsettings.mg.ui.main.MainActivity
    /// OpenApk("com.google.android.gms",".accountsettings.mg.ui.main.MainActivity");
    /// </summary>
    /// <param name="packageName"></param>
    /// <param name="activityName"></param>
    public void OpenApk(string packageName, string activityName) => AdbCommand($"shell am start -n {packageName}/{activityName}");

    public void DisableApk(string packageName) => AdbCommand($"shell pm disable {packageName}");

    public void EnableApk(string packageName) => AdbCommand($"shell pm enable {packageName}");

    public void UnInstallApk(string packageName) => AdbCommand($"uninstall {packageName}");

    public void ForceStopApk(string packageName) => AdbCommand($"shell am force-stop {packageName}");

    public void ClearApk(string packageName) => AdbCommand($"shell pm clear {packageName}");

    public void SetProxy(string proxy) => AdbCommand($"shell settings put global http_proxy {proxy}");

    public void ClearProxy() => AdbCommand("shell settings put global http_proxy :0");

    public Bitmap ScreenShot(string FilePath = null)
    {
      bool IsDelete = false;
      if (string.IsNullOrEmpty(FilePath))
      {
        FilePath = (string.IsNullOrEmpty(DeviceId) ? Guid.NewGuid().ToString() : DeviceId.Replace(":", "_")) + ".png";
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
      string result = AdbCommandCmd("shell dumpsys display | Find \"mCurrentDisplayRect\"");//
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

    public static IEnumerable<string> GetDevicesOnline() => GetDevices().Where(x => !x.EndsWith("\toffline"));
  }
}