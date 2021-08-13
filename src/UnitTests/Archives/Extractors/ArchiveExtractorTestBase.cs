// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Security.Cryptography;
using FluentAssertions;
using NanoByte.Common;
using NanoByte.Common.Streams;
using NanoByte.Common.Tasks;
using Xunit;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Archives.Extractors
{
    public abstract class ArchiveExtractorTestBase
    {
        protected abstract string MimeType { get; }

        private static readonly DateTime _defaultTimestamp = new(2000, 1, 1, 12, 0, 0);

        protected static ManifestNormalFile Normal(string content, DateTime? timestamp = null)
            => new(content.Hash(SHA1.Create()), (timestamp ?? _defaultTimestamp), content.Length);

        protected static ManifestExecutableFile Executable(string content, DateTime? timestamp = null)
            => new(content.Hash(SHA1.Create()), (timestamp ?? _defaultTimestamp), content.Length);

        protected static ManifestSymlink Symlink(string destination)
            => new(destination.Hash(SHA1.Create()), destination.Length);

        protected void Test(string embeddedFile, Manifest expected, string? subDir = null)
        {
            using var stream = typeof(ArchiveExtractorTestBase).GetEmbeddedStream(embeddedFile);
            var builder = new ManifestBuilder(expected.Format);
            ArchiveExtractor.For(MimeType, new SilentTaskHandler())
                            .Extract(builder, new NonSeekableStream(stream), subDir);

            builder.Manifest.Should().BeEquivalentTo(expected);
        }

        [SkippableFact]
        public void ExtractInvalidData()
        {
            try
            {
                var extractor = ArchiveExtractor.For(MimeType, new SilentTaskHandler());
                extractor.Invoking(x => x.Extract(new ManifestBuilder(ManifestFormat.Sha1New), new MemoryStream(new byte[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16})))
                         .Should().Throw<IOException>();
            }
            catch (NotSupportedException)
            {
                Skip.If(true, "Archive type not supported on this platform");
            }
        }
    }
}
