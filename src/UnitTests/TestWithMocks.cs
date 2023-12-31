// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall;

/// <summary>
/// Common base class for test fixtures that use a <see cref="Moq.MockRepository"/>.
/// </summary>
public abstract class TestWithMocks : IDisposable
{
    private readonly Dictionary<Type, Mock> _mocks = [];

    /// <summary>
    /// Retrieves a <see cref="Mock"/> for a specific type. Multiple requests for the same type return the same mock instance.
    /// </summary>
    /// <remarks>All created <see cref="Mock"/>s are automatically verified after the test completes.</remarks>
    protected Mock<T> GetMock<T>() where T : class
        => (Mock<T>)_mocks.GetOrAdd(typeof(T), () => new Mock<T>(MockBehavior.Strict));

    public virtual void Dispose()
    {
        foreach (var mock in _mocks.Values)
            mock.VerifyAll();
    }
}
