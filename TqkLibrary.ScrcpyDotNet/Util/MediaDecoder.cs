using FFmpeg.AutoGen;
using System;
using static FFmpeg.AutoGen.ffmpeg;

namespace TqkLibrary.ScrcpyDotNet.Util
{
  internal unsafe class MediaDecoder : IDisposable
  {
    AVFrame* decoding_frame;
    AVCodecContext* codec_ctx;

    internal MediaDecoder(AVCodecID aVCodecID) : this(avcodec_find_decoder(aVCodecID))
    {

    }

    internal MediaDecoder(AVCodec* codec)
    {
      if (codec == null) throw new ArgumentNullException(nameof(codec));

      decoding_frame = av_frame_alloc();
      //rendering_frame = av_frame_alloc();
      codec_ctx = avcodec_alloc_context3(codec);
      if (codec_ctx == null)
        throw new ScrcpyException(0, "MediaDecoder avcodec_alloc_context3 failed");

      avcodec_open2(codec_ctx, codec, null).CheckError("MediaDecoder avcodec_alloc_context3 failed");
    }

    public void Dispose()
    {
      avcodec_close(codec_ctx);
      fixed (AVCodecContext** f = &codec_ctx) avcodec_free_context(f);
      fixed (AVFrame** f = &decoding_frame) av_frame_free(f);
    }

    internal AVFrame* decoder_push(AVPacket* packet)
    {
      if (packet == null) return null;

      int ret = avcodec_send_packet(codec_ctx, packet);
      if (ret != 0)
      {
        Console.Error.WriteLine("MediaDecoder avcodec_send_packet: code " + ret);
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
        Console.Error.WriteLine("MediaDecoder avcodec_receive_frame: code " + ret);
        return null;
      }
    }
  }
}
