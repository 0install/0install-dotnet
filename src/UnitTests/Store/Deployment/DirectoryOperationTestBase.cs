// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Deployment;

/// <summary>
/// Common base class for testing classes <see cref="DirectoryOperation"/> derived from.
/// </summary>
public abstract class DirectoryOperationTestBase : IDisposable
{
    protected readonly TemporaryDirectory TempDir = new("0install-test-dir");
    public virtual void Dispose() => TempDir.Dispose();

    protected readonly string File1Path;
    protected readonly string SubdirPath;
    protected readonly string File2Path;
    protected readonly Manifest Manifest;

    protected DirectoryOperationTestBase()
    {
        File1Path = Path.Combine(TempDir, "file1");
        FileUtils.Touch(File1Path);

        SubdirPath = Path.Combine(TempDir, "subdir");
        Directory.CreateDirectory(SubdirPath);

        File2Path = Path.Combine(SubdirPath, "file2");
        FileUtils.Touch(File2Path);

        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        new ReadDirectory(TempDir, builder).Run();
        Manifest = builder.Manifest;
    }
}
