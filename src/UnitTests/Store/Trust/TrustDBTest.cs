// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Store.Trust;

/// <summary>
/// Contains test methods for <see cref="TrustDB"/>.
/// </summary>
public class TrustDBTest
{
    [Fact]
    public void TestIsTrusted()
    {
        var trust = new TrustDB
        {
            Keys =
            {
                new() { Fingerprint = "abc", Domains = {new("example1.com") } }
            }
        };
        trust.IsTrusted("abc", new("example1.com"))
             .Should().BeTrue();
        trust.IsTrusted("abc", new("example2.com"))
             .Should().BeFalse();
    }

    [Fact]
    public void TestAddTrust()
    {
        var trust = new TrustDB
        {
            Keys =
            {
                new() { Fingerprint = "abc", Domains = { new("example1.com") } }
            }
        };
        trust.TrustKey("abc", new("example2.com"));
        trust.TrustKey("xyz", new("example2.com"));

        trust.Should().Be(new TrustDB
        {
            Keys =
            {
                new() { Fingerprint = "abc", Domains = { new("example1.com"), new("example2.com") } },
                new() { Fingerprint = "xyz", Domains = { new("example2.com") } }
            }
        });
    }

    [Fact]
    public void TestRemoveTrustKey()
    {
        var trust = new TrustDB
        {
            Keys =
            {
                new() { Fingerprint = "abc", Domains = { new("example1.com"), new("example2.com") } }
            }
        };
        trust.UntrustKey("abc");

        trust.Should().Be(new TrustDB());
    }

    [Fact]
    public void TestRemoveTrustDomain()
    {
        var trust = new TrustDB
        {
            Keys =
            {
                new() { Fingerprint = "abc", Domains = { new("example1.com"), new("example2.com") } }
            }
        };
        trust.UntrustKey("abc", new("example1.com"));

        trust.Should().Be(new TrustDB
        {
            Keys =
            {
                new() { Fingerprint = "abc", Domains = { new("example2.com") } }
            }
        });
    }

    [Fact] // Ensures that the class is correctly serialized and deserialized.
    public void TestSaveLoad()
    {
        TrustDB trust1 = new() { Keys = { new() { Fingerprint = "abc", Domains = { new("example.com") } } } }, trust2;
        using (var tempFile = new TemporaryFile("0install-test-trustdb"))
        {
            // Write and read file
            trust1.Save(tempFile);
            trust2 = TrustDB.Load(tempFile);
        }

        // Ensure data stayed the same
        trust2.Should().Be(trust1, because: "Serialized objects should be equal.");
        trust2.GetHashCode().Should().Be(trust1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
        ReferenceEquals(trust1, trust2).Should().BeFalse(because: "Serialized objects should not return the same reference.");
    }

    [Fact]
    public void Save()
    {
        using var tempFile = new TemporaryFile("0install-test-trustdb");
        var original = new TrustDB();
        original.Save(tempFile);

        var loaded = TrustDB.Load(tempFile);

        original.Save().Should().BeFalse(because: "No loaded-from path to save back to");
        loaded.Save().Should().BeTrue(because: "Loaded from disk");
    }

    [Fact]
    public void TestClone()
    {
        var trust1 = new TrustDB() { Keys = { new() { Fingerprint = "abc", Domains = { new("example.com") } } } };
        var trust2 = trust1.Clone();

        // Ensure data stayed the same
        trust2.Should().Be(trust1, because: "Cloned objects should be equal.");
        trust2.GetHashCode().Should().Be(trust1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
        trust2.Should().NotBeSameAs(trust1, because: "Cloning should not return the same reference.");
    }
}
