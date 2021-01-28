using FFmpeg.AutoGen;
using static FFmpeg.AutoGen.ffmpeg;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.InteropServices;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal unsafe class ScrcpyStream
  {
    readonly object _lock = new object();
    const int BUFSIZE = 0x10000;
    const ulong NO_PTS = ulong.MaxValue;
    const int HEADER_SIZE = 12;

    readonly TcpClient client;
    readonly int Width;
    readonly int Height;
    public ScrcpyStream(TcpClient client,int width,int height)
    {
      this.client = client;
      this.Width = width;
      this.Height = height;
    }

    bool has_pending = false;
    AVPacket pending;

    AVCodec* h264_codec;
    AVCodecContext* h264_codec_ctx;
    AVCodecParserContext* h264_parser;
    public unsafe void RunStream()
    {
      av_register_all();
      avformat_network_init();
      
      try
      {
        h264_codec = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
        if (h264_codec == null)
          throw new ScrcpyException(0, "Decoder AV_CODEC_ID_H264 not found");

        h264_codec_ctx = avcodec_alloc_context3(h264_codec);

        h264_parser = av_parser_init((int)AVCodecID.AV_CODEC_ID_H264);
        if (h264_parser == null)
          throw new ScrcpyException(0, "parser AV_CODEC_ID_H264 not found");
        h264_parser->flags |= PARSER_FLAG_COMPLETE_FRAMES;

        InitDecoderH264();
        InitMjpegEncode();
        InitAvpicture();
        while (true)
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
      }
      finally
      {
        UnInitAvpicture();
        UnInitDecoderH264();
        UnInitMjpegEncode();
        fixed (AVCodecContext** fix_ = &h264_codec_ctx) avcodec_free_context(fix_);
        Console.WriteLine("Exit");
      }      
    }

    //push byte[] to packet
    unsafe bool stream_recv_packet(AVPacket* packet)
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
    unsafe bool stream_push_packet(AVPacket* packet)
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
          //bool ok = stream_parse(packet);
          //if (!ok) return false;
        }
        else
        {
          bool ok = stream_parse(packet);
          if (has_pending)
          {
            // the pending packet must be discarded (consumed or error)
            has_pending = false;
            av_packet_unref(fix_pending);
          }
          if (!ok) return false;
        }
        return true;
      }
    }

    unsafe bool stream_parse(AVPacket* packet)
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

      if (h264_parser->key_frame == 1) packet->flags |= AV_PKT_FLAG_KEY;

      process_frame(packet);
      return true;
    }

    unsafe void process_frame(AVPacket* packet)
    {
      Console.WriteLine("process_frame size:" + packet->size);
      //packet->dts = packet->pts;

      AVPacket out_packet;
      ffmpeg.av_init_packet(&out_packet);//Need release
      out_packet.data = null;
      out_packet.size = 0;
      int code = 0;
      try
      {
        int error_code = ffmpeg.avcodec_send_packet(h264_decoder_codec_ctx, packet);
        if (error_code < 0)
        {
          Console.WriteLine("ffmpeg.avcodec_send_packet(h264_decoder_codec_ctx, &avPacket); " + error_code);
          return;
        }

        error_code = ffmpeg.avcodec_receive_frame(h264_decoder_codec_ctx, frame_h264_decoder);
        if (error_code < 0)
        {
          Console.WriteLine("ffmpeg.avcodec_receive_frame(h264_decoder_codec_ctx, frame_h264_decoder); " + error_code);
          return;
        }

        error_code = ffmpeg.sws_scale(sws, frame_h264_decoder->data, frame_h264_decoder->linesize,
                  0, MJPEG_encoder_codec_ctx->height, frame_MJPEG_encoder->data, frame_MJPEG_encoder->linesize);
        if (error_code < 0)
        {
          Console.WriteLine("sws_scale " + error_code);
          return;
        }

        error_code = ffmpeg.avcodec_encode_video2(MJPEG_encoder_codec_ctx, &out_packet, frame_MJPEG_encoder, &code);
        if (error_code < 0)
        {
          Console.WriteLine("avcodec_encode_video2 " + error_code);
          return;
        }

        byte[] buffer_result = new byte[out_packet.size];
        Marshal.Copy(new IntPtr(out_packet.data), buffer_result, 0, out_packet.size);
        //lock (_lock) this.buffer_image = buffer_result;
#if DEBUG
        Console.WriteLine("Got frame");
        using FileStream fileStream = new FileStream($"D:\\temp\\test\\{i++.ToString("0000")}.jpeg", FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        fileStream.Write(buffer_result, 0, buffer_result.Length);
#endif
      }
      finally
      {
        ffmpeg.av_free_packet(&out_packet);
      }
    }
    int i = 0;


    AVCodec* h264_codec_decoder;
    AVCodecContext* h264_decoder_codec_ctx;
    AVFrame* frame_h264_decoder;
    unsafe void InitDecoderH264()
    {
      h264_codec_decoder = avcodec_find_decoder(AVCodecID.AV_CODEC_ID_H264);
      h264_decoder_codec_ctx = avcodec_alloc_context3(h264_codec_decoder);//Need release
      h264_decoder_codec_ctx->width = Width;
      h264_decoder_codec_ctx->height = Height;
      h264_decoder_codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_RGB24;
      h264_decoder_codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
      h264_decoder_codec_ctx->skip_frame = AVDiscard.AVDISCARD_NONINTRA;//AVDISCARD_NONREF;//AVDISCARD_NONINTRA;
      //h264_decoder_codec_ctx->extradata = streamInputCodec->extradata;
      //h264_decoder_codec_ctx->extradata_size = streamInputCodec->extradata_size;
      //h264_decoder_codec_ctx->time_base = streamInputCodec->time_base;
      h264_decoder_codec_ctx->flags = h264_decoder_codec_ctx->flags | ffmpeg.AV_CODEC_FLAG_GLOBAL_HEADER;
      //h264_decoder_codec_ctx->time_base.num = 1;
      //h264_decoder_codec_ctx->time_base.den = 30;
      avcodec_open2(h264_decoder_codec_ctx, h264_codec_decoder, null).CheckError("ffmpeg.avcodec_open2 h264 decode");//Need release
      if (h264_codec_decoder == null) throw new ScrcpyException(0, "h264_codec is null");
      frame_h264_decoder = av_frame_alloc();//Need release---
    }
    unsafe void UnInitDecoderH264()
    {
      fixed (AVFrame** fix_ = &frame_h264_decoder) av_frame_free(fix_);
      avcodec_close(h264_decoder_codec_ctx);
      fixed (AVCodecContext** fix_ =  &h264_decoder_codec_ctx) avcodec_free_context(fix_);
    }


    AVCodec* MJPEG_codec_encoder;
    AVCodecContext* MJPEG_encoder_codec_ctx;
    AVFrame* frame_MJPEG_encoder;
    unsafe void InitMjpegEncode()
    {
      MJPEG_codec_encoder = avcodec_find_encoder(AVCodecID.AV_CODEC_ID_MJPEG);
      MJPEG_encoder_codec_ctx = avcodec_alloc_context3(MJPEG_codec_encoder);//Need release
      if (MJPEG_encoder_codec_ctx == null) throw new ScrcpyException(0, "AVCodecContext (AV_CODEC_ID_MJPEG) is null");
      MJPEG_encoder_codec_ctx->bit_rate = h264_codec_ctx->bit_rate;
      MJPEG_encoder_codec_ctx->width = Width;
      MJPEG_encoder_codec_ctx->height = Height;
      MJPEG_encoder_codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
      MJPEG_encoder_codec_ctx->codec_id = AVCodecID.AV_CODEC_ID_MJPEG;
      MJPEG_encoder_codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
      //MJPEG_encoder_codec_ctx->time_base.num = codec_ctx->time_base.num;
      //MJPEG_encoder_codec_ctx->time_base.den = codec_ctx->time_base.den;
      MJPEG_encoder_codec_ctx->flags = MJPEG_encoder_codec_ctx->flags | AV_CODEC_FLAG_GLOBAL_HEADER;
      MJPEG_encoder_codec_ctx->time_base.num = 1;
      MJPEG_encoder_codec_ctx->time_base.den = 30;
      if (MJPEG_codec_encoder == null) throw new ScrcpyException(0, "AVCodec (AV_CODEC_ID_MJPEG) is null");

      avcodec_open2(MJPEG_encoder_codec_ctx, MJPEG_codec_encoder, null).CheckError("ffmpeg.avcodec_open2 AV_CODEC_ID_MJPEG");//Need release
      frame_MJPEG_encoder = av_frame_alloc();//Need release
      if (frame_MJPEG_encoder == null) throw new ScrcpyException(0, "oframe (AV_CODEC_ID_MJPEG) is null");
    }
    unsafe void UnInitMjpegEncode()
    {
      fixed (AVFrame** fix_ = &frame_MJPEG_encoder) av_frame_free(fix_);
      avcodec_close(MJPEG_encoder_codec_ctx);
      fixed (AVCodecContext** fix_ = &MJPEG_encoder_codec_ctx) avcodec_free_context(fix_);
    }


    byte* MJPEG_encoder_buffer;
    SwsContext* sws;
    unsafe void InitAvpicture()
    {
      int out_buf_size = avpicture_get_size(MJPEG_encoder_codec_ctx->pix_fmt, Width, Height);//MJPEG_encoder_codec_ctx->pix_fmt
      MJPEG_encoder_buffer = (byte*)av_malloc((ulong)out_buf_size);//Need release
      avpicture_alloc((AVPicture*)frame_MJPEG_encoder, MJPEG_encoder_codec_ctx->pix_fmt, Width, Height);
      frame_MJPEG_encoder->format = (int)AVPixelFormat.AV_PIX_FMT_RGB24;
      frame_MJPEG_encoder->width = Width;
      frame_MJPEG_encoder->height = Height;
      sws = ffmpeg.sws_getContext(Width, Height, AVPixelFormat.AV_PIX_FMT_RGB24,
               Width, Height, MJPEG_encoder_codec_ctx->pix_fmt, ffmpeg.SWS_BILINEAR, null, null, null);//Need release
    }
    unsafe void UnInitAvpicture()
    {
      sws_freeContext(sws);
      av_free(MJPEG_encoder_buffer);
    }
  }
}
