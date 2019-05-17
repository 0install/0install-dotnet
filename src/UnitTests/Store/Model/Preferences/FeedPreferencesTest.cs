// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Store.Model.Preferences
{
    /// <summary>
    /// Contains test methods for <see cref="FeedPreferences"/>.
    /// </summary>
    public class FeedPreferencesTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="FeedPreferences"/>.
        /// </summary>
        public static FeedPreferences CreateTestFeedPreferences() => new FeedPreferences
        {
            LastChecked = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Implementations = {new ImplementationPreferences {ID = "test_id", UserStability = Stability.Testing}}
        };

        [Fact] // Ensures that the class is correctly serialized and deserialized.
        public void TestSaveLoad()
        {
            FeedPreferences preferences1 = CreateTestFeedPreferences(), preferences2;
            using (var tempFile = new TemporaryFile("0install-unit-tests"))
            {
                // Write and read file
                preferences1.SaveXml(tempFile);
                preferences2 = XmlStorage.LoadXml<FeedPreferences>(tempFile);
            }

            // Ensure data stayed the same
            preferences2.Should().Be(preferences1, because: "Serialized objects should be equal.");
            preferences2.GetHashCode().Should().Be(preferences1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
            preferences2.Should().NotBeSameAs(preferences1, because: "Serialized objects should not return the same reference.");
        }

        [Fact] // Ensures that the class can be correctly cloned.
        public void TestClone()
        {
            var preferences1 = CreateTestFeedPreferences();
            var preferences2 = preferences1.Clone();

            // Ensure data stayed the same
            preferences2.Should().Be(preferences1, because: "Cloned objects should be equal.");
            preferences2.GetHashCode().Should().Be(preferences1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            preferences2.Should().NotBeSameAs(preferences1, because: "Cloning should not return the same reference.");
        }

        /// <summary>
        /// Ensures that <see cref="FeedPreferences.Normalize"/> correctly removes superfluous entries from <see cref="FeedPreferences.Implementations"/>.
        /// </summary>
        [Fact]
        public void TestNormalize()
        {
            var keep = new ImplementationPreferences {ID = "id1", UserStability = Stability.Testing};
            var superfluous = new ImplementationPreferences {ID = "id2"};
            var preferences = new FeedPreferences {Implementations = {keep, superfluous}};

            preferences.Normalize();
            preferences.Implementations.Should().BeEquivalentTo(keep);
        }

        [Fact]
        public void TestGetImplementationPreferences()
        {
            var preferences = new FeedPreferences();
            var preferences1 = preferences["id1"];
            preferences["id1"].Should().BeSameAs(preferences1, because: "Second call with same ID should return same reference");

            var preferences2 = new ImplementationPreferences {ID = "id2"};
            preferences.Implementations.Add(preferences2);
            preferences["id2"].Should().BeSameAs(preferences2, because: "Call with pre-existing ID should return existing reference");

            preferences.Implementations.Should().BeEquivalentTo(preferences1, preferences2);
        }
    }
}
