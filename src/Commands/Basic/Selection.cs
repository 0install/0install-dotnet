// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Services.Solvers;
using ZeroInstall.Store.Configuration;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic
{
    /// <summary>
    /// Select a version of the program identified by URI, and compatible versions of all of its dependencies.
    /// </summary>
#if NETFRAMEWORK
    [SuppressMessage("ReSharper", "AssignNullToNotNullAttribute")]
#endif
    public class Selection : CliCommand
    {
        #region Metadata
        /// <summary>The name of this command as used in command-line arguments in lower-case.</summary>
        public const string Name = "select";

        /// <inheritdoc/>
        public override string Description => Resources.DescriptionSelect;

        /// <inheritdoc/>
        public override string Usage => "[OPTIONS] URI";

        /// <inheritdoc/>
        protected override int AdditionalArgsMin => 1;

        /// <inheritdoc/>
        protected override int AdditionalArgsMax => 1;
        #endregion

        #region State
        /// <summary>
        /// A set of requirements/restrictions imposed by the user on the implementation selection process as parsed from the command-line arguments.
        /// </summary>
        protected Requirements Requirements { get; } = new();

        // Intermediate variables, transferred to Requirements after parsing
        private VersionRange? _version;
        private ImplementationVersion? _before, _notBefore;

        /// <summary>Indicates the user provided a pre-computed <see cref="Selections"/> XML document instead of using the <see cref="ISolver"/>.</summary>
        protected bool SelectionsDocument;

        /// <summary>Indicates the user wants a UI to modify the <see cref="Selections"/>.</summary>
        protected bool CustomizeSelections;

        /// <summary>Indicates the user wants a machine-readable output.</summary>
        protected bool ShowXml;

        /// <summary>
        /// Creates a new select command.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        public Selection(ICommandHandler handler)
            : this(handler, outputOptions: true)
        {}

        /// <summary>
        /// Creates a new select command.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        /// <param name="outputOptions">Whether to add command-line options controlling output.</param>
        /// <param name="refreshOptions">Whether to add command-line options controlling refresh behavior.</param>
        /// <param name="customizeOptions">Whether to add command-line options for customizing selected implementations.</param>
        protected Selection(ICommandHandler handler, bool outputOptions = true, bool refreshOptions = true, bool customizeOptions = true)
            : base(handler)
        {
            Options.Add("before=", () => Resources.OptionBefore,
                (ImplementationVersion version) => _before = version);
            Options.Add("not-before=", () => Resources.OptionNotBefore,
                (ImplementationVersion version) => _notBefore = version);
            Options.Add("version=", () => Resources.OptionVersionRange,
                (VersionRange range) => _version = range);
            Options.Add("version-for==", () => Resources.OptionVersionRangeFor,
                (FeedUri interfaceUri, VersionRange range) => Requirements.ExtraRestrictions[interfaceUri] = range);
            Options.Add("language=", () => Resources.OptionLanguage,
                (CultureInfo lang) => Requirements.Languages.Add(lang));

            if (customizeOptions)
            {
                Options.Add("command=", () => Resources.OptionCommand,
                    command => Requirements.Command = command);
                Options.Add("s|source", () => Resources.OptionSource,
                    _ => Requirements.Source = true);
                Options.Add("os=", () => Resources.OptionOS + Environment.NewLine + SupportedValues<OS>(),
                    (OS os) => Requirements.Architecture = new(os, Requirements.Architecture.Cpu));
                Options.Add("cpu=", () => Resources.OptionCpu + Environment.NewLine + SupportedValues<Cpu>(),
                    (Cpu cpu) => Requirements.Architecture = new(Requirements.Architecture.OS, cpu));
                Options.Add("customize", () => Resources.OptionCustomize, _ => CustomizeSelections = true);
            }

            if (refreshOptions)
            {
                Options.Add("o|offline", () => Resources.OptionOffline, _ => Config.NetworkUse = NetworkLevel.Offline);
                Options.Add("r|refresh", () => Resources.OptionRefresh, _ => FeedManager.Refresh = true);
            }

            if (outputOptions)
                Options.Add("xml", () => Resources.OptionXml, _ => ShowXml = true);

            Options.Add("with-store=", () => Resources.OptionWithStore, delegate(string path)
            {
                if (string.IsNullOrEmpty(path)) throw new OptionException(string.Format(Resources.MissingOptionValue, "--with-store"), "with-store");
                ImplementationStore = new CompositeImplementationStore(new[] {new ImplementationStore(path), ImplementationStore});
            });
        }
        #endregion

        /// <inheritdoc/>
        public override void Parse(IReadOnlyList<string> args)
        {
            base.Parse(args);

            SetInterfaceUri(GetCanonicalUri(AdditionalArgs[0]));
            AdditionalArgs.RemoveAt(0);

            if (Requirements.InterfaceUri.IsFile && File.Exists(Requirements.InterfaceUri.LocalPath))
                TryParseSelectionsDocument();
        }

        /// <summary>
        /// Sets <see cref="Model.Requirements.InterfaceUri"/> and applies <see cref="Requirements"/> options that need to be deferred to the end of the parsing process.
        /// </summary>
        protected void SetInterfaceUri(FeedUri uri)
        {
            Requirements.InterfaceUri = uri;

            if (_version != null)
                Requirements.ExtraRestrictions[Requirements.InterfaceUri] = _version;
            else if (_notBefore != null || _before != null)
                Requirements.ExtraRestrictions[Requirements.InterfaceUri] = new Constraint {NotBefore = _notBefore, Before = _before};
        }

        /// <inheritdoc/>
        public override ExitCode Execute()
        {
            Solve();
            if (FeedManager.ShouldRefresh)
            {
                Log.Info("Running Refresh Solve because feeds have become stale");
                RefreshSolve();
            }
            BackgroundSelfUpdate();

            Show(Selections);

            Handler.CancellationToken.ThrowIfCancellationRequested();
            return ShowOutput();
        }

        /// <summary>Cached <see cref="ISolver"/> results.</summary>
        protected Selections? Selections;

        /// <summary>
        /// Tries to parse <see cref="Model.Requirements.InterfaceUri"/> as a pre-computed <see cref="Selection.Selections"/> document.
        /// </summary>
        /// <seealso cref="SelectionsDocument"/>
        private void TryParseSelectionsDocument()
        {
            try
            { // Try to parse as selections document
                Selections = XmlStorage.LoadXml<Selections>(Requirements.InterfaceUri.LocalPath);
                Selections.Normalize();
                Requirements.InterfaceUri = Selections.InterfaceUri;
                SelectionsDocument = true;
            }
            catch (InvalidDataException)
            { // If that fails assume it is an interface
            }
        }

        /// <summary>
        /// Runs <see cref="ISolver.Solve"/> (unless <see cref="SelectionsDocument"/> is <c>true</c>) and stores the result in <see cref="Selections"/>.
        /// </summary>
        /// <returns>The same result as stored in <see cref="Selections"/>.</returns>
        /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
        /// <exception cref="WebException">A file could not be downloaded from the internet.</exception>
        /// <exception cref="IOException">An external application or file required by the solver could not be accessed.</exception>
        /// <exception cref="SolverException">The <see cref="ISolver"/> was unable to provide <see cref="Selections"/> that fulfill the <see cref="Requirements"/>.</exception>
        [MemberNotNull(nameof(Selections))]
        protected virtual void Solve()
        {
            // TODO: Handle named apps

            // Don't run the solver if the user provided an external selections document
            if (SelectionsDocument)
            {
                Debug.Assert(Selections != null);
                return;
            }

            try
            {
                Selections = Solver.Solve(Requirements);
            }
            #region Error handling
            catch
            {
                // Suppress any left-over errors if the user canceled anyway
                Handler.CancellationToken.ThrowIfCancellationRequested();
                throw;
            }
            #endregion

            try
            {
                Selections.Name = FeedCache.GetFeed(Selections.InterfaceUri).Name;
            }
            #region Error handling
            catch (KeyNotFoundException)
            {
                // Fall back to using feed file name
                Selections.Name = Selections.InterfaceUri.ToString().GetRightPartAtLastOccurrence('/');
            }
            #endregion

            Handler.CancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Run <see cref="Solve"/> with <see cref="IFeedManager.Refresh"/> set to <c>true</c>.
        /// </summary>
        [MemberNotNull(nameof(Selections))]
        protected void RefreshSolve()
        {
            FeedManager.Stale = false;
            FeedManager.Refresh = true;
            Solve();
        }

        /// <summary>
        /// Displays the <paramref name="selections"/> to the user.
        /// </summary>
        protected void Show(Selections selections)
        {
            Handler.ShowSelections(selections, FeedManager);
            if (CustomizeSelections && !SelectionsDocument) Handler.CustomizeSelections(SolveCallback);
            Handler.CancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Run <see cref="Solve"/> and inform the caller as well as the <see cref="ICommandHandler"/> of any changes.
        /// </summary>
        private Selections SolveCallback()
        {
            // Clear caches to pick up any preference changes made by the user
            SelectionCandidateProvider.Clear();
            FeedManager.Clear();

            using (FeedManager.PauseRefresh())
                Solve();

            Handler.ShowSelections(Selections, FeedManager);
            return Selections;
        }

        protected virtual ExitCode ShowOutput()
        {
            Debug.Assert(Selections != null);

            if (ShowXml) Handler.Output(Resources.SelectedImplementations, Selections.ToXmlString());
            else Handler.Output(Resources.SelectedImplementations, SelectionsManager.GetTree(Selections));
            return ExitCode.OK;
        }
    }
}
