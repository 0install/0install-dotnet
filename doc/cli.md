---
uid: command-line-interface
---

# Command-line interface

The <xref:ZeroInstall.Commands> namespace provides a command-line interface for Zero Install. This both implements an actual command-line executable and provides a library for building other clients.

## Processing arguments

[ProgramUtils.Init()](xref:ZeroInstall.Commands.ProgramUtils#ZeroInstall_Commands_ProgramUtils_Init) should be called by all clients right after startup to initialize process-wide state.

Afterwards [ProgramUtils.Run()](xref:ZeroInstall.Commands.ProgramUtils#ZeroInstall_Commands_ProgramUtils_Run_System_String_System_Collections_Generic_IReadOnlyList_System_String__ZeroInstall_Commands_ICommandHandler_) can be called with the command-line arguments to be parsed along with an <xref:ZeroInstall.Commands.ICommandHandler>. `ICommandHandler` extends the <xref:NanoByte.Common.Tasks.ITaskHandler> interface with various UI interactions that commands can use to display intermediate results, ask for user input, etc..

`ProgramUtils.Run()` parses the provided command-line arguments and selects the appropriate <xref:ZeroInstall.Commands.CliCommand> to handle them.

## Inheritance hierarchy

Each [command](https://docs.0install.net/details/cli/) (e.g., `0install run`) is represented by a class derived from <xref:ZeroInstall.Commands.CliCommand> (e.g., <xref:ZeroInstall.Commands.Basic.Run>).

`CliCommand` derives from <xref:ZeroInstall.Commands.ScopedOperation>. This provides a more basic starting point, for any operation that requires scoped [dependency resolution](xref:services#dependency-injection). It provides this by itself deriving from <xref:ZeroInstall.Services.ServiceProvider>.
