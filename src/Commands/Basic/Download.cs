// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Tasks;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Services;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// This behaves similarly to <see cref="Selection"/>, except that it also downloads the selected versions if they are not already cached.
    /// </summary>
#if NETFRAMEWORK
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
#endif
    public class Download : Selection
    {
        public new const string Name = "download";
        public override string Description => Resources.DescriptionDownload;

        /// <summary>Indicates the user wants the implementation locations on the disk.</summary>
        private bool _show;

        /// <summary><see cref="Implementation"/>s referenced in <see cref="Selection.Selections"/> that are not available in the <see cref="IImplementationStore"/>.</summary>
        protected List<Implementation>? UncachedImplementations;

        /// <summary>
        /// Creates a new download command.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public Download(ICommandHandler handler)
            : this(handler, outputOptions: true)
        {}

        /// <summary>
        /// Creates a new download command.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        /// <param name="outputOptions">Whether to add command-line options controlling output.</param>
        /// <param name="refreshOptions">Whether to add command-line options controlling refresh behavior.</param>
        /// <param name="customizeOptions">Whether to add command-line options for customizing selected implementations.</param>
        protected Download(ICommandHandler handler, bool outputOptions = true, bool refreshOptions = true, bool customizeOptions = true)
            : base(handler, outputOptions, refreshOptions, customizeOptions)
        {
            if (outputOptions)
                Options.Add("show", () => Resources.OptionShow, _ => _show = true);
        }

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            try
            {
                Solve();
                if (FeedManager.ShouldRefresh)
                {
                    Log.Info("Running Refresh Solve because feeds have become stale");
                    RefreshSolve();
                }

                Handle(UncachedImplementations);
            }
            #region Error handling
            catch (WebException ex) when (Handler.Background)
            {
                Log.Info("Suppressed network-related error message due to background mode");
                Log.Info(ex);
                return ExitCode.WebError;
            }
            catch (SolverException ex) when (Handler.Background)
            {
                Log.Info("Suppressed Solver-related error message due to background mode");
                Log.Info(ex);
                return ExitCode.SolverError;
            }
            #endregion

            BackgroundSelfUpdate();

            Handler.CancellationToken.ThrowIfCancellationRequested();
            return ShowOutput();
        }

        /// <inheritdoc/>
        [MemberNotNull(nameof(UncachedImplementations))]
        protected override void Solve()
        {
            base.Solve();

            try
            {
                UncachedImplementations = SelectionsManager.GetUncachedImplementations(Selections);
            }
            #region Error handling
            catch (Exception ex) when (ex is KeyNotFoundException or InvalidDataException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new SolverException(ex.Message, ex);
            }
            #endregion
        }

        /// <summary>
        /// Downloads any <see cref="Implementation"/>s in <see cref="Selection"/> that are missing from <see cref="IImplementationStore"/>.
        /// </summary>
        /// <remarks>Makes sure <see cref="ISolver"/> ran with up-to-date feeds before downloading any implementations.</remarks>
        protected void Handle(List<Implementation> uncachedImplementations)
        {
            if (uncachedImplementations.Count != 0 && !FeedManager.Refresh)
            {
                Log.Info("Running Refresh Solve because there are un-cached implementations");
                RefreshSolve();
            }

            if ((CustomizeSelections || uncachedImplementations.Count != 0) && Selections != null)
                Show(Selections);

            if (uncachedImplementations.Count != 0)
                FetchAll(uncachedImplementations);
        }

        protected override ExitCode ShowOutput()
        {
            if (_show || ShowXml) return base.ShowOutput();

            Handler.OutputLow(Resources.DownloadComplete, Resources.AllComponentsDownloaded);
            return ExitCode.OK;
        }
    }
}
