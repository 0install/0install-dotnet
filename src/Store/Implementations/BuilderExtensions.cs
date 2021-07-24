// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Archives;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Provides extensions methods for <see cref="IBuilder"/>s.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Adds a subdirectory to the implementation and returns a wrapped <see cref="IBuilder"/> to elements inside this subdirectory.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="path">The path  of the directory to create relative to the implementation root.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        /// <returns>An <see cref="IBuilder"/> wrapped around <paramref name="builder"/> that prepends <paramref name="path"/> to paths.</returns>
        public static IBuilder BuildDirectory(this IBuilder builder, string? path)
        {
            if (string.IsNullOrEmpty(path)) return builder;
            builder.AddDirectory(path);
            return new PrefixBuilder(builder, path);
        }

        public static void Apply(this IBuilder builder, DownloadRetrievalMethod download, Stream stream, ITaskHandler handler, object? tag = null)
        {
            switch (download)
            {
                case SingleFile singleFile:
                    Apply(builder, singleFile, stream);
                    break;
                case Archive archive:
                    Apply(builder, archive, stream, handler, tag);
                    break;
                default:
                    throw new NotSupportedException($"Unknown download retrieval method: ${download}");
            }
        }

        public static void Apply(this IBuilder builder, SingleFile singleFile, Stream stream)
            => builder.AddFile(singleFile.Destination, stream, 0, singleFile.Executable);

        public static void Apply(this IBuilder builder, Archive archive, Stream stream, ITaskHandler handler, object? tag = null)
        {
            var extractor = ArchiveExtractor.For(archive.MimeType ?? throw new ArgumentException($"{nameof(Archive.MimeType)} not set.", nameof(archive)), handler);
            extractor.Tag = tag;
            extractor.Extract(builder.BuildDirectory(archive.Destination), stream, archive.Extract);
        }

        public static void Apply(this IBuilder builder, RemoveStep remove)
            => builder.Remove(remove.Path);

        public static void Apply(this IBuilder builder, RenameStep rename)
            => builder.Rename(rename.Source, rename.Destination);

        public static void Apply(this IBuilder builder, CopyFromStep copyFrom, string path, ITaskHandler handler)
            => handler.RunTask(new ReadDirectory(path, builder.BuildDirectory(copyFrom.Destination)));
    }
}
