// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Model.Selection
{
    /// <summary>
    /// Represents a set of <see cref="ImplementationBase"/>s chosen by a solver.
    /// </summary>
    [Serializable, XmlRoot("selections", Namespace = Feed.XmlNamespace), XmlType("selections", Namespace = Feed.XmlNamespace)]
    public sealed class Selections : XmlUnknown, IInterfaceUri, ICloneable<Selections>, IEquatable<Selections>
    {
        /// <summary>
        /// The URI or local path of the interface this selection is based on.
        /// </summary>
        [Description("The URI or local path of the interface this selection is based on.")]
        [XmlIgnore]
        public FeedUri InterfaceUri { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="InterfaceUri"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [XmlAttribute("interface"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string InterfaceUriString { get => InterfaceUri?.ToStringRfc(); set => InterfaceUri = (value == null) ? null : new FeedUri(value); }
        #endregion

        /// <summary>
        /// The name specified by the feed at <see cref="InterfaceUri"/>.
        /// </summary>
        [Description("The name specified by the feed at InterfaceUri.")]
        [XmlElement("name"), CanBeNull]
        public string Name { get; set; }

        /// <summary>
        /// Indicates whether the selection was generated for <see cref="Cpu.Source"/>.
        /// </summary>
        [XmlAttribute("source")]
        public bool Source { get; set; }

        /// <summary>
        /// The name of the <see cref="Command"/> in the interface to be started.
        /// </summary>
        [Description("The name of the command in the interface to be started.")]
        [XmlAttribute("command")]
        public string Command { get; set; }

        /// <summary>
        /// A list of <see cref="ImplementationSelection"/>s chosen in this selection.
        /// </summary>
        [SuppressMessage("Microsoft.Design", "CA1002:DoNotExposeGenericLists", Justification = "Used for XML serialization")]
        [Description("A list of implementations chosen in this selection.")]
        [XmlElement("selection"), NotNull]
        public List<ImplementationSelection> Implementations { get; } = new List<ImplementationSelection>();

        /// <summary>
        /// The main implementation in the selection (the actual program to launch). Identified by <see cref="InterfaceUri"/>.
        /// </summary>
        /// <exception cref="KeyNotFoundException">No <see cref="ImplementationSelection"/> matching <see cref="InterfaceUri"/> was found in <see cref="Implementations"/>.</exception>
        [XmlIgnore]
        public ImplementationSelection MainImplementation => this[InterfaceUri];

        /// <summary>
        /// Gets a list of all <see cref="Restriction"/>s and <see cref="Dependency"/> that point to a specific <paramref name="interfaceUri"/>.
        /// </summary>
        [NotNull, ItemNotNull]
        public IEnumerable<Restriction> RestrictionsFor(FeedUri interfaceUri)
        {
            foreach (var implementation in Implementations)
            {
                foreach (var restriction in implementation.GetEffectiveRestrictions())
                {
                    if (restriction.InterfaceUri == interfaceUri)
                        yield return restriction;
                }
                foreach (var command in implementation.Commands)
                {
                    foreach (var restriction in command.GetEffectiveRestrictions())
                    {
                        if (restriction.InterfaceUri == interfaceUri)
                            yield return restriction;
                    }
                }
            }
        }

        /// <summary>
        /// Indicates whether the CPU architecture of one or more <see cref="Implementations"/> is 32-bit.
        /// </summary>
        public bool Is32Bit => Implementations.Any(x => x.Architecture.Cpu.Is32Bit());

        /// <summary>
        /// Indicates whether the CPU architecture of one or more <see cref="Implementations"/> is 64-bit.
        /// </summary>
        public bool Is64Bit => Implementations.Any(x => x.Architecture.Cpu.Is64Bit());

        /// <summary>
        /// Creates an empty selections document.
        /// </summary>
        public Selections() {}

        /// <summary>
        /// Creates a selections document pre-filled with <see cref="ImplementationSelection"/>s.
        /// </summary>
        public Selections(IEnumerable<ImplementationSelection> implementations)
        {
            Implementations.AddRange(implementations);
        }

        /// <summary>
        /// Determines whether an <see cref="ImplementationSelection"/> for a specific interface is listed in the selection.
        /// </summary>
        /// <param name="interfaceUri">The <see cref="ImplementationSelection.InterfaceUri"/> to look for.</param>
        /// <returns><c>true</c> if an implementation was found; <c>false</c> otherwise.</returns>
        public bool ContainsImplementation([NotNull] FeedUri interfaceUri)
            => Implementations.Any(implementation => implementation.InterfaceUri == interfaceUri);

        /// <summary>
        /// Returns the <see cref="ImplementationSelection"/> for a specific interface.
        /// </summary>
        /// <param name="interfaceUri">The <see cref="ImplementationSelection.InterfaceUri"/> to look for.</param>
        /// <returns>The first matching implementation.</returns>
        /// <exception cref="KeyNotFoundException">No matching implementation was found.</exception>
        [NotNull]
        public ImplementationSelection this[[NotNull] FeedUri interfaceUri]
        {
            get
            {
                #region Sanity checks
                if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
                #endregion

                try
                {
                    return Implementations.First(implementation => implementation.InterfaceUri == interfaceUri);
                }
                #region Error handling
                catch (InvalidOperationException)
                {
                    throw new KeyNotFoundException(string.Format(Resources.ImplementationNotInSelection, interfaceUri));
                }
                #endregion
            }
        }

        /// <summary>
        /// Returns the <see cref="ImplementationSelection"/> for a specific interface. Safe for missing elements.
        /// </summary>
        /// <param name="interfaceUri">The <see cref="ImplementationSelection.InterfaceUri"/> to look for.</param>
        /// <returns>The first matching implementation; <c>null</c> if no matching one was found.</returns>
        [CanBeNull]
        public ImplementationSelection GetImplementation([NotNull] FeedUri interfaceUri)
        {
            #region Sanity checks
            if (interfaceUri == null) throw new ArgumentNullException(nameof(interfaceUri));
            #endregion

            return Implementations.FirstOrDefault(implementation => implementation.InterfaceUri == interfaceUri);
        }

        #region Normalize
        /// <summary>
        /// Calls <see cref="ImplementationBase.Normalize"/> for all <see cref="Implementations"/>.
        /// </summary>
        /// <exception cref="InvalidDataException">One or more required fields are not set.</exception>
        public void Normalize()
        {
            EnsureNotNull(InterfaceUri, xmlAttribute: "interface", xmlTag: "selections");
            EnsureNotNull(Command, xmlAttribute: "command", xmlTag: "selections");

            foreach (var implementation in Implementations)
                implementation.Normalize(implementation.FromFeed ?? implementation.InterfaceUri);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Selections"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Selections"/>.</returns>
        public Selections Clone()
        {
            var selections = new Selections {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, InterfaceUri = InterfaceUri, Command = Command};
            selections.Implementations.AddRange(Implementations.CloneElements());
            return selections;
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the selections as XML. Not safe for parsing!
        /// </summary>
        public override string ToString() => this.ToXmlString();
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Selections other)
            => other != null
            && base.Equals(other)
            && InterfaceUri == other.InterfaceUri
            && Command == other.Command
            && Implementations.UnsequencedEquals(other.Implementations);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Selections selections && Equals(selections);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                InterfaceUri,
                Command,
                Implementations.GetUnsequencedHashCode());
        #endregion
    }
}
