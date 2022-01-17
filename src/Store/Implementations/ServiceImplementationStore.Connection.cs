// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if NETFRAMEWORK
using System.Collections;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using System.Security.Principal;

namespace ZeroInstall.Store.Implementations;

public partial class ServiceImplementationStore
{
    /// <summary>
    /// The IPC port to use to contact the store service.
    /// </summary>
    public const string IpcPort = "ZeroInstall.Store.Service";

    /// <summary>
    /// The IPC port to use to contact the store service.
    /// </summary>
    public const string IpcCallbackPort = IpcPort + ".Callback";

    /// <summary>
    /// ACL for IPC named pipes. Allows object owners, normal users and the system write access.
    /// </summary>
    public static readonly CommonSecurityDescriptor IpcAcl;

    [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Must build ACL during init")]
    static ServiceImplementationStore()
    {
        var dacl = new DiscretionaryAcl(false, false, 1);
        dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
        dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
        dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
        IpcAcl = new(false, false, ControlFlags.GroupDefaulted | ControlFlags.OwnerDefaulted | ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);
    }

    private static readonly object _lock = new();
    private static volatile IImplementationSink? _proxy;

    /// <summary>
    /// Provides a proxy object for accessing the <see cref="IImplementationStore"/> in the store service.
    /// </summary>
    /// <exception cref="RemotingException">There is a problem connecting with the store service.</exception>
    /// <remarks>Always returns the same instance. Opens named pipes on first call. Connection is only established on demand.</remarks>
    [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "May throw exceptions")]
    private static IImplementationSink GetProxy()
    {
        // Thread-safe singleton with double-check
        if (_proxy == null)
        {
            lock (_lock)
            {
                if (_proxy == null)
                    _proxy = CreateServiceProxy();
            }
        }
        return _proxy;
    }

    /// <summary>
    /// Sets up named pipes and creates a proxy object for accessing the <see cref="IImplementationStore"/> in the store service.
    /// </summary>
    /// <remarks>Must only be called once per process!</remarks>
    private static IImplementationSink CreateServiceProxy()
    {
        Log.Debug("Attempting to connect to Store Service");

        // IPC channel for accessing the server
        ChannelServices.RegisterChannel(
            new IpcClientChannel(
                new Hashtable
                {
                    {"name", IpcPort},
                    {"secure", true},
                    {"tokenImpersonationLevel", "impersonation"} // Allow server to use identity of client
                },
                new BinaryClientFormatterSinkProvider()),
            ensureSecurity: false);

        // IPC channel for providing callbacks to the server
        ChannelServices.RegisterChannel(new IpcServerChannel(
            new Hashtable
            {
                {"name", IpcCallbackPort},
                {"portName", IpcCallbackPort + "." + System.IO.Path.GetRandomFileName()} // Random port to allow multiple instances
            },
            new BinaryServerFormatterSinkProvider {TypeFilterLevel = TypeFilterLevel.Full}, // Allow deserialization of custom types
            IpcAcl), ensureSecurity: false);

        // Create proxy object
        return (IImplementationSink)Activator.GetObject(typeof(IImplementationSink), $"ipc://{IpcPort}/{nameof(IImplementationSink)}");
    }
}
#endif
