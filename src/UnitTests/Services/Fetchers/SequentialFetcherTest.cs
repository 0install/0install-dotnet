// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Net;
using Xunit;
using ZeroInstall.Model;

namespace ZeroInstall.Services.Fetchers
{
    /// <summary>
    /// Runs test methods for <see cref="SequentialFetcher"/>
    /// </summary>
    public class SequentialFetcherTest : FetcherTest
    {
        protected override IFetcher BuildFetcher() => new SequentialFetcher(Config, StoreMock.Object, Handler);

        [Fact]
        public void DownloadSingleArchiveMirror()
        {
            StoreMock.Setup(x => x.Flush());
            using var mirrorServer = new MicroServer("archive/http/invalid/directory%23archive.zip", ZipArchiveStream);
            Config.FeedMirror = new FeedUri(mirrorServer.ServerUri);
            TestDownloadArchives(new Archive
            {
                Href = new Uri("http://invalid/directory/archive.zip"),
                MimeType = Archive.MimeTypeZip,
                Size = ZipArchiveStream.Length,
                Extract = "extract",
                Destination = "destination"
            });
        }
    }
}
