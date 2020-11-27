using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace TqkLibrary.Media.Sounds
{
  public static class ReadMp3Duration
  {
    public static TimeSpan Read(Stream stream,int length)
    {
      var id3v2Tag = Id3v2Tag.ReadTag(stream);
      long dataStartPosition = stream.Position;
      var firstFrame = Mp3Frame.LoadFromStream(stream);
      if (firstFrame == null)
        throw new InvalidDataException("Invalid MP3 file - no MP3 Frames Detected");
      double bitRate = firstFrame.BitRate;
      var xingHeader = XingHeader.LoadXingHeader(firstFrame);
      // If the header exists, we can skip over it when decoding the rest of the file
      if (xingHeader != null) dataStartPosition = stream.Position;

      // workaround for a longstanding issue with some files failing to load
      // because they report a spurious sample rate change
      var secondFrame = Mp3Frame.LoadFromStream(stream);
      if (secondFrame != null &&
          (secondFrame.SampleRate != firstFrame.SampleRate ||
           secondFrame.ChannelMode != firstFrame.ChannelMode))
      {
        // assume that the first frame was some kind of VBR/LAME header that we failed to recognise properly
        dataStartPosition = secondFrame.FileOffset;
        // forget about the first frame, the second one is the first one we really care about
        firstFrame = secondFrame;
      }

      long mp3DataLength = length - dataStartPosition - 128;
      double totaltime = mp3DataLength * 8.0 / firstFrame.BitRate;
      return TimeSpan.FromSeconds(totaltime);
    }
  }
}
