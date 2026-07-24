// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Undo;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Publish;

/// <summary>
/// Helpers for editing <see cref="Feed"/>s.
/// </summary>
/// <remarks>
/// These operate on the un-normalized feed structure, i.e. <see cref="Group"/> inheritance is resolved
/// on the fly rather than by calling <see cref="Feed.Normalize"/>. This keeps the feed serializable.
/// </remarks>
public static class FeedExtensions
{
    /// <summary>
    /// Adds a new <see cref="Implementation"/> with a local <see cref="ImplementationBase.ID"/> to the feed.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="version">The version number of the new implementation.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <returns>The newly added implementation.</returns>
    /// <remarks>The implementation is placed next to the last existing one, inside the same <see cref="Group"/> (if any).</remarks>
    public static Implementation AddVersion(this Feed feed, ImplementationVersion version, ICommandExecutor executor)
    {
        #region Sanity checks
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        if (version == null) throw new ArgumentNullException(nameof(version));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        #endregion

        var implementation = new Implementation {ID = ".", Version = version};
        executor.Execute(AddToCollection.For(
            (ContainerOfLastImplementation(feed) ?? feed).Elements,
            implementation));
        return implementation;
    }

    private static IElementContainer? ContainerOfLastImplementation(IElementContainer container)
    {
        foreach (var element in container.Elements.AsEnumerable().Reverse())
        {
            switch (element)
            {
                case Implementation:
                    return container;
                case Group group when ContainerOfLastImplementation(group) is {} result:
                    return result;
            }
        }
        return null;
    }

    /// <summary>
    /// Determines which <see cref="Implementation"/>s in the feed a <c>--set-*</c> style operation should be applied to.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="version">The version number to look for; <c>null</c> to look for the implementation that has not been released yet.</param>
    /// <returns>One or more implementations. Never empty.</returns>
    /// <exception cref="InvalidDataException">No matching implementation was found or the selection was ambiguous.</exception>
    public static IReadOnlyList<Implementation> SelectImplementations(this Feed feed, ImplementationVersion? version = null)
    {
        #region Sanity checks
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        var candidates = feed.Walk().ToList();
        if (candidates.Count == 0) throw new InvalidDataException(Resources.NoImplementations);

        if (version != null)
        {
            var matches = candidates.Where(x => version.Equals(x.Version))
                                    .Select(x => x.Implementation)
                                    .ToList();
            return matches.Count == 0
                ? throw new InvalidDataException(string.Format(Resources.NoImplementationWithVersion, version))
                : matches;
        }

        // Default to the implementation that has not been released yet
        var unreleased = candidates.Where(x => x.Released == null)
            .Select(x => x.Implementation)
            .ToList();
        return unreleased switch
        {
            [] when candidates is [var single] => [single.Implementation],
            [] => throw new InvalidDataException(Resources.AllImplementationsReleased),
            [var single] => [single],
            _ => throw new InvalidDataException(Resources.MultipleUnreleasedImplementations)
        };
    }

    /// <summary>
    /// Marks the latest <see cref="Stability.Testing"/> <see cref="Implementation"/>(s) in the feed as <see cref="Stability.Stable"/>.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <exception cref="InvalidDataException">No implementation is currently testing.</exception>
    public static void MarkStable(this Feed feed, ICommandExecutor executor)
    {
        #region Sanity checks
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        if (executor == null) throw new ArgumentNullException(nameof(executor));
        #endregion

        // Implementations without an explicit stability default to testing
        var testing = feed.Walk()
            .Where(x => x.Stability is Stability.Unset or Stability.Testing)
            .ToList();
        if (testing.Count == 0) throw new InvalidDataException(Resources.NoTestingImplementations);

        var latestVersion = testing.Max(x => x.Version);
        var latest = testing.Where(x => Equals(x.Version, latestVersion)).ToList();
        if (latest.Count < testing.Count)
            Log.Warn(string.Format(Resources.MultipleTestingVersions, latest.Count, testing.Count, latestVersion));

        foreach (var candidate in latest)
            executor.Execute(SetValueCommand.For(() => candidate.Implementation.Stability, newValue: Stability.Stable));
    }

    /// <summary>
    /// Adds an <see cref="Archive"/> to the matching <see cref="Implementation"/> in the feed.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="href">The URL the archive can be downloaded from.</param>
    /// <param name="localPath">A local copy of the archive; <c>null</c> to look for a file named like <paramref name="href"/> in the current directory.</param>
    /// <param name="extract">The subdirectory of the archive to extract; <c>null</c> for entire archive.</param>
    /// <param name="formats">The manifest formats to calculate digests for. Leave empty for default.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <remarks>
    /// The archive is added to the implementation that already carries the calculated digest as its
    /// <see cref="ImplementationBase.ID"/>, or else to the feed's only implementation with a local ID.
    /// </remarks>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="FileNotFoundException">No local copy of the archive could be found.</exception>
    /// <exception cref="InvalidDataException">No suitable implementation to add the archive to was found.</exception>
    /// <exception cref="NotSupportedException">The archive type could not be determined.</exception>
    public static void AddArchive(this Feed feed, Uri href, string? localPath, string? extract, IEnumerable<ManifestFormat>? formats, ICommandExecutor executor, ITaskHandler handler)
    {
        #region Sanity checks
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        var (archive, digest) = ImplementationExtensions.BuildArchive(href, localPath, extract, formats, executor, handler);
        feed.FindArchiveTarget(digest).AddArchive(archive, digest, executor);
    }

    private static Implementation FindArchiveTarget(this Feed feed, ManifestDigest digest)
    {
        var implementations = feed.Implementations.ToList();

        if (implementations.FirstOrDefault(x => digest.AvailableDigests.Contains(x.ID)) is {} match)
            return match;

        var local = implementations.Where(x => x.ID.StartsWith(".") || x.ID.StartsWith("/")).ToList();
        return local switch
        {
            [var single] => single,
            [] => throw new InvalidDataException(string.Format(Resources.NoLocalImplementation, digest.Best)),
            _ => throw new InvalidDataException(string.Format(Resources.MultipleLocalImplementations, digest.Best))
        };
    }

    /// <summary>
    /// Adds digests in an additional <see cref="ManifestFormat"/> to all <see cref="Implementation"/>s in the feed that have a cached copy.
    /// </summary>
    /// <param name="feed">The feed.</param>
    /// <param name="format">The manifest format to add digests for.</param>
    /// <param name="implementationStore">Used to look for cached copies of the implementations.</param>
    /// <param name="executor">Used to modify properties in an undoable fashion.</param>
    /// <param name="handler">A callback object used when the user is to be informed about progress.</param>
    /// <exception cref="OperationCanceledException">The user canceled the task.</exception>
    /// <exception cref="DigestMismatchException">A cached implementation does not match its expected digest.</exception>
    public static void AddDigests(this Feed feed, ManifestFormat format, IImplementationStore implementationStore, ICommandExecutor executor, ITaskHandler handler)
    {
        #region Sanity checks
        if (feed == null) throw new ArgumentNullException(nameof(feed));
        #endregion

        foreach (var implementation in feed.Implementations.ToList())
            implementation.AddDigest(format, implementationStore, executor, handler);
    }

    /// <summary>
    /// An <see cref="Implementation"/> along with the values it effectively has after resolving <see cref="Group"/> inheritance.
    /// </summary>
    private sealed record Candidate(Implementation Implementation, ImplementationVersion? Version, string? Released, Stability Stability);

    /// <summary>
    /// Recursively enumerates all <see cref="Implementation"/>s in a container, resolving <see cref="Group"/> inheritance.
    /// </summary>
    private static IEnumerable<Candidate> Walk(
        this IElementContainer container,
        ImplementationVersion? version = null,
        string? released = null,
        Stability stability = Stability.Unset)
    {
        foreach (var element in container.Elements)
        {
            var elementVersion = element.Version ?? version;
            string? elementReleased = element.ReleasedString ?? released;
            var elementStability = element.Stability == Stability.Unset ? stability : element.Stability;

            switch (element)
            {
                case Implementation implementation:
                    yield return new(implementation, elementVersion, elementReleased, elementStability);
                    break;

                case Group group:
                    foreach (var result in group.Walk(elementVersion, elementReleased, elementStability))
                        yield return result;
                    break;
            }
        }
    }
}
