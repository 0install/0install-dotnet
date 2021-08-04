// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using NanoByte.Common.Collections;

namespace ZeroInstall.Model
{
    /// <summary>
    /// Retrieves an implementation by applying a list of <see cref="IRecipeStep"/>s, such as downloading and combining multiple archives.
    /// </summary>
    [Description("Retrieves an implementation by applying a list of recipe steps, such as downloading and combining multiple archives.")]
    [Serializable, XmlRoot("recipe", Namespace = Feed.XmlNamespace), XmlType("recipe", Namespace = Feed.XmlNamespace)]
    public sealed class Recipe : RetrievalMethod, IEquatable<Recipe>
    {
        /// <summary>
        /// An ordered list of <see cref="IRecipeStep"/>s to execute.
        /// </summary>
        [Description("An ordered list of archives to extract.")]
        [XmlIgnore]
        public List<IRecipeStep> Steps { get; } = new();

        #region XML serialization
        /// <summary>Used for XML serialization.</summary>
        /// <seealso cref="Steps"/>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Used for XML serialization.")]
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden), EditorBrowsable(EditorBrowsableState.Never)]
        [XmlElement(typeof(Archive)), XmlElement(typeof(SingleFile)), XmlElement(typeof(RenameStep)), XmlElement(typeof(RemoveStep)), XmlElement(typeof(CopyFromStep))]
        public object[]? StepsArray
        {
            get => Steps.Cast<object>().ToArray();
            set
            {
                Steps.Clear();
                if (value is {Length: > 0}) Steps.AddRange(value.OfType<IRecipeStep>());
            }
        }
        #endregion

        /// <summary>
        /// Indicates whether this recipe contains steps of unknown type and therefore can not be processed.
        /// </summary>
        [Browsable(false)]
        [XmlIgnore]
        public bool ContainsUnknownSteps => UnknownElements is {Length: > 0};

        #region Normalize
        /// <summary>
        /// Converts legacy elements, sets default values and ensures required elements.
        /// </summary>
        /// <param name="feedUri">The feed the data was originally loaded from.</param>
        /// <exception cref="UriFormatException"><see cref="DownloadRetrievalMethod.Href"/> is relative and <paramref name="feedUri"/> is a remote URI.</exception>
        /// <exception cref="InvalidDataException">One or more required elements are not set.</exception>
        public override void Normalize(FeedUri? feedUri = null)
        {
            base.Normalize(feedUri);

            // Apply if-0install-version filter
            Steps.RemoveAll(FilterMismatch);

            foreach (var step in Steps)
                step.Normalize(feedUri);
        }
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the recipe in the form "X steps". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Steps.Count} steps";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="Recipe"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="Recipe"/>.</returns>
        public override RetrievalMethod Clone()
        {
            var recipe = new Recipe {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements};
            recipe.Steps.AddRange(Steps.CloneElements());
            return recipe;
        }
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(Recipe? other)
            => other != null
            && base.Equals(other)
            && Steps.SequencedEquals(other.Steps);

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is Recipe recipe && Equals(recipe);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                Steps.GetSequencedHashCode());
        #endregion
    }
}
