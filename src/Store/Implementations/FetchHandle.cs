// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Provides a way to share an <see cref="Implementation"/> fetch callback as per-thread ambient state.
    /// </summary>
    /// <remarks>This is useful for making the high-level Fetcher service available to low-level systems such as a Recipe step.</remarks>
    public static class FetchHandle
    {
        [ThreadStatic]
        private static Func<Implementation, string> _callback;

        /// <summary>
        /// Registers an <see cref="Implementation"/> fetch callback for the current thread.
        /// </summary>
        /// <param name="callback">A callback that downloads an implementation to a local cache if missing and returns its path.</param>
        /// <returns>A handle that can be used to remove the registration.</returns>
        [NotNull]
        public static IDisposable Register([NotNull] Func<Implementation, string> callback)
        {
            #region Sanity checks
            if (callback == null) throw new ArgumentNullException(nameof(callback));
            #endregion

            var previousValue = _callback;
            _callback = callback;
            return new Disposable(() => _callback = previousValue);
        }

        /// <summary>
        /// Downloads an <see cref="Implementation"/> to a local cache if missing and returns its path. <see cref="Register"/> must be called first on the same thread.
        /// </summary>
        /// <param name="implementation">The implementation to be downloaded.</param>
        /// <returns>A fully qualified path to the directory containing the implementation.</returns>
        /// <exception cref="InvalidOperationException"><see cref="Register"/> was not called first.</exception>
        [NotNull]
        public static string Use([NotNull] Implementation implementation)
        {
            #region Sanity checks
            if (implementation == null) throw new ArgumentNullException(nameof(implementation));
            #endregion

            if (_callback == null) throw new InvalidOperationException("Implementation provider must be registered first on the same thread.");

            return _callback(implementation);
        }
    }
}
