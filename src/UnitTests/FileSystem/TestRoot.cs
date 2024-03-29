// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.FileSystem;

/// <summary>
/// Represents a directory structure used for testing file system operations.
/// It can either be realized on-disk or compared against an existing on-disk directory.
/// </summary>
/// <seealso cref="TestDirectory"/>
/// <seealso cref="TestFile"/>
/// <seealso cref="TestSymlink"/>
public class TestRoot : List<TestElement>
{
    /// <summary>
    /// Realizes the directory structure as an on-disk directory.
    /// </summary>
    /// <param name="path">The full path of the directory to realize the structure in.</param>
    public void Build(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        foreach (var element in this)
            element.Build(path);
    }

    /// <summary>
    /// Compares the structure against an existing on-disk directory using assertions.
    /// </summary>
    /// <param name="path">The full path of the directory to compare the structure against.</param>
    public void Verify(string path)
    {
        foreach (var element in this)
            element.Verify(path);
    }
}
