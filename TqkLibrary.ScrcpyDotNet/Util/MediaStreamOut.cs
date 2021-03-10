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

    internal MediaStreamOut(MediaStreamIn mediaStreamIn, int width, int height,int fps = 24, int buffer_size = 1024*1024)
    {
      this.fps = fps;
      this.mediaStreamIn = mediaStreamIn;
      
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
      //av_dict_set(&options, "buffer_size", buffer_size.ToString(), 0).CheckError("av_dict_set buffer_size");
      AVIOContext* server;
      avio_open2(&server, StreamUri, AVIO_FLAG_WRITE, null, &options).CheckError("avio_open2");
      
      this.server = server;
      Task.Factory.StartNew(WriteFrame, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
    }
    AVIOContext* server;
    void WriteFrame()
    {
      while (mediaStreamIn.IsRunning)
      {
        Thread.Sleep(1000 / fps);
        AVPacket* pkt = mediaStreamIn.GetVideoStreamPacket();
        if (pkt != null)
        {
          avio_write(server, pkt->data, pkt->size);
          avio_flush(server);
          Console.WriteLine("avio_write:" + pkt->size + ", pts:" + pkt->pts);
        }
        else
        {
          Console.Error.WriteLine("mediaStreamIn.GetH264Packet failed");
        }
      }
    }

    //internal void WritePacket(AVPacket* h264_pkt)
    //{
    //  if(ready)
    //  {
    //    if (h264_pkt != null)
    //    {
    //      avio_write(server, h264_pkt->data, h264_pkt->size);
    //      avio_flush(server);
    //      Console.WriteLine("avio_write:" + h264_pkt->size + ", pts:" + h264_pkt->pts);
    //    }
    //  }
    //}

    public void Dispose()
    {

    }
  }
}
#endif