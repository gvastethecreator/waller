namespace Waller.Native.Core.Rendering;

internal sealed class PixelBuffer
{
    public PixelBuffer(int width, int height, byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Pixel buffer width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Pixel buffer height must be positive.");
        }

        var expectedLength = checked(width * height * 3);
        if (data.Length != expectedLength)
        {
            throw new ArgumentException(
                $"Pixel buffer data length must be {expectedLength} bytes.",
                nameof(data));
        }

        Width = width;
        Height = height;
        Data = data.ToArray();
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Data { get; }

    public static PixelBuffer CreateSolid(int width, int height, RgbColor color)
    {
        var data = new byte[checked(width * height * 3)];
        var buffer = new PixelBuffer(width, height, data);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                buffer.SetPixel(x, y, color);
            }
        }

        return buffer;
    }

    public RgbColor GetPixel(int x, int y)
    {
        var offset = GetOffset(x, y);
        return new RgbColor(Data[offset], Data[offset + 1], Data[offset + 2]);
    }

    public void SetPixel(int x, int y, RgbColor color)
    {
        var offset = GetOffset(x, y);
        Data[offset] = color.Red;
        Data[offset + 1] = color.Green;
        Data[offset + 2] = color.Blue;
    }

    private int GetOffset(int x, int y)
    {
        if (x < 0 || x >= Width)
        {
            throw new ArgumentOutOfRangeException(nameof(x), "Pixel x coordinate is outside the buffer.");
        }

        if (y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException(nameof(y), "Pixel y coordinate is outside the buffer.");
        }

        return checked(((y * Width) + x) * 3);
    }
}
