// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using ZeroInstall.Model.Design;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Associates a <see cref="Command"/> with a user-friendly name and description.
    /// </summary>
    /// <seealso cref="Feed.EntryPoints"/>
    [Description("Associates a command with a user-friendly name and description.")]
    [Serializable, XmlRoot("entry-point", Namespace = Feed.XmlNamespace), XmlType("entry-point", Namespace = Feed.XmlNamespace)]
    public sealed class EntryPoint : FeedElement, IIconContainer, ISummaryContainer, ICloneable<EntryPoint>, IEquatable<EntryPoint>
    {
        /// <summary>
        /// The name of the <see cref="Command"/> this entry point represents.
        /// </summary>
        [Description("The name of the command this entry point represents.")]
        [TypeConverter(typeof(CommandNameConverter))]
        [XmlAttribute("command")]
        public string Command { get; set; } = default!;

        /// <summary>
        /// The canonical name of the binary supplying the command (without file extensions). This is used to suggest suitable alias names.
        /// </summary>
        /// <remarks>Will default to <see cref="Command"/> when left <c>null</c>.</remarks>
        [Description("The canonical name of the binary supplying the command (without file extensions). This is used to suggest suitable alias names.")]
        [XmlAttribute("binary-name"), DefaultValue("")]
        public string? BinaryName { get; set; }

        /// <summary>
        /// The Application User Model ID; used by Windows to associate shortcuts and pinned taskbar entries with running processes.
        /// </summary>
        [Description("The Application User Model ID; used by Windows to associate shortcuts and pinned taskbar entries with running processes.")]
        [XmlAttribute("app-id")]
        public string? AppId { get; set; }

        /// <summary>
        /// If <c>true</c>, indicates that the <see cref="Command"/> represented by this entry point requires a terminal in order to run.
        /// </summary>
        [Description("If true, indicates that the Command represented by this entry point requires a terminal in order to run.")]
        [XmlIgnore, DefaultValue(false)]
        public bool NeedsTerminal { get; set; }

        /// <summary>
        /// If <c>true</c>, indicates that this entry point should be offered as an auto-start candidate to the user.
        /// </summary>
        [Description("If true, indicates that this entry point should be offered as an auto-start candidate to the user.")]
        [XmlIgnore, DefaultValue(false)]
        public bool SuggestAutoStart { get; set; }

        /// <summary>
        /// If <c>true</c>, indicates that this entry point should be offered as a candidate for the "Send To" context menu to the user.
        /// </summary>
        [Description("If true, indicates that this entry point should be offered as a candidate for the \"Send To\" context menu to the user.")]
        [XmlIgnore, DefaultValue(false)]
        public bool SuggestSendTo { get; set; }

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="NeedsTerminal"/>
        [XmlElement("needs-terminal"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string? NeedsTerminalString { get => (NeedsTerminal ? "" : null); set => NeedsTerminal = (value != null); }

        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="SuggestAutoStart"/>
        [XmlElement("suggest-auto-start"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string? SuggestAutoStartString { get => (SuggestAutoStart ? "" : null); set => SuggestAutoStart = (value != null); }

        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="SuggestSendTo"/>
        [XmlElement("suggest-send-to"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string? SuggestSendToString { get => (SuggestSendTo ? "" : null); set => SuggestSendTo = (value != null); }
        #endregion

        /// <summary>
        /// User-friendly names for the command. If not present, <see cref="Command"/> is used instead.
        /// </summary>
        [Browsable(false)]
        [XmlElement("name")]
        public LocalizableStringCollection Names { get; } = new();

        /// <inheritdoc/>
        [Browsable(false)]
        [XmlElement("summary")]
        public LocalizableStringCollection Summaries { get; } = new();

        /// <inheritdoc/>
        [Browsable(false)]
        [XmlElement("description")]
        public LocalizableStringCollection Descriptions { get; } = new();

        /// <summary>
        /// Zero or more icons representing the command. Used for desktop icons, menu entries, etc..
        /// </summary>
        [Browsable(false)]
        [XmlElement("icon")]
        public List<Icon> Icons { get; } = new();

        #region Normalize
        /// <summary>
        /// Converts legacy elements, sets default values and ensures required elements.
        /// </summary>
        /// <exception cref="InvalidDataException">One or more required elements are not set.</exception>
        public void Normalize()
            => EnsureTag(Command, "command");
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the EntryPoint in the form "Command (BinaryName)". Not safe for parsing!
        /// </summary>
        public override string ToString() => string.IsNullOrEmpty(BinaryName)
            ? Command
            : Command + " (" + BinaryName + ")";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="EntryPoint"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="EntryPoint"/>.</returns>
        public EntryPoint Clone()
        {
            var newEntryPoint = new EntryPoint {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Command = Command, BinaryName = BinaryName, NeedsTerminal = NeedsTerminal};
            newEntryPoint.Names.AddRange(Names.CloneElements());
            newEntryPoint.Summaries.AddRange(Summaries.CloneElements());
            newEntryPoint.Descriptions.AddRange(Descriptions.CloneElements());
            newEntryPoint.Icons.AddRange(Icons);
            return newEntryPoint;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(EntryPoint? other)
            => other != null
            && base.Equals(other)
            && Command == other.Command
            && BinaryName == other.BinaryName
            && AppId == other.AppId
            && NeedsTerminal == other.NeedsTerminal
            && Names.SequencedEquals(other.Names)
            && Summaries.SequencedEquals(other.Summaries)
            && Descriptions.SequencedEquals(other.Descriptions)
            && Icons.SequencedEquals(other.Icons);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is EntryPoint point && Equals(point);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(base.GetHashCode());
            hash.Add(Command);
            hash.Add(BinaryName);
            hash.Add(AppId);
            hash.Add(NeedsTerminal);
            hash.Add(Names.GetSequencedHashCode());
            hash.Add(Summaries.GetSequencedHashCode());
            hash.Add(Descriptions.GetSequencedHashCode());
            hash.Add(Icons.GetSequencedHashCode());
            return hash.ToHashCode();
        }
        #endregion
    }
}
