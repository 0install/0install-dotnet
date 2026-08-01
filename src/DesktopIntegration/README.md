# ZeroInstall.DesktopIntegration

Integrating applications with [desktop environments](https://docs.0install.net/details/desktop-integration/) (creating menu entries, etc.).

[Zero Install](https://0install.net/) is a decentralized cross-platform software installation system. This package is part of the [.NET implementation](https://github.com/0install/0install-dotnet) of Zero Install.

The `IntegrationManager` class applies and removes access points for apps in the user's app list. The `ZeroInstall.DesktopIntegration.AccessPoints` namespace models the various kinds of integration, such as menu entries, desktop icons, file type associations and auto start entries.

Platform-specific implementations live in the `ZeroInstall.DesktopIntegration.Windows`, `.Unix` and `.MacOS` namespaces.

`SyncIntegrationManager` additionally synchronizes the app list and its access points with a [sync server](https://docs.0install.net/details/sync/).
