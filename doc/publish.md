---
uid: publishing
---

# Publishing

The <xref:ZeroInstall.Publish> namespace provides utilities for creating and modifying feed files. This provides the basis for the [Zero Install Publishing Tools](https://github.com/0install/0publish-win). You can also use it to create your own tools.

## Feed signing

The <xref:ZeroInstall.Publish.SignedFeed> class provides a wrapper around <xref:ZeroInstall.Model.Feed> that adds [OpenPGP signatures](https://docs.0install.net/specifications/feed/#digital-signatures) to feed files when saving.

## Set missing values

The [.SetMissing()](xref:ZeroInstall.Publish.ImplementationExtensions#ZeroInstall_Publish_ImplementationExtensions_SetMissing_ZeroInstall_Model_Implementation_NanoByte_Common_Undo_ICommandExecutor_NanoByte_Common_Tasks_ITaskHandler_) extension method for <xref:ZeroInstall.Model.Implementation> sets missing properties by downloading, extracting and hashing files as needed.

## Feed editing

The <xref:ZeroInstall.Publish.FeedEditing> class is a container for editing feeds with an <xref:undo-system>.
