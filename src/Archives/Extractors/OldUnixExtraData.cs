// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ICSharpCode.SharpZipLib.Zip;
using NanoByte.Common;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Class representing the old Info-ZIP extra block for Unix.
/// </summary>
internal class OldUnixExtraData : ITaggedData
{
    public short TagID => 0x5855;

    public UnixTime ModificationTime { get; set; }

    public UnixTime AccessTime { get; set; }

    public void SetData(byte[] data, int index, int count)
    {
        var stream = new MemoryStream(data, index, count);
        ModificationTime = ReadLEInt(stream);
        AccessTime = ReadLEInt(stream);
    }

    private static int ReadLEInt(Stream stream)
        => ReadLEShort(stream) | (ReadLEShort(stream) << 16);

    private static int ReadLEShort(Stream stream)
    {
        int byteValue1 = stream.ReadByte();

        if (byteValue1 < 0)
            throw new EndOfStreamException();

        int byteValue2 = stream.ReadByte();
        if (byteValue2 < 0)
            throw new EndOfStreamException();

        return byteValue1 | (byteValue2 << 8);
    }

    public byte[] GetData() => throw new NotImplementedException();
}
