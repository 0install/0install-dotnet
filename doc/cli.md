# Command-line interface

The \ref ZeroInstall.Commands namespace provides a command-line interface for Zero Install. This both implements an actual command-line executable and provides a library for building other clients.

### Processing arguments

\ref ZeroInstall.Commands.ProgramUtils.Init "ProgramUtils.Init()" should be called by all clients right after startup to initialize process-wide state.

Afterwards \ref ZeroInstall.Commands.ProgramUtils.Run "ProgramUtils.Run()" can be called with the command-line arguments to be parsed along with an \ref ZeroInstall.Commands.ICommandHandler "ICommandHandler". `ICommandHandler` extends the [ITaskHandler](https://common.nano-byte.net/md_tasks.html) interface with various UI interactions that commands can use to display intermediate results, ask for user input, etc..

`ProgramUtils.Run()` parses the provided command-line arguments and selects the appropriate \ref ZeroInstall.Commands.CliCommand "CliCommand" to handle them.

### Inheritance hierarchy

Each [command](https://docs.0install.net/details/cli/) (e.g., `0install run`) is represented by a class derived from \ref ZeroInstall.Commands.CliCommand "CliCommand" (e.g., \ref ZeroInstall.Commands.Basic.Run).

`CliCommand` derives from \ref ZeroInstall.Commands.ScopedOperation "ScopedOperation". This provides a more basic starting point, for any operation that requires scoped \ref md_services "dependency resolution". It provides this by itself deriving from \ref ZeroInstall.Services.ServiceProvider.
