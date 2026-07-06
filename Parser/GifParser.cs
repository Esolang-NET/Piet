namespace Esolang.Piet.Parser;

static class GifParser
{
    public static bool LooksLikeGif(byte[] bytes) =>
        bytes.Length >= 6 &&
        bytes[0] == (byte)'G' &&
        bytes[1] == (byte)'I' &&
        bytes[2] == (byte)'F' &&
        bytes[3] == (byte)'8' &&
        (bytes[4] == (byte)'7' || bytes[4] == (byte)'9') &&
        bytes[5] == (byte)'a';

    public static bool TryDecodeFirstImage(byte[] bytes, out int width, out int height, out byte[] rgbPixels, CancellationToken cancellationToken = default)
    {
        width = 0;
        height = 0;
        rgbPixels = default!;

        if (!LooksLikeGif(bytes))
            return false;

        var offset = 6;
        if (!TryReadLogicalScreenDescriptor(bytes, ref offset, out width, out height, out var globalColorTable, out var backgroundColorIndex))
            return false;

        var transparentColorIndex = -1;
        while (offset < bytes.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var blockType = bytes[offset++];
            switch (blockType)
            {
                case 0x21:
                    if (!TryReadExtension(bytes, ref offset, ref transparentColorIndex))
                        return false;
                    break;
                case 0x2C:
                    return TryReadImageBlock(
                        bytes,
                        ref offset,
                        width,
                        height,
                        globalColorTable,
                        backgroundColorIndex,
                        transparentColorIndex,
                        out rgbPixels,
                        cancellationToken);
                case 0x3B:
                    return false;
                default:
                    return false;
            }
        }

        return false;
    }

    static bool TryReadLogicalScreenDescriptor(byte[] bytes, ref int offset, out int width, out int height, out byte[] globalColorTable, out byte backgroundColorIndex)
    {
        width = 0;
        height = 0;
        globalColorTable = default!;
        backgroundColorIndex = 0;

        if (offset + 7 > bytes.Length)
            return false;

        width = ReadUInt16LittleEndian(bytes, offset);
        height = ReadUInt16LittleEndian(bytes, offset + 2);
        var packed = bytes[offset + 4];
        backgroundColorIndex = bytes[offset + 5];
        offset += 7;

        if ((packed & 0x80) == 0)
            return false;

        var globalColorTableSize = 3 * (1 << ((packed & 0x07) + 1));
        if (offset + globalColorTableSize > bytes.Length)
            return false;

        globalColorTable = new byte[globalColorTableSize];
        Array.Copy(bytes, offset, globalColorTable, 0, globalColorTableSize);
        offset += globalColorTableSize;
        return true;
    }

    static bool TryReadExtension(byte[] bytes, ref int offset, ref int transparentColorIndex)
    {
        if (offset >= bytes.Length)
            return false;

        var label = bytes[offset++];
        if (label == 0xF9)
        {
            if (offset >= bytes.Length)
                return false;
            var blockSize = bytes[offset++];
            if (blockSize != 4 || offset + blockSize >= bytes.Length)
                return false;

            var packed = bytes[offset];
            transparentColorIndex = (packed & 0x01) != 0 ? bytes[offset + 3] : -1;
            offset += blockSize;
            if (offset >= bytes.Length || bytes[offset++] != 0x00)
                return false;
            return true;
        }

        return SkipSubBlocks(bytes, ref offset);
    }

    static bool TryReadImageBlock(
        byte[] bytes,
        ref int offset,
        int canvasWidth,
        int canvasHeight,
        byte[] globalColorTable,
        byte backgroundColorIndex,
        int transparentColorIndex,
        out byte[] rgbPixels,
        CancellationToken cancellationToken)
    {
        rgbPixels = default!;

        if (offset + 9 > bytes.Length)
            return false;

        var left = ReadUInt16LittleEndian(bytes, offset);
        var top = ReadUInt16LittleEndian(bytes, offset + 2);
        var imageWidth = ReadUInt16LittleEndian(bytes, offset + 4);
        var imageHeight = ReadUInt16LittleEndian(bytes, offset + 6);
        var packed = bytes[offset + 8];
        offset += 9;

        var hasLocalColorTable = (packed & 0x80) != 0;
        var isInterlaced = (packed & 0x40) != 0;
        byte[] colorTable;
        if (hasLocalColorTable)
        {
            var localColorTableSize = 3 * (1 << ((packed & 0x07) + 1));
            if (offset + localColorTableSize > bytes.Length)
                return false;

            colorTable = new byte[localColorTableSize];
            Array.Copy(bytes, offset, colorTable, 0, localColorTableSize);
            offset += localColorTableSize;
        }
        else
        {
            colorTable = globalColorTable;
        }

        if (offset >= bytes.Length)
            return false;

        var minimumCodeSize = bytes[offset++];
        if (!TryReadDataSubBlocks(bytes, ref offset, out var compressedData))
            return false;
        if (!TryLzwDecode(compressedData, minimumCodeSize, imageWidth * imageHeight, out var colorIndices, cancellationToken))
            return false;

        var canvasIndices = new byte[canvasWidth * canvasHeight];
        for (var i = 0; i < canvasIndices.Length; i++)
            canvasIndices[i] = backgroundColorIndex;

        WriteImageToCanvas(
            colorIndices,
            imageWidth,
            imageHeight,
            left,
            top,
            canvasWidth,
            canvasHeight,
            isInterlaced,
            transparentColorIndex,
            canvasIndices,
            cancellationToken);

        rgbPixels = new byte[canvasWidth * canvasHeight * 3];
        for (var i = 0; i < canvasIndices.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var colorIndex = canvasIndices[i] * 3;
            if (colorIndex + 2 >= colorTable.Length)
                return false;

            var rgbOffset = i * 3;
            rgbPixels[rgbOffset] = colorTable[colorIndex];
            rgbPixels[rgbOffset + 1] = colorTable[colorIndex + 1];
            rgbPixels[rgbOffset + 2] = colorTable[colorIndex + 2];
        }

        return true;
    }

    static void WriteImageToCanvas(
        byte[] colorIndices,
        int imageWidth,
        int imageHeight,
        int left,
        int top,
        int canvasWidth,
        int canvasHeight,
        bool isInterlaced,
        int transparentColorIndex,
        byte[] canvasIndices,
        CancellationToken cancellationToken)
    {
        var sourceIndex = 0;
        foreach (var row in EnumerateGifRows(imageHeight, isInterlaced))
        {
            cancellationToken.ThrowIfCancellationRequested();
            for (var x = 0; x < imageWidth; x++)
            {
                var colorIndex = colorIndices[sourceIndex++];
                if (colorIndex == transparentColorIndex)
                    continue;

                var canvasX = left + x;
                var canvasY = top + row;
                if ((uint)canvasX >= (uint)canvasWidth || (uint)canvasY >= (uint)canvasHeight)
                    continue;

                canvasIndices[canvasY * canvasWidth + canvasX] = colorIndex;
            }
        }
    }

    static IEnumerable<int> EnumerateGifRows(int height, bool isInterlaced)
    {
        if (!isInterlaced)
        {
            for (var y = 0; y < height; y++)
                yield return y;
            yield break;
        }

        var passes = new (int Start, int Step)[]
        {
            (0, 8),
            (4, 8),
            (2, 4),
            (1, 2),
        };

        foreach (var (start, step) in passes)
            for (var y = start; y < height; y += step)
                yield return y;
    }

    static bool TryReadDataSubBlocks(byte[] bytes, ref int offset, out byte[] data)
    {
        data = default!;
        using var stream = new MemoryStream();
        while (offset < bytes.Length)
        {
            var blockSize = bytes[offset++];
            if (blockSize == 0)
            {
                data = stream.ToArray();
                return true;
            }

            if (offset + blockSize > bytes.Length)
                return false;

            stream.Write(bytes, offset, blockSize);
            offset += blockSize;
        }

        return false;
    }

    static bool SkipSubBlocks(byte[] bytes, ref int offset)
    {
        while (offset < bytes.Length)
        {
            var blockSize = bytes[offset++];
            if (blockSize == 0)
                return true;
            if (offset + blockSize > bytes.Length)
                return false;
            offset += blockSize;
        }

        return false;
    }

    static bool TryLzwDecode(byte[] compressedData, int minimumCodeSize, int expectedLength, out byte[] decoded, CancellationToken cancellationToken)
    {
        decoded = default!;
        if (minimumCodeSize is < 2 or > 8)
            return false;

        var clearCode = 1 << minimumCodeSize;
        var endCode = clearCode + 1;
        var dictionary = new List<byte[]>(4096);

        void ResetDictionary()
        {
            dictionary.Clear();
            for (var i = 0; i < clearCode; i++)
                dictionary.Add([(byte)i]);
            dictionary.Add([]);
            dictionary.Add([]);
        }

        ResetDictionary();
        var codeSize = minimumCodeSize + 1;
        var nextCodeThreshold = 1 << codeSize;
        byte[]? previous = null;
        using var output = new MemoryStream(expectedLength);
        var bitReader = new GifBitReader(compressedData);

        while (bitReader.TryReadBits(codeSize, out var code))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (code == clearCode)
            {
                ResetDictionary();
                codeSize = minimumCodeSize + 1;
                nextCodeThreshold = 1 << codeSize;
                previous = null;
                continue;
            }

            if (code == endCode)
            {
                decoded = output.ToArray();
                return decoded.Length >= expectedLength;
            }

            byte[] entry;
            if (code < dictionary.Count)
            {
                entry = dictionary[code];
            }
            else if (code == dictionary.Count && previous is null && dictionary.Count == clearCode + 2)
            {
                entry = dictionary[0];
            }
            else if (code == dictionary.Count && previous is not null)
            {
                entry = AppendByte(previous, previous[0]);
            }
            else
            {
                return false;
            }

            output.Write(entry, 0, entry.Length);

            if (previous is not null && dictionary.Count < 4096)
            {
                dictionary.Add(AppendByte(previous, entry[0]));
                if (dictionary.Count == nextCodeThreshold && codeSize < 12)
                {
                    codeSize++;
                    nextCodeThreshold <<= 1;
                }
            }

            previous = entry;
            if (output.Length >= expectedLength)
            {
                decoded = output.ToArray();
                return true;
            }
        }

        return false;
    }

    static byte[] AppendByte(byte[] source, byte value)
    {
        var result = new byte[source.Length + 1];
        Buffer.BlockCopy(source, 0, result, 0, source.Length);
#pragma warning disable IDE0056
        result[result.Length - 1] = value;
#pragma warning restore IDE0056
        return result;
    }

    static int ReadUInt16LittleEndian(byte[] bytes, int offset) =>
        bytes[offset] | (bytes[offset + 1] << 8);

    sealed class GifBitReader
    {
        readonly byte[] data;
        int bitOffset;

        public GifBitReader(byte[] data) => this.data = data;

        public bool TryReadBits(int bitCount, out int value)
        {
            value = 0;
            for (var i = 0; i < bitCount; i++)
            {
                var absoluteBit = bitOffset + i;
                var byteIndex = absoluteBit / 8;
                if (byteIndex >= data.Length)
                    return false;

                var bitIndex = absoluteBit % 8;
                value |= ((data[byteIndex] >> bitIndex) & 0x01) << i;
            }

            bitOffset += bitCount;
            return true;
        }
    }
}
