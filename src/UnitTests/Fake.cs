// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using ZeroInstall.Model;
using ZeroInstall.Model.Selection;

namespace ZeroInstall
{
    public static class Fake
    {
        public static readonly FeedUri Feed1Uri = new("http://example.com/test1.xml");
        public static readonly FeedUri Feed2Uri = new("http://example.com/test2.xml");
        public static readonly FeedUri SubFeed1Uri = new("http://example.com/sub1.xml");
        public static readonly FeedUri SubFeed2Uri = new("http://example.com/sub2.xml");
        public static readonly FeedUri SubFeed3Uri = new("http://example.com/sub3.xml");

        public static Feed Feed => new()
        {
            Uri = Feed1Uri,
            Name = "MyApp",
            Homepage = new Uri("http://example.com/"),
            Summaries = {"Summary"},
            Elements =
            {
                new Implementation
                {
                    ID = "id1",
                    ManifestDigest = new ManifestDigest(sha256: "123"),
                    Version = new ImplementationVersion("1.0")
                }
            }
        };

        public static Selections Selections => new()
        {
            InterfaceUri = Feed1Uri,
            Command = Command.NameRun,
            Implementations =
            {
                new ImplementationSelection
                {
                    InterfaceUri = Feed1Uri,
                    FromFeed = SubFeed1Uri,
                    ID = "id1",
                    ManifestDigest = new ManifestDigest(sha256: "123"),
                    Version = new ImplementationVersion("1.0")
                },
                new ImplementationSelection
                {
                    InterfaceUri = Feed2Uri,
                    FromFeed = SubFeed2Uri,
                    ID = "id2",
                    ManifestDigest = new ManifestDigest(sha256: "abc"),
                    Version = new ImplementationVersion("1.0")
                }
            }
        };
    }
}
