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

namespace TqkLibrary.ScrcpyCapture
{
  public class ScrcpyImageCapture
  {
    static readonly Random random = new Random();
    static string adbPath;


    public string DeviceName { get; set; }
    public int Width { get; set; } = -1;
    public int Height { get; set; } = -1;


    AVPacket LastPacket;
    readonly object _lock = new object();
    StreamSupport streamSupport = null;
    int reversePort = 34676;
    public readonly string deviceId;
    bool IsRunning = false;
    IntPtr codecCtx;
    public ScrcpyImageCapture(string deviceId = null, string adbPath = null)
    {
      this.deviceId = deviceId;
      if (!string.IsNullOrEmpty(adbPath))
      {
        if (!File.Exists(adbPath)) throw new FileNotFoundException(adbPath);
        else ScrcpyImageCapture.adbPath = adbPath;
      }
    }

    public void Start()
    {
      if(!IsRunning)
      {
        Task.Factory.StartNew(ListenerScrcpyServer, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        IsRunning = true;
      }
    }

    public void Stop()
    {
      IsRunning = false;
    }

    void ListenerScrcpyServer()
    {
      TcpListener server = new TcpListener(IPAddress.Parse("127.0.0.1"), reversePort);
      TcpClient client = null;
      NetworkStream stream = null;
      try
      {
        server.Start();
        byte[] buffer = new byte[1024 * 1024];
        byte[] sizebuff = new byte[2];

        Task.Factory.StartNew(InstallScrcpyServer, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        client = server.AcceptTcpClient();
        stream = client.GetStream();
        BufferedStream bf = new BufferedStream(stream);
        bf.Read(buffer, 0, 64);
        DeviceName = Encoding.ASCII.GetString(buffer, 0, 64);

        bf.Read(sizebuff, 0, sizebuff.Length);
        Width = BitConverter.ToInt16(BitConverter.IsLittleEndian ? sizebuff.Reverse().ToArray() : sizebuff, 0);

        bf.Read(sizebuff, 0, sizebuff.Length);
        Height = BitConverter.ToInt16(BitConverter.IsLittleEndian ? sizebuff.Reverse().ToArray() : sizebuff, 0);

        streamSupport = new StreamSupport(bf, buffer.Length);

        Task.Factory.StartNew(CaptureFrame, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Wait();
      }
      finally
      {
        client?.Dispose();
        server.Stop();
      }
    }

    void InstallScrcpyServer()
    {
      //get random port
      
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
      bool frame_meta = false;
      bool control = false;
      int display_id_string = 0;
      bool show_touches = false;
      bool stay_awake = true;
      string codec_options = "-";
      string encoder_name = "-";

      AdbCommand($"shell CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.genymobile.scrcpy.Server {version} {loglevel} {max_size_string} {bit_rate_string} {max_fps_string} {lock_video_orientation_string} {tunnel_forward} {crop} {frame_meta} {control} {display_id_string} {show_touches} {stay_awake} {codec_options} {encoder_name}");
    }

    unsafe void CaptureFrame()
    {
      ffmpeg.av_register_all();
      ffmpeg.avformat_network_init();
      avio_alloc_context_read_packet avio_Alloc_Context_Read_Packet = streamSupport.read_packet;
      AVFormatContext* aVFormatContext = null;
      byte* aviobuffer = null;
      AVIOContext* avio = null;
      try
      {
        AVInputFormat* aVInputFormat = ffmpeg.av_find_input_format("h264");
        aVFormatContext = ffmpeg.avformat_alloc_context();
        aviobuffer = (byte*)ffmpeg.av_malloc(4096);        
        avio = ffmpeg.avio_alloc_context(aviobuffer, 4096, 0, null, avio_Alloc_Context_Read_Packet, null, null);
        aVFormatContext->pb = avio;

        ffmpeg.avformat_open_input(&aVFormatContext, null, aVInputFormat, null).CheckError("ffmpeg.avformat_open_input h264");
        ffmpeg.avformat_find_stream_info(aVFormatContext, null).CheckError("ffmpeg.avformat_find_stream_info h264");

        AVCodec* codec = null;
        int stream_index = ffmpeg.av_find_best_stream(aVFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
        if (stream_index == -1) throw new ScrcpyImageCaptureException(-1, "ffmpeg.av_find_best_stream Can't find stream_index");
        if(codec == null) throw new ScrcpyImageCaptureException(0, "ffmpeg.av_find_best_stream found stream_index but AVCodec is null");

        AVCodecContext* codecCtx = aVFormatContext->streams[stream_index]->codec;
        if (codecCtx == null) throw new ScrcpyImageCaptureException(0, "Codec in stream_index is null");
        this.codecCtx = new IntPtr(codecCtx);

        ffmpeg.avcodec_open2(codecCtx, codec, null).CheckError("ffmpeg.avcodec_open2 h264");

        AVStream* aVStream = aVFormatContext->streams[stream_index];
        AVPacket avPacket;
        while (IsRunning)
        {
          int error_code = ffmpeg.av_read_frame(aVFormatContext, &avPacket);
          if (error_code >= 0 && avPacket.stream_index == stream_index)
          {
            lock(_lock) LastPacket = avPacket;
          }
        }        
      }
      finally
      {
        ffmpeg.avformat_free_context(aVFormatContext);
        ffmpeg.av_free(aviobuffer);
        ffmpeg.avio_context_free(&avio);
        lock (_lock) this.codecCtx = IntPtr.Zero;
      }
    }

    public Bitmap ScreenShot()
    {
      byte[] buffer = GetBufferImageJPEG();
      MemoryStream memoryStream = new MemoryStream(buffer);
      return (Bitmap)Bitmap.FromStream(memoryStream);
    }
    
    unsafe byte[] GetBufferImageJPEG()
    {
      AVFrame* thumbnail_frame = null;
      AVCodecContext* pMJPEGCtx = null;
      AVFrame* oframe = null;
     
      byte* out_buf = null;
      byte[] buffer_result = null;
      try
      {
        AVCodecContext* avctx;
        AVPacket avpkt;
        lock (_lock)
        {
          if (this.codecCtx == IntPtr.Zero) return null;
          avctx = (AVCodecContext*)this.codecCtx.ToPointer();
          avpkt = this.LastPacket;
        }

        AVCodec* h264_codec = ffmpeg.avcodec_find_decoder(avctx->codec_id);
        AVCodecContext* h264_decoder_codec_ctx = ffmpeg.avcodec_alloc_context3(h264_codec);
        h264_decoder_codec_ctx->width = avctx->width;
        h264_decoder_codec_ctx->height = avctx->height;
        h264_decoder_codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_RGB24;
        h264_decoder_codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
        h264_decoder_codec_ctx->skip_frame = AVDiscard.AVDISCARD_NONINTRA;//AVDISCARD_NONREF;//AVDISCARD_NONINTRA;
        h264_decoder_codec_ctx->extradata = avctx->extradata;
        h264_decoder_codec_ctx->extradata_size = avctx->extradata_size;
        h264_decoder_codec_ctx->time_base = avctx->time_base;
        //h264_decoder_codec_ctx->time_base.num = 1;
        //h264_decoder_codec_ctx->time_base.den = 30;

        ffmpeg.avcodec_open2(h264_decoder_codec_ctx, h264_codec, null).CheckError("ffmpeg.avcodec_open2 h264 decode");
        if (h264_codec == null) throw new ScrcpyImageCaptureException(0, "h264_codec is null");

        ffmpeg.avcodec_send_packet(h264_decoder_codec_ctx, &avpkt).CheckError("ffmpeg.avcodec_send_packet h264 decode");

        thumbnail_frame = ffmpeg.av_frame_alloc();
        ffmpeg.avcodec_receive_frame(h264_decoder_codec_ctx, thumbnail_frame).CheckError("ffmpeg.avcodec_receive_frame h264 decode");


        // JPEG encode
        AVCodec* pMJPEGCodec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MJPEG);
        pMJPEGCtx = ffmpeg.avcodec_alloc_context3(pMJPEGCodec);
        if (pMJPEGCtx == null) throw new ScrcpyImageCaptureException(0, "AVCodecContext (AV_CODEC_ID_MJPEG) is null");
        pMJPEGCtx->bit_rate = avctx->bit_rate;
        pMJPEGCtx->width = avctx->width;
        pMJPEGCtx->height = avctx->height;
        pMJPEGCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
        pMJPEGCtx->codec_id = AVCodecID.AV_CODEC_ID_MJPEG;
        pMJPEGCtx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
        pMJPEGCtx->time_base.num = avctx->time_base.num;
        pMJPEGCtx->time_base.den = avctx->time_base.den;
        //pMJPEGCtx->time_base.num = 1;
        //pMJPEGCtx->time_base.den = 30;

        if (pMJPEGCodec == null ) throw new ScrcpyImageCaptureException(0, "AVCodec (AV_CODEC_ID_MJPEG) is null");

        ffmpeg.avcodec_open2(pMJPEGCtx, pMJPEGCodec, null).CheckError("ffmpeg.avcodec_open2 AV_CODEC_ID_MJPEG");
        
        oframe = ffmpeg.av_frame_alloc();
        if (oframe == null) throw new ScrcpyImageCaptureException(0, "oframe (AV_CODEC_ID_MJPEG) is null");
        int out_buf_size = ffmpeg.avpicture_get_size(pMJPEGCtx->pix_fmt, pMJPEGCtx->width, pMJPEGCtx->height);
        out_buf = (byte*)ffmpeg.av_malloc((ulong)out_buf_size);

        ffmpeg.avpicture_alloc((AVPicture*)oframe, pMJPEGCtx->pix_fmt, pMJPEGCtx->width, pMJPEGCtx->height);

        SwsContext* sws = null;
        try
        {
          sws = ffmpeg.sws_getContext(pMJPEGCtx->width, pMJPEGCtx->height, avctx->pix_fmt,
              pMJPEGCtx->width, pMJPEGCtx->height, pMJPEGCtx->pix_fmt, ffmpeg.SWS_BILINEAR, null, null, null);
          ffmpeg.sws_scale(sws, thumbnail_frame->data, thumbnail_frame->linesize,
                  0, pMJPEGCtx->height, oframe->data, oframe->linesize).CheckError("ffmpeg.sws_scale");
        }
        finally
        {
          ffmpeg.sws_freeContext(sws);
        }

        AVPacket pp2;
        ffmpeg.av_init_packet(&pp2);
        pp2.data = null;
        pp2.size = 0;
        int code = 0;
        ffmpeg.avcodec_encode_video2(pMJPEGCtx, &pp2, oframe, &code);
        if (code != 0)
        {
          buffer_result = new byte[pp2.size];
          Marshal.Copy(new IntPtr(pp2.data), buffer_result, 0, pp2.size);
        }
        else throw new ScrcpyImageCaptureException(code, "ffmpeg.avcodec_encode_video2");
      }
      finally
      {
        ffmpeg.av_frame_free(&thumbnail_frame);
        ffmpeg.avcodec_free_context(&pMJPEGCtx);
        ffmpeg.av_frame_free(&oframe);
        ffmpeg.av_free(out_buf);
      }
      return buffer_result;
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
  }
}
