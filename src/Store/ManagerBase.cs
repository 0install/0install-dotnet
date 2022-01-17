// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Threading;

#if NETFRAMEWORK
using System.Security.AccessControl;
using System.Security.Principal;
#endif

namespace ZeroInstall.Store;

/// <summary>
/// Common base class for managers that need an <see cref="ITaskHandler"/> and <see cref="Mutex"/>-based locking.
/// </summary>
[PrimaryConstructor]
public abstract partial class ManagerBase : IDisposable
{
    /// <summary>
    /// A callback object used when the the user needs to be asked questions or informed about download and IO tasks.
    /// </summary>
    protected readonly ITaskHandler Handler;

    /// <summary>
    /// Apply operations machine-wide instead of just for the current user.
    /// </summary>
    public bool MachineWide { get; }

    /// <summary>
    /// The name of the cross-process mutex used by <see cref="AcquireMutex"/>.
    /// </summary>
    protected abstract string MutexName { get; }

    private Mutex? _mutex;

    /// <summary>
    /// Tries to acquire a mutex with the name <see cref="MutexName"/>. Call this at the end of your constructors.
    /// </summary>
    /// <exception cref="TimeoutException">Another process is already holding the mutex.</exception>
    protected void AcquireMutex()
    {
#if NETFRAMEWORK
            if (MachineWide)
            {
                var mutexSecurity = new MutexSecurity();
                mutexSecurity.AddAccessRule(new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), MutexRights.FullControl, AccessControlType.Allow));
                _mutex = new Mutex(false, @"Global\" + MutexName, out _, mutexSecurity);
            }
            else
#endif
        {
            _mutex = new Mutex(false, MutexName);
        }

        _mutex.WaitOne(Handler.CancellationToken, (Handler.Verbosity == Verbosity.Batch) ? 30 : 1);
    }

    /// <summary>
    /// Releases the mutex.
    /// </summary>
    public void Dispose()
    {
        _mutex?.ReleaseMutex();
        _mutex?.Close();
    }
}
