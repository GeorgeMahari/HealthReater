using System.Buffers.Binary;

namespace HealthRater.Core.Imaging;

public enum ImageFormat
{
    Jpeg,
    Png,
    Webp,
}

public record ImageInfo(ImageFormat Format, int Width, int Height)
{
    public string ContentType => Format switch
    {
        ImageFormat.Jpeg => "image/jpeg",
        ImageFormat.Png => "image/png",
        _ => "image/webp",
    };
}

/// <summary>
/// Identifies JPEG / PNG / WEBP images from their bytes (never from the file name or the
/// client-supplied content type) and reads their pixel dimensions from the header.
/// Anything else — SVG, GIF, executables, truncated files — is rejected.
/// </summary>
public static class ImageInspector
{
    public static bool TryInspect(ReadOnlySpan<byte> data, out ImageInfo info)
    {
        info = null!;
        ImageInfo? result = null;

        if (data.Length >= 24 && data[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }))
            result = ReadPng(data);
        else if (data.Length >= 4 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            result = ReadJpeg(data);
        else if (data.Length >= 30 && data[..4].SequenceEqual("RIFF"u8) && data.Slice(8, 4).SequenceEqual("WEBP"u8))
            result = ReadWebp(data);

        if (result is null || result.Width <= 0 || result.Height <= 0) return false;
        info = result;
        return true;
    }

    private static ImageInfo? ReadPng(ReadOnlySpan<byte> d)
    {
        // Signature, then the IHDR chunk: length(4) "IHDR"(4) width(4) height(4).
        if (!d.Slice(12, 4).SequenceEqual("IHDR"u8)) return null;
        var width = BinaryPrimitives.ReadInt32BigEndian(d.Slice(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(d.Slice(20, 4));
        return new ImageInfo(ImageFormat.Png, width, height);
    }

    private static ImageInfo? ReadJpeg(ReadOnlySpan<byte> d)
    {
        // Walk the marker segments until a Start-Of-Frame (SOF0–SOF15, except DHT/JPG/DAC).
        var i = 2;
        while (i + 9 < d.Length)
        {
            if (d[i] != 0xFF) return null;
            var marker = d[i + 1];
            if (marker == 0xFF) { i++; continue; }            // fill byte
            if (marker is 0xD8 or 0x01 || marker is >= 0xD0 and <= 0xD7) { i += 2; continue; } // no length
            var length = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(i + 2, 2));
            if (length < 2) return null;
            var isSof = marker is >= 0xC0 and <= 0xCF && marker is not (0xC4 or 0xC8 or 0xCC);
            if (isSof)
            {
                var height = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(i + 5, 2));
                var width = BinaryPrimitives.ReadUInt16BigEndian(d.Slice(i + 7, 2));
                return new ImageInfo(ImageFormat.Jpeg, width, height);
            }
            if (marker == 0xDA) return null; // start of scan before any frame header
            i += 2 + length;
        }
        return null;
    }

    private static ImageInfo? ReadWebp(ReadOnlySpan<byte> d)
    {
        var chunk = d.Slice(12, 4);
        if (chunk.SequenceEqual("VP8 "u8))
        {
            // Lossy: frame tag (3) + start code 9D 01 2A, then 14-bit width/height.
            if (d[23] != 0x9D || d[24] != 0x01 || d[25] != 0x2A) return null;
            var width = BinaryPrimitives.ReadUInt16LittleEndian(d.Slice(26, 2)) & 0x3FFF;
            var height = BinaryPrimitives.ReadUInt16LittleEndian(d.Slice(28, 2)) & 0x3FFF;
            return new ImageInfo(ImageFormat.Webp, width, height);
        }
        if (chunk.SequenceEqual("VP8L"u8))
        {
            // Lossless: signature 0x2F, then 14-bit (width-1) and (height-1) packed LSB-first.
            if (d[20] != 0x2F) return null;
            var bits = BinaryPrimitives.ReadUInt32LittleEndian(d.Slice(21, 4));
            var width = (int)(bits & 0x3FFF) + 1;
            var height = (int)((bits >> 14) & 0x3FFF) + 1;
            return new ImageInfo(ImageFormat.Webp, width, height);
        }
        if (chunk.SequenceEqual("VP8X"u8))
        {
            // Extended: 24-bit (canvas width-1) and (height-1) at offsets 24 and 27.
            var width = (d[24] | d[25] << 8 | d[26] << 16) + 1;
            var height = (d[27] | d[28] << 8 | d[29] << 16) + 1;
            return new ImageInfo(ImageFormat.Webp, width, height);
        }
        return null;
    }
}
