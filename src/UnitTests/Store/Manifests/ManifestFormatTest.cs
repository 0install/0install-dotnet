// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using Xunit;

namespace ZeroInstall.Store.Manifests
{
    /// <summary>
    /// Contains test methods for <see cref="ManifestFormat"/>.
    /// </summary>
    public class ManifestFormatTest
    {
        [Fact]
        public void FromPrefix()
        {
            ManifestFormat.FromPrefix("sha1new=abc").Should().BeSameAs(ManifestFormat.Sha1New);
            ManifestFormat.FromPrefix("sha256=abc").Should().BeSameAs(ManifestFormat.Sha256);
            ManifestFormat.FromPrefix("sha256new_abc").Should().BeSameAs(ManifestFormat.Sha256New);
        }
    }
}
