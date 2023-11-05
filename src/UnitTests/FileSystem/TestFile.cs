// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Text;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.FileSystem;

/// <summary>
/// Represents a file used for testing file system operations.
/// It can either be realized on-disk or compared against an existing on-disk file.
/// </summary>
/// <param name="name">The name of the directory.</param>
/// <seealso cref="TestRoot"/>
public class TestFile(string name) : TestElement(name)
{
    /// <summary>
    /// The default value for <see cref="LastWrite"/>.
    /// </summary>
    public static readonly UnixTime DefaultLastWrite = new DateTime(2000, 1, 1, 0, 0, 0);

    /// <summary>
    /// The last write time of the file.
    /// </summary>
    public UnixTime LastWrite { get; init; } = DefaultLastWrite;

    /// <summary>
    /// The default value for <see cref="Contents"/>.
    /// </summary>
    public const string DefaultContents = "AAA";

    /// <summary>
    /// The contents of the file encoded in UTF8 without a BOM.
    /// </summary>
    public string Contents { get; init; } = DefaultContents;

    /// <summary>
    /// Is the file marked as executable.
    /// </summary>
    public bool IsExecutable { get; init; }

    public override void Build(string parentPath)
    {
        string path = Path.Combine(parentPath, Name);
        File.WriteAllText(path, Contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.SetLastWriteTimeUtc(path, LastWrite);

        if (IsExecutable) ImplFileUtils.SetExecutable(path);
    }

    public override void Verify(string parentPath)
    {
        string path = Path.Combine(parentPath, Name);
        File.Exists(path).Should().BeTrue(because: $"File '{path}' should exist.");
        File.GetLastWriteTimeUtc(path).Should().Be(LastWrite, because: $"File '{path}' should have correct last-write time.");

        ImplFileUtils.IsExecutable(path).Should().Be(IsExecutable);
    }
}
