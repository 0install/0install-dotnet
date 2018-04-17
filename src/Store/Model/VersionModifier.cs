// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Model
{
    /// <see cref="VersionPart.Modifier"/>
    public enum VersionModifier
    {
        /// <summary>No modifier; empty string</summary>
        None = 0,

        /// <summary>Pre-release</summary>
        Pre = -2,

        /// <summary>Release candidate</summary>
        RC = -1,

        /// <summary>Post-release</summary>
        Post = 1
    }
}
