using FFmpeg.AutoGen;
using System;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal unsafe class encoder : IDisposable
  {
    AVCodec* codec;
    AVPacket* rendering_packet;
    AVCodecContext* codec_ctx;
    public encoder(AVCodec* codec, int width, int height)
    {
      if (codec == null) throw new ArgumentNullException(nameof(codec));

      this.codec = codec;
      rendering_packet = av_packet_alloc();
      InitCodecContext(width, height);
    }

    public void Dispose()
    {
      avcodec_close(codec_ctx);
      fixed (AVCodecContext** f = &codec_ctx) avcodec_free_context(f);
      fixed (AVPacket** f = &rendering_packet) av_packet_free(f);
    }

    void InitCodecContext(int width, int height)
    {
      codec_ctx = avcodec_alloc_context3(codec);
      if (codec_ctx == null)
        throw new ScrcpyException(0, "encoder.init avcodec_alloc_context3");

      codec_ctx->time_base.num = 1;
      codec_ctx->time_base.den = 30;
      codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
      codec_ctx->width = width;
      codec_ctx->height = height;
      codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
      codec_ctx->skip_frame = AVDiscard.AVDISCARD_NONINTRA;//AVDISCARD_NONREF;//AVDISCARD_NONINTRA;
      //codec_ctx->extradata = streamInputCodec->extradata;
      //codec_ctx->extradata_size = streamInputCodec->extradata_size;
      //codec_ctx->time_base = streamInputCodec->time_base;
      codec_ctx->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;

      avcodec_open2(codec_ctx, codec, null).CheckError("encoder.init avcodec_open2");
    }


    public AVPacket* encoder_push(AVFrame* frame)
    {
      if(codec_ctx->width != frame->width || codec_ctx->height != frame->height)
      {
        avcodec_close(codec_ctx);
        fixed (AVCodecContext** f = &codec_ctx) avcodec_free_context(f);

        InitCodecContext(frame->width, frame->height);
      }
     
      int ret = avcodec_send_frame(codec_ctx, frame);
      if (ret != 0)
      {
        Console.Error.WriteLine("encoder avcodec_send_frame: code " + ret);
        return null;
      }

      ret = avcodec_receive_packet(codec_ctx, rendering_packet);

      if (ret == 0)
      {
        rendering_packet->pts = frame->pts;
        return rendering_packet;
      }
      else
      {
        Console.Error.WriteLine("encoder avcodec_receive_packet: code " + ret);
        return null;
      }
    }
  }
}
