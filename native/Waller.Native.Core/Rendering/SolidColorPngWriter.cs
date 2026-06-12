using Waller.Native.Core.Storage;

namespace Waller.Native.Core.Rendering;

internal static class SolidColorPngWriter
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static async Task WriteAsync(
        string path,
        PixelBuffer pixels,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pixels);

        await AtomicFileWriter.WriteAsync(
            path,
            async (file, token) =>
            {
                await file.WriteAsync(PngSignature, token);
                await WriteChunkAsync(file, "IHDR", CreateIhdr(pixels.Width, pixels.Height), token);
                await WriteChunkAsync(file, "IDAT", CreateZlibPayload(pixels), token);
                await WriteChunkAsync(file, "IEND", [], token);
            },
            cancellationToken);
    }

    private static byte[] CreateIhdr(int width, int height)
    {
        var data = new byte[13];
        WriteBigEndian(data, 0, width);
        WriteBigEndian(data, 4, height);
        data[8] = 8;
        data[9] = 2;
        data[10] = 0;
        data[11] = 0;
        data[12] = 0;
        return data;
    }

    private static byte[] CreateZlibPayload(PixelBuffer pixels)
    {
        var rowLength = checked(1 + (pixels.Width * 3));
        var rawLength = checked(rowLength * pixels.Height);
        var raw = new byte[rawLength];

        for (var y = 0; y < pixels.Height; y++)
        {
            var rowOffset = y * rowLength;
            raw[rowOffset] = 0;
            Buffer.BlockCopy(
                pixels.Data,
                y * pixels.Width * 3,
                raw,
                rowOffset + 1,
                pixels.Width * 3);
        }

        using var stream = new MemoryStream();
        stream.WriteByte(0x78);
        stream.WriteByte(0x01);

        var offset = 0;
        while (offset < raw.Length)
        {
            var blockLength = Math.Min(65535, raw.Length - offset);
            var isFinal = offset + blockLength >= raw.Length;
            stream.WriteByte(isFinal ? (byte)0x01 : (byte)0x00);
            stream.WriteByte((byte)(blockLength & 0xff));
            stream.WriteByte((byte)((blockLength >> 8) & 0xff));

            var onesComplement = (ushort)~blockLength;
            stream.WriteByte((byte)(onesComplement & 0xff));
            stream.WriteByte((byte)((onesComplement >> 8) & 0xff));
            stream.Write(raw, offset, blockLength);
            offset += blockLength;
        }

        WriteBigEndian(stream, Adler32(raw));
        return stream.ToArray();
    }

    private static async Task WriteChunkAsync(
        Stream stream,
        string type,
        byte[] data,
        CancellationToken cancellationToken)
    {
        var typeBytes = System.Text.Encoding.ASCII.GetBytes(type);
        var length = new byte[4];
        WriteBigEndian(length, 0, data.Length);
        await stream.WriteAsync(length, cancellationToken);
        await stream.WriteAsync(typeBytes, cancellationToken);
        await stream.WriteAsync(data, cancellationToken);

        var crcInput = new byte[typeBytes.Length + data.Length];
        Buffer.BlockCopy(typeBytes, 0, crcInput, 0, typeBytes.Length);
        Buffer.BlockCopy(data, 0, crcInput, typeBytes.Length, data.Length);

        var crc = new byte[4];
        WriteBigEndian(crc, 0, unchecked((int)Crc32(crcInput)));
        await stream.WriteAsync(crc, cancellationToken);
    }

    private static void WriteBigEndian(byte[] buffer, int offset, int value)
    {
        buffer[offset] = (byte)((value >> 24) & 0xff);
        buffer[offset + 1] = (byte)((value >> 16) & 0xff);
        buffer[offset + 2] = (byte)((value >> 8) & 0xff);
        buffer[offset + 3] = (byte)(value & 0xff);
    }

    private static void WriteBigEndian(Stream stream, uint value)
    {
        stream.WriteByte((byte)((value >> 24) & 0xff));
        stream.WriteByte((byte)((value >> 16) & 0xff));
        stream.WriteByte((byte)((value >> 8) & 0xff));
        stream.WriteByte((byte)(value & 0xff));
    }

    private static uint Adler32(byte[] data)
    {
        const uint mod = 65521;
        uint a = 1;
        uint b = 0;

        foreach (var value in data)
        {
            a = (a + value) % mod;
            b = (b + a) % mod;
        }

        return (b << 16) | a;
    }

    private static uint Crc32(byte[] data)
    {
        uint crc = 0xffffffff;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 1) == 1
                    ? (crc >> 1) ^ 0xedb88320
                    : crc >> 1;
            }
        }

        return ~crc;
    }
}
