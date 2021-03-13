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
  internal unsafe class MediaStreamIn : IDisposable
  {
    internal event ResolutionChange resolutionChange;
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

    MediaEncoder encoder_mjpeg;
    MediaDecoder decoder_h264;

    bool has_pending = false;
    AVPacket pending;
    readonly byte[] header_buff = new byte[HEADER_SIZE];
    readonly byte[] content_buff;

    int length_buffer_image = 0;
    readonly byte[] buffer_image;
    readonly object lock_buffer_image = new object();

    bool isResolutionChange = false;

#if LiveStream
    AVFrame* temp_raw_frame;
    readonly object lock_temp_raw_frame = new object();
    readonly object lock_stream = new object();
    MediaStreamOut streamOut;
    MediaEncoder mediaEncoder_liveStream;
    long pts_liveStream = 0;
#endif

    internal MediaStreamIn(TcpClient client, int width, int height, int bufferLength)
    {
      this.client = client;
      this.Width = width;
      this.Height = height;
      this.networkStream = client.GetStream();

      content_buff = new byte[bufferLength];
      buffer_image = new byte[bufferLength];

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

#if LiveStream
      temp_raw_frame = av_frame_alloc();
#endif

      encoder_mjpeg = new MediaEncoder(AVCodecID.AV_CODEC_ID_MJPEG, width, height);
      encoder_mjpeg.resolutionChange += (w, h) => isResolutionChange = true;
      decoder_h264 = new MediaDecoder(h264_codec);
    }

    public void Dispose()
    {
      av_parser_close(h264_parser);
      fixed (AVCodecContext** f = &h264_codec_ctx) avcodec_free_context(f);
#if LiveStream
      fixed (AVFrame** f = &temp_raw_frame) av_frame_free(f);
#endif
      decoder_h264?.Dispose();
      encoder_mjpeg?.Dispose();
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
        length_buffer_image = 0;
        stopCallback?.Invoke(IsRunning);
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
      AVFrame* raw_frame = decoder_h264.decoder_push(packet);//decode to raw frame
      if (raw_frame == null)
      {
        Console.Error.WriteLine("Decoder h264 failed");
        lock (lock_buffer_image) length_buffer_image = 0;
        return;
      }

      AVPacket* image_mjpeg = encoder_mjpeg.encoder_push(raw_frame);
      if(image_mjpeg == null)
      {
        Console.Error.WriteLine("Encoder mjpeg failed");
        lock (lock_buffer_image) length_buffer_image = 0;
        return;
      }

      if (isResolutionChange)
      {
        isResolutionChange = false;
        resolutionChange?.Invoke(raw_frame->width, raw_frame->height);
      }

      lock (lock_buffer_image)
      {
        Marshal.Copy(new IntPtr(image_mjpeg->data), buffer_image, 0, image_mjpeg->size);
        length_buffer_image = image_mjpeg->size;
      }

      firstFrameTrigger?.Invoke();

#if LiveStream
#if LiveStream1
      if (mediaEncoder_liveStream != null)
      {
        lock (lock_temp_raw_frame)
        {
          av_frame_unref(temp_raw_frame);
          int ret = av_frame_ref(temp_raw_frame, raw_frame);
          if (ret < 0)
          {
            Console.Error.WriteLine("MediaStreamIn.process_frame av_frame_ref failed " + ret);
            return;
          }
          ret = av_frame_copy(temp_raw_frame, raw_frame);
          if (ret < 0)
          {
            Console.Error.WriteLine("MediaStreamIn.process_frame av_frame_copy failed " + ret);
            return;
          }
          ret = av_frame_copy_props(temp_raw_frame, raw_frame);
          if (ret < 0)
          {
            Console.Error.WriteLine("MediaStreamIn.process_frame av_frame_copy_props failed " + ret);
            return;
          }
        }
      }
#elif LiveStream2
      if (streamOut != null)
      {
        streamOut.WritePacket(packet);
      }
#endif
#endif
    }



    internal Bitmap GetScreenShot()
    {
      lock (lock_buffer_image)
      {
        if (IsRunning && length_buffer_image > 0)
        {
          MemoryStream memoryStream = new MemoryStream();
          memoryStream.Write(buffer_image, 0, length_buffer_image);
          return (Bitmap)Bitmap.FromStream(memoryStream);
        }
      }
      return null;
    }

    internal byte[] GetScreenShotByteArray()
    {
      lock (lock_buffer_image)
      {
        if (IsRunning && length_buffer_image > 0)
        {
          byte[] tempbuff = new byte[length_buffer_image];
          Array.Copy(buffer_image, tempbuff, length_buffer_image);
          return tempbuff;
        }
      }
      return null;
    }

#if LiveStream
    internal string InitVideoStream(AVCodecID streamCodec, int fps = 24)
    {
      lock(lock_stream)
      {
        if (streamOut == null)
        {
          pts_liveStream = 0;
          mediaEncoder_liveStream = new MediaEncoder(streamCodec, Width, Height, fps);
          streamOut = new MediaStreamOut(this, Width, Height, fps, content_buff.Length);
        }
        return streamOut.StreamUri;
      }
    }

    internal void StopStream()
    {
      lock (lock_stream)
      {
        streamOut?.Dispose();
        streamOut = null;
        mediaEncoder_liveStream?.Dispose();
        mediaEncoder_liveStream = null;
      }
    }

    internal AVPacket* GetVideoStreamPacket()
    {
      lock (lock_temp_raw_frame)
      {
        temp_raw_frame->pts = pts_liveStream++;
        return mediaEncoder_liveStream.encoder_push(temp_raw_frame);
      }
    }
#endif
  }
}
