// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml.Serialization;
using Generator.Equals;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Store.Icons;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// A mock access point that does nothing (used for testing). Points to a <see cref="Model.Capabilities.FileType"/>.
/// </summary>
[XmlType("mock", Namespace = AppList.XmlNamespace)]
[Equatable]
public partial class MockAccessPoint : DefaultAccessPoint
{
    /// <inheritdoc/>
    public override IEnumerable<string> GetConflictIDs(AppEntry appEntry) => string.IsNullOrEmpty(ID)
        ? Enumerable.Empty<string>()
        : new[] {$"mock:{ID}"};

    /// <summary>
    /// An identifier that controls the result of <see cref="GetConflictIDs"/>.
    /// </summary>
    [XmlAttribute("id")]
    public string? ID { get; set; }

    /// <summary>
    /// The path to a file to create when <see cref="Apply"/> is called.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag"), XmlAttribute("apply-flag-path")]
    public string? ApplyFlagPath { get; set; }

    /// <summary>
    /// The path to a file to create when <see cref="Unapply"/> is called.
    /// </summary>
    [SuppressMessage("Microsoft.Naming", "CA1726:UsePreferredTerms", MessageId = "Flag"), XmlAttribute("unapply-flag-path")]
    public string? UnapplyFlagPath { get; set; }

    /// <inheritdoc/>
    public override void Apply(AppEntry appEntry, Feed feed, IIconStore iconStore, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        if (!string.IsNullOrEmpty(ID))
        {
            // Trigger exceptions in case invalid capabilities are referenced
            appEntry.LookupCapability<Model.Capabilities.FileType>(Capability);
        }

        if (!string.IsNullOrEmpty(ApplyFlagPath)) FileUtils.Touch(ApplyFlagPath);
    }

    /// <inheritdoc/>
    public override void Unapply(AppEntry appEntry, bool machineWide)
    {
        #region Sanity checks
        if (appEntry == null) throw new ArgumentNullException(nameof(appEntry));
        #endregion

        if (!string.IsNullOrEmpty(ID))
        {
            // Trigger exceptions in case invalid capabilities are referenced
            appEntry.LookupCapability<Model.Capabilities.FileType>(Capability);
        }

        if (!string.IsNullOrEmpty(UnapplyFlagPath)) FileUtils.Touch(UnapplyFlagPath);
    }

    #region Conversion
    /// <summary>
    /// Returns the access point in the form "MockAccessPoint: ID". Not safe for parsing!
    /// </summary>
    public override string ToString() => $"MockAccessPoint: {ID} (ApplyFlagPath: {ApplyFlagPath}, UnapplyFlagPath: {UnapplyFlagPath})";
    #endregion

    #region Clone
    /// <inheritdoc/>
    public override AccessPoint Clone() => new MockAccessPoint
    {
        ID = ID,
        Capability = Capability,
        ApplyFlagPath = ApplyFlagPath,
        UnapplyFlagPath = UnapplyFlagPath,
        UnknownAttributes = UnknownAttributes,
        UnknownElements = UnknownElements
    };
    #endregion
}
