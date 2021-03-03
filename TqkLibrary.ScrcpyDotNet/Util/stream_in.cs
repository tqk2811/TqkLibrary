using FFmpeg.AutoGen;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal delegate void StopCallback(bool byUser);
  internal delegate void FirstFrameTrigger();
  internal unsafe class stream_in : IDisposable
  {
    public event FirstFrameTrigger firstFrameTrigger;
    public event StopCallback stopCallback;
    public bool IsRunning { get; set; } = false;
    readonly object _lock = new object();
    const int BUFSIZE = 0x10000;
    const ulong NO_PTS = ulong.MaxValue;
    const int HEADER_SIZE = 12;

    readonly TcpClient client;
    readonly int Width;
    readonly int Height;

    AVCodec* h264_codec;
    AVCodec* image_codec;

    AVCodecContext* h264_codec_ctx;
    AVCodecParserContext* h264_parser;

    encoder encoder;
    decoder decoder;

    bool has_pending = false;
    AVPacket pending;
    public stream_in(TcpClient client, int width, int height, int imageBufferLength)
    {
      this.client = client;
      this.Width = width;
      this.Height = height;
      buffer_result = new byte[imageBufferLength];
      av_register_all();
      avformat_network_init();

      h264_codec = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
      if (h264_codec == null)
        throw new ScrcpyException(0, "AV_CODEC_ID_H264 not found");

      image_codec = avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MJPEG);
      if (image_codec == null)
        throw new ScrcpyException(0, "AV_CODEC_ID_MJPEG not found");

      h264_codec_ctx = avcodec_alloc_context3(h264_codec);

      h264_parser = av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
      if (h264_parser == null)
        throw new ScrcpyException(0, "parser AV_CODEC_ID_H264 not found");
      h264_parser->flags |= PARSER_FLAG_COMPLETE_FRAMES;

      encoder = new encoder(image_codec, width, height);
      decoder = new decoder(h264_codec);
    }

    public void Dispose()
    {
      av_parser_close(h264_parser);
      fixed (AVCodecContext** f = &h264_codec_ctx) avcodec_free_context(f);
      decoder?.Dispose();
      encoder?.Dispose();
    }

    public void RunStream()
    {
      while (IsRunning)
      {
        AVPacket packet;
        bool ok = stream_recv_packet(&packet);//push byte[] to packet
        if (!ok)
          break;//eof

        ok = stream_push_packet(&packet);
        av_packet_unref(&packet);
        if (!ok)
          break;// cannot process packet
      }
      Console.WriteLine("Scrcpy Exit");
      stopCallback.Invoke(IsRunning);
      IsRunning = false;
      length_Result = 0;
    }

    //push byte[] to packet
    bool stream_recv_packet(AVPacket* packet)
    {
      byte[] header = new byte[HEADER_SIZE];
      NetworkStream networkStream = client.GetStream();
      int r = networkStream.Read(header, 0, header.Length);
      if (r < HEADER_SIZE)
        return false;

      ulong pts = BitConverter.ToUInt64(header.Take(8).Reverse().ToArray(), 0);//buffer_read64be
      uint len = BitConverter.ToUInt32(header.Skip(8).Take(4).Reverse().ToArray(), 0);//buffer_read32be

      if ((pts == NO_PTS || (pts & 0x8000000000000000) == 0) && len > 0)
      {
        if (av_new_packet(packet, (int)len) != 0)
        {
#if DEBUG
          Console.WriteLine("Could not allocate packet");
#endif
          return false;
        }

        byte[] buffer_data = new byte[len];
        r = networkStream.Read(buffer_data, 0, buffer_data.Length);
        if (r < 0 || (uint)r < len)
        {
          av_packet_unref(packet);
          return false;
        }
        Marshal.Copy(buffer_data, 0, new IntPtr(packet->data), r);

        packet->pts = pts != NO_PTS ? (long)pts : AV_NOPTS_VALUE;
        return true;
      }
      else return false;
    }

    //write data from packet to pending
    bool stream_push_packet(AVPacket* packet)
    {
      fixed (AVPacket* fix_pending = &pending)
      {
        bool is_config = packet->pts == AV_NOPTS_VALUE;
        if (has_pending || is_config)
        {
          int offset;

          if (has_pending)
          {
            offset = pending.size;
            if (av_grow_packet(fix_pending, packet->size) != 0)//increase size pending packet
            {
#if DEBUG
              Console.WriteLine("Could not grow packet");
#endif
              return false;
            }
          }
          else
          {
            offset = 0;
            if (av_new_packet(fix_pending, packet->size) != 0)//create new pending packet
            {
#if DEBUG
              Console.WriteLine("Could not create packet");
#endif
              return false;
            }
            has_pending = true;
          }

          Buffer.MemoryCopy(packet->data, fix_pending->data + offset, packet->size, packet->size);//Copy from packet to pending

          if (!is_config)//set pending
          {
            fix_pending->pts = packet->pts;
            fix_pending->dts = packet->dts;
            fix_pending->flags = packet->flags;
            packet = fix_pending;
          }
        }

        if (is_config)
        {
          bool ok = process_config_packet(packet);
          if (!ok) return false;
        }
        else
        {
          bool ok = stream_parse(packet);
          if (has_pending)
          {
            has_pending = false;
            av_packet_unref(fix_pending);
          }
          if (!ok) return false;
        }
        return true;
      }
    }

    bool process_config_packet(AVPacket* packet)
    {
      //recoder
      return true;
    }

    bool stream_parse(AVPacket* packet)
    {
      byte* in_data = packet->data;
      int in_len = packet->size;
      byte* out_data = null;
      int out_len = 0;
      int r = av_parser_parse2(h264_parser, h264_codec_ctx,
                               &out_data, &out_len, in_data, in_len,
                               AV_NOPTS_VALUE, AV_NOPTS_VALUE, -1);

      // PARSER_FLAG_COMPLETE_FRAMES is set
      if (r != in_len)
        throw new Exception();
      //(void)r;
      if (out_len != in_len)
        throw new Exception();

      if (h264_parser->key_frame == 1)
        packet->flags |= AV_PKT_FLAG_KEY;

      process_frame(packet);
      return true;
    }

    void process_frame(AVPacket* packet)
    {
      AVFrame* decode_frame = decoder.decoder_push(packet);
      if (decode_frame == null)
      {
        Console.Error.WriteLine("Decoder h264 failed");
        return;
      }

      AVPacket* image = encoder.encoder_push(decode_frame);
      if (image == null)
      {
        Console.Error.WriteLine("encoder mjpeg failed");
        return;
      }

#if TestVideo
      lock (lock_stream)
      {
        if (streamOut != null)
        {
          streamOut.WriteVideoFrame(packet);
        }
      }
#endif

      lock (lock_)
      {
        if (image->size > buffer_result.Length) return;
        length_Result = image->size;
        Marshal.Copy(new IntPtr(image->data), buffer_result, 0, image->size);
      }

      firstFrameTrigger?.Invoke();
    }






    readonly object lock_ = new object();
    int length_Result = 0;
    readonly byte[] buffer_result;
    public Bitmap GetScreenShot()
    {
      lock (lock_)
      {
        if (length_Result > 0)
        {
          MemoryStream memoryStream = new MemoryStream();
          memoryStream.Write(buffer_result, 0, length_Result);
          return (Bitmap)Bitmap.FromStream(memoryStream);
        }
        return null;
      }
    }

    public byte[] GetScreenShotByteArray()
    {
      lock (lock_)
      {
        if (length_Result > 0)
        {
          byte[] tempbuff = new byte[length_Result];
          Array.Copy(buffer_result, tempbuff, length_Result);
          return tempbuff;
        }
        return null;
      }
    }

#if TestVideo
    readonly object lock_stream = new object();
    stream_out streamOut;
    public string InitVideoH264Stream()
    {
      lock(lock_stream)
      {
        if (streamOut == null) streamOut = new stream_out(Width, Height);
        return streamOut.StreamUri;
      }
    }

    public void StopStream()
    {
      lock (lock_stream)
      {
        streamOut.Dispose();
        streamOut = null;
      }
    }





    //public void InitStream(NetworkStream stream)
    //{

    //}

    //public void UnInitStream()
    //{

    //}
#endif
  }
}
