// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Model.Design;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Collects information about a potential candidate for an entry point.
    /// The subclass type determines the type of executable (native binary, interpreted script, etc.).
    /// </summary>
    public abstract class Candidate
    {
        /// <summary>
        /// Analyzes a file to determine whether it matches this candidate type and extracts meta data.
        /// </summary>
        /// <param name="baseDirectory">The base directory containing the entire application.</param>
        /// <param name="file">The file to be analyzed. Must be located within the <paramref name="baseDirectory"/> or a subdirectory.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="file"/> matches this candidate type. The object will then contain all available metadata.
        /// <c>false</c> if <paramref name="file"/>does not match this candidate type. The object will then be in an inconsistent state. Do not reuse!
        /// </returns>
        internal virtual bool Analyze(DirectoryInfo baseDirectory, FileInfo file)
        {
            BaseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
            RelativePath = file?.RelativeTo(BaseDirectory) ?? throw new ArgumentNullException(nameof(file));
            return true;
        }

        /// <summary>
        /// Determines whether a file is executable.
        /// </summary>
        protected bool IsExecutable(string path)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            #endregion

            Debug.Assert(BaseDirectory != null);

            return
                FileUtils.IsExecutable(path) ||
                FlagUtils.GetFiles(FlagUtils.XbitFile, BaseDirectory.FullName).Contains(path);
        }

        /// <summary>
        /// The base directory containing the entire application.
        /// </summary>
        [Browsable(false)]
        protected DirectoryInfo? BaseDirectory { get; private set; }

        /// <summary>
        /// The path of this entry point relative to <see cref="BaseDirectory"/> using Unix-style directory separators.
        /// </summary>
        [Browsable(false)]
        public string? RelativePath { get; internal set; }

        /// <summary>
        /// The application's name.
        /// </summary>
        /// <remarks>A suggestion for <see cref="Feed.Name"/>.</remarks>
        [Category("Basic (required)"), Description("The application's name.")]
        public string? Name { get; set; }

        /// <summary>
        /// Short one-line description; the first word should not be upper-case unless it is a proper noun (e.g. "cures all ills").
        /// </summary>
        /// <remarks>A suggestion for <see cref="Feed.Summaries"/>.</remarks>
        [Category("Basic (required)"), Description("Short one-line description; the first word should not be upper-case unless it is a proper noun (e.g. \"cures all ills\").")]
        public string? Summary { get; set; }

        /// <summary>
        /// A suggestion for <see cref="Feed.NeedsTerminal"/>.
        /// </summary>
        [Browsable(false)]
        public bool NeedsTerminal { get; internal set; }

        /// <summary>
        /// The application's current version.
        /// </summary>
        /// <remarks>A suggestion for <see cref="Element.Version"/>.</remarks>
        [Category("Basic (required)"), Description("The application's current version.")]
        public ImplementationVersion? Version { get; set; }

        /// <summary>
        /// A suggestion for <see cref="TargetBase.Architecture"/>.
        /// </summary>
        [Browsable(false)]
        public Architecture Architecture { get; internal set; }

        /// <summary>
        /// The main category of the application. May influence the placement in the start menu.
        /// </summary>
        [Category("Details"), Description("The main category of the application. May influence the placement in the start menu.")]
        [TypeConverter(typeof(CategoryNameConverter))]
        public string? Category { get; set; }

        /// <summary>
        /// Creates a <see cref="Command"/> to launch this entry point.
        /// </summary>
        [Browsable(false)]
        public abstract Command CreateCommand();

        /// <summary>The <see cref="Command.Name"/> used by <see cref="CreateCommand"/>.</summary>
        protected string CommandName => (Path.GetFileNameWithoutExtension(RelativePath) ?? "unknown").Replace(" ", "-");

        public override string ToString() => RelativePath + " (" + GetType().Name + ")";

        #region Equality
        protected bool Equals(Candidate other)
            => other != null
            && RelativePath == other.RelativePath
            && Name == other.Name
            && Summary == other.Summary
            && Category == other.Category
            && Equals(Version, other.Version)
            && Architecture == other.Architecture
            && NeedsTerminal == other.NeedsTerminal;

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Candidate)obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(
                RelativePath,
                Name,
                Summary,
                Category,
                Version,
                Architecture,
                NeedsTerminal);
        #endregion
    }
}
