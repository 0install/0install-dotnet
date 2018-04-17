// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall
{
    /// <summary>
    /// Common base class for test fixtures that use a <see cref="Moq.MockRepository"/> and <see cref="LocationsRedirect"/>.
    /// </summary>
    [Collection("LocationsRedirect")]
    public abstract class TestWithMocksAndRedirect : TestWithMocks
    {
        private readonly LocationsRedirect _redirect = new LocationsRedirect("0install-unit-tests");

        public override void Dispose()
        {
            try
            {
                base.Dispose();
            }
            finally
            {
                _redirect.Dispose();
            }
        }
    }
}
