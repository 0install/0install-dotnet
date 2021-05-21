// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall
{
    /// <summary>
    /// Common base class for test fixtures that use a <see cref="LocationsRedirect"/>.
    /// </summary>
    [Collection("Static state")]
    public abstract class TestWithRedirect : IDisposable
    {
        private readonly LocationsRedirect _redirect = new("0install-test-redirect");

        public virtual void Dispose() => _redirect.Dispose();
    }
}
