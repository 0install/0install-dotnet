// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall
{
    /// <summary>
    /// Common base class for test fixtures that use a <see cref="Moq.MockRepository"/> and <see cref="LocationsRedirect"/>.
    /// </summary>
    [Collection("Static state")]
    public abstract class TestWithMocksAndRedirect : TestWithMocks
    {
        private readonly LocationsRedirect _redirect = new("0install-test-redirect");

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
