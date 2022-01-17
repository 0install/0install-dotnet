// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Runtime.InteropServices;
using NanoByte.Common.Values;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Extracts meta data from PE (Portable Executable) file headers.
/// </summary>
public partial class PEHeader
{
    public ImageDosHeader DosHeader { get; }

    public ImageFileHeader FileHeader { get; }

    public ImageOptionalHeader32 OptionalHeader32 { get; }

    public ImageOptionalHeader64 OptionalHeader64 { get; }

    public bool Is32BitHeader
    {
        get
        {
            const ushort imageFile32BitMachine = 0x0100;
            return FileHeader.Characteristics.HasFlag(imageFile32BitMachine);
        }
    }

    public PESubsystem Subsystem => Is32BitHeader ? OptionalHeader32.Subsystem : OptionalHeader64.Subsystem;

    /// <summary>
    /// Reads the PE header of a file.
    /// </summary>
    /// <param name="path">The file to read.</param>
    public PEHeader(string path)
    {
        using var stream = File.OpenRead(path);
        var reader = new BinaryReader(stream);

        DosHeader = Read<ImageDosHeader>(reader);
        stream.Seek(DosHeader.e_lfanew, SeekOrigin.Begin);
        reader.ReadUInt32(); // Skip ntHeadersSignature

        FileHeader = Read<ImageFileHeader>(reader);
        if (Is32BitHeader) OptionalHeader32 = Read<ImageOptionalHeader32>(reader);
        else OptionalHeader64 = Read<ImageOptionalHeader64>(reader);
    }

    private static T Read<T>(BinaryReader reader)
    {
        byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
        var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
        var structure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T))!;
        handle.Free();
        return structure;
    }
}
