// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.Diagnostics;
using NanoByte.Common.Native;
#endif

namespace ZeroInstall.Commands.Desktop.SelfManagement;

partial class SelfManager
{
#if NETFRAMEWORK
    private static readonly string _ngenExe = Path.Combine(WindowsUtils.GetNetFxDirectory(WindowsUtils.NetFx40), "ngen.exe");

    private static readonly string[] _ngenAssemblies =
    {
        "0install.exe",
        "0install-win.exe",
        "0launch.exe",
        "0alias.exe",
        "0store.exe",
        "0store-service.exe",
        "ZeroInstall.exe",
        "ZeroInstall.OneGet.dll",
        "ZeroInstall.Model.XmlSerializers.dll"
    };
#endif

    /// <summary>
    /// Runs ngen in the background to pre-compile new/updated .NET assemblies.
    /// </summary>
    private void NgenApply()
    {
#if NETFRAMEWORK
        if (!WindowsUtils.IsWindows) return;
        if (!File.Exists(_ngenExe)) return;

        Handler.RunTask(ForEachTask.Create(Resources.RunNgen, _ngenAssemblies, assembly =>
        {
            string arguments = new[] {"install", Path.Combine(TargetDir, assembly), "/queue"}.JoinEscapeArguments();
            new ProcessStartInfo(_ngenExe, arguments) {WindowStyle = ProcessWindowStyle.Hidden}.Run();
        }));
#endif
    }

    /// <summary>
    /// Runs ngen to remove pre-compiled .NET assemblies.
    /// </summary>
    private void NgenRemove()
    {
#if NETFRAMEWORK
        if (!WindowsUtils.IsWindows) return;
        if (!File.Exists(_ngenExe)) return;

        foreach (string assembly in _ngenAssemblies)
        {
            string arguments = new[] {"uninstall", Path.Combine(TargetDir, assembly)}.JoinEscapeArguments();
            new ProcessStartInfo(_ngenExe, arguments) {WindowStyle = ProcessWindowStyle.Hidden}.Run();
        }
#endif
    }
}
