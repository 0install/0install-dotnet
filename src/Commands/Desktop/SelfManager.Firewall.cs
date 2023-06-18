// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Diagnostics;
using NanoByte.Common.Native;

namespace ZeroInstall.Commands.Desktop;

partial class SelfManager
{
    private const string
        FirewallSharing = "Zero Install - Implementation sharing",
        FirewallDiscoveryCli = "Zero Install - Service discovery (0install)",
        FirewallDiscoveryGui = "Zero Install - Service discovery (0install-win)";

    /// <summary>
    /// Adds Windows Firewall rules for implementation sharing in local network.
    /// </summary>
    private void FirewallRulesApply()
    {
        if (!WindowsUtils.IsWindowsNT) return;

        Handler.RunTask(new ActionTask("Configuring Windows Firewall", () =>
        {
            var httpSys = ("binary", "system");
            var ianaPrivatePorts = ("tcp", "49152–65535");
            FirewallAddRule(FirewallSharing, target: httpSys, protocol: ianaPrivatePorts);

            var mDns = ("udp", "5353");
            FirewallAddRule(FirewallDiscoveryCli, target: ("program", Path.Combine(TargetDir, "0install.exe")), protocol: mDns);
            FirewallAddRule(FirewallDiscoveryGui, target: ("program", Path.Combine(TargetDir, "0install-win.exe")), protocol: mDns);
        }));
    }

    /// <summary>
    /// Removed Windows Firewall rules for implementation sharing in local network.
    /// </summary>
    private void FirewallRulesRemove()
    {
        if (!WindowsUtils.IsWindowsNT) return;

        Handler.RunTask(new ActionTask("Configuring Windows Firewall", () =>
        {
            FirewallRemoveRule(FirewallSharing);
            FirewallRemoveRule(FirewallDiscoveryCli);
            FirewallRemoveRule(FirewallDiscoveryGui);
        }));
    }

    private static void FirewallAddRule(string name, (string type, string id) target, (string type, string ports) protocol)
    {
        try
        {
            RunHidden("netsh", "advfirewall", "firewall", "add", "rule", $"name={name}",
                $"{target.type}={target.id}", "profile=private,domain",
                "action=allow", "dir=in", $"protocol={protocol.type}", $"localport={protocol.ports}");
        }
        #region Error handling
        catch (Exception ex)
        {
            // Firewall rules are not needed for most 0install features. Failure here should not block entire deployment.
            Log.Warn("Failed to apply firewall rules", ex);
        }
        #endregion
    }

    private static void FirewallRemoveRule(string name)
    {
        try
        {
            RunHidden("netsh", "advfirewall", "firewall", "delete", "rule", $"name={name}");
        }
        #region Error handling
        catch (Exception ex)
        {
            // Orphaned firewall rules have no effect. Failure here should not block entire removal.
            Log.Warn("Failed to remove firewall rules", ex);
        }
        #endregion
    }
}
