// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Store.Trust
{
    /// <summary>
    /// Contains test methods for <see cref="TrustDB"/>.
    /// </summary>
    public class TrustDBTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="TrustDB"/>.
        /// </summary>
        private static TrustDB CreateTestTrust() => new()
        {
            Keys =
            {
                new()
                {
                    Fingerprint = "abc",
                    Domains =
                    {
                        new() {Value = "0install.de"},
                        new() {Value = "eicher.net"}
                    }
                }
            }
        };

        [Fact] // Ensures that methods for adding and removing trusted keys work correctly.
        public void TestAddRemoveTrust()
        {
            var trust = new TrustDB();
            trust.IsTrusted("abc", new("domain")).Should().BeFalse();

            trust.TrustKey("abc", new("domain"));
            trust.Keys.Should().Equal(new Key {Fingerprint = "abc", Domains = {new("domain")}});
            trust.IsTrusted("abc", new Domain("domain")).Should().BeTrue();

            trust.UntrustKey("abc", new("domain"));
            trust.IsTrusted("abc", new("domain")).Should().BeFalse();
        }

        [Fact] // Ensures that the class is correctly serialized and deserialized.
        public void TestSaveLoad()
        {
            TrustDB trust1 = CreateTestTrust(), trust2;
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

        [Fact] // Ensures that the class can be correctly cloned.
        public void TestClone()
        {
            var trust1 = CreateTestTrust();
            var trust2 = trust1.Clone();

            // Ensure data stayed the same
            trust2.Should().Be(trust1, because: "Cloned objects should be equal.");
            trust2.GetHashCode().Should().Be(trust1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            trust2.Should().NotBeSameAs(trust1, because: "Cloning should not return the same reference.");
        }
    }
}
