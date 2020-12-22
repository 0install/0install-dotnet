// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Abstract base class for <see cref="Element"/> and <see cref="FeedReference"/>.
    /// Contains language and architecture parameters.
    /// </summary>
    [XmlType("target-base", Namespace = Feed.XmlNamespace)]
    public abstract class TargetBase : FeedElement
    {
        // Order is always alphabetical, duplicate entries are not allowed
        private LanguageSet _languages = new();

        /// <summary>
        /// The natural language(s) which an <see cref="Implementation"/> supports.
        /// </summary>
        /// <example>For example, the value "en_GB fr" would be used for a package supporting British English and French.</example>
        [SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly", Justification = "Complete set can be replaced by PropertyGrid.")]
        [Category("Release"), Description("The natural language(s) which an implementation supports.")]
        [XmlIgnore]
        public LanguageSet Languages { get => _languages; set => _languages = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// For platform-specific binaries, the platform for which an <see cref="Implementation"/> was compiled.
        /// </summary>
        /// <remarks>The injector knows that certain platforms are backwards-compatible with others, so binaries with arch="Linux-i486" will still be available on Linux-i686 machines, for example.</remarks>
        [Category("Release"), Description("For platform-specific binaries, the platform for which an implementation was compiled, in the form os-cpu.")]
        [XmlIgnore]
        public Architecture Architecture { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Languages"/>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("langs"), DefaultValue("")]
        public string LanguagesString { get => _languages.ToString(); set => _languages = new LanguageSet(value); }

        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Architecture"/>
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlAttribute("arch"), DefaultValue("*-*")]
        public string ArchitectureString { get => Architecture.ToString(); set => Architecture = new(value); }
        #endregion

        #region Clone
        /// <summary>
        /// Copies all known values from one instance to another. Helper method for instance cloning.
        /// </summary>
        protected static void CloneFromTo(TargetBase from, TargetBase to)
        {
            #region Sanity checks
            if (from == null) throw new ArgumentNullException(nameof(from));
            if (to == null) throw new ArgumentNullException(nameof(to));
            #endregion

            to.UnknownElements = from.UnknownElements;
            to.UnknownAttributes = from.UnknownAttributes;
            to.Languages.Clear();
            to.Languages = new LanguageSet(from.Languages);
            to.ArchitectureString = from.ArchitectureString;
        }
        #endregion

        #region Equality
        protected bool Equals(TargetBase? other)
            => other != null
            && base.Equals(other)
            && _languages.SetEquals(other._languages)
            && other.Architecture == Architecture;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                _languages.GetUnsequencedHashCode(),
                Architecture);
        #endregion
    }
}
