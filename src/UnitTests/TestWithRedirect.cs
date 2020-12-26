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
    [Collection("LocationsRedirect")]
    public abstract class TestWithRedirect : IDisposable
    {
        private readonly LocationsRedirect _redirect = new("0install-unit-tests");

        public virtual void Dispose() => _redirect.Dispose();
    }
}
