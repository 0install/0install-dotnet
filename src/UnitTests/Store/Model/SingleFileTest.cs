// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using FluentAssertions;
using Xunit;

namespace ZeroInstall.Store.Model
{
    /// <summary>
    /// Contains test methods for <see cref="SingleFile"/>.
    /// </summary>
    public class SingleFileTest
    {
        /// <summary>
        /// Creates a fictive test <see cref="SingleFile"/>.
        /// </summary>
        internal static SingleFile CreateTestSingleFile() => new SingleFile
        {
            Href = new Uri("http://example.com/test.exe"),
            Size = 128,
            Destination = "dest",
            Executable = true
        };

        /// <summary>
        /// Ensures that the class can be correctly cloned.
        /// </summary>
        [Fact]
        public void TestClone()
        {
            var singleFile1 = CreateTestSingleFile();
            var singleFile2 = singleFile1.Clone();

            // Ensure data stayed the same
            singleFile2.Should().Be(singleFile1, because: "Cloned objects should be equal.");
            singleFile2.GetHashCode().Should().Be(singleFile1.GetHashCode(), because: "Cloned objects' hashes should be equal.");
            singleFile2.Should().NotBeSameAs(singleFile1, because: "Cloning should not return the same reference.");
        }
    }
}
