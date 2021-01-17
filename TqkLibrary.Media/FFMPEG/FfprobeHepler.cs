using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TqkLibrary.Media.FFMPEG
{
  public class FfprobeHepler
  {
    private readonly string ffprobePath;

    public FfprobeHepler(string ffprobePath = "FFMPEG\\ffprobe.exe")
    {
      if (!File.Exists(ffprobePath)) throw new FileNotFoundException(ffprobePath);
      this.ffprobePath = ffprobePath;
    }

    private string Analyse_Console(string filePath)
    {
      string arguments = $"-print_format json -show_format -show_streams \"{filePath}\"";//-show_format -sexagesimal
      ProcessStartInfo processStartInfo = new ProcessStartInfo(ffprobePath);
      processStartInfo.Arguments = arguments;
      processStartInfo.CreateNoWindow = true;
      processStartInfo.UseShellExecute = false;
      processStartInfo.RedirectStandardOutput = true;
      using (Process process = Process.Start(processStartInfo)) return process.StandardOutput.ReadToEnd();
    }

    public MediaInfo Analyse(string filePath)
    {
      string text_json = Analyse_Console(filePath);
      return JsonConvert.DeserializeObject<MediaInfo>(text_json);
    }
  }

  public class MediaStream
  {
    public string codec_name { get; set; }
    public string codec_type { get; set; }
    public string r_frame_rate { get; set; }
    public double? duration { get; set; }
    public int? width { get; set; }
    public int? height { get; set; }
    public int? bit_rate { get; set; }
    public int? sample_rate { get; set; }
  }

  public class MediaFormat
  {
    public double? duration { get; set; }
    public int? bit_rate { get; set; }
  }

  public class MediaInfo
  {
    public List<MediaStream> streams { get; set; }
    public MediaFormat format { get; set; }
  }
}