// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using Generator.Equals;
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
    [Equatable]
    public sealed partial class Verb : XmlUnknown, IDescriptionContainer, ICloneable<Verb>
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
        /// The name of the verb. Must be an alphanumeric string.
        /// Use canonical names to get automatic localization; specify <see cref="Descriptions"/> otherwise.
        /// </summary>
        [Description("The name of the verb. Use canonical names to get automatic localization; specify Descriptions otherwise.")]
        [TypeConverter(typeof(VerbNameConverter))]
        [XmlAttribute("name")]
        public string Name { get; set; } = default!;

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
        [OrderedEquality]
        public List<Arg> Arguments { get; } = new();

        /// <summary>
        /// Command-line arguments to be passed to the command in escaped form. "%V" gets replaced with the path of the file being opened.
        /// This is ignored if <see cref="Arguments"/> has elements.
        /// </summary>
        [DisplayName("Arguments"), Description("Command-line arguments to be passed to the command in escaped form. \"%V\" gets replaced with the path of the file being opened. This is ignored if Arguments has elements.")]
        [XmlAttribute("args"), DefaultValue("")]
        public string? ArgumentsLiteral { get; set; }

        /// <summary>
        /// Set this to <c>true</c> to hide the verb if more than one element is selected.
        /// </summary>
        /// <remarks>Use this to help avoid running out of resources if the user opens too many files.</remarks>
        [Description("Set this to true to hide the verb if more than one element is selected. Use this to help avoid running out of resources if the user opens too many files.")]
        [XmlAttribute("single-element-only"), DefaultValue(false)]
        public bool SingleElementOnly { get; set; }

        /// <summary>
        /// Set this to <c>true</c> to hide the verb in the Windows context menu unless the Shift key is pressed when opening the menu.
        /// </summary>
        [Description("Set this to true to hide the verb in the Windows context menu unless the Shift key is pressed when opening the menu.")]
        [XmlAttribute("extended"), DefaultValue(false)]
        public bool Extended { get; set; }

        /// <inheritdoc/>
        [Browsable(false)]
        [XmlElement("description")]
        [OrderedEquality]
        public LocalizableStringCollection Descriptions { get; } = new();

        #region Normalize
        /// <summary>
        /// Converts legacy elements, sets default values, etc..
        /// </summary>
        /// <exception cref="InvalidDataException">A required property is not set or invalid.</exception>
        public void Normalize()
            => EnsureAttributeSafeID(Name, "name");
        #endregion

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
            var newVerb = new Verb {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, Command = Command, ArgumentsLiteral = ArgumentsLiteral, SingleElementOnly = SingleElementOnly, Extended = Extended};
            newVerb.Descriptions.AddRange(Descriptions.CloneElements());
            newVerb.Arguments.AddRange(Arguments.CloneElements());
            return newVerb;
        }
        #endregion
    }
}
