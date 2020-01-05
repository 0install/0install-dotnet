// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System;
using System.IO;
using System.Threading;
using NanoByte.Common.Streams;
using SevenZip.Sdk;
using SevenZip.Sdk.Compression.Lzma;

namespace ZeroInstall.Store.Implementations.Archives
{
    /// <summary>
    /// Creates a LZMA-compressed TAR archive from a directory. Preserves executable bits, symlinks, hardlinks and timestamps.
    /// </summary>
    public class TarLzmaGenerator : TarGenerator
    {
        internal TarLzmaGenerator(string sourcePath, Stream stream)
            : base(sourcePath, GetCompressionStream(stream))
        {}

        /// <summary>
        /// Provides a filter for compressing a <see cref="Stream"/> with LZMA.
        /// </summary>
        /// <param name="stream">The underlying <see cref="Stream"/> to write the compressed data to.</param>
        /// <param name="bufferSize">The maximum number of uncompressed bytes to buffer. 32k (the step size of <see cref="SevenZip"/>) is a sensible minimum.</param>
        /// <remarks>
        /// This method internally uses multi-threading and a <see cref="CircularBufferStream"/>.
        /// The <paramref name="stream"/> may be closed with a delay.
        /// </remarks>
        private static Stream GetCompressionStream(Stream stream, int bufferSize = 128 * 1024)
        {
            var bufferStream = new CircularBufferStream(bufferSize);
            var encoder = new Encoder();

            var consumerThread = new Thread(() =>
            {
                try
                {
                    // Write LZMA header
                    encoder.SetCoderProperties(
                        new[] {CoderPropId.DictionarySize, CoderPropId.PosStateBits, CoderPropId.LitContextBits, CoderPropId.LitPosBits, CoderPropId.Algorithm, CoderPropId.NumFastBytes, CoderPropId.MatchFinder, CoderPropId.EndMarker},
                        new object[] {1 << 23, 2, 3, 0, 2, 128, "bt4", true});
                    encoder.WriteCoderProperties(stream);

                    // Write "uncompressed length" header
                    var uncompressedLengthData = BitConverter.GetBytes(TarLzmaExtractor.UnknownSize);
                    if (!BitConverter.IsLittleEndian) Array.Reverse(uncompressedLengthData);
                    stream.Write(uncompressedLengthData);

                    encoder.Code(
                        inStream: bufferStream, outStream: stream,
                        inSize: TarLzmaExtractor.UnknownSize, outSize: TarLzmaExtractor.UnknownSize,
                        progress: null);
                }
                catch (ObjectDisposedException)
                {
                    // If the buffer stream is closed too early the user probably just canceled the compression process
                }
                finally
                {
                    stream.Dispose();
                }
            }) {IsBackground = true};
            consumerThread.Start();

            return new DisposeWarpperStream(bufferStream, disposeHandler: () =>
            {
                bufferStream.DoneWriting();
                consumerThread.Join();
            });
        }
    }
}
#endif
