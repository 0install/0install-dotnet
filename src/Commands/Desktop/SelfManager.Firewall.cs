// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using WindowsFirewallHelper;

namespace ZeroInstall.Commands.Desktop;

partial class SelfManager
{
    private const string
        FirewallDiscovery = "Zero Install - Service discovery",
        FirewallDiscoveryGui = "Zero Install - Service discovery (GUI)";

    /// <summary>
    /// Adds Windows Firewall rules for implementation sharing in local network.
    /// </summary>
    private void FirewallRulesApply()
    {
        if (!WindowsUtils.IsWindowsVista) return;

        Handler.RunTask(new ActionTask("Windows Firewall", () =>
        {
            try
            {
                var firewall = FirewallWAS.Instance;
                void AddMDnsRule(string name, string path)
                {
                    var rule = firewall.CreatePortRule(
                        FirewallProfiles.Private | FirewallProfiles.Domain,
                        name,
                        FirewallAction.Allow, FirewallDirection.Inbound,
                        portNumber: 5353, FirewallProtocol.UDP);
                    rule.ApplicationName = path;
                    rule.Grouping = "Zero Install";
                    firewall.Rules.Remove(rule.Name); // Overwrite existing rule if present
                    firewall.Rules.Add(rule);
                }
                AddMDnsRule(FirewallDiscovery, Path.Combine(TargetDir, "0install.exe"));
                AddMDnsRule(FirewallDiscoveryGui, Path.Combine(TargetDir, "0install-win.exe"));
            }
            #region Error handling
            catch (Exception ex)
            {
                // Firewall rules are not needed for most 0install features. Failure here should not block entire deployment.
                Log.Warn("Failed to apply Windows Firewall rules", ex);
            }
            #endregion
        }));
    }

    /// <summary>
    /// Removed Windows Firewall rules for implementation sharing in local network.
    /// </summary>
    private void FirewallRulesRemove()
    {
        if (!WindowsUtils.IsWindowsVista) return;

        Handler.RunTask(new ActionTask("Windows Firewall", () =>
        {
            try
            {
                var firewall = FirewallWAS.Instance;
                firewall.Rules.Remove(FirewallDiscovery);
                firewall.Rules.Remove(FirewallDiscoveryGui);
            }
            #region Error handling
            catch (Exception ex)
            {
                // Orphaned firewall rules have no effect. Failure here should not block entire removal.
                Log.Warn("Failed to remove Windows Firewall rules", ex);
            }
            #endregion
        }));
    }
}
