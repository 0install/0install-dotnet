// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml.Serialization;

namespace ZeroInstall.DesktopIntegration.AccessPoints;

/// <summary>
/// Creates some form of icon in the desktop environment.
/// </summary>
[XmlType("icon-access-point", Namespace = AppList.XmlNamespace)]
public abstract class IconAccessPoint : CommandAccessPoint
{}