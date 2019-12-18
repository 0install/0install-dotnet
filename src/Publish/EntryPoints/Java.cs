// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.ComponentModel;
using JetBrains.Annotations;
using ZeroInstall.Publish.EntryPoints.Design;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// A compiled Java application.
    /// </summary>
    public abstract class Java : Candidate
    {
        /// <summary>
        /// The minimum version of the Java Runtime Environment required by the application.
        /// </summary>
        [Category("Details (Java)"), DisplayName(@"Minimum Java version"), Description("The minimum version of the Java Runtime Environment required by the application.")]
        [DefaultValue("")]
        [TypeConverter(typeof(JavaVersionConverter))]
        [UsedImplicitly, CanBeNull]
        public ImplementationVersion MinimumRuntimeVersion { get; set; }

        /// <summary>
        /// Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!
        /// </summary>
        [Category("Details (Java)"), DisplayName(@"External dependencies"), Description("Does this application have external dependencies that need to be injected by Zero Install? Only enable if you are sure!")]
        [DefaultValue(false)]
        [UsedImplicitly]
        public bool ExternalDependencies { get; set; }

        /// <summary>
        /// Does this application have a graphical interface an no terminal output? Only enable if you are sure!
        /// </summary>
        [Category("Details (Java)"), DisplayName(@"GUI only"), Description("Does this application have a graphical interface an no terminal output? Only enable if you are sure!")]
        [UsedImplicitly]
        public bool GuiOnly { get => !NeedsTerminal; set => NeedsTerminal = !value; }

        #region Equality
        protected bool Equals(Java other)
            => other != null
            && base.Equals(other)
            && Equals(MinimumRuntimeVersion, other.MinimumRuntimeVersion)
            && ExternalDependencies == other.ExternalDependencies;

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((Java)obj);
        }

        public override int GetHashCode()
            => HashCode.Combine(
                base.GetHashCode(),
                MinimumRuntimeVersion,
                ExternalDependencies);
        #endregion
    }
}
