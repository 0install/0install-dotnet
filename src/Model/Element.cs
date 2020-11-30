// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Model.Design;
using ZeroInstall.Model.Properties;

namespace ZeroInstall.Model
{
    #region Enumerations
    /// <summary>
    /// A stability rating for an <see cref="Implementation"/>.
    /// </summary>
    public enum Stability
    {
        /// <summary>Inherit stability from <see cref="Group"/> or default to <see cref="Testing"/></summary>
        [XmlIgnore]
        Unset,

        /// <summary>Set by user as a personal preference overriding other stability criteria.</summary>
        [XmlEnum("preferred")]
        Preferred,

        /// <summary>Indicates that an implementation is provided as a <see cref="PackageImplementation"/>.</summary>
        [XmlEnum("packaged")]
        Packaged,

        /// <summary>No serious problems.</summary>
        [XmlEnum("stable")]
        Stable,

        /// <summary>Any new release.</summary>
        [XmlEnum("testing")]
        Testing,

        /// <summary>More extreme version of <see cref="Testing"/>, expected to have bugs.</summary>
        [XmlEnum("developer")]
        Developer,

        /// <summary>Known bugs, none security-related.</summary>
        [XmlEnum("buggy")]
        Buggy,

        /// <summary>Known bugs, some or all security-related.</summary>
        [XmlEnum("insecure")]
        Insecure
    }
    #endregion

    /// <summary>
    /// Abstract base class for <see cref="ImplementationBase"/> and <see cref="Group"/>.
    /// Contains those parameters that can be transferred from a <see cref="Group"/> to an <see cref="Implementation"/>.
    /// </summary>
    [XmlType("element", Namespace = Feed.XmlNamespace)]
    public abstract class Element : TargetBase, IBindingContainer, IDependencyContainer, ICloneable<Element>
    {
        #region Constants
        /// <summary>
        /// The <see cref="string.Format(string,object[])"/> format used by <see cref="ReleasedString"/>
        /// </summary>
        public const string ReleaseDateFormat = "yyyy-MM-dd";
        #endregion

        /// <summary>
        /// A flat list of all <see cref="Implementation"/>s contained in this element. May be the element itself or its children.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        internal abstract IEnumerable<Implementation> Implementations { get; }

        /// <summary>
        /// The version number of the implementation.
        /// </summary>
        [Category("Release"), Description("The version number of the implementation.")]
        [XmlIgnore]
        public ImplementationVersion Version { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Version"/>
        [XmlAttribute("version"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public virtual string VersionString { get => Version?.ToString(); set => Version = string.IsNullOrEmpty(value) ? null : new ImplementationVersion(value); }
        #endregion

        /// <summary>
        /// A string to be appended to the version. The purpose of this is to allow complex version numbers (such as "1.0-rc2") in older versions of the injector.
        /// </summary>
        [Category("Release"), Description("A string to be appended to the version. The purpose of this is to allow complex version numbers (such as \"1.0-rc2\") in older versions of the injector.")]
        [XmlAttribute("version-modifier"), DefaultValue("")]
        public virtual string? VersionModifier { get; set; }

        /// <summary>
        /// The date this implementation was made available. For development versions checked out from version control this attribute should not be present.
        /// </summary>
        [Category("Release"), Description("The date this implementation was made available. For development versions checked out from version control this attribute should not be present.")]
        [XmlIgnore]
        public virtual DateTime Released { get; set; }

        /// <summary>
        /// Used to store the unparsed release date string (instead of <see cref="Released"/>) if it <see cref="ModelUtils.ContainsTemplateVariables"/>.
        /// </summary>
        protected string? ReleasedVerbatim;

        /// <summary>
        /// The string form of <see cref="Released"/>. Only use this if the string <see cref="ModelUtils.ContainsTemplateVariables"/>.
        /// </summary>
        /// <seealso cref="Released"/>
        [Category("Release"), Description("The string form of Released. Only use this if the string contains template variables.")]
        [XmlAttribute("released")]
        public virtual string? ReleasedString
        {
            get
            {
                if (ReleasedVerbatim != null) return ReleasedVerbatim;
                else return (Released == default ? null : Released.ToString(ReleaseDateFormat));
            }
            set
            {
                if (string.IsNullOrEmpty(value)) Released = default;
                else if (ModelUtils.ContainsTemplateVariables(value))
                {
                    Released = default;
                    ReleasedVerbatim = value;
                }
                else
                {
                    Released = DateTime.ParseExact(value, ReleaseDateFormat, CultureInfo.InvariantCulture);
                    ReleasedVerbatim = null;
                }
            }
        }

        /// <summary>
        /// The default stability rating for this implementation.
        /// </summary>
        [Category("Release"), Description("The default stability rating for this implementation.")]
        [XmlAttribute("stability"), DefaultValue(typeof(Stability), "Unset")]
        public virtual Stability Stability { get; set; } = Stability.Unset;

        /// <summary>
        /// License terms (typically a Trove category, as used on freshmeat.net).
        /// </summary>
        [Category("Release"), Description("License terms (typically a Trove category, as used on freshmeat.net).")]
        [TypeConverter(typeof(LicenseNameConverter))]
        [XmlAttribute("license"), DefaultValue("")]
        public string? License { get; set; }

        /// <summary>
        /// The relative path of an executable inside the implementation that should be executed by default when the interface is run. If an implementation has no main setting, then it cannot be executed without specifying one manually. This typically means that the interface is for a library.
        /// </summary>
        /// <remarks>
        /// This is deprecated in favor of <see cref="Commands"/>.
        /// <c>null</c> corresponds to no <see cref="Command"/>s.
        /// An empty string corresponds to a <see cref="Command"/> with no <see cref="Command.Path"/>.
        /// </remarks>
        [Category("Execution"), Description("The relative path of an executable inside the implementation that should be executed by default when the interface is run. If an implementation has no main setting, then it cannot be executed without specifying one manually. This typically means that the interface is for a library.")]
        [XmlAttribute("main"), DefaultValue("")]
        public string? Main { get; set; }

        /// <summary>
        /// The relative path of an executable inside the implementation that can be executed to test the program. The program must be non-interactive (e.g. it can't open any windows or prompt for input). It should return with an exit status of 0 if the tests pass. Any other status indicates failure.
        /// </summary>
        /// <remarks>
        /// This is deprecated in favor of <see cref="Commands"/>.
        /// <c>null</c> corresponds to no <see cref="Command"/>s.
        /// An empty string corresponds to a <see cref="Command"/> with no <see cref="Command.Path"/>.
        /// </remarks>
        [Category("Execution"), Description("The relative path of an executable inside the implementation that can be executed to test the program. The program must be non-interactive (e.g. it can't open any windows or prompt for input). It should return with an exit status of 0 if the tests pass. Any other status indicates failure.")]
        [XmlAttribute("self-test"), DefaultValue("")]
        public string? SelfTest { get; set; }

        /// <summary>
        /// The relative path of a directory inside the implementation that contains the package's documentation. This is the directory that would end up inside /usr/share/doc on a traditional Linux system.
        /// </summary>
        [Category("Execution"), Description("The relative path of a directory inside the implementation that contains the package's documentation. This is the directory that would end up inside /usr/share/doc on a traditional Linux system.")]
        [XmlAttribute("doc-dir"), DefaultValue("")]
        public string? DocDir { get; set; }

        /// <summary>
        /// A list of interfaces this implementation depends upon.
        /// </summary>
        [Browsable(false)]
        [XmlElement("requires")]
        public List<Dependency> Dependencies { get; } = new();

        /// <summary>
        /// A list of interfaces that are restricted to specific versions when used.
        /// </summary>
        [Browsable(false)]
        [XmlElement("restricts")]
        public List<Restriction> Restrictions { get; } = new();

        /// <summary>
        /// A list of <see cref="Binding"/>s for <see cref="Implementation"/>s to locate <see cref="Dependency"/>s.
        /// </summary>
        [Browsable(false)]
        [XmlElement(typeof(GenericBinding)), XmlElement(typeof(EnvironmentBinding)), XmlElement(typeof(OverlayBinding)), XmlElement(typeof(ExecutableInVar)), XmlElement(typeof(ExecutableInPath))]
        public List<Binding> Bindings { get; } = new();

        /// <summary>
        /// A list of commands that can be used to launch this implementation.
        /// </summary>
        /// <remarks>This will eventually replace <see cref="Main"/> and <see cref="SelfTest"/>.</remarks>
        [Browsable(false)]
        [XmlElement("command")]
        public List<Command> Commands { get; } = new();

        /// <summary>
        /// Determines whether <see cref="Commands"/> contains a <see cref="Command"/> with a specific name.
        /// </summary>
        /// <param name="name">The <see cref="Command.Name"/> to look for; <see cref="string.Empty"/> for none.</param>
        /// <returns><c>true</c> if a matching command was found or if <paramref name="name"/> is <see cref="string.Empty"/>; <c>false</c> otherwise.</returns>
        public bool ContainsCommand(string name)
        {
            #region Sanity checks
            if (name == null) throw new ArgumentNullException(nameof(name));
            #endregion

            if (name.Length == 0) return true;
            return Commands.Select(command => command.Name).Contains(name);
        }

        /// <summary>
        /// Returns the <see cref="Command"/> with a specific name.
        /// </summary>
        /// <param name="name">The <see cref="Command.Name"/> to look for; <see cref="string.Empty"/> for none.</param>
        /// <returns>The first matching command; <c>null</c> if <paramref name="name"/> is <see cref="string.Empty"/>.</returns>
        /// <exception cref="KeyNotFoundException">No matching <see cref="Command"/> was found.</exception>
        /// <remarks>Should only be called after <see cref="Normalize"/> has been called, otherwise nested <see cref="Implementation"/>s will not be considered.</remarks>
        public Command? this[string name]
        {
            get
            {
                #region Sanity checks
                if (name == null) throw new ArgumentNullException(nameof(name));
                #endregion

                if (name.Length == 0) return null;
                try
                {
                    return Commands.First(command => command != null && command.Name == name);
                }
                #region Error handling
                catch (InvalidOperationException)
                {
                    throw new KeyNotFoundException(string.Format(Resources.CommandNotFound, name));
                }
                #endregion
            }
        }

        /// <summary>
        /// Returns the <see cref="Command"/> with a specific name. Safe for missing elements.
        /// </summary>
        /// <param name="name">The <see cref="Command.Name"/> to look for.</param>
        /// <returns>The first matching command; <c>null</c> if no matching one was found.</returns>
        /// <remarks>Should only be called after <see cref="Normalize"/> has been called, otherwise nested <see cref="Implementation"/>s will not be considered.</remarks>
        public Command? GetCommand(string name)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(name);
            #endregion

            return Commands.FirstOrDefault(command => command != null && command.Name == name);
        }

        #region Normalize
        /// <summary>
        /// Sets missing default values and handles legacy elements.
        /// </summary>
        /// <param name="feedUri">The feed the data was originally loaded from.</param>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        public virtual void Normalize(FeedUri? feedUri = null)
        {
            // Apply if-0install-version filter
            Commands.RemoveAll(FilterMismatch);
            Dependencies.RemoveAll(FilterMismatch);
            Restrictions.RemoveAll(FilterMismatch);
            Bindings.RemoveAll(FilterMismatch);

            // Convert legacy launch commands
            if (Main != null) Commands.Add(new Command {Name = Command.NameRun, Path = Main});
            if (SelfTest != null) Commands.Add(new Command {Name = Command.NameTest, Path = SelfTest});

            foreach (var command in Commands) command.Normalize();
            foreach (var dependency in Dependencies) dependency.Normalize();
            foreach (var restriction in Restrictions) restriction.Normalize();
        }

        /// <summary>
        /// Transfers attributes from another <see cref="Element"/> object to this one.
        /// Existing values are not replaced. Provides an inheritance-like relation.
        /// </summary>
        /// <param name="parent">The object to take the attributes from.</param>
        internal void InheritFrom(Element parent)
        {
            #region Sanity checks
            if (parent == null) throw new ArgumentNullException(nameof(parent));
            #endregion

            // Check if values are unset and need inheritance)
            if (Version == null) Version = parent.Version;
            if (VersionModifier == null) VersionModifier = parent.VersionModifier;
            if (Released == default) Released = parent.Released;
            if (Main == null) Main = parent.Main;
            if (SelfTest == null) SelfTest = parent.SelfTest;
            if (DocDir == null) DocDir = parent.DocDir;
            if (License == null) License = parent.License;
            if (Stability == Stability.Unset) Stability = parent.Stability;
            if (Languages.Count == 0) Languages = new LanguageSet(parent.Languages);
            if (Architecture == default) Architecture = parent.Architecture;

            // Accumulate list entries
            Commands.AddRange(parent.Commands);
            Dependencies.AddRange(parent.Dependencies);
            Restrictions.AddRange(parent.Restrictions);
            Bindings.AddRange(parent.Bindings);

            // Inherit unknown XML attributes and elements
            UnknownAttributes = (UnknownAttributes ?? new XmlAttribute[0]).Concat(parent.UnknownAttributes ?? new XmlAttribute[0]).DistinctBy(x => x.Name).ToArray();
            UnknownElements = (UnknownElements ?? new XmlElement[0]).Concat(parent.UnknownElements ?? new XmlElement[0]).ToArray();
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Element"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Element"/>.</returns>
        public abstract Element Clone();

        /// <summary>
        /// Copies all known values from one instance to another. Helper method for instance cloning.
        /// </summary>
        protected static void CloneFromTo(Element from, Element to)
        {
            #region Sanity checks
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            #endregion

            TargetBase.CloneFromTo(from, to);
            to.Version = from.Version;
            to.VersionModifier = from.VersionModifier;
            to.Released = from.Released;
            to.ReleasedVerbatim = from.ReleasedVerbatim;
            to.Stability = from.Stability;
            to.License = from.License;
            to.Main = from.Main;
            to.SelfTest = from.SelfTest;
            to.DocDir = from.DocDir;
            to.Commands.AddRange(from.Commands.CloneElements());
            to.Dependencies.AddRange(from.Dependencies.CloneElements());
            to.Restrictions.AddRange(from.Restrictions.CloneElements());
            to.Bindings.AddRange(from.Bindings.CloneElements());
        }
        #endregion

        #region Equality
        protected bool Equals(Element? other)
            => other != null
            && base.Equals(other)
            && other.Version == Version
            && other.VersionModifier == VersionModifier
            && other.Released == Released
            && other.ReleasedVerbatim == ReleasedVerbatim
            && other.Stability == Stability
            && other.License == License
            && other.Main == Main
            && other.SelfTest == SelfTest
            && other.DocDir == DocDir
            && Commands.SequencedEquals(other.Commands)
            && Dependencies.SequencedEquals(other.Dependencies)
            && Restrictions.SequencedEquals(other.Restrictions)
            && Bindings.SequencedEquals(other.Bindings);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(Version);
            hash.Add(VersionModifier);
            hash.Add(Released);
            hash.Add(ReleasedVerbatim);
            hash.Add(Stability);
            hash.Add(License);
            hash.Add(Main);
            hash.Add(SelfTest);
            hash.Add(DocDir);
            hash.Add(Commands.GetSequencedHashCode());
            hash.Add(Dependencies.GetSequencedHashCode());
            hash.Add(Restrictions.GetSequencedHashCode());
            hash.Add(Bindings.GetSequencedHashCode());
            return hash.ToHashCode();
        }
        #endregion
    }
}
