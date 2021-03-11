using FFmpeg.AutoGen;
using System;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal unsafe class MediaEncoder : IDisposable
  {
    readonly int fps;
    AVCodec* codec;
    AVPacket* rendering_packet;
    AVCodecContext* codec_ctx;
    internal MediaEncoder(AVCodecID aVCodecID, int width, int height,int fps = 30) : this(avcodec_find_encoder(aVCodecID), width, height, fps)
    {

    }

    internal MediaEncoder(AVCodec* codec, int width, int height, int fps = 30)
    {
      if (codec == null) throw new ArgumentNullException(nameof(codec));
      this.fps = fps;
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
        throw new ScrcpyException(0, "MediaEncoder.init avcodec_alloc_context3");

      codec_ctx->time_base.num = 1;
      codec_ctx->time_base.den = fps;
      codec_ctx->width = width;
      codec_ctx->height = height;
      codec_ctx->bit_rate = 4000000;

      codec_ctx->gop_size = 12;
      codec_ctx->max_b_frames = 0;
      //codec_ctx->extradata = streamInputCodec->extradata;
      //codec_ctx->extradata_size = streamInputCodec->extradata_size;
      //codec_ctx->time_base = streamInputCodec->time_base;
      switch (codec->type)
      {
        case AVMediaType.AVMEDIA_TYPE_VIDEO:
          codec_ctx->codec_type = AVMediaType.AVMEDIA_TYPE_VIDEO;
          switch (codec->id)
          {
            case AVCodecID.AV_CODEC_ID_MJPEG:
              codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUVJ420P;
              codec_ctx->skip_frame = AVDiscard.AVDISCARD_NONINTRA;//AVDISCARD_NONREF;//AVDISCARD_NONINTRA;
              codec_ctx->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;
              break;

            case AVCodecID.AV_CODEC_ID_H264:
              codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
             
              //codec_ctx->skip_frame = AVDiscard.AVDISCARD_NONINTRA;//AVDISCARD_NONREF;//AVDISCARD_NONINTRA;
              //codec_ctx->flags |= AV_CODEC_FLAG_GLOBAL_HEADER;
              av_opt_set(codec_ctx->priv_data, "preset", "ultrafast", 0);
              av_opt_set(codec_ctx->priv_data, "tune", "zerolatency", 0);
              break;

            case AVCodecID.AV_CODEC_ID_MPEG4:
              codec_ctx->pix_fmt = AVPixelFormat.AV_PIX_FMT_YUV420P;
              av_opt_set(codec_ctx->priv_data, "preset", "ultrafast", 0);
              av_opt_set(codec_ctx->priv_data, "tune", "zerolatency", 0);

              break;

            default: throw new NotSupportedException(codec->id.ToString());
          }
          break;

        default: throw new NotSupportedException(codec->type.ToString());
      }
      
      avcodec_open2(codec_ctx, codec, null).CheckError("MediaEncoder.init avcodec_open2");
    }


    internal AVPacket* encoder_push(AVFrame* frame)
    {
      if (frame == null || frame->width == 0 || frame->height == 0) return null;

      if(codec_ctx->width != frame->width || codec_ctx->height != frame->height)
      {
        avcodec_close(codec_ctx);
        fixed (AVCodecContext** f = &codec_ctx) avcodec_free_context(f);

        InitCodecContext(frame->width, frame->height);
      }
     
      int ret = avcodec_send_frame(codec_ctx, frame);
      if (ret != 0)
      {
        Console.Error.WriteLine("MediaEncoder avcodec_send_frame: code " + ret);
        return null;
      }

      av_packet_unref(rendering_packet);
      ret = avcodec_receive_packet(codec_ctx, rendering_packet);
      
      if (ret == 0)
      {
        rendering_packet->pts = frame->pts;
        return rendering_packet;
      }
      else
      {
        if (ret == AVERROR(EAGAIN) || ret == AVERROR_EOF)
        {
          Console.Error.WriteLine("MediaEncoder avcodec_receive_packet  AVERROR(EAGAIN)");
        }
        else Console.Error.WriteLine("MediaEncoder avcodec_receive_packet: code " + ret);
        return null;
      }
    }
  }
}
