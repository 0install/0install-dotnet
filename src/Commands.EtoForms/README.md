# ZeroInstall.Commands.EtoForms

Cross-platform graphical user interface for Zero Install commands, based on [Eto.Forms](https://github.com/picoe/Eto).

[Zero Install](https://0install.net/) is a decentralized cross-platform software installation system. This package is part of the [.NET implementation](https://github.com/0install/0install-dotnet) of Zero Install.

This library contains no Eto.Forms platform backend. Reference the appropriate `Eto.Platform.*` package in your application, start an Eto.Forms `Application` and pass a `GuiCommandHandler` to `ProgramUtils.Run()`.
