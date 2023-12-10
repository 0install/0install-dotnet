// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using NanoByte.Common.Streams;
using Org.BouncyCastle.Bcpg;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Security;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Provides access to the OpenPGP signature functions of Bouncy Castle.
/// </summary>
/// <param name="homeDir">The GnuPG home dir to use.</param>
public partial class BouncyCastle(string homeDir) : IOpenPgp
{
    /// <inheritdoc/>
    public IEnumerable<OpenPgpSignature> Verify(ArraySegment<byte> data, byte[] signature)
    {
        var signatureList = ParseObject<PgpSignatureList>(new MemoryStream(signature));

        var result = new OpenPgpSignature[signatureList.Count];
        for (int i = 0; i < result.Length; i++)
        {
            try
            {
                result[i] = Verify(data, signatureList[i]);
            }
            #region Error handling
            catch (Exception ex) when (ex is PgpException or SecurityUtilityException or InvalidDataException)
            {
                // Wrap exception since only certain exception types are allowed
                throw new SignatureException(Resources.InvalidSignature, ex);
            }
            #endregion
        }
        return result;
    }

    private OpenPgpSignature Verify(ArraySegment<byte> data, PgpSignature signature)
    {
        if (PublicBundle.GetPublicKey(signature.KeyId) is {} key)
        {
            signature.InitVerify(key);
            signature.Update(data.Array, data.Offset, data.Count);

            if (signature.Verify())
                return new ValidSignature(key.KeyId, new OpenPgpFingerprint(key.GetFingerprint()), signature.CreationTime);
            else
            {
                var badSig = new BadSignature(signature.KeyId);
                Log.Warn(badSig.ToString());
                return badSig;
            }
        }
        else return new MissingKeySignature(signature.KeyId);
    }

    /// <inheritdoc/>
    public byte[] Sign(ArraySegment<byte> data, OpenPgpSecretKey secretKey, string? passphrase = null)
    {
        #region Sanity checks
        if (secretKey == null) throw new ArgumentNullException(nameof(secretKey));
        #endregion

        var pgpSecretKey = SecretBundle.GetSecretKey(secretKey.KeyID)
                        ?? throw new KeyNotFoundException("Specified OpenPGP key not found on system");
        var pgpPrivateKey = GetPrivateKey(pgpSecretKey, passphrase);

        var signatureGenerator = new PgpSignatureGenerator(pgpSecretKey.PublicKey.Algorithm, HashAlgorithmTag.Sha1);
        signatureGenerator.InitSign(PgpSignature.BinaryDocument, pgpPrivateKey);
        signatureGenerator.Update(data.Array, data.Offset, data.Count);
        return signatureGenerator.Generate().GetEncoded();
    }

    private static PgpPrivateKey GetPrivateKey(PgpSecretKey secretKey, string? passphrase)
    {
        try
        {
            return secretKey.ExtractPrivateKey(passphrase?.ToCharArray());
        }
        catch (PgpException)
        {
            throw new WrongPassphraseException();
        }
    }

    /// <inheritdoc/>
    public void ImportKey(ArraySegment<byte> data)
    {
        var ring = ParseObject<PgpPublicKeyRing>(PgpUtilities.GetDecoderStream(data.ToStream()));
        try
        {
            PublicBundle = PgpPublicKeyRingBundle.AddPublicKeyRing(PublicBundle, ring);
        }
        catch (ArgumentException)
        {
            // Bundle already contains key
        }
    }

    /// <inheritdoc/>
    public string ExportKey(IKeyIDContainer keyIDContainer)
    {
        #region Sanity checks
        if (keyIDContainer == null) throw new ArgumentNullException(nameof(keyIDContainer));
        #endregion

        var publicKey = SecretBundle.GetSecretKey(keyIDContainer.KeyID)?.PublicKey
                     ?? PublicBundle.GetPublicKey(keyIDContainer.KeyID)
                     ?? throw new KeyNotFoundException("Specified OpenPGP key not found on system");

        var output = new MemoryStream();
        using (var armored = new ArmoredOutputStream(output))
            publicKey.Encode(armored);
        return output.ReadToString(Encoding.ASCII).Replace(Environment.NewLine, "\n");
    }

    /// <inheritdoc/>
    public IEnumerable<OpenPgpSecretKey> ListSecretKeys()
        => from PgpSecretKeyRing ring in SecretBundle.GetKeyRings()
           select ring.GetSecretKey() into key
           select new OpenPgpSecretKey(
               key.KeyId,
               new OpenPgpFingerprint(key.PublicKey.GetFingerprint()),
               key.UserIds.First());

    private static T ParseObject<T>(Stream stream) where T : PgpObject
    {
        try
        {
            return ParseObjects<T>(stream).FirstOrDefault()
                ?? throw new InvalidDataException($"Unable to find instance of {typeof(T).Name} in stream");
        }
        #region Error handling
        catch (IOException ex)
        {
            throw new InvalidDataException(ex.Message, ex);
        }
        #endregion
    }

    private static IEnumerable<T> ParseObjects<T>(Stream stream) where T : PgpObject
    {
        var factory = new PgpObjectFactory(stream);

        while (factory.NextPgpObject() is {} obj)
        {
            if (obj is T target) yield return target;

            if (obj is PgpCompressedData compressed)
            {
                foreach (var result in ParseObjects<T>(compressed.GetDataStream()))
                    yield return result;
            }
        }
    }
}
