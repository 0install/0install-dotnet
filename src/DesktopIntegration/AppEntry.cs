// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Dispatch;
using NanoByte.Common.Storage;
using ZeroInstall.DesktopIntegration.AccessPoints;
using ZeroInstall.DesktopIntegration.Properties;
using ZeroInstall.Model;
using ZeroInstall.Model.Capabilities;

namespace ZeroInstall.DesktopIntegration
{
    /// <summary>
    /// Represents an application in the <see cref="AppList"/> identified by its interface URI.
    /// </summary>
    [XmlType("app", Namespace = AppList.XmlNamespace)]
    public sealed class AppEntry : XmlUnknown, IMergeable<AppEntry>, ICloneable<AppEntry>
    {
        /// <summary>
        /// The URI or local path of the interface defining the application or the pet-name if <see cref="Requirements"/> is set.
        /// </summary>
        [Description("The URI or local path of the interface defining the application or the pet-name if Requirements is set.")]
        [XmlIgnore]
        public FeedUri InterfaceUri { get; set; }

        string IMergeable<AppEntry>.MergeID => InterfaceUri.ToStringRfc();

        /// <summary>
        /// The name of the application. Usually equal to <see cref="Feed.Name"/>.
        /// </summary>
        [Description("The name of the application. Usually equal to the Name specified in the Feed.")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// Set to <c>true</c> to automatically download the newest available version of the application as a regular background task. Update checks will still be performed when the application is launched when set to <c>false</c>.
        /// </summary>
        [Description("Set to true to automatically download the newest available version of the application as a regular background task. Update checks will still be performed when the application is launched when set to false.")]
        [XmlAttribute("auto-update"), DefaultValue(true)]
        public bool AutoUpdate { get; set; } = true;

        /// <summary>
        /// A regular expression a computer's hostname must match for this entry to be applied. Enables machine-specific entry filtering.
        /// </summary>
        [Description("A regular expression a computer's hostname must match for this entry to be applied. Enables machine-specific entry filtering.")]
        [XmlAttribute("hostname"), DefaultValue("")]
        public string Hostname { get; set; }

        /// <summary>
        /// A set of requirements/restrictions imposed by the user on the implementation selection process.
        /// </summary>
        [Description("A set of requirements/restrictions imposed by the user on the implementation selection process.")]
        [XmlIgnore]
        public Requirements Requirements { get; set; }

        /// <summary>
        /// The <see cref="Requirements"/> if it is set, otherwise a basic reference to <see cref="InterfaceUri"/>.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public Requirements EffectiveRequirements => Requirements ?? new Requirements(InterfaceUri);

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="InterfaceUri"/>
        [SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings", Justification = "Used for XML serialization")]
        [XmlAttribute("interface"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string InterfaceUriString { get => InterfaceUri?.ToStringRfc(); set => InterfaceUri = string.IsNullOrEmpty(value) ? null : new FeedUri(value); }

        /// <summary>Used for XML+JSON serialization.</summary>
        /// <seealso cref="Requirements"/>
        [XmlElement("requirements-json"), Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        public string RequirementsJson { get => Requirements?.ToJsonString(); set => Requirements = JsonStorage.FromJsonString<Requirements>(value); }
        #endregion

        /// <summary>
        /// A set of <see cref="Capability"/> lists to be registered in the desktop environment. Only compatible architectures are handled.
        /// </summary>
        [Browsable(false)]
        [XmlElement("capabilities", Namespace = CapabilityList.XmlNamespace)]
        public List<CapabilityList> CapabilityLists { get; } = new();

        /// <summary>
        /// A set of <see cref="AccessPoints"/>s to be registered in the desktop environment. Is <c>null</c> if no desktop integration has been performed yet.
        /// </summary>
        [Description("A set of AccessPoints to be registered in the desktop environment. Is null if no desktop integration has been performed yet.")]
        [XmlElement("access-points")]
        public AccessPointList AccessPoints { get; set; }

        /// <inheritdoc/>
        [Browsable(false)]
        [XmlIgnore]
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// The time this entry was last modified encoded as Unix time (number of seconds since the epoch).
        /// </summary>
        /// <remarks>This value is ignored by clone and equality methods.</remarks>
        [Browsable(false)]
        [XmlAttribute("timestamp"), DefaultValue(0)]
        public long TimestampUnix { get => Timestamp.ToUnixTime(); set => Timestamp = FileUtils.FromUnixTime(value); }

        /// <summary>
        /// Retrieves the first <see cref="Capability"/> that matches a specific type and ID and is compatible with <see cref="Architecture.CurrentSystem"/>.
        /// </summary>
        /// <typeparam name="T">The capability type to match.</typeparam>
        /// <param name="id">The <see cref="Capability.ID"/> to match.</param>
        /// <returns>The first matching <see cref="Capability"/>.</returns>
        /// <exception cref="KeyNotFoundException">No capability matching <paramref name="id"/> and <typeparamref name="T"/> was found.</exception>
        public T LookupCapability<T>(string id) where T : Capability
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            #endregion

            try
            {
                return CapabilityLists.CompatibleCapabilities()
                                      .OfType<T>()
                                      .First(specificCapability => specificCapability.ID == id);
            }
            #region Error handling
            catch (InvalidOperationException)
            {
                throw new KeyNotFoundException(string.Format(Resources.UnableToFindTypeID, typeof(T).Name, id));
            }
            #endregion
        }

        #region Conversion
        /// <summary>
        /// Returns the entry in the form "Name (InterfaceUri)". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Name} ({InterfaceUri})";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="AppEntry"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="AppEntry"/>.</returns>
        public AppEntry Clone()
        {
            var appList = new AppEntry {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, Name = Name, InterfaceUri = InterfaceUri};
            if (Requirements != null) appList.Requirements = Requirements.Clone();
            if (AccessPoints != null) appList.AccessPoints = AccessPoints.Clone();
            appList.CapabilityLists.AddRange(CapabilityLists.CloneElements());

            return appList;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(AppEntry other)
            => other != null
            && base.Equals(other)
            && InterfaceUri == other.InterfaceUri
            && Name == other.Name
            && AutoUpdate == other.AutoUpdate
            && Equals(Requirements, other.Requirements)
            && CapabilityLists.SequencedEquals(other.CapabilityLists)
            && Equals(AccessPoints, other.AccessPoints);

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is AppEntry entry && Equals(entry);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                InterfaceUri,
                Name,
                AutoUpdate,
                Requirements,
                CapabilityLists.GetSequencedHashCode(),
                AccessPoints);
        #endregion
    }
}
