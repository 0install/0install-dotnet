// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Xml.Serialization;
using NanoByte.Common;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model
{
    /// <summary>
    /// An implementation is a specific version of an application that can be downloaded and executed (e.g. Firefox 3.6 for Windows).
    /// </summary>
    /// <seealso cref="Feed.Elements"/>
    [Description("An implementation is a specific version of an application that can be downloaded and executed (e.g. Firefox 3.6 for Windows).")]
    [Serializable, XmlRoot("implementation", Namespace = Feed.XmlNamespace), XmlType("implementation", Namespace = Feed.XmlNamespace)]
    public class Implementation : ImplementationBase, IEquatable<Implementation>
    {
        /// <inheritdoc/>
        internal override IEnumerable<Implementation> Implementations => new[] {this};

        /// <summary>
        /// A list of <see cref="RetrievalMethod"/>s for downloading the implementation.
        /// </summary>
        [Browsable(false)]
        [XmlElement(typeof(Archive)), XmlElement(typeof(SingleFile)), XmlElement(typeof(Recipe))]
        public List<RetrievalMethod> RetrievalMethods { get; } = new();

        #region Normalize
        /// <summary>
        /// Sets missing default values and handles legacy elements.
        /// </summary>
        /// <param name="feedUri">The feed the data was originally loaded from.</param>
        /// <remarks>This method should be called to prepare a <see cref="Feed"/> for solver processing. Do not call it if you plan on serializing the feed again since it may loose some of its structure.</remarks>
        public override void Normalize(FeedUri? feedUri = null)
        {
            base.Normalize(feedUri);

            // Apply if-0install-version filter
            RetrievalMethods.RemoveAll(FilterMismatch);

            var toRemove = new List<RetrievalMethod>();
            foreach (var retrievalMethod in RetrievalMethods)
            {
                try
                {
                    retrievalMethod.Normalize(feedUri);
                }
                #region Error handling
                catch (UriFormatException ex)
                {
                    Log.Error(ex);
                    toRemove.Add(retrievalMethod);
                }
                #endregion
            }
            RetrievalMethods.RemoveRange(toRemove);
        }
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Implementation"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Implementation"/>.</returns>
        public Implementation CloneImplementation()
        {
            var implementation = new Implementation();
            CloneFromTo(this, implementation);
            implementation.RetrievalMethods.AddRange(RetrievalMethods.CloneElements());
            return implementation;
        }

        /// <summary>
        /// Creates a deep copy of this <see cref="Implementation"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Implementation"/>.</returns>
        public override Element Clone() => CloneImplementation();
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Implementation? other)
            => other != null && base.Equals(other) && RetrievalMethods.SequencedEquals(other.RetrievalMethods);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj.GetType() != typeof(Implementation)) return false;
            return Equals((Implementation)obj);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), RetrievalMethods.GetSequencedHashCode());
        #endregion
    }
}
