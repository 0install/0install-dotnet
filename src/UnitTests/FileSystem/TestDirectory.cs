// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Collections;

namespace ZeroInstall.FileSystem;

/// <summary>
/// Represents a directory used for testing file system operations.
/// It can either be realized on-disk or compared against an existing on-disk directory.
/// </summary>
/// <param name="name">The name of the directory.</param>
/// <seealso cref="TestRoot"/>
public class TestDirectory(string name) : TestElement(name), IEnumerable<TestElement>
{
    /// <summary>
    /// The <see cref="TestElement"/>s contained within the directory.
    /// Walked recursively by <see cref="Build"/> and <see cref="Verify"/>.
    /// </summary>
    public List<TestElement> Children { get; } = [];

    public IEnumerator<TestElement> GetEnumerator() => Children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Adds an element to <see cref="Children"/>.
    /// </summary>
    public void Add(TestElement element) => Children.Add(element);

    public override void Build(string parentPath)
    {
        string path = Path.Combine(parentPath, Name);
        Directory.CreateDirectory(path);

        foreach (var element in Children)
            element.Build(path);
    }

    public override void Verify(string parentPath)
    {
        string path = Path.Combine(parentPath, Name);
        Directory.Exists(path).Should().BeTrue(because: $"Directory '{path}' should exist.");

        foreach (var element in Children)
            element.Verify(path);
    }
}
