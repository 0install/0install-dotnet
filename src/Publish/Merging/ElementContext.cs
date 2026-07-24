// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Xml;

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// Everything an <see cref="Element"/> provides to itself and its children, including inherited values.
/// </summary>
internal sealed class ElementContext
{
    /// <summary>
    /// Attributes keyed by their XML name. Values are boxed property values or <see cref="XmlAttribute"/>s.
    /// </summary>
    public Dictionary<string, object> Attributes { get; } = new();

    /// <summary>
    /// All <c>&lt;requires&gt;</c> and <c>&lt;restricts&gt;</c> from ancestor <see cref="Group"/>s.
    /// </summary>
    public List<Restriction> Restrictions { get; } = [];

    /// <summary>
    /// All <c>&lt;command&gt;</c>s from ancestor <see cref="Group"/>s.
    /// </summary>
    public Dictionary<CommandKey, Command> Commands { get; } = new();

    /// <summary>
    /// Indicates whether both a <c>main</c> attribute and a <c>&lt;command name="run"&gt;</c> are present. This case requires special care.
    /// </summary>
    public bool HasMainAndRun
        => Attributes.ContainsKey(ElementAttribute.MainName) && Commands.Keys.Any(x => x.Name == Command.NameRun);
}

/// <summary>
/// Identifies a <see cref="Command"/> by its name and version filter, so that commands can be matched across feeds.
/// </summary>
/// <param name="Name">The <see cref="Command.Name"/>.</param>
/// <param name="IfZeroInstallVersion">The <see cref="FeedElement.IfZeroInstallVersion"/> range as a string; <c>null</c> if the command applies to all versions.</param>
internal sealed record CommandKey(string Name, string? IfZeroInstallVersion);
