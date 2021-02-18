using FFmpeg.AutoGen;
using System;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal unsafe class encoder : IDisposable
  {
    AVPacket* rendering_packet;
    AVCodecContext* codec_ctx;
    public encoder(AVCodec* codec, int width, int height)
    {
      rendering_packet = av_packet_alloc();
      codec_ctx = avcodec_alloc_context3(codec);
      if (codec_ctx == null)
        throw new Exception();
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

      int error = avcodec_open2(codec_ctx, codec, null);
      if (error < 0)
        throw new Exception();
    }

    public void Dispose()
    {
      avcodec_close(codec_ctx);
      fixed (AVCodecContext** f = &codec_ctx) avcodec_free_context(f);
      fixed (AVPacket** f = &rendering_packet) av_packet_free(f);
    }

    public AVPacket* encoder_push(AVFrame* frame)
    {
      int ret = avcodec_send_frame(codec_ctx, frame);
      if (ret != 0)
      {
        Console.WriteLine("encoder avcodec_send_frame: code " + ret);
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
        Console.WriteLine("encoder avcodec_receive_packet: code " + ret);
        return null;
      }
    }
  }
}
