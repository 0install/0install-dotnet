# ZeroInstall.Bootstrap

Library for building bootstrappers that download and run Zero Install or an application using Zero Install.

[Zero Install](https://0install.net/) is a decentralized cross-platform software installation system.

A bootstrapper is a small executable that does not require Zero Install to already be deployed on the system. It uses `BootstrapProcess` to download a full version of Zero Install and hands over to it.

## Samples

A minimal command-line bootstrapper:

```csharp
using ZeroInstall.Bootstrap;

ProgramUtils.Init();

using var handler = new CliBootstrapHandler();
return (int)ProgramUtils.Run(args, handler);
```

Implement `IBootstrapHandler` to provide your own user interface.

## Configuration

`BootstrapConfig` loads its settings from an INI file named after the executable and placed next to it (e.g., `my-app.ini` for `my-app.exe`). If there is no such file it falls back to the default config embedded in this library.

To customize the settings, add your own INI file to your project and have it copied to the output directory. You can use the `BootstrapConfig.ini` in the root of this package, which documents all available options, as a starting point. For example, to target a specific application:

```ini
[bootstrap]
app_uri=https://example.com/your-feed.xml
app_name=Your App
```

## Embedded content

Files embedded in your executable as resources named `ZeroInstall.content.*` are imported into Zero Install when the bootstrapper runs. This can be used to bundle feeds, OpenPGP keys, icons and implementation archives. The resource names do not depend on your namespace, so set them explicitly:

```xml
<EmbeddedResource Include="content\*" LogicalName="ZeroInstall.content.%(Filename)%(Extension)" />
```
