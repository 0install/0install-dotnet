// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.FileSystem;

/// <summary>
/// Represents a file system element used for testing file system operations.
/// It can either be realized on-disk or compared against an existing on-disk element.
/// </summary>
/// <seealso cref="TestRoot"/>
public abstract class TestElement
{
    /// <summary>
    /// The name of the file system element.
    /// </summary>
    public string Name { get; }

    protected TestElement(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Realizes the element as an on-disk element.
    /// </summary>
    /// <param name="parentPath">The full path of the existing directory to realize the element in.</param>
    public abstract void Build(string parentPath);

    /// <summary>
    /// Compares the element against an existing on-disk element using assertions.
    /// </summary>
    /// <param name="parentPath">The full path of the directory containing the element.</param>
    public abstract void Verify(string parentPath);
}
