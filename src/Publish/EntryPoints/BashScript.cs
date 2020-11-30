// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using NanoByte.Common;
using ZeroInstall.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A script written in Perl.
    /// </summary>
    public sealed class BashScript : InterpretedScript
    {
        /// <inheritdoc/>
        internal override bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
        {
            if (!base.Analyze(baseDirectory, file)) return false;
            return
                StringUtils.EqualsIgnoreCase(file.Extension, @".sh") ||
                HasShebang(file, "sh") || HasShebang(file, "bash");
        }

        /// <inheritdoc/>
        protected override FeedUri InterpreterInterface => new("https://apps.0install.net/utils/bash.xml");
    }
}
