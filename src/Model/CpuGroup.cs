// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Model;

/// <summary>
/// Describes a group of CPU architectures that can be combined within a single process.
/// </summary>
public enum CpuGroup
{
    X86,
    X64,
    Arm32,
    Arm64
}
