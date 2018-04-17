// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Runs test methods for <see cref="BouncyCastle"/>.
    /// </summary>
    public class BouncyCastleTest : OpenPgpTest
    {
        public BouncyCastleTest()
            : base(new BouncyCastle())
        {}
    }
}
