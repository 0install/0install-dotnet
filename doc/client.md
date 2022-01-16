---
uid: client-library
---

# Client library

The <xref:ZeroInstall.Client> namespace allows you to invoke Zero Install commands from within other applications.

```csharp
var zeroInstall = ZeroInstallClient.Default;
var feedUri = ZeroInstallEnvironment.FeedUri ?? new FeedUri("https://example.com/your-feed.xml");

// Download updates
var selections = await zeroInstall.DownloadAsync(feedUri, refresh: true);
if (selections.MainImplementation.Version > CurrentVersion) {
    // New version is available!

    // Restart app
    zeroInstall.Run(feedUri);
    Environment.Exit(0);
}

// Manage auto-start
bool isAutoStartEnabled = (await zeroInstall.GetIntegrationAsync(feedUri)).Contains("auto-start");
await zeroInstall.IntegrateAsync(feedUri, add: new[] { "auto-start" });
await zeroInstall.IntegrateAsync(feedUri, remove: new[] { "auto-start" });
```
