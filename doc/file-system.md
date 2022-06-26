---
uid: file-system
---

# File system

The <xref:ZeroInstall.Store.FileSystem> namespace provides interfaces and methods for building and reading file system structures.

The <xref:ZeroInstall.Store.FileSystem.IBuilder> interface represents a implementation directory being constructed.  
<xref:ZeroInstall.Store.FileSystem.IForwardOnlyBuilder> is a more limited subset, that only allows the addition of files but not the modification of files that have already been added.

There are various implementations of <xref:ZeroInstall.Store.FileSystem.IBuilder>:

- <xref:ZeroInstall.Store.FileSystem.DirectoryBuilder> for creating a real on-disk directory
- <xref:ZeroInstall.Store.Manifests.ManifestBuilder> for calculating an in-memory [manifest](xref:ZeroInstall.Store.Manifests.Manifest) of the directory
- <xref:ZeroInstall.Archives.Builders> for creating archives (`.zip`, `.tar`, etc.)

## Implementation store

When adding an implementation to an <xref:ZeroInstall.Store.Implementations.IImplementationStore> the caller is provided an <xref:ZeroInstall.Store.FileSystem.IBuilder> via a callback:

```csharp
store.Add(manifestDigest, builder =>
{
    builder.AddFile(...);
    builder.AddFile(...);
});
```

This allows the implementation store to control how and where the implementation gets constructed (usually via a composite of a <xref:ZeroInstall.Store.FileSystem.DirectoryBuilder> and a <xref:ZeroInstall.Store.Manifests.ManifestBuilder>).

## Retrieval methods

<xref:ZeroInstall.Store.FileSystem.BuilderExtensions> and <xref:ZeroInstall.Archives.BuilderExtensions> provide extension methods for applying <xref:ZeroInstall.Model.RetrievalMethod>s to an <xref:ZeroInstall.Store.FileSystem.IBuilder>. These methods internally pass the <xref:ZeroInstall.Store.FileSystem.IBuilder> to <xref:ZeroInstall.Store.FileSystem.ReadDirectory>, <xref:ZeroInstall.Archives.Extractors>, etc..
