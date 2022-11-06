// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Uses mocking to ensure <see cref="CompositeImplementationStore"/> correctly delegates work to its child <see cref="IImplementationStore"/>s.
/// </summary>
public class CompositeImplementationStoreTest : IDisposable
{
    #region Constants
    private static readonly ManifestDigest _digest1 = new(Sha1New: "abc");
    private static readonly ManifestDigest _digest2 = new(Sha1New: "123");
    #endregion

    private readonly Mock<IImplementationStore> _mockStore1 = new(), _mockStore2 = new();
    private readonly CompositeImplementationStore _testStore;

    public CompositeImplementationStoreTest()
    {
        _testStore = new CompositeImplementationStore(new[] {_mockStore1.Object, _mockStore2.Object});
    }

    public void Dispose()
    {
        _mockStore1.VerifyAll();
        _mockStore2.VerifyAll();
    }

    #region List all
    [Fact]
    public void ListAll()
    {
        _mockStore1.Setup(x => x.ListAll()).Returns(new[] {_digest1});
        _mockStore2.Setup(x => x.ListAll()).Returns(new[] {_digest2});
        _testStore.ListAll().Should().BeEquivalentTo(new[] {_digest1, _digest2}, because: "Should combine results from all stores");
    }
    #endregion

    #region Contains
    [Fact]
    public void ContainsFirst()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(true);
        _testStore.Contains(_digest1).Should().BeTrue();
    }

    [Fact]
    public void ContainsSecond()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Contains(_digest1)).Returns(true);
        _testStore.Contains(_digest1).Should().BeTrue();
    }

    [Fact]
    public void ContainsFalse()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Contains(_digest1)).Returns(false);
        _testStore.Contains(_digest1).Should().BeFalse();
    }
    #endregion

    #region Get path
    [Fact]
    public void GetPathFirst()
    {
        _mockStore1.Setup(x => x.GetPath(_digest1)).Returns("path");
        _testStore.GetPath(_digest1).Should().Be("path", because: "Should get path from first mock");
    }

    [Fact]
    public void GetPathSecond()
    {
        _mockStore1.Setup(x => x.GetPath(_digest1)).Returns(() => null);
        _mockStore2.Setup(x => x.GetPath(_digest1)).Returns("path");
        _testStore.GetPath(_digest1).Should().Be("path", because: "Should get path from second mock");
    }

    [Fact]
    public void GetPathFail()
    {
        _mockStore1.Setup(x => x.GetPath(_digest1)).Returns(() => null);
        _mockStore2.Setup(x => x.GetPath(_digest1)).Returns(() => null);
        _testStore.GetPath(_digest1).Should().BeNull();
    }
    #endregion

    #region Add
    [Fact]
    public void AddFirst()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Contains(_digest1)).Returns(false);

        Action<IBuilder> build = _ => {};
        _mockStore2.Setup(x => x.Add(_digest1, build));
        _testStore.Add(_digest1, build);
    }

    [Fact]
    public void AddSecond()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Contains(_digest1)).Returns(false);

        Action<IBuilder> build = _ => {};
        _mockStore2.Setup(x => x.Add(_digest1, build)).Throws(new IOException("Fake IO exception for testing"));
        _mockStore1.Setup(x => x.Add(_digest1, build));
        _testStore.Add(_digest1, build);
    }

    [Fact]
    public void AddFail()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Contains(_digest1)).Returns(false);

        Action<IBuilder> build = _ => {};
        _mockStore2.Setup(x => x.Add(_digest1, build)).Throws(new IOException("Fake IO exception for testing"));
        _mockStore1.Setup(x => x.Add(_digest1, build)).Throws(new IOException("Fake IO exception for testing"));
        Assert.Throws<IOException>(() => _testStore.Add(_digest1, build));
    }

    [Fact]
    public void AddFailAlreadyInStore()
    {
        _mockStore1.Setup(x => x.Contains(_digest1)).Returns(true);

        Assert.Throws<ImplementationAlreadyInStoreException>(() => _testStore.Add(_digest1, _ => {}));
    }
    #endregion

    #region Remove
    [Fact]
    public void RemoveTwo()
    {
        _mockStore1.Setup(x => x.Remove(_digest1)).Returns(true);
        _mockStore2.Setup(x => x.Remove(_digest1)).Returns(true);
        _testStore.Remove(_digest1).Should().BeTrue();
    }

    [Fact]
    public void RemoveOne()
    {
        _mockStore1.Setup(x => x.Remove(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Remove(_digest1)).Returns(true);
        _testStore.Remove(_digest1).Should().BeTrue();
    }

    [Fact]
    public void RemoveNone()
    {
        _mockStore1.Setup(x => x.Remove(_digest1)).Returns(false);
        _mockStore2.Setup(x => x.Remove(_digest1)).Returns(false);
        _testStore.Remove(_digest1).Should().BeFalse();
    }
    #endregion

    #region Verify
    [Fact]
    public void VerifyExitsAfterFirstSuccess()
    {
        _mockStore1.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore1.Setup(x => x.Verify(_digest1));

        _testStore.Verify(_digest1);
    }

    [Fact]
    public void VerifySkipsServiceStore()
    {
        _mockStore1.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.Service);

        _mockStore2.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore2.Setup(x => x.Verify(_digest1));

        _testStore.Verify(_digest1);
    }

    [Fact]
    public void VerifyContinuesOnNotFound()
    {
        _mockStore1.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore1.Setup(x => x.Verify(_digest1))
                   .Throws<ImplementationNotFoundException>();

        _mockStore2.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore2.Setup(x => x.Verify(_digest1));

        _testStore.Verify(_digest1);
    }

    [Fact]
    public void VerifyReportsIfAllNotFound()
    {
        _mockStore1.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore1.Setup(x => x.Verify(_digest1))
                   .Throws<ImplementationNotFoundException>();

        _mockStore2.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore2.Setup(x => x.Verify(_digest1))
                   .Throws<ImplementationNotFoundException>();

        _testStore.Invoking(x => x.Verify(_digest1))
                  .Should().Throw<ImplementationNotFoundException>();
    }

    [Fact]
    public void VerifyReportsFailsFast()
    {
        _mockStore1.SetupGet(x => x.Kind).Returns(ImplementationStoreKind.ReadWrite);
        _mockStore1.Setup(x => x.Verify(_digest1))
                   .Throws<IOException>();

        _testStore.Invoking(x => x.Verify(_digest1))
                  .Should().Throw<IOException>();
    }
    #endregion

    #region Temp
    [Fact]
    public void ListTemp()
    {
        _mockStore1.Setup(x => x.ListTemp()).Returns(new[] {"abc"});
        _mockStore2.Setup(x => x.ListTemp()).Returns(new[] {"def"});
        _testStore.ListTemp().Should().BeEquivalentTo(new[] {"abc", "def"}, because: "Should combine results from all stores");
    }

    [Fact]
    public void TryToRemoveTempFromLastStoreFirst()
    {
        _mockStore2.Setup(x => x.RemoveTemp("abc")).Returns(true);
        _testStore.RemoveTemp("abc").Should().BeTrue();
    }

    [Fact]
    public void TryToRemoveTempFromAllStores()
    {
        _mockStore2.Setup(x => x.RemoveTemp("abc")).Returns(false);
        _mockStore1.Setup(x => x.RemoveTemp("abc")).Returns(true);
        _testStore.RemoveTemp("abc").Should().BeTrue();
    }

    [Fact]
    public void ReportFailureRemovingFromAllStores()
    {
        _mockStore2.Setup(x => x.RemoveTemp("abc")).Returns(false);
        _mockStore1.Setup(x => x.RemoveTemp("abc")).Returns(false);
        _testStore.RemoveTemp("abc").Should().BeFalse();
    }
    #endregion
}
