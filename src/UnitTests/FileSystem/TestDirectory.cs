// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
namespace ZeroInstall.FileSystem
{
    /// <summary>
    /// Represents a directory used for testing file system operations.
    /// It can either be realized on-disk or compared against an existing on-disk directory.
    /// </summary>
    /// <seealso cref="TestRoot"/>
    public class TestDirectory : TestElement, IEnumerable<TestElement>
    {
        /// <summary>
        /// The last write time of the directory.
        /// </summary>
        public DateTime LastWrite { get; set; }

        /// <summary>
        /// The <seealso cref="TestElement"/>s contained within the directory.
        /// Walked recursively by <seealso cref="Build"/> and <seealso cref="Verify"/>.
        /// </summary>
        public List<TestElement> Children { get; } = new();

        public IEnumerator<TestElement> GetEnumerator() => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        /// <summary>
        /// Adds an element to <seealso cref="Children"/>.
        /// </summary>
        public void Add(TestElement element) => Children.Add(element);

        /// <summary>
        /// Creates a new test directory.
        /// </summary>
        /// <param name="name">The name of the directory.</param>
        public TestDirectory(string name)
            : base(name)
        {}

        public override void Build(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            Directory.CreateDirectory(path);

            foreach (var element in Children)
                element.Build(path);

            if (LastWrite != default)
                Directory.SetLastWriteTimeUtc(path, LastWrite);
        }

        public override void Verify(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            Directory.Exists(path).Should().BeTrue(because: $"Directory '{path}' should exist.");
            if (LastWrite != default)
                Directory.GetLastWriteTimeUtc(path).Should().Be(LastWrite, because: $"Directory '{path}' should have correct last-write time.");

            foreach (var element in Children)
                element.Verify(path);
        }
    }
}
