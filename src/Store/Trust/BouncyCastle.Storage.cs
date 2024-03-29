// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Org.BouncyCastle.Bcpg.OpenPgp;

namespace ZeroInstall.Store.Trust;

partial class BouncyCastle
{
    private readonly string _publicBundlePath = Path.Combine(homeDir, "pubring.gpg");

    private PgpPublicKeyRingBundle? _publicBundle;

    /// <summary>
    /// Stores imported public keys on disk.
    /// Intentionally separate from the normal GnuPG public keyring to keep the user's GnuPG profile clean.
    /// </summary>
    /// <remarks>Data is cached in memory for life-time of instance.</remarks>
    private PgpPublicKeyRingBundle PublicBundle
    {
        get
        {
            // Multiple-read races are OK
            if (_publicBundle != null) return _publicBundle;

            try
            {
                using (new AtomicRead(_publicBundlePath))
                {
                    using var stream = File.OpenRead(_publicBundlePath);
                    return _publicBundle = new(PgpUtilities.GetDecoderStream(stream));
                }
            }
            #region Error handling
            catch (IOException ex) when (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            catch (IOException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingKeyBundle, _publicBundle), ex);
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            #endregion
        }
        set
        {
            // Lost-write races are OK, since public keys are easily reacquired
            _publicBundle = value;
            using var atomic = new AtomicWrite(_publicBundlePath);
            using (var stream = File.Create(atomic.WritePath))
                value.Encode(stream);
            atomic.Commit();
        }
    }

    private readonly string _secretBundlePath = Path.Combine(homeDir, "secring.gpg");

    private PgpSecretKeyRingBundle? _secretBundle;

    /// <summary>
    /// Stores secret keys on disk.
    /// </summary>
    /// <remarks>Data is cached in memory for life-time of instance.</remarks>
    private PgpSecretKeyRingBundle SecretBundle
    {
        get
        {
            // Multiple-read races are OK
            if (_secretBundle != null) return _secretBundle;

            try
            {
                using var stream = File.OpenRead(_secretBundlePath);
                return _secretBundle = new(PgpUtilities.GetDecoderStream(stream));
            }
            #region Error handling
            catch (IOException ex) when (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            catch (IOException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingKeyBundle, _secretBundle), ex);
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            #endregion
        }
    }
}
