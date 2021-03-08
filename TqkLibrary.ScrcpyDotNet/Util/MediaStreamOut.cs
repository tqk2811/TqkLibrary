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
    readonly int fps = 24;
    int port = 0;
    public string StreamUri { get; private set; }
    readonly MediaStreamIn mediaStreamIn;
    readonly MediaDecoder mediaDecoder;
    readonly MediaEncoder mediaEncoder;

    internal MediaStreamOut(MediaStreamIn mediaStreamIn, int width, int height,int fps = 24, int buffer_size = 1024*1024)
    {
      this.fps = fps;
      this.mediaStreamIn = mediaStreamIn;
      mediaDecoder = new MediaDecoder(AVCodecID.AV_CODEC_ID_MJPEG);
      mediaEncoder = new MediaEncoder(AVCodecID.AV_CODEC_ID_H264, width, height, fps);

      #region FindOpenPort
      TcpListener tcpListener = null;
      while (true)
      {
        try
        {
          port = new Random().Next(10000, 55000);
          tcpListener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
          tcpListener.Start();
          break;
        }
        catch (Exception)
        {

        }
      }
      tcpListener.Stop();
#endregion

      StreamUri = $"udp://127.0.0.1:{port}";///live.sdp

      AVDictionary* options = null;
      //av_dict_set(&options, "pkt_size", "32768", 0).CheckError("av_dict_set pkt_size");
      av_dict_set(&options, "buffer_size", buffer_size.ToString(), 0).CheckError("av_dict_set buffer_size");
      AVIOContext* server;
      avio_open2(&server, StreamUri, AVIO_FLAG_WRITE, null, &options).CheckError("avio_open2");

      this.server = server;

      Task.Factory.StartNew(WriteFrame, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    int pts = 0;
    AVIOContext* server;
    void WriteFrame()
    {
      AVPacket pkt;
      while (mediaStreamIn.IsRunning)
      {
        Thread.Sleep(1000 / fps);
        if(mediaStreamIn.GetImageMjpegPacket(&pkt))
        {
          pkt.pts = pts;
          pts += fps;

          AVFrame* frame_raw = mediaDecoder.decoder_push(&pkt);
          if(frame_raw == null)
          {
            Console.Error.WriteLine("mediaDecoder.decoder_push failed");
            continue;
          }

          AVPacket* h264_packet = mediaEncoder.encoder_push(frame_raw);
          if (h264_packet == null)
          {
            Console.Error.WriteLine(" mediaEncoder.encoder_push failed");
            continue;
          }

          avio_write(server, h264_packet->data, h264_packet->size);
          avio_flush(server);
          Console.WriteLine("avio_write:" + h264_packet->size + ", pts:" + pkt.pts);
        }
        else
        {
          Console.Error.WriteLine("mediaStreamIn.GetImageMjpegPacket failed");
        }
      }
    }

    public void Dispose()
    {

    }
  }
}
#endif