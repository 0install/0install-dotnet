// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Properties;
using ZeroInstall.Store.Trust;

namespace ZeroInstall.Store.Feeds
{
    /// <summary>
    /// Provides utility methods for managing <see cref="Feed"/>s.
    /// </summary>
    public static class FeedUtils
    {
        /// <summary>
        /// The string signifying the start of a signature block.
        /// </summary>
        public const string SignatureBlockStart = "<!-- Base64 Signature\n";

        /// <summary>
        /// The string signifying the end of a signature block.
        /// </summary>
        public const string SignatureBlockEnd = "\n-->\n";

        /// <summary>
        /// The encoding used when looking for signature blocks in feed files.
        /// </summary>
        public static readonly Encoding Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// <see cref="SignatureBlockStart"/> encoded with <see cref="Encoding"/>.</summary>
        private static readonly byte[] _signatureBlockStartEncoded = Encoding.GetBytes(SignatureBlockStart);

        /// <summary>
        /// Determines which signatures a feed is signed with.
        /// </summary>
        /// <param name="openPgp">The OpenPGP-compatible system used to validate the signatures.</param>
        /// <param name="feedData">The feed data containing an embedded signature.</param>
        /// <returns>A list of signatures found, both valid and invalid.</returns>
        /// <exception cref="SignatureException">There is no valid signature data embedded in the <paramref name="feedData"/>.</exception>
        public static IEnumerable<OpenPgpSignature> GetSignatures(IOpenPgp openPgp, byte[] feedData)
        {
            #region Sanity checks
            if (openPgp == null) throw new ArgumentNullException(nameof(openPgp));
            if (feedData == null) throw new ArgumentNullException(nameof(feedData));
            #endregion

            if (feedData.Length == 0) return Enumerable.Empty<OpenPgpSignature>();

            int signatureStartIndex = GetSignatureStartIndex(feedData);
            if (signatureStartIndex == -1) return Enumerable.Empty<OpenPgpSignature>();

            var data = IsolateFeed(feedData, signatureStartIndex);
            var signature = IsolateAndDecodeSignature(feedData, signatureStartIndex);
            try
            {
                return openPgp.Verify(data, signature);
            }
            #region Error handling
            catch (InvalidDataException ex)
            {
                throw new SignatureException(Resources.InvalidSignature, ex);
            }
            #endregion
        }

        /// <summary>
        /// Finds the point in a data array where the signature block starts.
        /// </summary>
        /// <param name="feedData">The feed data containing a signature block.</param>
        /// <returns>The index of the first byte of the signature block; -1 if none was found.</returns>
        private static int GetSignatureStartIndex(byte[] feedData)
        {
            int signatureStartIndex = -1;

            for (int currentFeedDataIndex = 0; currentFeedDataIndex < feedData.Length; currentFeedDataIndex++)
            {
                bool validStartingPoint = true;
                for (int i = 0, j = currentFeedDataIndex; j < feedData.Length && i < _signatureBlockStartEncoded.Length; i++, j++)
                {
                    if (feedData[j] == _signatureBlockStartEncoded[i]) continue;

                    validStartingPoint = false;
                    break;
                }
                if (validStartingPoint) signatureStartIndex = currentFeedDataIndex;
            }

            return signatureStartIndex;
        }

        /// <summary>
        /// Isolates the actual feed from the signature block.
        /// </summary>
        /// <param name="feedData">The feed data containing a signature block.</param>
        /// <param name="signatureStartIndex">The index of the first byte of the signature block.</param>
        /// <returns>The isolated feed.</returns>
        /// <exception cref="SignatureException">The signature block does not start on a new line.</exception>
        private static byte[] IsolateFeed(byte[] feedData, int signatureStartIndex)
        {
            if (signatureStartIndex <= 0 || feedData[signatureStartIndex - 1] != Encoding.GetBytes("\n")[0])
                throw new SignatureException(Resources.XmlSignatureMissingNewLine);

            var feed = new byte[signatureStartIndex];
            Array.Copy(feedData, 0, feed, 0, signatureStartIndex);
            return feed;
        }

        /// <summary>
        /// Isolates and decodes the Base64-encoded signature.
        /// </summary>
        /// <param name="feedData">The feed data containing a signature block.</param>
        /// <param name="signatureStartIndex">The index of the first byte of the signature block.</param>
        /// <returns>The decoded signature data.</returns>
        /// <exception cref="SignatureException">The signature contains invalid characters.</exception>
        private static byte[] IsolateAndDecodeSignature(byte[] feedData, int signatureStartIndex)
        {
            // Isolate and decode signature string
            string signatureString = Encoding.GetString(feedData, signatureStartIndex, feedData.Length - signatureStartIndex);
            if (!signatureString.EndsWith(SignatureBlockEnd)) throw new SignatureException(Resources.XmlSignatureInvalidEnd);

            // Concatenate Base64 lines and decode
            string base64Characters = signatureString.Substring(SignatureBlockStart.Length, signatureString.Length - SignatureBlockStart.Length - SignatureBlockEnd.Length).Replace("\n", "");
            try
            {
                return Convert.FromBase64String(base64Characters);
            }
            #region Error handling
            catch (FormatException ex)
            {
                throw new SignatureException(Resources.XmlSignatureNotBase64 + ex.Message, ex);
            }
            #endregion
        }
    }
}
