// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Org.BouncyCastle.Bcpg.OpenPgp;

namespace ZeroInstall.Store.Trust;

partial class BouncyCastle
{
    private readonly string _publicBundlePath = Path.Combine(homeDir, "pubring.gpg");

    /// <summary>
    /// Stores imported public keys on disk.
    /// Intentionally separate from the normal GnuPG public keyring to keep the user's GnuPG profile clean.
    /// </summary>
    /// <remarks>Data is cached in memory for life-time of instance.</remarks>
    [field: AllowNull, MaybeNull]
    private PgpPublicKeyRingBundle PublicBundle
    {
        get
        {
            // Multiple-read races are OK
            if (field != null) return field;

            try
            {
                using (new AtomicRead(_publicBundlePath))
                {
                    using var stream = File.OpenRead(_publicBundlePath);
                    return field = new(PgpUtilities.GetDecoderStream(stream));
                }
            }
            #region Error handling
            catch (IOException ex) when (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            catch (IOException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingKeyBundle, field), ex);
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            #endregion
        }
        set
        {
            // Lost-write races are OK, since public keys are easily reacquired
            field = value;
            using var atomic = new AtomicWrite(_publicBundlePath);
            using (var stream = File.Create(atomic.WritePath))
                value.Encode(stream);
            atomic.Commit();
        }
    }

    private readonly string _secretBundlePath = Path.Combine(homeDir, "secring.gpg");

    /// <summary>
    /// Stores secret keys on disk.
    /// </summary>
    /// <remarks>Data is cached in memory for life-time of instance.</remarks>
    [field: AllowNull, MaybeNull]
    private PgpSecretKeyRingBundle SecretBundle
    {
        get
        {
            // Multiple-read races are OK
            if (field != null) return field;

            try
            {
                using var stream = File.OpenRead(_secretBundlePath);
                return field = new(PgpUtilities.GetDecoderStream(stream));
            }
            #region Error handling
            catch (IOException ex) when (ex is DirectoryNotFoundException or FileNotFoundException)
            {
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            catch (IOException ex)
            {
                Log.Warn(string.Format(Resources.ErrorLoadingKeyBundle, field), ex);
                return new(Enumerable.Empty<PgpSecretKeyRing>());
            }
            #endregion
        }
    }
}
