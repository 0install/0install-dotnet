// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;
using ZeroInstall.Store;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A script written in Ruby.
    /// </summary>
    public sealed class RubyScript : InterpretedScript
    {
        /// <inheritdoc/>
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
        {
            if (!base.Analyze(baseDirectory, file)) return false;
            return
                StringUtils.EqualsIgnoreCase(file.Extension, @".rb") ||
                HasShebang(file, "ruby");
        }

        /// <inheritdoc/>
        protected override FeedUri InterpreterInterface => new FeedUri("http://repo.roscidus.com/ruby/ruby");
    }
}
