using FFmpeg.AutoGen;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using static FFmpeg.AutoGen.ffmpeg;
namespace TqkLibrary.ScrcpyDotNet.Util
{
  unsafe class MediaStreamOut : IDisposable
  {
    int port = 0;
    public string StreamUri { get; private set; }
    AVIOContext* server;

    internal MediaStreamOut()
    {      
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
    }

    
    internal void WritePacket(AVPacket* pkt)
    {
      if(pkt != null)
      {
        avio_write(server, pkt->data, pkt->size);
        avio_flush(server);
#if DEBUG
        Console.WriteLine("avio_write:" + pkt->size + ", pts:" + pkt->pts + ", keyframe:" + ((pkt->flags & AV_PKT_FLAG_KEY) != 0));
#endif
      }
    }


    public void Dispose()
    {
      avio_feof(server);
      avio_flush(server);
      avio_close(server);
    }
  }
}