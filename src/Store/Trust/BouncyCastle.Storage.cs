// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NanoByte.Common;
using NanoByte.Common.Storage;
using Org.BouncyCastle.Bcpg.OpenPgp;

namespace ZeroInstall.Store.Trust
{
    partial class BouncyCastle
    {
        private string PublicBundlePath => Path.Combine(
            // Avoid polluting user profile with auto-imported public keys
            (HomeDir == GnuPG.DefaultHomeDir) ? Locations.GetCacheDirPath("0install.net", machineWide: false) : HomeDir,
            "pubring.gpg");

        [CanBeNull]
        private PgpPublicKeyRingBundle _publicBundle;

        /// <summary>
        /// Stores imported public keys on disk.
        /// Intentionally separate from the normal GnuPG public keyring to keep the user's GnuPG profile clean.
        /// </summary>
        /// <remarks>Data is cached in memory for life-time of instance.</remarks>
        [NotNull]
        private PgpPublicKeyRingBundle PublicBundle
        {
            get
            {
                // Multiple-read races are OK
                if (_publicBundle != null) return _publicBundle;

                try
                {
                    using (new AtomicRead(PublicBundlePath))
                    using (var stream = File.OpenRead(PublicBundlePath))
                        return _publicBundle = new PgpPublicKeyRingBundle(PgpUtilities.GetDecoderStream(stream));
                }
                #region Error handling
                catch (DirectoryNotFoundException)
                {
                    return new PgpPublicKeyRingBundle(Enumerable.Empty<PgpPublicKeyRing>());
                }
                catch (FileNotFoundException)
                {
                    return new PgpPublicKeyRingBundle(Enumerable.Empty<PgpPublicKeyRing>());
                }
                catch (IOException ex)
                {
                    Log.Warn(ex);
                    return new PgpPublicKeyRingBundle(Enumerable.Empty<PgpPublicKeyRing>());
                }
                #endregion
            }
            set
            {
                // Lost-write races are OK, since public keys are easily reacquired
                _publicBundle = value;
                using (var atomic = new AtomicWrite(PublicBundlePath))
                {
                    using (var stream = File.Create(atomic.WritePath))
                        value.Encode(stream);
                    atomic.Commit();
                }
            }
        }

        [CanBeNull]
        private PgpSecretKeyRingBundle _secretBundle;

        /// <summary>
        /// Stores secret keys on disk.
        /// </summary>
        /// <remarks>Data is cached in memory for life-time of instance.</remarks>
        [NotNull]
        private PgpSecretKeyRingBundle SecretBundle
        {
            get
            {
                // Multiple-read races are OK
                if (_secretBundle != null) return _secretBundle;

                try
                {
                    using (var stream = File.OpenRead(Path.Combine(HomeDir, "secring.gpg")))
                        return _secretBundle = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(stream));
                }
                #region Error handling
                catch (DirectoryNotFoundException)
                {
                    return new PgpSecretKeyRingBundle(Enumerable.Empty<PgpSecretKeyRing>());
                }
                catch (FileNotFoundException)
                {
                    return new PgpSecretKeyRingBundle(Enumerable.Empty<PgpSecretKeyRing>());
                }
                catch (IOException ex)
                {
                    Log.Warn(ex);
                    return new PgpSecretKeyRingBundle(Enumerable.Empty<PgpSecretKeyRing>());
                }
                #endregion
            }
        }
    }
}
