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

namespace TqkLibrary.ScrcpyDotNet
{
  //https://github.com/Genymobile/scrcpy/issues/673
  public class Scrcpy
  {
    static readonly Random random = new Random();
    static string adbPath = "adb.exe";


    public string DeviceName { get; set; }
    public int Width { get; set; } = -1;
    public int Height { get; set; } = -1;
    public ScrcpyControl Control { get; }

    readonly object _lock = new object();
    FfmpegBufferReaderSupport streamSupport = null;
    int reversePort = 34676;
    public readonly string deviceId;
    bool IsRunning = false; 
    byte[] buffer_image;
    AutoResetEvent AutoResetEvent = new AutoResetEvent(false);
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

    public Bitmap GetLastedFrame()
    {
      lock (_lock)
      {
        if (buffer_image == null) return null;
        MemoryStream memoryStream = new MemoryStream(buffer_image);
        return (Bitmap)Bitmap.FromStream(memoryStream);
      }
    }

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
        byte[] buffer = new byte[1024 * 1024];
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
        BufferedStream bf = new BufferedStream(stream);
        bf.Read(buffer, 0, 64);
        DeviceName = Encoding.ASCII.GetString(buffer, 0, 64);

        bf.Read(sizebuff, 0, sizebuff.Length);
        Width = BitConverter.ToInt16(BitConverter.IsLittleEndian ? sizebuff.Reverse().ToArray() : sizebuff, 0);

        bf.Read(sizebuff, 0, sizebuff.Length);
        Height = BitConverter.ToInt16(BitConverter.IsLittleEndian ? sizebuff.Reverse().ToArray() : sizebuff, 0);

        //bf.Read(buffer, 0, 12);
        streamSupport = new FfmpegBufferReaderSupport(bf, buffer.Length);
        Control._controlStream = bf;


        Control.SendControl(ScrcpyControlMessage.BackOrScreenOn());
        Thread.Sleep(1000);
        Control.SendControl(ScrcpyControlMessage.CreateSetClipboard("day la test",true));


        Thread.Sleep(1000);
        Control.SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_DOWN, AndroidKeyCode.KEYCODE_ZOOM_OUT));
        Thread.Sleep(1000); 
        Control.SendControl(ScrcpyControlMessage.CreateInjectKeycode(AndroidKeyEventAction.ACTION_UP, AndroidKeyCode.KEYCODE_ZOOM_IN));

        CaptureFrame();//Task.Factory.StartNew(, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default).Wait();
      }
      finally
      {
        client?.Dispose();
        server.Stop();
      }
    }

    void DeployServer()
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
      bool control = true;
      int display_id_string = 0;
      bool show_touches = true;
      bool stay_awake = true;
      string codec_options = "-";
      string encoder_name = "-";

      AdbCommand($"shell CLASSPATH=/data/local/tmp/scrcpy-server.jar app_process / com.genymobile.scrcpy.Server {version} {loglevel} {max_size_string} {bit_rate_string} {max_fps_string} {lock_video_orientation_string} {tunnel_forward} {crop} {frame_meta} {control} {display_id_string} {show_touches} {stay_awake} {codec_options} {encoder_name}");
    }

    int i = 0;
    unsafe void CaptureFrame()
    {
      ffmpeg.av_register_all();
      ffmpeg.avformat_network_init();
      avio_alloc_context_read_packet avio_Alloc_Context_Read_Packet = streamSupport.read_packet;
      AVFormatContext* aVFormatContext = null;
      byte* aviobuffer = null;
      AVIOContext* avio = null;
      AVCodecContext* h264_decoder_codec_ctx = null;
      AVFrame* frame_h264_decoder = null;
      AVCodecContext* MJPEG_encoder_codec_ctx = null;
      AVFrame* frame_MJPEG_encoder = null;
      byte* MJPEG_encoder_buffer = null;
      SwsContext* sws = null;
      AVPacket out_packet;
      try
      {
        AVInputFormat* aVInputFormat = ffmpeg.av_find_input_format("h264");
        aVFormatContext = ffmpeg.avformat_alloc_context();//Need release
        aviobuffer = (byte*)ffmpeg.av_malloc(4096);//Need release  
        avio = ffmpeg.avio_alloc_context(aviobuffer, 4096, 0, null, avio_Alloc_Context_Read_Packet, null, null);//Need release
        aVFormatContext->pb = avio;

        ffmpeg.avformat_open_input(&aVFormatContext, null, aVInputFormat, null).CheckError("ffmpeg.avformat_open_input h264");//Need release
        ffmpeg.avformat_find_stream_info(aVFormatContext, null).CheckError("ffmpeg.avformat_find_stream_info h264");

        AVCodec* codec = null;
        int stream_index = ffmpeg.av_find_best_stream(aVFormatContext, AVMediaType.AVMEDIA_TYPE_VIDEO, -1, -1, &codec, 0);
        if (stream_index == -1) throw new ScrcpyException(-1, "ffmpeg.av_find_best_stream Can't find stream_index");
        if(codec == null) throw new ScrcpyException(0, "ffmpeg.av_find_best_stream found stream_index but AVCodec is null");

        AVCodecContext* streamInputCodec = aVFormatContext->streams[stream_index]->codec;
        streamInputCodec->flags = streamInputCodec->flags | ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        if (streamInputCodec == null) throw new ScrcpyException(0, "Codec in stream_index is null");

        ffmpeg.avcodec_open2(streamInputCodec, codec, null).CheckError("ffmpeg.avcodec_open2 h264");

        AVStream* aVStream = aVFormatContext->streams[stream_index];
        AVPacket avPacket;

        //decode h264
        AVCodec* h264_codec_decoder = ffmpeg.avcodec_find_decoder(streamInputCodec->codec_id);
        h264_decoder_codec_ctx = ffmpeg.avcodec_alloc_context3(h264_codec_decoder);//Need release
        h264_decoder_codec_ctx->width = streamInputCodec->width;
        h264_decoder_codec_ctx->height = streamInputCodec->height;
        h264_decoder_codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_RGB24;
        h264_decoder_codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
        h264_decoder_codec_ctx->skip_frame = AVDiscard.AVDISCARD_NONINTRA;//AVDISCARD_NONREF;//AVDISCARD_NONINTRA;
        h264_decoder_codec_ctx->extradata = streamInputCodec->extradata;
        h264_decoder_codec_ctx->extradata_size = streamInputCodec->extradata_size;
        h264_decoder_codec_ctx->time_base = streamInputCodec->time_base;
        h264_decoder_codec_ctx->flags = h264_decoder_codec_ctx->flags | ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        //h264_decoder_codec_ctx->time_base.num = 1;
        //h264_decoder_codec_ctx->time_base.den = 30;
        ffmpeg.avcodec_open2(h264_decoder_codec_ctx, h264_codec_decoder, null).CheckError("ffmpeg.avcodec_open2 h264 decode");//Need release
        if (h264_codec_decoder == null) throw new ScrcpyException(0, "h264_codec is null");
        frame_h264_decoder = ffmpeg.av_frame_alloc();//Need release---
        
        //encode to mjpeg
        AVCodec* MJPEG_codec_encoder = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MJPEG);
        MJPEG_encoder_codec_ctx = ffmpeg.avcodec_alloc_context3(MJPEG_codec_encoder);//Need release
        if (MJPEG_encoder_codec_ctx == null) throw new ScrcpyException(0, "AVCodecContext (AV_CODEC_ID_MJPEG) is null");
        MJPEG_encoder_codec_ctx->bit_rate = streamInputCodec->bit_rate;
        MJPEG_encoder_codec_ctx->width = streamInputCodec->width;
        MJPEG_encoder_codec_ctx->height = streamInputCodec->height;
        MJPEG_encoder_codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
        MJPEG_encoder_codec_ctx->codec_id = AVCodecID.AV_CODEC_ID_MJPEG;
        MJPEG_encoder_codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
        MJPEG_encoder_codec_ctx->time_base.num = streamInputCodec->time_base.num;
        MJPEG_encoder_codec_ctx->time_base.den = streamInputCodec->time_base.den;
        MJPEG_encoder_codec_ctx->flags = MJPEG_encoder_codec_ctx->flags | ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
        //pMJPEGCtx->time_base.num = 1;
        //pMJPEGCtx->time_base.den = 30;
        if (MJPEG_codec_encoder == null) throw new ScrcpyException(0, "AVCodec (AV_CODEC_ID_MJPEG) is null");

        ffmpeg.avcodec_open2(MJPEG_encoder_codec_ctx, MJPEG_codec_encoder, null).CheckError("ffmpeg.avcodec_open2 AV_CODEC_ID_MJPEG");//Need release
        frame_MJPEG_encoder = ffmpeg.av_frame_alloc();//Need release
        if (frame_MJPEG_encoder == null) throw new ScrcpyException(0, "oframe (AV_CODEC_ID_MJPEG) is null");

        int out_buf_size = ffmpeg.avpicture_get_size(MJPEG_encoder_codec_ctx->pix_fmt, MJPEG_encoder_codec_ctx->width, MJPEG_encoder_codec_ctx->height);
        MJPEG_encoder_buffer = (byte*)ffmpeg.av_malloc((ulong)out_buf_size);//Need release
        ffmpeg.avpicture_alloc((AVPicture*)frame_MJPEG_encoder, MJPEG_encoder_codec_ctx->pix_fmt, MJPEG_encoder_codec_ctx->width, MJPEG_encoder_codec_ctx->height);
        frame_MJPEG_encoder->format = (int)streamInputCodec->pix_fmt;
        frame_MJPEG_encoder->width = streamInputCodec->width;
        frame_MJPEG_encoder->height = streamInputCodec->height;
        sws = ffmpeg.sws_getContext(MJPEG_encoder_codec_ctx->width, MJPEG_encoder_codec_ctx->height, streamInputCodec->pix_fmt,
                  MJPEG_encoder_codec_ctx->width, MJPEG_encoder_codec_ctx->height, MJPEG_encoder_codec_ctx->pix_fmt, ffmpeg.SWS_BILINEAR, null, null, null);//Need release

        AutoResetEvent.Set();

        while (IsRunning)
        {
          int error_code = ffmpeg.av_read_frame(aVFormatContext, &avPacket);
          if (error_code == 0 && avPacket.stream_index == stream_index)
          {
            error_code = ffmpeg.avcodec_send_packet(h264_decoder_codec_ctx, &avPacket);
            if (error_code < 0)
            {
              Console.WriteLine("ffmpeg.avcodec_send_packet(h264_decoder_codec_ctx, &avPacket); " + error_code);
              continue;
            }

            error_code = ffmpeg.avcodec_receive_frame(h264_decoder_codec_ctx, frame_h264_decoder);
            if (error_code < 0)
            {
              Console.WriteLine("ffmpeg.avcodec_receive_frame(h264_decoder_codec_ctx, frame_h264_decoder); " + error_code);
              continue;
            }

            error_code = ffmpeg.sws_scale(sws, frame_h264_decoder->data, frame_h264_decoder->linesize,
                      0, MJPEG_encoder_codec_ctx->height, frame_MJPEG_encoder->data, frame_MJPEG_encoder->linesize);
            if (error_code < 0)
            {
              Console.WriteLine("sws_scale " + error_code);
              continue;
            }
            
            ffmpeg.av_init_packet(&out_packet);//Need release
            out_packet.data = null;
            out_packet.size = 0;
            int code = 0;
            try
            {
              error_code = ffmpeg.avcodec_encode_video2(MJPEG_encoder_codec_ctx, &out_packet, frame_MJPEG_encoder, &code);
              if (error_code < 0)
              {
                Console.WriteLine("avcodec_encode_video2 " + error_code);
                continue;
              }

              byte[] buffer_result = new byte[out_packet.size];
              Marshal.Copy(new IntPtr(out_packet.data), buffer_result, 0, out_packet.size);
              lock (_lock) this.buffer_image = buffer_result;

              using FileStream fileStream = new FileStream($"D:\\temp\\test\\{i++.ToString("0000")}.jpeg",FileMode.Create,FileAccess.Write,FileShare.ReadWrite);
              fileStream.Write(buffer_result, 0, buffer_result.Length);
            }
            finally
            {
              ffmpeg.av_free_packet(&out_packet);
            }
          }
        }        
      }
      finally
      {
        ffmpeg.sws_freeContext(sws);
        ffmpeg.av_free(MJPEG_encoder_buffer);
        ffmpeg.av_frame_free(&frame_MJPEG_encoder);
        ffmpeg.avcodec_close(MJPEG_encoder_codec_ctx);
        ffmpeg.avcodec_free_context(&MJPEG_encoder_codec_ctx);
        ffmpeg.avcodec_close(h264_decoder_codec_ctx);
        ffmpeg.avcodec_free_context(&h264_decoder_codec_ctx);

        ffmpeg.avformat_close_input(&aVFormatContext);
        ffmpeg.avio_context_free(&avio);
        ffmpeg.av_free(aviobuffer);
        ffmpeg.avformat_free_context(aVFormatContext);
      }
    }

    unsafe byte[] GetBufferImageJPEG(AVCodecContext* avctx,AVPacket* avpkt)
    {
      byte[] buffer_result = null;
      AVFrame* thumbnail_frame = null;
      AVCodecContext* pMJPEGCtx = null;
      AVFrame* oframe = null;
      AVCodecContext* h264_decoder_codec_ctx = null;
      byte* out_buf = null;
      AVPacket pp2;
      try
      {
        AVCodec* h264_codec = ffmpeg.avcodec_find_decoder(avctx->codec_id);
        h264_decoder_codec_ctx = ffmpeg.avcodec_alloc_context3(h264_codec);//Need release
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

        ffmpeg.avcodec_open2(h264_decoder_codec_ctx, h264_codec, null).CheckError("ffmpeg.avcodec_open2 h264 decode");//Need release
        if (h264_codec == null) throw new ScrcpyException(0, "h264_codec is null");

        ffmpeg.avcodec_send_packet(h264_decoder_codec_ctx, avpkt).CheckError("ffmpeg.avcodec_send_packet h264 decode");

        thumbnail_frame = ffmpeg.av_frame_alloc();//Need release---
        if (ffmpeg.avcodec_receive_frame(h264_decoder_codec_ctx, thumbnail_frame) < 0) return buffer_result;//CheckError("ffmpeg.avcodec_receive_frame h264 decode");

        // JPEG encode
        AVCodec * pMJPEGCodec = ffmpeg.avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MJPEG);
        pMJPEGCtx = ffmpeg.avcodec_alloc_context3(pMJPEGCodec);//Need release
        if (pMJPEGCtx == null) throw new ScrcpyException(0, "AVCodecContext (AV_CODEC_ID_MJPEG) is null");
        pMJPEGCtx->bit_rate = avctx->bit_rate;
        pMJPEGCtx->width = avctx->width;
        pMJPEGCtx->height = avctx->height;
        pMJPEGCtx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
        pMJPEGCtx->codec_id = AVCodecID.AV_CODEC_ID_MJPEG;
        pMJPEGCtx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
        pMJPEGCtx->time_base.num = avctx->time_base.num;
        pMJPEGCtx->time_base.den = avctx->time_base.den;
        //pMJPEGCtx->time_base.num = 1;
        //pMJPEGCtx->time_base.den = 30;

        if (pMJPEGCodec == null ) throw new ScrcpyException(0, "AVCodec (AV_CODEC_ID_MJPEG) is null");

        ffmpeg.avcodec_open2(pMJPEGCtx, pMJPEGCodec, null).CheckError("ffmpeg.avcodec_open2 AV_CODEC_ID_MJPEG");//Need release

        oframe = ffmpeg.av_frame_alloc();//Need release
        if (oframe == null) throw new ScrcpyException(0, "oframe (AV_CODEC_ID_MJPEG) is null");
        int out_buf_size = ffmpeg.avpicture_get_size(pMJPEGCtx->pix_fmt, pMJPEGCtx->width, pMJPEGCtx->height);
        out_buf = (byte*)ffmpeg.av_malloc((ulong)out_buf_size);//Need release        
        ffmpeg.avpicture_alloc((AVPicture*)oframe, pMJPEGCtx->pix_fmt, pMJPEGCtx->width, pMJPEGCtx->height);

        oframe->format = (int)avctx->pix_fmt;
        oframe->width = avctx->width;
        oframe->height = avctx->height;
        SwsContext* sws = null;
        try
        {
          sws = ffmpeg.sws_getContext(pMJPEGCtx->width, pMJPEGCtx->height, avctx->pix_fmt,
              pMJPEGCtx->width, pMJPEGCtx->height, pMJPEGCtx->pix_fmt, ffmpeg.SWS_BILINEAR, null, null, null);//Need release
          ffmpeg.sws_scale(sws, thumbnail_frame->data, thumbnail_frame->linesize,
                  0, pMJPEGCtx->height, oframe->data, oframe->linesize).CheckError("ffmpeg.sws_scale");
        }
        finally
        {
          ffmpeg.sws_freeContext(sws);
        }

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
        else throw new ScrcpyException(code, "ffmpeg.avcodec_encode_video2");
      }
      finally
      {
        ffmpeg.av_packet_unref(&pp2);
        ffmpeg.av_free(out_buf);
        ffmpeg.av_frame_free(&oframe);
        ffmpeg.avcodec_close(pMJPEGCtx);
        ffmpeg.avcodec_free_context(&pMJPEGCtx);
        ffmpeg.av_frame_free(&thumbnail_frame);        
        ffmpeg.avcodec_close(h264_decoder_codec_ctx);
        ffmpeg.avcodec_free_context(&h264_decoder_codec_ctx);
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
