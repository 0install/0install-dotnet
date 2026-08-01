# ZeroInstall.Archives

Extracting and building archives (`.zip`, `.tar`, etc.) for Zero Install.

[Zero Install](https://0install.net/) is a decentralized cross-platform software installation system. This package is part of the [.NET implementation](https://github.com/0install/0install-dotnet) of Zero Install.

The `ZeroInstall.Archives.Extractors` namespace provides extractors for the [archive formats](https://docs.0install.net/specifications/feed/#archive) supported by Zero Install. Use `ArchiveExtractor.For()` to get an extractor for a specific MIME type.

The `ZeroInstall.Archives.Builders` namespace provides the inverse: builders that create archives from directories, for use when publishing new implementations.
