// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Generator.Equals;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Copies files or directories from another implementation specified elsewhere in the same feed.
    /// </summary>
    [Description("Copies files or directories from another implementation specified elsewhere in the same feed.")]
    [Serializable, XmlRoot("copy-from", Namespace = Feed.XmlNamespace), XmlType("copy-from", Namespace = Feed.XmlNamespace)]
    [Equatable]
    public sealed partial class CopyFromStep : FeedElement, IRecipeStep
    {
        /// <summary>
        /// The <see cref="ImplementationBase.ID"/> of the <see cref="Implementation"/> to copy from.
        /// </summary>
        [Description("The ID of the implementation to copy from.")]
        [XmlAttribute("id"), DefaultValue("")]
        public string? ID { get; set; }

        /// <summary>
        /// The source file or directory relative to the source implementation root as a Unix-style path; <c>null</c> or <see cref="string.Empty"/> for entire implementation.
        /// </summary>
        [Description("The source file or directory relative to the source implementation root as a Unix-style path; unset or empty for entire implementation.")]
        [XmlAttribute("source"), DefaultValue("")]
        public string? Source { get; set; }

        /// <summary>
        /// The destination file or directory relative to the implementation root as a Unix-style path; <c>null</c> for top-level. Must be set if <see cref="Source"/> points to a file.
        /// </summary>
        [Description("The destination file or directory relative to the implementation root as a Unix-style path; unset or empty for top-level. Must be set if Source points to a file.")]
        [XmlAttribute("dest"), DefaultValue("")]
        public string? Destination { get; set; }

        /// <summary>
        /// Used to hold the <see cref="Implementation"/> the <see cref="ID"/> references after <see cref="Feed.Normalize"/> has been executed.
        /// </summary>
        [XmlIgnore, Browsable(false)]
        public Implementation? Implementation { get; set; }

        #region Normalize
        /// <inheritdoc/>
        public void Normalize(FeedUri? feedUri = null) {}
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the copy-from step in the form "Copy from ID (Source => Destination)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"Copy from {ID} ({Source} => {Destination})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="CopyFromStep"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="CopyFromStep"/>.</returns>
        // ReSharper disable once ConstantConditionalAccessQualifier
        public IRecipeStep Clone() => new CopyFromStep {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, ID = ID, Implementation = Implementation?.CloneImplementation(), Source = Source, Destination = Destination};
        #endregion
    }
}
