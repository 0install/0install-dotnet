// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using JetBrains.Annotations;
using NanoByte.Common.Tasks;

namespace ZeroInstall.Store
{
    /// <summary>
    /// Common base class for managers that need an <see cref="ITaskHandler"/> and <see cref="Mutex"/>-based locking.
    /// </summary>
    public abstract class ManagerBase : IDisposable
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
        /// Creates a new manager.
        /// </summary>
        /// <param name="handler">A callback object used when the the user needs to be asked questions or informed about download and IO tasks.</param>
        /// <param name="machineWide">Apply operations machine-wide instead of just for the current user.</param>
        protected ManagerBase([NotNull] ITaskHandler handler, bool machineWide = false)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            MachineWide = machineWide;
        }

        /// <summary>
        /// The name of the cross-process mutex used by <see cref="AcquireMutex"/>.
        /// </summary>
        protected abstract string MutexName { get; }

        private Mutex _mutex;

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
                _mutex = new Mutex(false, MutexName);

            var timeout = TimeSpan.FromSeconds((Handler.Verbosity == Verbosity.Batch) ? 30 : 1);
            _mutex.WaitOne(timeout, Handler.CancellationToken);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        ~ManagerBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Releases the mutex.
        /// </summary>
        /// <param name="disposing"><c>true</c> if called manually and not by the garbage collector.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Close();
            }
        }
    }
}
