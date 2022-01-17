// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Archives.Extractors;

/// <summary>
/// Extracts implementation archives.
/// </summary>
/// <remarks>Implementations of this interface are immutable and thread-safe.</remarks>
public interface IArchiveExtractor
{
    /// <summary>
    /// Extracts an archive.
    /// </summary>
    /// <param name="builder">The builder receiving the extracted files.</param>
    /// <param name="stream">The archive data to be extracted.</param>
    /// <param name="subDir">The Unix-style path of the subdirectory in the archive to extract; <c>null</c> to extract entire archive.</param>
    /// <exception cref="OperationCanceledException">The operation was canceled.</exception>
    /// <exception cref="IOException">A problem occurred while extracting the archive.</exception>
    void Extract(IBuilder builder, Stream stream, string? subDir = null);

    /// <summary>
    /// A <see cref="ITask.Tag"/> to set for see cref="ITask"/>s spawned by this extractor; can be <c>null</c>.
    /// </summary>
    object? Tag { get; set; }
}
