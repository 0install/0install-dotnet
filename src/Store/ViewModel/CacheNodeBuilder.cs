// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using NanoByte.Common;
using NanoByte.Common.Collections;
using NanoByte.Common.Tasks;
using ZeroInstall.Store.Feeds;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store.ViewModel
{
    /// <summary>
    /// Builds a list of <see cref="CacheNode"/>s for <see cref="Feed"/>s and <see cref="Implementation"/>s.
    /// </summary>
    public sealed class CacheNodeBuilder : TaskBase
    {
        #region Dependencies
        private readonly IImplementationStore _implementationStore;
        private readonly IFeedCache _feedCache;

        /// <summary>
        /// Creates a new list builder
        /// </summary>
        /// <param name="implementationStore">Used to list <see cref="Implementation"/>s</param>
        /// <param name="feedCache">Used to load <see cref="Feed"/>s.</param>
        public CacheNodeBuilder(IImplementationStore implementationStore, IFeedCache feedCache)
        {
            _implementationStore = implementationStore ?? throw new ArgumentNullException(nameof(implementationStore));
            _feedCache = feedCache ?? throw new ArgumentNullException(nameof(feedCache));
        }
        #endregion

        /// <inheritdoc/>
        public override string Name => "Loading";

        /// <inheritdoc/>
        protected override bool UnitsByte => false;

        /// <summary>
        /// All generated nodes.
        /// </summary>
        public NamedCollection<CacheNode> Nodes { get; private set; }

        /// <summary>
        /// The total size of all <see cref="Implementation"/>s in bytes.
        /// </summary>
        public long TotalSize { get; private set; }

        private IEnumerable<Feed> _feeds;

        /// <inheritdoc/>
        protected override void Execute()
        {
            Nodes = new NamedCollection<CacheNode>();
            _feeds = _feedCache.GetAll();

            foreach (var feed in _feeds)
            {
                feed.Normalize(feed.Uri);
                Add(feed);
            }
            foreach (var digest in _implementationStore.ListAll()) Add(digest);
            foreach (string path in _implementationStore.ListAllTemp()) Add(path);
        }

        private void Add(Feed feed) => Add(new FeedNode(feed, _feedCache));

        private void Add(ManifestDigest digest)
        {
            try
            {
                var implementation = _feeds.GetImplementation(digest, out var feed);

                ImplementationNode implementationNode;
                if (implementation == null || feed == null) implementationNode = new OrphanedImplementationNode(digest, _implementationStore);
                else implementationNode = new OwnedImplementationNode(digest, implementation, new FeedNode(feed, _feedCache), _implementationStore);

                TotalSize += implementationNode.Size;
                Add(implementationNode);
            }
            #region Error handling
            catch (FormatException ex)
            {
                Log.Error($"Problem processing the manifest file for '{digest}'.");
                Log.Error(ex);
            }
            catch (IOException ex)
            {
                Log.Error($"Problem processing '{digest}'.");
                Log.Error(ex);
            }
            catch (UnauthorizedAccessException ex)
            {
                Log.Error($"Problem processing '{digest}'.");
                Log.Error(ex);
            }
            #endregion
        }

        private void Add(string path) => Add(new TempDirectoryNode(path, _implementationStore));

        private void Add(CacheNode entry)
        {
            // Avoid name collisions by incrementing suffix
            while (Nodes.Contains(entry.Name)) entry.SuffixCounter++;

            Nodes.Add(entry);
        }
    }
}
