// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.FileSystem;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

public static class ImplementationStoreExtensions
{
    /// <summary>
    /// Adds a dummy/test implementation to the implementation store.
    /// </summary>
    public static string Add(this ImplementationStore store, ManifestDigest manifestDigest, TestRoot root)
    {
        string id = manifestDigest.Best!;
        string path = Path.Combine(store.Path, id);

        root.Build(path);
        var builder = new ManifestBuilder(ManifestFormat.FromPrefix(id));
        new ReadDirectory(path, builder).Run();
        builder.Manifest.Save(Path.Combine(path, Manifest.ManifestFile));

        return path;
    }
}
