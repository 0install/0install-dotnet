# ZeroInstall.Client

Client library for invoking Zero Install commands from within other applications.

[Zero Install](https://0install.net/) is a decentralized cross-platform software installation system.

Use `ZeroInstallClient.Detect` to automatically discover the locations of the `0install` and/or `0install-win` executables. This gives you an instance of `IZeroInstallClient`, which enables easy programmatic access to a subset of the Zero Install command-line interface.

## Samples

Detect `0install` location and the feed URI used to launch the app:

```csharp
var zeroInstall = ZeroInstallClient.Detect;
var feedUri = ZeroInstallEnvironment.FeedUri ?? new FeedUri("https://example.com/your-feed.xml");
```

Download updates if available:

```csharp
if (await zeroInstall.UpdateAsync(feedUri))
    NotifyUpdateAvailable();
```

Restart the app to run the latest version:

```csharp
zeroInstall.Run(feedUri);
Environment.Exit(0);
```

Toggle the app's auto start [desktop integration](https://docs.0install.net/details/desktop-integration/):

```csharp
bool isAutoStartEnabled = zeroInstall.GetIntegration(feedUri).Contains(IntegrationCategories.AutoStart);
if (isAutoStartEnabled)
    await zeroInstall.IntegrateAsync(feedUri, remove: new[] {IntegrationCategories.AutoStart});
else
    await zeroInstall.IntegrateAsync(feedUri, add: new[] {IntegrationCategories.AutoStart});
```

## Error handling

The client library maps `0install`'s exit codes to exceptions. Your code should be ready to catch:

- `IOException`: `0install` could not be launched or there was a problem accessing the filesystem.
- `WebException`: There was a problem downloading a file.
- `OperationCanceledException`: The user canceled the operation.
- `NanoByte.Common.ExitCodeException`: Any other kind of error.
