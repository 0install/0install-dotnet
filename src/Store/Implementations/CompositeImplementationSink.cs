// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Threading;
using ZeroInstall.Store.FileSystem;

#if NETFRAMEWORK
using System.Runtime.Remoting;
#endif

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Combines multiple <see cref="IImplementationSink"/>s as a composite.
/// </summary>
/// <remarks>
///   <para>When adding new <see cref="Implementation"/>s the last child <see cref="IImplementationSink"/> that doesn't throw an <see cref="UnauthorizedAccessException"/> is used.</para>
/// </remarks>
public class CompositeImplementationSink : MarshalNoTimeout, IImplementationSink
{
    private readonly IReadOnlyList<IImplementationSink> _sinks;

    /// <summary>
    /// Creates a new composite implementation sink with a set of <see cref="IImplementationSink"/>s.
    /// </summary>
    /// <param name="sinks">
    ///   A priority-sorted list of <see cref="IImplementationSink"/>s.
    ///   Queried last-to-first for adding new <see cref="Implementation"/>.
    /// </param>
    public CompositeImplementationSink(IReadOnlyList<IImplementationSink> sinks)
    {
        _sinks = sinks ?? throw new ArgumentNullException(nameof(sinks));
    }

    /// <inheritdoc/>
    public bool Contains(ManifestDigest manifestDigest)
        => _sinks.Any(x => x.Contains(manifestDigest));

    /// <inheritdoc />
    public void Add(ManifestDigest manifestDigest, Action<IBuilder> build)
    {
        #region Sanity checks
        if (build == null) throw new ArgumentNullException(nameof(build));
        #endregion

        if (Contains(manifestDigest)) throw new ImplementationAlreadyInStoreException(manifestDigest);

        // Find the last sink the implementation can be added to (some might be write-protected)
        Exception? innerException = null;
        foreach (var sink in _sinks.Reverse())
        {
            try
            {
                // Try to add implementation to this sink
                sink.Add(manifestDigest, build);
                return;
            }
            #region Error handling
            catch (IOException ex)
            {
                innerException = ex; // Remember the last error
            }
            catch (UnauthorizedAccessException ex)
            {
                innerException = ex; // Remember the last error
            }
#if NETFRAMEWORK
            catch (RemotingException ex)
            {
                innerException = ex; // Remember the last error
            }
#endif
            #endregion
        }

        // If we reach this, the implementation could not be added to any sink
        innerException?.Rethrow();
    }
}
