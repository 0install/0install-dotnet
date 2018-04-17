// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Net;
using JetBrains.Annotations;
using NanoByte.Common;
using ZeroInstall.Services.Feeds;
using ZeroInstall.Store.Model;
using ZeroInstall.Store.Model.Selection;

namespace ZeroInstall.Services.Solvers
{
    /// <summary>
    /// A wrapper around an <see cref="ISolver"/> that handles automatic refreshing of stale feeds.
    /// </summary>
    /// <remarks>Do not use this if you want manual control over when refreshing happens, e.g. to ensure fast startup time of cached implementations.</remarks>
    public class RefreshingSolver : ISolver
    {
        #region Dependencies
        private readonly ISolver _innerSolver;
        private readonly IFeedManager _feedManager;

        public RefreshingSolver([NotNull] ISolver innerSolver, [NotNull] IFeedManager feedManager)
        {
            _innerSolver = innerSolver ?? throw new ArgumentNullException(nameof(innerSolver));
            _feedManager = feedManager ?? throw new ArgumentNullException(nameof(feedManager));
        }
        #endregion

        /// <inheritdoc/>
        public Selections Solve(Requirements requirements)
        {
            var selections = _innerSolver.Solve(requirements);

            if (_feedManager.ShouldRefresh)
            {
                Log.Info("Running Refresh Solve because feeds have become stale");

                _feedManager.Stale = false;
                _feedManager.Refresh = true;
                try
                {
                    selections = _innerSolver.Solve(requirements);
                }
                catch (WebException ex)
                {
                    Log.Warn(ex.Message);
                }
            }

            return selections;
        }
    }
}
