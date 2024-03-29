// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Values.Design;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Store.ViewModel;

/// <summary>
/// Models information about an implementation in an <see cref="IImplementationStore"/> with a known owning interface for display in a UI.
/// </summary>
public sealed class OwnedImplementationNode : ImplementationNode
{
    private readonly Implementation _implementation;
    private readonly FeedNode _parent;

    /// <summary>
    /// Creates a new owned implementation node.
    /// </summary>
    /// <param name="path">The path of the directory.</param>
    /// <param name="implementation">Information about the implementation from a <see cref="Feed"/> file.</param>
    /// <param name="parent">The node of the feed owning the implementation.</param>
    /// <exception cref="IOException">The manifest file could not be read.</exception>
    /// <exception cref="UnauthorizedAccessException">Read access to the manifest file is not permitted.</exception>
    /// <exception cref="FormatException">The manifest file is not valid.</exception>
    public OwnedImplementationNode(string path, Implementation implementation, FeedNode parent)
        : base(path, implementation.ManifestDigest)
    {
        _parent = parent ?? throw new ArgumentNullException(nameof(parent));
        _implementation = implementation ?? throw new ArgumentNullException(nameof(implementation));
    }

    /// <inheritdoc/>
    public override string Name { get => _parent.Name + Named.TreeSeparator + Version + (SuffixCounter == 0 ? "" : $" {SuffixCounter}"); set => throw new NotSupportedException(); }

    /// <inheritdoc/>
    public override FeedUri FeedUri => _parent.Uri;

    /// <summary>
    /// The version number of the implementation.
    /// </summary>
    [Description("The version number of the implementation.")]
    public ImplementationVersion Version => _implementation.Version;

    /// <summary>
    /// The version number of the implementation.
    /// </summary>
    [Description("The version number of the implementation.")]
    [TypeConverter(typeof(StringConstructorConverter<Architecture>))]
    public Architecture Architecture => _implementation.Architecture;

    /// <summary>
    /// A unique identifier for the implementation. Used when storing implementation-specific user preferences.
    /// </summary>
    [Description("A unique identifier for the implementation. Used when storing implementation-specific user preferences.")]
    public string ID => _implementation.ID;

    /// <summary>
    /// Creates string representation suitable for console output.
    /// </summary>
    public override string ToString()
        => $"{FeedUri.ToStringRfc()} [{Version}, {Architecture}] - {base.ToString()}";
}
