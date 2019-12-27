// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using ZeroInstall.Store;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A script written in PHP.
    /// </summary>
    public sealed class PhpScript : InterpretedScript
    {
        /// <inheritdoc/>
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
        {
            if (!base.Analyze(baseDirectory, file)) return false;
            return
                file.Extension.StartsWith(@".php", StringComparison.OrdinalIgnoreCase) ||
                HasShebang(file, "php");
        }

        /// <inheritdoc/>
        protected override FeedUri InterpreterInterface => new FeedUri("https://apps.0install.net/php/php.xml");
    }
}
