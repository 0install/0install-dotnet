// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Undo;

namespace ZeroInstall.Publish.Merging;

/// <summary>
/// Contains test methods for <see cref="FeedMerge"/>.
/// </summary>
/// <remarks>Ported from the test suite of the Python implementation of 0publish.</remarks>
public class FeedMergeTest
{
    #region Test data
    private const string Header = """
        <?xml version="1.0" ?>
        <interface xmlns="http://zero-install.sourceforge.net/2004/injector/interface" uri="http://test/hello.xml">
          <name>test</name>
          <summary>for testing</summary>
          <description>This is for testing.</description>
        """;

    private const string Footer = "</interface>";

    /// <summary>A local feed with a single implementation in a group with a main.</summary>
    private const string Local = """
        <?xml version="1.0" ?>
        <interface xmlns="http://zero-install.sourceforge.net/2004/injector/interface">
          <name>hello</name>
          <summary>prints hello</summary>
          <description>Hello, local.</description>
          <feed-for interface="http://test/hello.xml"/>
          <group main="hello">
            <implementation id="sha1=002" version="0.2"/>
          </group>
        </interface>
        """;

    /// <summary>A local feed with a requirement and foreign-namespace attributes.</summary>
    private const string LocalReq = """
        <?xml version="1.0" ?>
        <interface xmlns="http://zero-install.sourceforge.net/2004/injector/interface" xmlns:myns="http://mynamespace/foo">
          <name>hello</name>
          <summary>prints hello</summary>
          <description>Hello, local.</description>
          <feed-for interface="http://test/hello.xml"/>
          <group main="hello" myns:bob="bob">
            <requires interface="http://foo"/>
            <implementation id="sha1=003" version="0.3" xmlns:y="yns" x="x" y:z="z"/>
          </group>
        </interface>
        """;

    /// <summary>A local feed with a command and a requirement with a binding.</summary>
    private const string LocalCommand = """
        <?xml version="1.0" ?>
        <interface xmlns="http://zero-install.sourceforge.net/2004/injector/interface">
          <name>hello</name>
          <summary>prints hello</summary>
          <description>Hello, local.</description>
          <feed-for interface="http://test/hello.xml"/>
          <group>
            <command name="run" path="run.sh"/>
            <requires interface="http://foo">
              <environment name="TESTING" value="true" mode="replace"/>
            </requires>
            <implementation id="sha1=003" version="0.3"/>
          </group>
        </interface>
        """;

    /// <summary>A local feed with both a main and a run command.</summary>
    private const string LocalMainAndCommand = """
        <?xml version="1.0" ?>
        <interface xmlns="http://zero-install.sourceforge.net/2004/injector/interface">
          <name>hello</name>
          <summary>prints hello</summary>
          <group main="main">
            <command name="run" path="run.sh"/>
            <implementation id="sha1=002" version="0.2"/>
          </group>
        </interface>
        """;

    /// <summary>A local feed with version-filtered commands.</summary>
    private const string LocalIf = """
        <?xml version="1.0" ?>
        <interface xmlns="http://zero-install.sourceforge.net/2004/injector/interface">
          <name>hello</name>
          <summary>prints hello</summary>
          <group>
            <command name="run" path="run-old.sh" if-0install-version="..!2"/>
            <command name="run" path="run-new.sh" if-0install-version="2.."/>
            <implementation id="sha1=003" version="0.3"/>
          </group>
        </interface>
        """;
    #endregion

    private static Feed Merge(string masterBody, string local)
    {
        var master = XmlStorage.FromXmlString<Feed>($"{Header}{masterBody}{Footer}");
        master.AddFrom(XmlStorage.FromXmlString<Feed>(local), new SimpleCommandExecutor());
        return master;
    }

    private static IEnumerable<Group> GetGroups(IElementContainer container)
        => container.Elements.OfType<Group>().SelectMany(group => GetGroups(group).Prepend(group));

    [Fact]
    public void CreateFromLocal()
    {
        var feed = FeedTemplate.CreateFromLocal(XmlStorage.FromXmlString<Feed>(Local));

        feed.Uri.Should().Be(new FeedUri("http://test/hello.xml"));
        feed.FeedFor.Should().BeEmpty();
        feed.Implementations.Should().ContainSingle().Which.ID.Should().Be("sha1=002");
    }

    [Fact]
    public void CreateFromLocalRejectsFeedWithoutFeedFor()
        => Assert.Throws<InvalidDataException>(() => FeedTemplate.CreateFromLocal(XmlStorage.FromXmlString<Feed>(LocalIf)));

    [Fact]
    public void MergeIntoEmptyFeed()
    {
        var feed = Merge("", Local);

        feed.Implementations.Should().ContainSingle().Which.ID.Should().Be("sha1=002");
    }

    [Fact]
    public void MergeAlongsideExistingImplementation()
    {
        var feed = Merge("""<implementation id="sha1=123" version="1"/>""", Local);

        feed.Implementations.Should().HaveCount(2);
    }

    [Fact]
    public void MergeRejectsDuplicateID()
    {
        var feed = Merge("""<implementation id="sha1=123" version="1"/>""", Local);

        Assert.Throws<InvalidDataException>(() => feed.AddFrom(XmlStorage.FromXmlString<Feed>(Local), new SimpleCommandExecutor()));
    }

    [Fact]
    public void MergeIntoExistingGroup()
    {
        var feed = Merge("""<group><implementation id="sha1=123" version="1"/></group>""", Local);

        feed.Implementations.Should().HaveCount(2);
        GetGroups(feed).Should().ContainSingle(because: "The implementation should join the existing group");
        feed["sha1=002"].Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void MergeSkipsGroupWithUnwantedAttribute()
    {
        var feed = Merge("""<group doc-dir="doc"><implementation id="sha1=123" version="1"/></group>""", Local);

        feed.Implementations.Should().HaveCount(2);
        feed.Elements.OfType<Implementation>().Should().ContainSingle().Which.ID.Should().Be("sha1=002", because: "The existing group sets an attribute the new implementation does not want");
        feed["sha1=002"].DocDir.Should().BeNull();
    }

    [Fact]
    public void MergePicksGroupWithMatchingRequirements()
    {
        const string masterBody = """
            <group>
              <implementation id="sha1=123" version="1"/>
            </group>
            <group>
              <requires interface="http://foo"/>
              <implementation id="sha1=002" version="2"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalReq);

        feed.Implementations.Should().HaveCount(3);
        GetGroups(feed).Should().HaveCount(2, because: "The group with the matching <requires> should be reused");

        var implementation = feed["sha1=003"];
        implementation.Dependencies.Should().BeEmpty(because: "The requirement is inherited from the group");
        implementation.Main.Should().Be("hello", because: "Attributes not provided by the group move to the implementation");
    }

    [Fact]
    public void MergePicksGroupWithMatchingRequirementsRegardlessOfOrder()
    {
        const string masterBody = """
            <group>
              <requires interface="http://foo"/>
              <implementation id="sha1=002" version="2"/>
            </group>
            <group>
              <implementation id="sha1=123" version="1"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalReq);

        feed.Implementations.Should().HaveCount(3);
        GetGroups(feed).Should().HaveCount(2);
        feed["sha1=003"].Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void MergeKeepsForeignNamespaceAttributes()
    {
        var feed = Merge("", LocalReq);

        var attributes = (feed["sha1=003"].UnknownAttributes ?? []).ToDictionary(x => x.LocalName, x => x.Value);
        attributes.Should().Contain("x", "x")
                  .And.Contain("z", "z")
                  .And.Contain("bob", "bob", because: "Foreign attributes on the group are inherited by the implementation");
    }

    [Fact]
    public void MergeCreatesSubGroupForNewCommand()
    {
        const string masterBody = """
            <group>
              <requires interface="http://foo">
                <environment name="TESTING" value="true" mode="replace"/>
              </requires>
              <implementation id="sha1=002" version="2"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalCommand);

        feed.Implementations.Should().HaveCount(2);
        GetGroups(feed).Should().HaveCount(2, because: "A sub-group is needed to hold the new <command>");
        GetGroups(feed).SelectMany(x => x.Dependencies).Should().ContainSingle(because: "The <requires> should be shared");
        GetGroups(feed).SelectMany(x => x.Commands).Should().ContainSingle();
    }

    [Fact]
    public void MergeSharesMatchingCommand()
    {
        const string masterBody = """
            <group>
              <requires interface="http://foo">
                <environment name="TESTING" value="true" mode="replace"/>
              </requires>
              <command name="run" path="run.sh"/>
              <implementation id="sha1=002" version="2"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalCommand);

        feed.Implementations.Should().HaveCount(2);
        GetGroups(feed).Should().ContainSingle(because: "Both the <requires> and the <command> can be shared");
        GetGroups(feed).SelectMany(x => x.Commands).Should().ContainSingle();
    }

    [Fact]
    public void MergeOverridesDifferingCommand()
    {
        const string masterBody = """
            <group>
              <requires interface="http://foo">
                <environment name="TESTING" value="true" mode="replace"/>
              </requires>
              <command name="run" path="old-run.sh"/>
              <implementation id="sha1=002" version="2"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalCommand);

        feed.Implementations.Should().HaveCount(2);
        GetGroups(feed).Should().HaveCount(2);
        GetGroups(feed).SelectMany(x => x.Dependencies).Should().ContainSingle(because: "The <requires> should still be shared");
        GetGroups(feed).SelectMany(x => x.Commands).Should().HaveCount(2);
    }

    [Fact]
    public void MergeMainAndCommandIntoMatchingGroup()
    {
        const string masterBody = """
            <group main="main">
              <command name="run" path="run.sh"/>
              <implementation id="sha1=001" version="0.1"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalMainAndCommand);

        feed.Implementations.Should().HaveCount(2);
        GetGroups(feed).Should().ContainSingle();
        feed["sha1=002"].Main.Should().BeNull(because: "The group already provides the same main");
    }

    [Fact]
    public void MergeMainAndCommandNeedsOwnGroup()
    {
        const string masterBody = """
            <group>
              <command name="run" path="run.sh"/>
              <implementation id="sha1=001" version="0.1"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalMainAndCommand);

        feed.Implementations.Should().HaveCount(2);
        var groups = GetGroups(feed).ToList();
        groups.Should().HaveCount(2, because: "The main must not override the inherited run command");
        groups.Should().Contain(x => x.Main == "main");
        feed["sha1=002"].Main.Should().BeNull(because: "The new sub-group provides the main");
    }

    [Fact]
    public void MergeKeepsVersionFilteredCommandsApart()
    {
        const string masterBody = """
            <group>
              <command name="run" path="run.sh"/>
              <implementation id="sha1=004" version="0.4"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalIf);

        GetGroups(feed).SelectMany(x => x.Commands).Should().HaveCount(3, because: "Commands with different if-0install-version are distinct");
    }

    [Fact]
    public void MergeSharesVersionFilteredCommand()
    {
        const string masterBody = """
            <group>
              <command name="run" path="run-old.sh" if-0install-version="..!2"/>
              <command name="run" path="run-mid.sh" if-0install-version="2.."/>
              <implementation id="sha1=004" version="0.4"/>
            </group>
            """;
        var feed = Merge(masterBody, LocalIf);

        GetGroups(feed).SelectMany(x => x.Commands).Should().HaveCount(3, because: "Only the differing run-new.sh command needs to be added");
    }
}
