// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq;
using NanoByte.Common;
using ZeroInstall.Commands.Properties;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// List all known interface (program) URIs.
    /// </summary>
    /// <remarks>If a search term is given, only URIs containing that string are shown (case insensitive).</remarks>
    public class List : CliCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "list";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionList;

        /// <inheritdoc/>
        public override string Usage => "[PATTERN]";

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 1;
        #endregion

        /// <inheritdoc/>
        public List(ICommandHandler handler)
            : base(handler)
        {}

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            var feeds = FeedCache.ListAll().Select(x => x.ToStringRfc());
            if (AdditionalArgs.Count > 0) feeds = feeds.Where(x => x.ContainsIgnoreCase(AdditionalArgs[0]));

            Handler.Output(Resources.FeedsCached, feeds);
            return ExitCode.OK;
        }
    }
}
