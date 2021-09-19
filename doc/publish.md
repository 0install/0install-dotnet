# Publishing

The \ref ZeroInstall.Publish namespace provides utilities for creating and modifying feed files. This provides the basis for the [Zero Install Publishing Tools](https://github.com/0install/0publish-win). You can also use it to create your own tools.

### Feed signing

The \ref ZeroInstall.Publish.SignedFeed "SignedFeed" class provides a wrapper around \ref ZeroInstall.Model.Feed that adds [OpenPGP signatures](https://docs.0install.net/specifications/feed/#digital-signatures) to feed files when saving.

### Set missing values

The \ref ZeroInstall.Publish.ImplementationExtensions.SetMissing ".SetMissing()" extension method for \ref ZeroInstall.Model.Implementation sets missing properties by downloading, extracting and hashing files as needed.

### Feed editing

The \ref ZeroInstall.Publish.FeedEditing "FeedEditing" class is a container for editing feeds with an [undo system](https://common.nano-byte.net/md_undo.html).
