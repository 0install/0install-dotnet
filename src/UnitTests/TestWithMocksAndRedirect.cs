// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Storage;

namespace ZeroInstall;

/// <summary>
/// Common base class for test fixtures that use a <see cref="Moq.MockRepository"/> and <see cref="Locations.Redirect"/>.
/// </summary>
public abstract class TestWithMocksAndRedirect : TestWithMocks
{
    private readonly TestWithRedirect _redirect = new();

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
