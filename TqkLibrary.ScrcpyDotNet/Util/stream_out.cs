using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  //https://stackoverflow.com/a/23883598/5034139
  unsafe class stream_out : IDisposable
  {
    int port = 0;
    public string StreamUri { get; private set; }

    AVCodec* out_codec;
    AVStream* out_stream;
    public stream_out(int width, int height, int bitrate = 40000)
    {
      out_codec = avcodec_find_encoder(AVCodecID.AV_CODEC_ID_H264);
      //out_stream = avformat_new_stream(ofmt_ctx, out_codec);
      //out_codec_ctx = avcodec_alloc_context3(out_codec);



      InitOutputStream();
      Console.WriteLine("InitOutputStream");

      if (fmt->video_codec != AVCodecID.AV_CODEC_ID_NONE)
      {
        AddVideoToStream(out_codec, width, height, bitrate);
        Console.WriteLine("AddVideoToStream");
      }

      av_dump_format(oc, 0, StreamUri, 1);
      Console.WriteLine("av_dump_format");

      if ((fmt->flags & AVFMT_NOFILE) == 0)
      {
        Console.WriteLine("AVFMT_NOFILE");
        avio_open(&oc->pb, StreamUri, AVIO_FLAG_WRITE).CheckError("stream_out avio_open");
        Console.WriteLine("avio_open");
      }
      Task.Factory.StartNew(() =>
      {
        avformat_write_header(oc, null).CheckError("stream_out avformat_write_header");
        isWriteHeader = true;
        Console.WriteLine("avformat_write_header");
      }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    bool isWriteHeader = false;


    AVFormatContext* oc;
    AVOutputFormat* fmt;
    void InitOutputStream()
    {
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
      StreamUri = $"rtmp://127.0.0.1:{port}/live.sdp";

      int ret;
      fixed (AVFormatContext** f = &oc)
      {
        ret = avformat_alloc_output_context2(f, null, "rtsp", StreamUri);
        if (oc == null)
        {
          ret = avformat_alloc_output_context2(f, null, "mpeg", StreamUri);
          if (oc == null) throw new ScrcpyException(ret, "stream_out avformat_alloc_output_context2 failed");
        }
      }

      fmt = oc->oformat;
      if (fmt == null) throw new ScrcpyException(-1, "stream_out Error creating outformat");
    }

    AVStream* video_stream;
    void AddVideoToStream(AVCodec* codec_encoder, int width, int height, int bitrate)
    {
      video_stream = avformat_new_stream(oc, codec_encoder);
      if (video_stream == null) throw new ScrcpyException(-1, "AddStream avformat_new_stream failed");

      video_stream->id = (int)oc->nb_streams - 1;
      AVCodecParameters* c = video_stream->codecpar;

      switch (codec_encoder->type)
      {
        case AVMediaType.AVMEDIA_TYPE_VIDEO:
          if (codec_encoder->id == AVCodecID.AV_CODEC_ID_H264)
          {
            c->bit_rate = bitrate;
            c->width = width;
            c->height = height;
            c->codec_id = AVCodecID.AV_CODEC_ID_H264;
            c->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
            //c->time_base.den = 30;
            //c->time_base.num = 1;
            //stream_codecCtx->gop_size = 12;//emit one intra frame every twelve frames at most
            //c->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;

          }
          break;
        default: throw new ScrcpyException(-1, "stream_out wrong codec");
      }

      if ((oc->oformat->flags & AVFMT_GLOBALHEADER) != 0)
        oc->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;//CODEC_FLAG_GLOBAL_HEADER
    }

    public void WriteVideoFrame(AVPacket* pkt)
    {
      if (!isWriteHeader) return;
      int ret = av_interleaved_write_frame(oc, pkt);
    }

    public void Dispose()
    {
      
    }
  }
}
