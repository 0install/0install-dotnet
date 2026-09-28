// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;

namespace ZeroInstall.DesktopIntegration.Windows;

/// <summary>
/// Reads and writes Win32 resources in PE files (EXEs and DLLs).
/// </summary>
[SupportedOSPlatform("windows")]
internal static class Win32Resources
{
    public const ushort
        TypeIcon = 3,
        TypeRCData = 10,
        TypeGroupIcon = 14,
        TypeVersion = 16;

    /// <summary>The language ID used for all resources written by this class.</summary>
    private const ushort LangNeutral = 0;

    /// <summary>
    /// Reads a named resource from a PE file.
    /// </summary>
    /// <param name="path">The path of the PE file.</param>
    /// <param name="type">The resource type.</param>
    /// <param name="name">The resource name.</param>
    /// <returns>The data of the resource or <c>null</c> if the file is not a valid PE file or the resource does not exist.</returns>
    public static byte[]? TryRead(string path, ushort type, string name)
    {
        var module = NativeMethods.LoadLibraryEx(path, IntPtr.Zero, NativeMethods.LoadLibraryAsDatafile | NativeMethods.LoadLibraryAsImageResource);
        if (module == IntPtr.Zero) return null;
        try
        {
            var resource = NativeMethods.FindResource(module, name, new IntPtr(type));
            if (resource == IntPtr.Zero) return null;
            var handle = NativeMethods.LoadResource(module, resource);
            if (handle == IntPtr.Zero) return null;
            var pointer = NativeMethods.LockResource(handle);
            if (pointer == IntPtr.Zero) return null;

            var data = new byte[NativeMethods.SizeofResource(module, resource)];
            Marshal.Copy(pointer, data, 0, data.Length);
            return data;
        }
        finally
        {
            NativeMethods.FreeLibrary(module);
        }
    }

    /// <summary>
    /// Adds or replaces resources in a PE file.
    /// </summary>
    /// <param name="path">The path of the PE file to modify.</param>
    /// <param name="update">Callback for adding resources using <see cref="Updater"/>.</param>
    /// <exception cref="IOException">The resources could not be updated.</exception>
    /// <exception cref="UnauthorizedAccessException">Write access to the file is not permitted.</exception>
    public static void Update(string path, Action<Updater> update)
    {
        var handle = NativeMethods.BeginUpdateResource(path, bDeleteExistingResources: false);
        if (handle == IntPtr.Zero) throw BuildException(path);

        try
        {
            update(new Updater(handle, path));
        }
        catch
        {
            NativeMethods.EndUpdateResource(handle, fDiscard: true);
            throw;
        }

        if (!NativeMethods.EndUpdateResource(handle, fDiscard: false)) throw BuildException(path);
    }

    /// <summary>
    /// Adds or replaces resources during <see cref="Win32Resources.Update"/>.
    /// </summary>
    public sealed class Updater
    {
        private readonly IntPtr _handle;
        private readonly string _path;

        internal Updater(IntPtr handle, string path)
        {
            _handle = handle;
            _path = path;
        }

        /// <summary>
        /// Adds or replaces a resource identified by a numeric ID.
        /// </summary>
        public void Set(ushort type, ushort id, byte[] data)
        {
            if (!NativeMethods.UpdateResource(_handle, new IntPtr(type), new IntPtr(id), LangNeutral, data, (uint)data.Length))
                throw BuildException(_path);
        }

        /// <summary>
        /// Adds or replaces a resource identified by a name.
        /// </summary>
        public void Set(ushort type, string name, byte[] data)
        {
            if (!NativeMethods.UpdateResource(_handle, new IntPtr(type), name, LangNeutral, data, (uint)data.Length))
                throw BuildException(_path);
        }

        /// <summary>
        /// Adds an icon group as the application icon.
        /// </summary>
        /// <param name="icoFile">The contents of an <c>.ico</c> file.</param>
        /// <exception cref="InvalidDataException"><paramref name="icoFile"/> is not a valid <c>.ico</c> file.</exception>
        public void SetIcon(byte[] icoFile)
        {
            var images = ParseIco(icoFile);

            using var group = new MemoryStream();
            using (var writer = new BinaryWriter(group))
            {
                // GRPICONDIR
                writer.Write((ushort)0); // Reserved
                writer.Write((ushort)1); // Type: Icon
                writer.Write((ushort)images.Count);

                for (int i = 0; i < images.Count; i++)
                {
                    // GRPICONDIRENTRY: same as ICONDIRENTRY, but with resource ID instead of file offset
                    writer.Write(images[i].Header);
                    writer.Write((ushort)(i + 1));
                }
            }

            for (int i = 0; i < images.Count; i++)
                Set(TypeIcon, (ushort)(i + 1), images[i].Data);
            Set(TypeGroupIcon, ApplicationIconId, group.ToArray());
        }

        /// <summary>
        /// Adds or replaces the version information.
        /// </summary>
        /// <param name="strings">Values for the string file info, such as <c>FileDescription</c>.</param>
        public void SetVersionInfo(params (string Key, string Value)[] strings)
            => Set(TypeVersion, 1, BuildVersionInfo(strings));
    }

    /// <summary>The resource ID used for the application icon group (IDI_APPLICATION).</summary>
    private const ushort ApplicationIconId = 32512;

    /// <summary>
    /// Splits the contents of an <c>.ico</c> file into image headers and data.
    /// </summary>
    /// <exception cref="InvalidDataException"><paramref name="ico"/> is not a valid <c>.ico</c> file.</exception>
    internal static List<(byte[] Header, byte[] Data)> ParseIco(byte[] ico)
    {
        const int dirSize = 6, entrySize = 16, entryHeaderSize = 12;

        if (ico.Length < dirSize) throw new InvalidDataException("Icon file too short.");
        using var reader = new BinaryReader(new MemoryStream(ico, writable: false));
        if (reader.ReadUInt16() != 0 || reader.ReadUInt16() != 1) throw new InvalidDataException("Not an icon file.");
        int count = reader.ReadUInt16();
        if (count == 0 || ico.Length < dirSize + count * entrySize) throw new InvalidDataException("Icon file has no or truncated image entries.");

        var images = new List<(byte[] Header, byte[] Data)>(count);
        for (int i = 0; i < count; i++)
        {
            // ICONDIRENTRY: width, height, color count, reserved, planes, bit count, size (all copied to GRPICONDIRENTRY), followed by file offset
            var header = reader.ReadBytes(entryHeaderSize);
            uint size = BitConverter.ToUInt32(header, 8);
            uint offset = reader.ReadUInt32();
            if (size == 0 || offset + (ulong)size > (ulong)ico.Length) throw new InvalidDataException("Icon file contains an invalid image entry.");

            var data = new byte[size];
            Array.Copy(ico, offset, data, 0, size);
            images.Add((header, data));
        }
        return images;
    }

    /// <summary>
    /// Builds a <c>VS_VERSIONINFO</c> structure.
    /// </summary>
    private static byte[] BuildVersionInfo(IEnumerable<(string Key, string Value)> strings)
    {
        const ushort typeBinary = 0, typeText = 1;
        const string langCodePage = "000004b0"; // Language neutral, Unicode
        const uint translation = 0x04b0_0000;

        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, Encoding.Unicode);

        WriteBlock(writer, "VS_VERSION_INFO", typeBinary, valueLength: 52, writeValue: () =>
        {
            // VS_FIXEDFILEINFO
            writer.Write(0xFEEF04BDu); // Signature
            writer.Write(0x0001_0000u); // Struct version
            writer.Write(0u); // File version MS
            writer.Write(0u); // File version LS
            writer.Write(0u); // Product version MS
            writer.Write(0u); // Product version LS
            writer.Write(0x3Fu); // File flags mask
            writer.Write(0u); // File flags
            writer.Write(0x0004_0004u); // File OS: VOS_NT_WINDOWS32
            writer.Write(1u); // File type: VFT_APP
            writer.Write(0u); // File subtype
            writer.Write(0u); // File date MS
            writer.Write(0u); // File date LS
        }, writeChildren: () =>
        {
            WriteBlock(writer, "StringFileInfo", typeText, writeChildren: () =>
                WriteBlock(writer, langCodePage, typeText, writeChildren: () =>
                {
                    foreach ((string key, string value) in strings)
                        WriteBlock(writer, key, typeText, valueLength: (ushort)(value.Length + 1), writeValue: () => WriteString(writer, value));
                }));
            WriteBlock(writer, "VarFileInfo", typeText, writeChildren: () =>
                WriteBlock(writer, "Translation", typeBinary, valueLength: 4, writeValue: () => writer.Write(translation)));
        });

        writer.Flush();
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a block of a <c>VS_VERSIONINFO</c> structure with a header containing its length.
    /// </summary>
    private static void WriteBlock(BinaryWriter writer, string key, ushort type, ushort valueLength = 0, Action? writeValue = null, Action? writeChildren = null)
    {
        Align(writer);
        long start = writer.BaseStream.Position;
        writer.Write((ushort)0); // Length placeholder
        writer.Write(valueLength);
        writer.Write(type);
        WriteString(writer, key);
        if (writeValue != null)
        {
            Align(writer);
            writeValue();
        }
        writeChildren?.Invoke();

        long end = writer.BaseStream.Position;
        writer.BaseStream.Position = start;
        writer.Write(checked((ushort)(end - start)));
        writer.BaseStream.Position = end;
    }

    private static void WriteString(BinaryWriter writer, string value)
    {
        writer.Write(Encoding.Unicode.GetBytes(value));
        writer.Write((ushort)0);
    }

    private static void Align(BinaryWriter writer)
    {
        while (writer.BaseStream.Position % 4 != 0)
            writer.Write((byte)0);
    }

    private static Exception BuildException(string path)
    {
        int error = Marshal.GetLastWin32Error();
        var ex = new Win32Exception(error);
        const int errorAccessDenied = 5;
        return error == errorAccessDenied
            ? new UnauthorizedAccessException($"{path}: {ex.Message}", ex)
            : new IOException($"{path}: {ex.Message}", ex);
    }

    private static class NativeMethods
    {
        public const uint LoadLibraryAsDatafile = 0x00000002, LoadLibraryAsImageResource = 0x00000020;

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr LoadLibraryEx(string lpLibFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr FindResource(IntPtr hModule, string lpName, IntPtr lpType);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32", SetLastError = true)]
        public static extern IntPtr LockResource(IntPtr hResData);

        [DllImport("kernel32", SetLastError = true)]
        public static extern uint SizeofResource(IntPtr hModule, IntPtr hResInfo);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern IntPtr BeginUpdateResource(string pFileName, [MarshalAs(UnmanagedType.Bool)] bool bDeleteExistingResources);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, IntPtr lpName, ushort wLanguage, byte[] lpData, uint cb);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateResource(IntPtr hUpdate, IntPtr lpType, string lpName, ushort wLanguage, byte[] lpData, uint cb);

        [DllImport("kernel32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EndUpdateResource(IntPtr hUpdate, [MarshalAs(UnmanagedType.Bool)] bool fDiscard);
    }
}
