/*
 * Copyright 2010-2016 Bastian Eicher
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser Public License for more details.
 *
 * You should have received a copy of the GNU Lesser Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Retrieves an implementation by downloading a single file.
    /// </summary>
    [Description("Retrieves an implementation by downloading a single file.")]
    [Serializable, XmlRoot("file", Namespace = Feed.XmlNamespace), XmlType("file", Namespace = Feed.XmlNamespace)]
    public sealed class SingleFile : DownloadRetrievalMethod, IEquatable<SingleFile>
    {
        /// <summary>
        /// The file's target path relative to the implementation root as a Unix-style path.
        /// </summary>
        [Description("The file's target path relative to the implementation root as a Unix-style path.")]
        [XmlAttribute("dest"), CanBeNull]
        public string Destination { get; set; }

        /// <summary>
        /// Set this to <c>true</c> to mark the file as executable.
        /// </summary>
        [Description("Set this to true to mark the file as executable.")]
        [XmlAttribute("executable"), DefaultValue(false)]
        public bool Executable { get; set; }

        #region Normalize
        protected override string XmlTagName => "file";
        #endregion

        #region Conversion
        /// <summary>
        /// Returns the file in the form "Location (Size) => Destination". Not safe for parsing!
        /// </summary>
        public override string ToString() => $"{Href} ({Size}) => {Destination}";
        #endregion

        #region Clone
        /// <summary>
        /// Creates a deep copy of this <see cref="SingleFile"/> instance.
        /// </summary>
        /// <returns>The new copy of the <see cref="SingleFile"/>.</returns>
        public override RetrievalMethod Clone() => new SingleFile {UnknownAttributes = UnknownAttributes, UnknownElements = UnknownElements, IfZeroInstallVersion = IfZeroInstallVersion, Href = Href, Size = Size, Destination = Destination, Executable = Executable};
        #endregion

        #region Equality
        /// <inheritdoc/>
        public bool Equals(SingleFile other) => other != null && base.Equals(other) &&
            other.Destination == Destination && other.Executable == Executable;

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            return obj is SingleFile file && Equals(file);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int result = base.GetHashCode();
                result = (result * 397) ^ Destination?.GetHashCode() ?? 0;
                result = (result * 397) ^ Executable.GetHashCode();
                return result;
            }
        }
        #endregion
    }
}
