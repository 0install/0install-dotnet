// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq.Expressions;

namespace ZeroInstall;

public static class MockExtensions
{
    public static Mock<T> SetupFluent<T>(this Mock<T> mock, Expression<Func<T, T>> expression) where T : class
    {
        mock.Setup(expression).Returns(mock.Object);
        return mock;
    }
}
