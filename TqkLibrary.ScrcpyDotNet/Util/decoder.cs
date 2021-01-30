using FFmpeg.AutoGen;
using System;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal unsafe class decoder : IDisposable
  {
    AVFrame* decoding_frame;
    AVCodecContext* codec_ctx;

    public decoder(AVCodec* codec)
    {
      decoding_frame = av_frame_alloc();
      //rendering_frame = av_frame_alloc();
      codec_ctx = avcodec_alloc_context3(codec);
      if (codec_ctx == null) 
        throw new Exception();
      int error = avcodec_open2(codec_ctx, codec, null);
      if(error<0)
        throw new Exception();
    }

    public void Dispose()
    {
      avcodec_close(codec_ctx);
      fixed (AVCodecContext** f = &codec_ctx) avcodec_free_context(f);
      fixed (AVFrame** f = &decoding_frame) av_frame_free(f);
    }

    public AVFrame* decoder_push(AVPacket* packet)
    {
      int ret = avcodec_send_packet(codec_ctx, packet);
      if (ret != 0)
      {
        Console.WriteLine("decoder avcodec_send_packet: code " + ret);
        return null;
      }

      ret = avcodec_receive_frame(codec_ctx, decoding_frame);

      if (ret == 0)
      {
        decoding_frame->pts = packet->pts;
        return decoding_frame;
      }
      else
      {
        Console.WriteLine("decoder avcodec_receive_frame: code " + ret);
        return null;
      }
    }
  }
}
