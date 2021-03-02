// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Runs test methods for <see cref="Fetcher"/>.
    /// </summary>
    public class FetcherTest : FetcherTestBase
    {
        protected override IFetcher BuildFetcher() => new Fetcher(Config, StoreMock.Object, Handler);

        [Fact]
        public void DownloadSingleArchiveMirror()
        {
            StoreMock.Setup(x => x.Flush());
            using var mirrorServer = new MicroServer("archive/http/invalid/directory%23archive.zip", ZipArchiveStream);
            Config.FeedMirror = new FeedUri(mirrorServer.ServerUri);
            TestDownloadArchives(new Archive
            {
                Href = new("http://invalid/directory/archive.zip"),
                MimeType = Archive.MimeTypeZip,
                Size = ZipArchiveStream.Length,
                Extract = "extract",
                Destination = "destination"
            });
        }
    }
}
