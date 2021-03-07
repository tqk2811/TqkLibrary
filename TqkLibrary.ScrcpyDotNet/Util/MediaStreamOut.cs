#if TestVideo
using FFmpeg.AutoGen;
using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static FFmpeg.AutoGen.ffmpeg;
using System.Reflection;
namespace TqkLibrary.ScrcpyDotNet.Util
{
  //https://stackoverflow.com/a/23883598/5034139
  unsafe class MediaStreamOut : IDisposable
  {
    const int fps = 30;
    int port = 0;
    public string StreamUri { get; private set; }
    readonly MediaStreamIn mediaStreamIn;

    AVCodecContext* h264_codec_ctx;
    AVCodecParserContext* h264_parser;

    AVFormatContext* ofmt_ctx;
    AVStream* vid_stream;
    MediaDecoder mediaDecoder;
    MediaEncoder mediaEncoder;
    internal MediaStreamOut(MediaStreamIn mediaStreamIn, int width, int height, int bitrate = 40000,int buffer_length = 1024*1024)
    {
      this.mediaStreamIn = mediaStreamIn;
      buffer = new byte[buffer_length];
      mediaDecoder = new MediaDecoder(AVCodecID.AV_CODEC_ID_H264);

      AVCodec* h264_codec = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
      h264_codec_ctx = avcodec_alloc_context3(h264_codec);
      h264_parser = av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
      if (h264_parser == null)
        throw new ScrcpyException(0, "parser AV_CODEC_ID_H264 not found");
      h264_parser->flags |= PARSER_FLAG_COMPLETE_FRAMES;

#region FindOpenPort
      TcpListener server = null;
      while (true)
      {
        try
        {
          port = new Random().Next(10000, 55000);
          server = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
          server.Start();
          break;
        }
        catch (Exception)
        {

        }
      }
      server.Stop();
#endregion

      StreamUri = $"udp://127.0.0.1:{port}";///live.sdp

      AVFormatContext* ofmt_ctx;
      avformat_alloc_output_context2(&ofmt_ctx, null, "h264", StreamUri).CheckError("MediaStreamOut avformat_alloc_output_context2");
      //ofmt_ctx->oformat->video_codec = AVCodecID.AV_CODEC_ID_H264;

      mediaEncoder = new MediaEncoder(ofmt_ctx->oformat->video_codec, width, height);

      AVCodec* vid_codec = avcodec_find_encoder(ofmt_ctx->oformat->video_codec);
      if (vid_codec == null) throw new ScrcpyException(0, "MediaStreamOut avcodec_find_encoder");

      vid_stream = avformat_new_stream(ofmt_ctx, vid_codec);
      if (vid_stream == null) throw new ScrcpyException(0, "MediaStreamOut avformat_new_stream");
      vid_stream->codec->width = width;
      vid_stream->codec->height = height;
      vid_stream->codec->time_base.den = fps;
      vid_stream->codec->time_base.num = 1;
      //vid_stream->index = 0;
      //vid_stream->duration = long.MaxValue;
      //vid_stream->codec->codec_id = AVCodecID.AV_CODEC_ID_H264;
      vid_stream->codec->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
      //vid_stream->codec->flags = AV_CODEC_FLAG_GLOBAL_HEADER;
      //vid_stream->codec->time_base.den = fps;
      //vid_stream->codec->time_base.num = 1;
      //vid_stream->codec->gop_size = fps;
      //vid_stream->codec->bit_rate = bitrate;

      avio_open(&ofmt_ctx->pb, StreamUri, AVIO_FLAG_WRITE).CheckError("MediaStreamOut avio_open");

      this.ofmt_ctx = ofmt_ctx;

      clone_packet = av_packet_alloc();

      autoResetEvent.Reset();

      Task.Factory.StartNew(WriteFrame, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }

    void WriteFrame()
    {
      avformat_write_header(ofmt_ctx, null).CheckError("stream_out avformat_write_header");
      StreamReady = true;
      if (!autoResetEvent.WaitOne(10000))
      {
        Console.Error.WriteLine("MediaStream.WriteFrame wait for packet failed");
        return;
      }
      while (mediaStreamIn.IsRunning)
      {
        Thread.Sleep(1000 / fps);
        lock (lock_packet)
        {
          WritePacketToStream();
        }
      }
    }

    long pts = 0;

    readonly byte[] buffer;
    int buffer_data_length = 0;
    int buffer_flag = 0;

    readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);
    readonly object lock_packet = new object();
    bool StreamReady = false;

    AVPacket* clone_packet;
    internal void SendRawH264Packet(AVPacket* h264_rawPacket)
    {
      if (!StreamReady) return;
      lock(lock_packet)
      {
        Marshal.Copy(new IntPtr(h264_rawPacket->data), buffer, 0, h264_rawPacket->size);
        buffer_data_length = h264_rawPacket->size;
        buffer_flag = h264_rawPacket->flags;
        pts = h264_rawPacket->pts;
      }
      autoResetEvent.Set();
    }

    void WritePacketToStream()
    {
      int ret = 0; 
      av_packet_unref(clone_packet);
      lock (lock_packet)//copy frame from clone_packet->clone_packet2
      {        
        ret = av_new_packet(clone_packet, buffer_data_length);
        if (ret < 0)
        {
          Console.Error.WriteLine("WritePacketToStream av_new_packet " + ret);
          return;
        }
        Marshal.Copy(buffer, 0, new IntPtr(clone_packet->data), buffer_data_length);
        clone_packet->flags = buffer_flag;
      }
      Console.WriteLine("pts:" + pts);
      clone_packet->pts = pts;
      clone_packet->dts = clone_packet->pts;

      byte* in_data = clone_packet->data;
      int in_len = clone_packet->size;
      byte* out_data = null;
      int out_len = 0;
      int r = av_parser_parse2(h264_parser, h264_codec_ctx,
                               &out_data, &out_len, in_data, in_len,
                               AV_NOPTS_VALUE, AV_NOPTS_VALUE, -1);

      // PARSER_FLAG_COMPLETE_FRAMES is set
      if (r != in_len)
      {
        Console.Error.WriteLine("av_parser_parse2 r != in_len");
        return;
      }
      //(void)r;
      if (out_len != in_len)
      {
        Console.Error.WriteLine("av_parser_parse2 out_len != in_len");
        return;
      }

      if (h264_parser->key_frame != 0)
      {
        clone_packet->flags |= AV_PKT_FLAG_KEY;
        Console.WriteLine("key_frame");
      }





      
      AVFrame* frame = mediaDecoder.decoder_push(clone_packet);
      if (frame == null)
      {
        Console.Error.WriteLine("WritePacketToStream decoder_push error");
        return;
      }
      AVPacket* pack = mediaEncoder.encoder_push(frame);
      if (pack == null)
      {
        Console.Error.WriteLine("WritePacketToStream encoder_push error");
        return;
      }
      ret = av_interleaved_write_frame(ofmt_ctx, pack);
      if (ret < 0)
      {
        Console.Error.WriteLine("WritePacketToStream av_interleaved_write_frame error");
      }
    }




    public void Dispose()
    {

    }
  }
}
#endif