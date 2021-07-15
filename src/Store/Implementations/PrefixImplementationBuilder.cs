// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Wraps an <see cref="IImplementationBuilder"/> and prepends a directory prefix to paths.
    /// </summary>
    public class PrefixImplementationBuilder : IImplementationBuilder
    {
        private readonly IImplementationBuilder _underlyingBuilder;
        private readonly string _prefix;

        /// <summary>
        /// Creates a new prefix implementation builder.
        /// </summary>
        /// <param name="underlyingBuilder">The underlying <see cref="IImplementationBuilder"/> to wrap.</param>
        /// <param name="prefix">The directory prefix to prepend to paths.</param>
        public PrefixImplementationBuilder(IImplementationBuilder underlyingBuilder, string prefix)
        {
            _prefix = prefix;
            _underlyingBuilder = underlyingBuilder;
        }

        /// <inheritdoc/>
        public void AddDirectory(string path)
            => _underlyingBuilder.AddDirectory(Path.Combine(_prefix, path));

        /// <inheritdoc/>
        public void AddFile(string path, Stream stream, UnixTime modifiedTime, bool executable = false)
            => _underlyingBuilder.AddFile(Path.Combine(_prefix, path), stream, modifiedTime, executable);

        /// <inheritdoc/>
        public void AddHardlink(string path, string target, bool executable = false)
            => _underlyingBuilder.AddHardlink(Path.Combine(_prefix, path), Path.Combine(_prefix, target), executable);

        /// <inheritdoc/>
        public void AddSymlink(string path, string target)
            => _underlyingBuilder.AddSymlink(Path.Combine(_prefix, path), target);

        /// <inheritdoc/>
        public void Rename(string path, string target)
            => _underlyingBuilder.Rename(Path.Combine(_prefix, path), Path.Combine(_prefix, target));

        /// <inheritdoc/>
        public void Remove(string path)
            => _underlyingBuilder.Remove(Path.Combine(_prefix, path));

        /// <inheritdoc />
        public void MarkAsExecutable(string path)
            => _underlyingBuilder.MarkAsExecutable(Path.Combine(_prefix, path));

        /// <inheritdoc />
        public void TurnIntoSymlink(string path)
            => _underlyingBuilder.TurnIntoSymlink(Path.Combine(_prefix, path));
    }
}
