// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Undo;

namespace ZeroInstall.Publish;

/// <summary>
/// Contains test methods for <see cref="FeedExtensions"/>.
/// </summary>
public class FeedExtensionsTest
{
    private readonly ICommandExecutor _executor = new SimpleCommandExecutor();

    [Fact]
    public void AddVersionPlacesNextToLastImplementation()
    {
        var group = new Group {Elements = {new Implementation {ID = "sha1=1", Version = new("1.0")}}};
        var feed = new Feed {Name = "Test", Elements = {group}};

        var implementation = feed.AddVersion(new("2.0"), _executor);

        implementation.ID.Should().Be(".");
        group.Elements.Should().HaveCount(2).And.Contain(implementation);
        feed.Elements.Should().HaveCount(1);
    }

    [Fact]
    public void AddVersionFallsBackToFeedRoot()
    {
        var feed = new Feed {Name = "Test"};

        var implementation = feed.AddVersion(new("1.0"), _executor);

        feed.Elements.Should().Equal(implementation);
    }

    [Fact]
    public void SelectImplementationsByVersion()
    {
        var v1 = new Implementation {ID = "sha1=1", Version = new("1.0"), ReleasedString = "2020-01-01"};
        var v2 = new Implementation {ID = "sha1=2", Version = new("2.0"), ReleasedString = "2021-01-01"};
        var feed = new Feed {Name = "Test", Elements = {v1, v2}};

        feed.SelectImplementations(new("2.0")).Should().Equal(v2);
    }

    [Fact]
    public void SelectImplementationsByVersionInheritedFromGroup()
    {
        var implementation = new Implementation {ID = "sha1=1", Version = null!};
        var feed = new Feed {Name = "Test", Elements = {new Group {Version = new("1.0"), Elements = {implementation}}}};

        feed.SelectImplementations(new("1.0")).Should().Equal(implementation);
    }

    [Fact]
    public void SelectImplementationsRejectsUnknownVersion()
    {
        var feed = new Feed {Name = "Test", Elements = {new Implementation {ID = "sha1=1", Version = new("1.0")}}};

        Assert.Throws<InvalidDataException>(() => feed.SelectImplementations(new("2.0")));
    }

    [Fact]
    public void SelectImplementationsDefaultsToUnreleased()
    {
        var released = new Implementation {ID = "sha1=1", Version = new("1.0"), ReleasedString = "2020-01-01"};
        var unreleased = new Implementation {ID = "sha1=2", Version = new("2.0")};
        var feed = new Feed {Name = "Test", Elements = {released, unreleased}};

        feed.SelectImplementations().Should().Equal(unreleased);
    }

    [Fact]
    public void SelectImplementationsDefaultsToOnlyImplementation()
    {
        var implementation = new Implementation {ID = "sha1=1", Version = new("1.0"), ReleasedString = "2020-01-01"};
        var feed = new Feed {Name = "Test", Elements = {implementation}};

        feed.SelectImplementations().Should().Equal(implementation);
    }

    [Fact]
    public void SelectImplementationsRejectsAmbiguousDefault()
    {
        var feed = new Feed
        {
            Name = "Test",
            Elements =
            {
                new Implementation {ID = "sha1=1", Version = new("1.0")},
                new Implementation {ID = "sha1=2", Version = new("2.0")}
            }
        };

        Assert.Throws<InvalidDataException>(() => feed.SelectImplementations());
    }

    [Fact]
    public void SelectImplementationsRejectsEmptyFeed()
        => Assert.Throws<InvalidDataException>(() => new Feed {Name = "Test"}.SelectImplementations());

    [Fact]
    public void MarkStableUsesLatestTestingVersion()
    {
        var older = new Implementation {ID = "sha1=1", Version = new("1.0")};
        var newer = new Implementation {ID = "sha1=2", Version = new("2.0")};
        var feed = new Feed {Name = "Test", Elements = {older, newer}};

        feed.MarkStable(_executor);

        newer.Stability.Should().Be(Stability.Stable);
        older.Stability.Should().Be(Stability.Unset, because: "Only the latest testing version should be marked stable");
    }

    [Fact]
    public void MarkStableConsidersStabilityInheritedFromGroup()
    {
        var implementation = new Implementation {ID = "sha1=1", Version = new("1.0")};
        var feed = new Feed {Name = "Test", Elements = {new Group {Stability = Stability.Developer, Elements = {implementation}}}};

        Assert.Throws<InvalidDataException>(() => feed.MarkStable(_executor));
    }

    [Fact]
    public void MarkStableRejectsFeedWithoutTestingImplementations()
    {
        var feed = new Feed {Name = "Test", Elements = {new Implementation {ID = "sha1=1", Version = new("1.0"), Stability = Stability.Stable}}};

        Assert.Throws<InvalidDataException>(() => feed.MarkStable(_executor));
    }

}
