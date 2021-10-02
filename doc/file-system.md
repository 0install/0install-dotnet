# File system

The \ref ZeroInstall.Store.FileSystem namespace provides interfaces and methods for building and reading file system structures.

The \ref ZeroInstall.Store.FileSystem.IBuilder "IBuilder" interface represents a implementation directory being constructed. (\ref ZeroInstall.Store.FileSystem.IForwardOnlyBuilder "IForwardOnlyBuilder" is a more limited subset, that only allows the addition of files but not the modification of files that have already been added.)

There are various implementations of `IBuilder`:

- \ref ZeroInstall.Store.FileSystem.DirectoryBuilder for creating a real on-disk directory
- \ref ZeroInstall.Store.Manifests.ManifestBuilder for calculating an in-memory \ref ZeroInstall.Store.Manifests.Manifest "manifest" of the directory
- \ref ZeroInstall.Archives.Builders for creating archives (`.zip`, `.tar`, etc.)

**Implementation store**

When adding an implementation to an \ref ZeroInstall.Store.Implementations.IImplementationStore the caller is provided an `IBuilder` via a callback:

```{.cs}
store.Add(manifestDigest, builder =>
{
    builder.AddFile(...);
    builder.AddFile(...);
});
```

This allows the implementation store to control how and where the implementation gets constructed (usually via a composite of a `DirectoryBuilder` and a `ManifestBuilder`).

**Retrieval methods**

\ref ZeroInstall.Store.FileSystem.BuilderExtensions and \ref ZeroInstall.Archives.BuilderExtensions provide extension methods for applying \ref ZeroInstall.Model.RetrievalMethod "retrieval methods" to an `IBuilder`. These methods internally pass the `IBuilder` to \ref ZeroInstall.Store.FileSystem.ReadDirectory, \ref ZeroInstall.Archives.Extractors, etc..
