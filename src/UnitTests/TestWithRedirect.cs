// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Storage;

namespace ZeroInstall;

/// <summary>
/// Common base class for test fixtures that use <see cref="Locations.Redirect"/>.
/// </summary>
public class TestWithRedirect : IDisposable
{
    private readonly TemporaryDirectory _tempDir = new("0install-test-redirect");

    private readonly IDisposable _redirect;

    public TestWithRedirect()
    {
        _redirect = Locations.Redirect(_tempDir);
    }

    public virtual void Dispose()
    {
        _redirect.Dispose();
        _tempDir.Dispose();
    }
}
