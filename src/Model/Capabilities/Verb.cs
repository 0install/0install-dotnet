// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model.Capabilities
{
    /// <summary>
    /// The mapping of an action/verb (e.g. open, edit) to a <see cref="Command"/>.
    /// </summary>
    [Description("The mapping of an action/verb (e.g. open, edit) to a Command.")]
    [Serializable, XmlRoot("verb", Namespace = CapabilityList.XmlNamespace), XmlType("verb", Namespace = CapabilityList.XmlNamespace)]
    public sealed class Verb : XmlUnknown, IDescriptionContainer, ICloneable<Verb>, IEquatable<Verb>
    {
        #region Constants
        /// <summary>
        /// Canonical <see cref="Name"/> for opening a file.
        /// </summary>
        public const string NameOpen = "open";

        /// <summary>
        /// Canonical <see cref="Name"/> for opening a file in a new window.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
        public const string NameOpenNew = "opennew";

        /// <summary>
        /// Canonical <see cref="Name"/> for opening a file in an application of the user's choice.
        /// </summary>
        public const string NameOpenAs = "openas";

        /// <summary>
        /// Canonical <see cref="Name"/> for opening a file in editing mode.
        /// </summary>
        public const string NameEdit = "edit";

        /// <summary>
        /// Canonical <see cref="Name"/> for opening a media file and starting playback immediately.
        /// </summary>
        public const string NamePlay = "play";

        /// <summary>
        /// Canonical <see cref="Name"/> for printing a file while displaying as little as necessary to complete the task.
        /// </summary>
        public const string NamePrint = "print";

        /// <summary>
        /// Canonical <see cref="Name"/> for displaying a quick, simple response that allows the user to rapidly preview and dismiss items.
        /// </summary>
        public const string NamePreview = "Preview";
        #endregion

        /// <summary>
        /// The name of the verb. Use canonical names to get automatic localization; specify <see cref="Descriptions"/> otherwise.
        /// </summary>
        [Description("The name of the verb. Use canonical names to get automatic localization; specify Descriptions otherwise.")]
        [TypeConverter(typeof(VerbNameConverter))]
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// The name of the command in the <see cref="Feed"/> to use when launching via this capability; leave <c>null</c> for <see cref="Model.Command.NameRun"/>.
        /// </summary>
        [Description("The name of the command in the feed to use when launching via this capability; leave empty for 'run'.")]
        [TypeConverter(typeof(CommandNameConverter))]
        [XmlAttribute("command"), DefaultValue("")]
        public string? Command { get; set; }

        /// <summary>
        /// Command-line arguments to be passed to the command. Will be automatically escaped to allow proper concatenation of multiple arguments containing spaces.
        /// "${item}" gets replaced with the path of the file being opened.
        /// </summary>
        [Browsable(false)]
        [XmlElement("arg")]
        public List<Arg> Arguments { get; } = new();

        /// <summary>
        /// Command-line arguments to be passed to the command in escaped form. "%V" gets replaced with the path of the file being opened.
        /// This is ignored if <see cref="Arguments"/> has elements.
        /// </summary>
        [Description("Command-line arguments to be passed to the command in escaped form. \"%V\" gets replaced with the path of the file being opened. This is ignored if Arguments has elements.")]
        [XmlAttribute("args"), DefaultValue("")]
        public string? ArgumentsLiteral { get; set; }

        /// <summary>
        /// Set this to <c>true</c> to hide the verb in the Windows context menu unless the Shift key is pressed when opening the menu.
        /// </summary>
        [Description("Set this to true to hide the verb in the Windows context menu unless the Shift key is pressed when opening the menu.")]
        [XmlAttribute("extended"), DefaultValue(false)]
        public bool Extended { get; set; }

        /// <inheritdoc/>
        [Browsable(false)]
        [XmlElement("description")]
        public LocalizableStringCollection Descriptions { get; } = new();

        #region Conversion
        /// <summary>
        /// Returns the extension in the form "Name = Command". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Name} = {Command ?? Model.Command.NameRun}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Verb"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Verb"/>.</returns>
        public Verb Clone()
        {
            var newVerb = new Verb {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command, ArgumentsLiteral = ArgumentsLiteral, Extended = Extended};
            newVerb.Descriptions.AddRange(Descriptions.CloneElements());
            newVerb.Arguments.AddRange(Arguments.CloneElements());
            return newVerb;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Verb? other)
            => other != null
            && base.Equals(other)
            && Name == other.Name
            && Command == other.Command
            && ArgumentsLiteral == other.ArgumentsLiteral
            && Arguments.SequencedEquals(other.Arguments)
            && Extended == other.Extended
            && Descriptions.SequencedEquals(other.Descriptions);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Verb verb && Equals(verb);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                Name,
                Command,
                Arguments.GetSequencedHashCode(),
                ArgumentsLiteral,
                Extended,
                Descriptions.GetSequencedHashCode());
        #endregion
    }
}
