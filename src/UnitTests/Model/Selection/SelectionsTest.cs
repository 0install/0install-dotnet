// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections.Generic;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Model.Selection
{
    /// <summary>
    /// Contains test methods for <see cref="Selections"/>.
    /// </summary>
    public class SelectionsTest
    {
        /// <summary>
        /// Creates a <see cref="Selections"/> with two implementations, one using the other as a runner plus a number of bindings.
        /// </summary>
        public static Selections CreateTestSelections() => new()
        {
            InterfaceUri = FeedTest.Test1Uri,
            Command = Command.NameRun,
            Implementations =
            {
                ImplementationSelectionTest.CreateTestImplementation1(),
                ImplementationSelectionTest.CreateTestImplementation2()
            }
        };

        /// <summary>
        /// Ensures that <see cref="Selections.GetImplementation"/> and <see cref="Selections.this"/> correctly retrieve implementations.
        /// </summary>
        [Fact]
        public void GetImplementation()
        {
            var implementation = CreateTestSelections();

            implementation.GetImplementation(FeedTest.Test1Uri).Should().Be(implementation.Implementations[0]);
            implementation[FeedTest.Test1Uri].Should().Be(implementation.Implementations[0]);

            implementation.GetImplementation(new("http://invalid/")).Should().BeNull();

            Assert.Throws<KeyNotFoundException>(() => implementation[new FeedUri("http://invalid/")]);
        }

        /// <summary>
        /// Ensures that the class is correctly serialized and deserialized.
        /// </summary>
        [Fact]
        public void SaveLoad()
        {
            Selections selections1 = CreateTestSelections(), selections2;
            using (var tempFile = new TemporaryFile("0install-test-feed"))
            {
                // Write and read file
                selections1.SaveXml(tempFile);
                selections2 = XmlStorage.LoadXml<Selections>(tempFile);
            }

            // Ensure data stayed the same
            selections2.Should().Be(selections1, because: "Serialized objects should be equal.");
            selections2.GetHashCode().Should().Be(selections1.GetHashCode(), because: "Serialized objects' hashes should be equal.");
            selections2.Should().NotBeSameAs(selections1, because: "Serialized objects should not return the same reference.");
        }

        /// <summary>
        /// Ensures that the class can be correctly cloned and compared.
        /// </summary>
        [Fact]
        public void CloneEquals()
        {
            var selections1 = CreateTestSelections();
            selections1.Should().Be(selections1, because: "Equals() should be reflexive.");
            selections1.GetHashCode().Should().Be(selections1.GetHashCode(), because: "GetHashCode() should be reflexive.");

            var selections2 = selections1.Clone();
            selections2.Should().Be(selections1, because: "Cloned objects should be equal.");
            selections2.GetHashCode().Should().Be(selections1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            selections2.Should().NotBeSameAs(selections1, because: "Cloning should not return the same reference.");

            selections2.Implementations.Add(new ImplementationSelection {ID = "dummy"});
            selections2.Should().NotBe(selections1, because: "Modified objects should no longer be equal");
        }
    }
}
