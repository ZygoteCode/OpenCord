using Ionic.Zlib;
using System.Linq;

public class ZlibStreamContext
{
    private ZlibCodec deflator;

    public ZlibStreamContext()
    {
        deflator = new ZlibCodec();
        deflator.InitializeDeflate();
    }

    public byte[] Deflate(byte[] deflatedBytes)
    {
        deflator.InputBuffer = deflatedBytes;
        deflator.AvailableBytesIn = deflatedBytes.Length;
        deflator.OutputBuffer = new byte[System.Int16.MaxValue];
        deflator.AvailableBytesOut = deflator.OutputBuffer.Length;
        deflator.NextIn = 0;
        deflator.NextOut = 0;

        deflator.Deflate(FlushType.Sync);

        return deflator.OutputBuffer.Take(deflator.NextOut).ToArray();
    }
}