using System;
using NanoByte.Common.Tasks;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Provides content for building an implementation by copying the content of a directory.
    /// </summary>
    /// <param name="Path">The path of the directory to copy.</param>
    /// <param name="Destination">The destination directory relative to the implementation root as a Unix-style path; <c>null</c> for top-level.</param>
    /// <see cref="IImplementationStore.Add"/>
    [Serializable]
    public sealed record DirectoryImplementationSource(string Path, string? Destination = null) : IImplementationSource
    {
        /// <inheritdoc/>
        public ITask GetApplyTask(string targetPath)
            => new CloneDirectory(Path, targetPath)
            {
                TargetSuffix = Destination
            };
    }
}
