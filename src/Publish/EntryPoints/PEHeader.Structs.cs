// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

// ReSharper disable All
namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// The machine type specified by a PE file (i.e., the CPU architecture the binary runs on).
/// </summary>
[SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32")]
public enum PEMachineType : ushort
{
    Native = 0,
    I386 = 0x014c,
    Itanium = 0x0200,
    X64 = 0x8664
}

/// <summary>
/// The subsystem specified by a PE file (i.e., whether the binary is a console or GUI application).
/// </summary>
[SuppressMessage("Microsoft.Design", "CA1028:EnumStorageShouldBeInt32"), SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
public enum PESubsystem : ushort
{
    Native = 1,
    WindowsGui = 2,
    WindowsCui = 3,
    OS2Cui = 5,
    PosixCui = 7
}

public partial class PEHeader
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDosHeader
    {
        public UInt16 e_magic; // Magic number
        public UInt16 e_cblp; // Bytes on last page of file
        public UInt16 e_cp; // Pages in file
        public UInt16 e_crlc; // Relocations
        public UInt16 e_cparhdr; // Size of header in paragraphs
        public UInt16 e_minalloc; // Minimum extra paragraphs needed
        public UInt16 e_maxalloc; // Maximum extra paragraphs needed
        public UInt16 e_ss; // Initial (relative) SS value
        public UInt16 e_sp; // Initial SP value
        public UInt16 e_csum; // Checksum
        public UInt16 e_ip; // Initial IP value
        public UInt16 e_cs; // Initial (relative) CS value
        public UInt16 e_lfarlc; // File address of relocation table
        public UInt16 e_ovno; // Overlay number
        public UInt16 e_res_0; // Reserved words
        public UInt16 e_res_1; // Reserved words
        public UInt16 e_res_2; // Reserved words
        public UInt16 e_res_3; // Reserved words
        public UInt16 e_oemid; // OEM identifier (for e_oeminfo)
        public UInt16 e_oeminfo; // OEM information; e_oemid specific
        public UInt16 e_res2_0; // Reserved words
        public UInt16 e_res2_1; // Reserved words
        public UInt16 e_res2_2; // Reserved words
        public UInt16 e_res2_3; // Reserved words
        public UInt16 e_res2_4; // Reserved words
        public UInt16 e_res2_5; // Reserved words
        public UInt16 e_res2_6; // Reserved words
        public UInt16 e_res2_7; // Reserved words
        public UInt16 e_res2_8; // Reserved words
        public UInt16 e_res2_9; // Reserved words
        public UInt32 e_lfanew; // File address of new exe header
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDataDirectory
    {
        public UInt32 VirtualAddress;
        public UInt32 Size;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageOptionalHeader32
    {
        public UInt16 Magic;
        public Byte MajorLinkerVersion;
        public Byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt32 BaseOfData;
        public UInt32 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public PESubsystem Subsystem;
        public UInt16 DllCharacteristics;
        public UInt32 SizeOfStackReserve;
        public UInt32 SizeOfStackCommit;
        public UInt32 SizeOfHeapReserve;
        public UInt32 SizeOfHeapCommit;

        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        public UInt32 LoaderFlags;

        public UInt32 NumberOfRvaAndSizes;

        public ImageDataDirectory ExportTable;
        public ImageDataDirectory ImportTable;
        public ImageDataDirectory ResourceTable;
        public ImageDataDirectory ExceptionTable;
        public ImageDataDirectory CertificateTable;
        public ImageDataDirectory BaseRelocationTable;
        public ImageDataDirectory Debug;
        public ImageDataDirectory Architecture;
        public ImageDataDirectory GlobalPtr;
        public ImageDataDirectory TLSTable;
        public ImageDataDirectory LoadConfigTable;
        public ImageDataDirectory BoundImport;
        public ImageDataDirectory IAT;
        public ImageDataDirectory DelayImportDescriptor;
        public ImageDataDirectory CLRRuntimeHeader;
        public ImageDataDirectory Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageOptionalHeader64
    {
        public UInt16 Magic;
        public Byte MajorLinkerVersion;
        public Byte MinorLinkerVersion;
        public UInt32 SizeOfCode;
        public UInt32 SizeOfInitializedData;
        public UInt32 SizeOfUninitializedData;
        public UInt32 AddressOfEntryPoint;
        public UInt32 BaseOfCode;
        public UInt64 ImageBase;
        public UInt32 SectionAlignment;
        public UInt32 FileAlignment;
        public UInt16 MajorOperatingSystemVersion;
        public UInt16 MinorOperatingSystemVersion;
        public UInt16 MajorImageVersion;
        public UInt16 MinorImageVersion;
        public UInt16 MajorSubsystemVersion;
        public UInt16 MinorSubsystemVersion;
        public UInt32 Win32VersionValue;
        public UInt32 SizeOfImage;
        public UInt32 SizeOfHeaders;
        public UInt32 CheckSum;
        public PESubsystem Subsystem;
        public UInt16 DllCharacteristics;
        public UInt64 SizeOfStackReserve;
        public UInt64 SizeOfStackCommit;
        public UInt64 SizeOfHeapReserve;
        public UInt64 SizeOfHeapCommit;

        [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flags")]
        public UInt32 LoaderFlags;

        public UInt32 NumberOfRvaAndSizes;

        public ImageDataDirectory ExportTable;
        public ImageDataDirectory ImportTable;
        public ImageDataDirectory ResourceTable;
        public ImageDataDirectory ExceptionTable;
        public ImageDataDirectory CertificateTable;
        public ImageDataDirectory BaseRelocationTable;
        public ImageDataDirectory Debug;
        public ImageDataDirectory Architecture;
        public ImageDataDirectory GlobalPtr;
        public ImageDataDirectory TLSTable;
        public ImageDataDirectory LoadConfigTable;
        public ImageDataDirectory BoundImport;
        public ImageDataDirectory IAT;
        public ImageDataDirectory DelayImportDescriptor;
        public ImageDataDirectory CLRRuntimeHeader;
        public ImageDataDirectory Reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct ImageFileHeader
    {
        public PEMachineType Machine;
        public UInt16 NumberOfSections;
        public UInt32 TimeDateStamp;
        public UInt32 PointerToSymbolTable;
        public UInt32 NumberOfSymbols;
        public UInt16 SizeOfOptionalHeader;
        public UInt16 Characteristics;
    }
}
