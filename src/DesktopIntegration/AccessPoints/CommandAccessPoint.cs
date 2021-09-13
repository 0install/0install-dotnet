// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Xml.Serialization;
using ZeroInstall.DesktopIntegration.Properties;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Adds a way to explicitly launch the application to the desktop environment.
    /// </summary>
    /// <seealso cref="Model.Command"/>
    [XmlType("command-access-point", Namespace = AppList.XmlNamespace)]
    public abstract class CommandAccessPoint : AccessPoint
    {
        /// <summary>
        /// The name of the menu entry, icon, command-line, etc..
        /// </summary>
        [Description("The name of the menu entry, icon, command-line, etc..")]
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// The name of the <see cref="Model.Command"/> to use when launching via this access point. Leave empty to use default.
        /// </summary>
        [Description("The name of the Command to use when launching via this access point. Leave empty to use default.")]
        [XmlAttribute("command")]
        public string? Command { get; set; }

        /// <summary>
        /// Ensures that the given name can be used as a file name.
        /// </summary>
        /// <exception cref="IOException"><see cref="Name"/> contains invalid characters.</exception>
        [MemberNotNull(nameof(Name))]
        protected void ValidateName()
        {
            if (string.IsNullOrEmpty(Name) || Name.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
                throw new IOException(string.Format(Resources.NameInvalidChars, Name));
        }

        #region Conversion
        /// <summary>
        /// Returns the access point in the form "AccessPointType: Name (Command)". Not safe for parsing!
        /// </summary>
        public override string ToString()
        {
            string result = GetType().Name + ": " + Name;
            if (!string.IsNullOrEmpty(Command)) result += " (" + Command + ")";
            return result;
        }
        #endregion

        #region Equality
        protected bool Equals(CommandAccessPoint? other)
            => other != null
            && base.Equals(other)
            && other.Name == Name
            && other.Command == Command;

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(base.GetHashCode(), Name, Command);
        #endregion
    }
}
