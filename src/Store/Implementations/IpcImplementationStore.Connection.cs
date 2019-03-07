// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

#if !NETSTANDARD2_0
using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Serialization.Formatters;
using System.Security.AccessControl;
using System.Security.Principal;
using NanoByte.Common;

namespace ZeroInstall.Store.Implementations
{
    public partial class IpcImplementationStore
    {
        /// <summary>
        /// The port name to use to contact the store service.
        /// </summary>
        public const string IpcPortName = "ZeroInstall.Store.Service";

        /// <summary>
        /// The Uri fragment to use to request an <see cref="IImplementationStore"/> object from other processes.
        /// </summary>
        public const string IpcObjectUri = "Store";

        /// <summary>
        /// ACL for IPC named pipes. Allows object owners, normal users and the system write access.
        /// </summary>
        public static readonly CommonSecurityDescriptor IpcAcl;

        [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline", Justification = "Must build ACL during init")]
        static IpcImplementationStore()
        {
            var dacl = new DiscretionaryAcl(false, false, 1);
            dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.CreatorOwnerSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
            dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
            dacl.AddAccess(AccessControlType.Allow, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), -1, InheritanceFlags.None, PropagationFlags.None);
            IpcAcl = new CommonSecurityDescriptor(false, false, ControlFlags.GroupDefaulted | ControlFlags.OwnerDefaulted | ControlFlags.DiscretionaryAclPresent, null, null, null, dacl);
        }

        private static readonly object _lock = new object();
        private static volatile IImplementationStore _proxy;

        /// <summary>
        /// Provides a proxy object for accessing the <see cref="IImplementationStore"/> in the store service.
        /// </summary>
        /// <exception cref="RemotingException">There is a problem connecting with the store service.</exception>
        /// <remarks>Always returns the same instance. Opens named pipes on first call. Connection is only established on demand.</remarks>
        [SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate", Justification = "May throw exceptions")]
        private static IImplementationStore GetProxy()
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
        private static IImplementationStore CreateServiceProxy()
        {
            Log.Debug("Attempting to connect to Store Service");

            // IPC channel for accessing the server
            ChannelServices.RegisterChannel(
                new IpcClientChannel(
                    new Hashtable
                    {
                        {"name", IpcPortName},
                        {"secure", true},
                        {"tokenImpersonationLevel", "impersonation"} // Allow server to use identity of client
                    },
                    new BinaryClientFormatterSinkProvider()),
                ensureSecurity: false);

            // IPC channel for providing callbacks to the server
            ChannelServices.RegisterChannel(new IpcServerChannel(
                new Hashtable
                {
                    {"name", IpcPortName + ".Callback"},
                    {"portName", IpcPortName + ".Callback." + Path.GetRandomFileName()} // Random port to allow multiple instances
                },
                new BinaryServerFormatterSinkProvider {TypeFilterLevel = TypeFilterLevel.Full}, // Allow deserialization of custom types
                IpcAcl), ensureSecurity: false);

            // Create proxy object
            return (IImplementationStore)Activator.GetObject(typeof(IImplementationStore), "ipc://" + IpcPortName + "/" + IpcObjectUri);
        }
    }
}
#endif
