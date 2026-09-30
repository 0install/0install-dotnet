// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using NanoByte.Common.Native;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Access control helpers for the Store Service named pipe.
/// </summary>
[SupportedOSPlatform("windows")]
internal static class StoreServiceSecurity
{
    private static readonly SecurityIdentifier
        _localSystem = new(WellKnownSidType.LocalSystemSid, null),
        _administrators = new(WellKnownSidType.BuiltinAdministratorsSid, null),
        _authenticatedUsers = new(WellKnownSidType.AuthenticatedUserSid, null),
        _network = new(WellKnownSidType.NetworkSid, null),
        _creatorOwner = new(WellKnownSidType.CreatorOwnerSid, null),
        _trustedInstaller = new("S-1-5-80-956008885-3418522649-1831038044-1853292631-2271478464");

    /// <summary>
    /// The owners a client accepts for the Store Service pipe. Only privileged processes can create objects owned by these.
    /// </summary>
    public static IReadOnlyCollection<SecurityIdentifier> DefaultTrustedOwners => [_localSystem, _administrators];

    private static SecurityIdentifier CurrentUser
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User!;
        }
    }

    /// <summary>
    /// Creates the ACL for the Store Service pipe.
    /// </summary>
    /// <remarks>
    /// Authenticated users may connect, read and write, but not create new pipe instances (which would allow impersonating the service).
    /// Remote access via the network is denied.
    /// Low integrity processes (e.g., AppContainers) are blocked by the default mandatory integrity label.
    /// </remarks>
    public static PipeSecurity CreatePipeSecurity()
    {
        var security = new PipeSecurity();
        security.AddAccessRule(new(_network, PipeAccessRights.FullControl, AccessControlType.Deny));
        security.AddAccessRule(new(_localSystem, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new(_administrators, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new(CurrentUser, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new(_authenticatedUsers, PipeAccessRights.ReadWrite, AccessControlType.Allow));
        return security;
    }

    /// <summary>
    /// The owners objects created by the current process may have: the current user, or Administrators while running as admin.
    /// </summary>
    public static IReadOnlyCollection<SecurityIdentifier> SelfOwners
        => WindowsUtils.IsAdministrator ? [CurrentUser, _administrators] : [CurrentUser];

    /// <summary>
    /// Determines whether a pipe is owned by the current process's user (or by Administrators while running as admin).
    /// </summary>
    public static bool IsOwnedBySelf(PipeStream pipe)
        => IsOwnedBy(pipe, SelfOwners);

    /// <summary>
    /// Determines whether a pipe is owned by one of the <paramref name="trustedOwners"/>.
    /// </summary>
    public static bool IsOwnedBy(PipeStream pipe, IEnumerable<SecurityIdentifier> trustedOwners)
    {
        var owner = GetOwner(pipe);
        return owner != null && trustedOwners.Contains(owner);
    }

    private static SecurityIdentifier? GetOwner(PipeStream pipe)
        => pipe.GetAccessControl().GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;

    /// <summary>
    /// Determines the name of the user connected to a pipe for logging purposes.
    /// </summary>
    public static string GetClientName(NamedPipeServerStream pipe)
    {
        string? name = null;
        try
        {
            pipe.RunAsClient(() =>
            {
                using var identity = WindowsIdentity.GetCurrent(TokenAccessLevels.Query);
                name = identity.Name;
            });
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or SecurityException or InvalidOperationException)
        {
            Log.Debug("Failed to identify Store Service client", ex);
        }
        return name ?? "(unknown)";
    }

    private const FileSystemRights WriteRights =
        FileSystemRights.WriteData | FileSystemRights.AppendData | FileSystemRights.WriteAttributes | FileSystemRights.WriteExtendedAttributes
      | FileSystemRights.Delete | FileSystemRights.DeleteSubdirectoriesAndFiles | FileSystemRights.ChangePermissions | FileSystemRights.TakeOwnership;

    /// <summary>
    /// Determines whether someone other than the system, administrators or the current user can modify a directory.
    /// </summary>
    /// <param name="path">The directory to check.</param>
    /// <param name="reason">Describes the problem.</param>
    public static bool IsWritableByUntrusted(string path, [NotNullWhen(true)] out string? reason)
    {
        var acl = new DirectoryInfo(path).GetAccessControl();

        var owner = acl.GetOwner(typeof(SecurityIdentifier)) as SecurityIdentifier;
        if (!IsTrusted(owner))
        {
            reason = $"owned by {owner}";
            return true;
        }

        foreach (FileSystemAccessRule rule in acl.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)))
        {
            if (rule.AccessControlType != AccessControlType.Allow) continue;
            if (rule.PropagationFlags.HasFlag(PropagationFlags.InheritOnly)) continue; // Does not apply to the directory itself
            if (rule.IdentityReference is SecurityIdentifier sid && (IsTrusted(sid) || sid == _creatorOwner)) continue;

            if ((rule.FileSystemRights & WriteRights) != 0)
            {
                reason = $"{rule.IdentityReference} has {rule.FileSystemRights}";
                return true;
            }
        }

        reason = null;
        return false;
    }

    private static bool IsTrusted(SecurityIdentifier? sid)
        => sid != null && (sid == _localSystem || sid == _administrators || sid == _trustedInstaller || sid == CurrentUser);
}
