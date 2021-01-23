// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Services.Feeds;

namespace ZeroInstall.Commands.Basic
{
    partial class CatalogMan
    {
        private class Add : CatalogSubCommand
        {
            #region Metadata
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public const string Name = "add";

            public override string Description => Resources.DescriptionCatalogAdd;

            public override string Usage => "URI";

            protected override int AdditionalArgsMin => 1;

            protected override int AdditionalArgsMax => 1;
            #endregion

            #region State
            private bool _skipVerify;

            public Add(ICommandHandler handler)
                : base(handler)
            {
                Options.Add("skip-verify", () => Resources.OptionCatalogAddSkipVerify, _ => _skipVerify = true);
            }
            #endregion

            public override ExitCode Execute()
            {
                var uri = new FeedUri(AdditionalArgs[0]);
                if (!_skipVerify) CatalogManager.DownloadCatalog(uri);

                if (CatalogManager.AddSource(uri))
                {
                    if (!_skipVerify) CatalogManager.GetOnlineSafe();
                    return ExitCode.OK;
                }
                else
                {
                    Handler.OutputLow(Resources.CatalogSources, string.Format(Resources.CatalogAlreadyRegistered, uri.ToStringRfc()));
                    return ExitCode.NoChanges;
                }
            }
        }
    }
}
