---
uid: client-library
---

# Client library

The <xref:ZeroInstall.Client> namespace allows you to invoke Zero Install commands from within other applications.

Use <xref:ZeroInstall.Client.ZeroInstallClient.Detect> to automatically discover the locations of the `0install` and/or `0install-win` executables. This gives you an instance of <xref:ZeroInstall.Client.IZeroInstallClient>, which enables easy programmatic access to a subset of the <xref:command-line-interface>.

## Samples

Detect `0install` location and the feed URI used to launch the app:

```csharp
var zeroInstall = ZeroInstallClient.Detect;
var feedUri = ZeroInstallEnvironment.FeedUri ?? new FeedUri("https://example.com/your-feed.xml");
```

Download updates if available:

```csharp
var selections = await zeroInstall.DownloadAsync(feedUri, refresh: true);
if (selections.MainImplementation.Version > new ImplementationVersion(AppInfo.Current.Version ?? "0"))
    NotifyUpdateAvailable();
```

Restart the app to run the latest version:

```csharp
zeroInstall.Run(feedUri);
Environment.Exit(0);
```

Toggle the app's auto start [desktop integration](https://docs.0install.net/details/desktop-integration/):

```csharp
bool isAutoStartEnabled = (await zeroInstall.GetIntegrationAsync(feedUri)).Contains("auto-start");
if (isAutoStartEnabled)
    await zeroInstall.IntegrateAsync(feedUri, remove: new[] {"auto-start"});
else
    await zeroInstall.IntegrateAsync(feedUri, add: new[] {"auto-start"});
```

## Error handling

The client library maps `0install`'s exit codes to exceptions. Your code should be ready to catch:

- <xref:System.IO.IOException>: `0install` could not be launched or there was a problem accessing the filesystem.
- <xref:System.Net.WebException>: There was a problem downloading a file.
- <xref:System.OperationCanceledException>: The user canceled the operation.
- <xref:NanoByte.Common.ExitCodeException>: Any other kind of error.
