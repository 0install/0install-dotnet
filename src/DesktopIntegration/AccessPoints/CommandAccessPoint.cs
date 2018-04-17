// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.ComponentModel;
using System.Xml.Serialization;
using JetBrains.Annotations;

namespace ZeroInstall.DesktopIntegration.AccessPoints
{
    /// <summary>
    /// Adds a way to explicitly launch the application to the desktop environment.
    /// </summary>
    /// <seealso cref="ZeroInstall.Store.Model.Command"/>
    [XmlType("command-access-point", Namespace = AppList.XmlNamespace)]
    public abstract class CommandAccessPoint : AccessPoint
    {
        /// <summary>
        /// The name of the menu entry, icon, command-line, etc..
        /// </summary>
        [Description("The name of the menu entry, icon, command-line, etc..")]
        [XmlAttribute("name")]
        public string Name { get; set; }

        /// <summary>
        /// The name of the <see cref="Store.Model.Command"/> to use when launching via this access point. Leave empty to use default.
        /// </summary>
        [Description("The name of the Command to use when launching via this access point. Leave empty to use default.")]
        [XmlAttribute("command")]
        [CanBeNull]
        public string Command { get; set; }

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
        protected bool Equals(CommandAccessPoint other) => other != null && base.Equals(other) && other.Name == Name && other.Command == Command;

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (Name?.GetHashCode() ?? 0);
                hashCode = (hashCode * 397) ^ (Command?.GetHashCode() ?? 0);
                return hashCode;
            }
        }
        #endregion
    }
}
