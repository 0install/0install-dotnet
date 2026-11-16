// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.Linq.Expressions;

namespace ZeroInstall;

public static class MockExtensions
{
    extension<T>(Mock<T> mock) where T : class
    {
        public Mock<T> SetupFluent(Expression<Func<T, T>> expression)
        {
            mock.Setup(expression).Returns(mock.Object);
            return mock;
        }
    }
}
