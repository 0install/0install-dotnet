// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
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
        /// <summary>
        /// Creates a new Bouncy Castle instance.
        /// </summary>
        /// <param name="homeDir">The GnuPG home dir to use.</param>
        public BouncyCastle([NotNull] string homeDir)
        {
            #region Sanity checks
            if (string.IsNullOrEmpty(homeDir)) throw new ArgumentNullException(nameof(homeDir));
            #endregion

            _publicBundlePath = Path.Combine(homeDir, "pubring.gpg");
            _secretBundlePath = Path.Combine(homeDir, "secring.gpg");
        }

        [NotNull]
        private readonly string _publicBundlePath;

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
                    using (new AtomicRead(_publicBundlePath))
                    using (var stream = File.OpenRead(_publicBundlePath))
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
                using (var atomic = new AtomicWrite(_publicBundlePath))
                {
                    using (var stream = File.Create(atomic.WritePath))
                        value.Encode(stream);
                    atomic.Commit();
                }
            }
        }

        [NotNull]
        private readonly string _secretBundlePath;

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
                    using (var stream = File.OpenRead(_secretBundlePath))
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
