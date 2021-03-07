﻿using FFmpeg.AutoGen;
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
  internal unsafe class MediaStreamIn : IDisposable
  {
    public event FirstFrameTrigger firstFrameTrigger;
    public event StopCallback stopCallback;
    public bool IsRunning { get; set; } = false;
    public int ReadTryAgainTimes { get; set; } = 3;
    readonly object _lock = new object();
    const int BUFSIZE = 0x10000;
    const ulong NO_PTS = ulong.MaxValue;
    const int HEADER_SIZE = 12;

    readonly TcpClient client;
    readonly NetworkStream networkStream;
    readonly int Width;
    readonly int Height;

    AVCodec* h264_codec;

    AVCodecContext* h264_codec_ctx;
    AVCodecParserContext* h264_parser;

    MediaEncoder encoder;
    MediaDecoder decoder;

    bool has_pending = false;
    AVPacket pending;
    internal MediaStreamIn(TcpClient client, int width, int height, int bufferLength)
    {
      this.client = client;
      this.Width = width;
      this.Height = height;
      this.networkStream = client.GetStream();
      content_buff = new byte[bufferLength];
      buffer_result = new byte[bufferLength];

      avcodec_register_all();
      av_register_all();
      avformat_network_init();

      h264_codec = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
      if (h264_codec == null)
        throw new ScrcpyException(0, "AV_CODEC_ID_H264 not found");

      h264_codec_ctx = avcodec_alloc_context3(h264_codec);
      h264_parser = av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
      if (h264_parser == null)
        throw new ScrcpyException(0, "parser AV_CODEC_ID_H264 not found");
      h264_parser->flags |= PARSER_FLAG_COMPLETE_FRAMES;

      encoder = new MediaEncoder(AVCodecID.AV_CODEC_ID_MJPEG, width, height);
      decoder = new MediaDecoder(h264_codec);
    }

    public void Dispose()
    {
      av_parser_close(h264_parser);
      fixed (AVCodecContext** f = &h264_codec_ctx) avcodec_free_context(f);
      decoder?.Dispose();
      encoder?.Dispose();
    }

    internal void RunStream()
    {
      try
      {
        while (IsRunning)
        {
          AVPacket packet;
          try
          {
            bool ok = stream_recv_packet(&packet);//push byte[] to packet
            if (!ok)
            {
              Console.WriteLine("Scrcpy exit by eof");
              //continue;
              break;
            }

            ok = stream_push_packet(&packet);
            if (!ok)
            {
              Console.WriteLine("Scrcpy exit (cannot process packet)");
              break;
            }
          }
          finally
          {
            av_packet_unref(&packet);
          }
        }
      }
      finally
      {
        if (!IsRunning) Console.WriteLine("Scrcpy exit by user");
        IsRunning = false;
        length_Result = 0;
        stopCallback.Invoke(IsRunning);
      }
      
    }


    int ReadStream(byte[] buffer,int read_size)
    {
      int r = networkStream.Read(buffer, 0, read_size);
      int tryagain = 0;
      while(r < read_size && tryagain < this.ReadTryAgainTimes)
      {
#if DEBUG
        Console.WriteLine($"ReadStream try again {tryagain}: {r}/{read_size}");
#endif
        tryagain++;
        r += networkStream.Read(buffer, r, read_size - r);
      }
      return r;
    }


    //push byte[] to packet
    readonly byte[] header_buff = new byte[HEADER_SIZE];
    readonly byte[] content_buff;
    bool stream_recv_packet(AVPacket* packet)
    {
      int r = ReadStream(header_buff, header_buff.Length);
      if (r < HEADER_SIZE)
      {
        Console.Error.WriteLine($"HEADER_SIZE {r} < {HEADER_SIZE}");
        return false;
      }

      ulong pts = BitConverter.ToUInt64(header_buff.Take(8).Reverse().ToArray(), 0);//buffer_read64be
      uint len = BitConverter.ToUInt32(header_buff.Skip(8).Take(4).Reverse().ToArray(), 0);//buffer_read32be

      if ((pts == NO_PTS || (pts & 0x8000000000000000) == 0) && len > 0)
      {
        if (av_new_packet(packet, (int)len) != 0)
        {
          Console.Error.WriteLine("stream_in.stream_recv_packet Could not allocate packet");
          return false;
        }

        r = ReadStream(content_buff, (int)len);
        if (r < 0 || (uint)r < len)
        {
          Console.Error.WriteLine($"CONTENT_SIZE {r} < {len}");
          return false;
        }

        Marshal.Copy(content_buff, 0, new IntPtr(packet->data), r);

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
      {
        Console.Error.WriteLine("av_parser_parse2 r != in_len");
        return false;
      }
      //(void)r;
      if (out_len != in_len)
      {
        Console.Error.WriteLine("av_parser_parse2 out_len != in_len");
        return false;
      }

      if (h264_parser->key_frame != 0)
      {
        packet->flags |= AV_PKT_FLAG_KEY;
      }

      process_frame(packet);
      return true;
    }

    void process_frame(AVPacket* packet)//h264 packet
    {
      AVFrame* decode_frame = decoder.decoder_push(packet);//decode to raw frame
      if (decode_frame == null)
      {
        Console.Error.WriteLine("Decoder h264 failed");
        return;
      }

      AVPacket* image = encoder.encoder_push(decode_frame);//encode raw frame to MJPEG

      if (image == null)
      {
        Console.Error.WriteLine("Encoder mjpeg failed");
        return;
      }

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
    internal Bitmap GetScreenShot()
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

    internal byte[] GetScreenShotByteArray()
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
    MediaStreamOut streamOut;
    internal string InitVideoH264Stream()
    {
      lock(lock_stream)
      {
        if (streamOut == null) streamOut = new MediaStreamOut(this, Width, Height, content_buff.Length);
        return streamOut.StreamUri;
      }
    }

    internal void StopStream()
    {
      lock (lock_stream)
      {
        streamOut.Dispose();
        streamOut = null;
      }
    }

    internal bool GetImageMjpegPacket(AVPacket* packet)
    {
      av_packet_unref(packet);
      if (length_Result == 0) return false;
      lock (lock_)
      {
        av_new_packet(packet, length_Result);
        Marshal.Copy(buffer_result, 0, new IntPtr(packet->data), length_Result);
      }
      return true;
    }
#endif
  }
}