// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Commands;

ProgramUtils.Init();

using var handler = new CliCommandHandler();
return (int)ProgramUtils.Run(ProgramUtils.CliAssemblyName, args, handler);
