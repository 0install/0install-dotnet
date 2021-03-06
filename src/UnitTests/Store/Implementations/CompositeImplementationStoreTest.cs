// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using FluentAssertions;
using Moq;
using Xunit;
using ZeroInstall.Model;
using ZeroInstall.Services;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Uses mocking to ensure <see cref="CompositeImplementationStore"/> correctly delegates work to its child <see cref="IImplementationStore"/>s.
    /// </summary>
    public class CompositeImplementationStoreTest : TestWithMocks
    {
        #region Constants
        private static readonly ManifestDigest _digest1 = new(sha1New: "abc");
        private static readonly ManifestDigest _digest2 = new(sha1New: "123");
        private static readonly IImplementationSource[] _archives =
        {
            new ArchiveImplementationSource("path1", Archive.MimeTypeZip),
            new ArchiveImplementationSource("path2", Archive.MimeTypeZip)
        };
        #endregion

        private readonly MockTaskHandler _handler;
        private readonly Mock<IImplementationStore> _mockStore1, _mockStore2;
        private readonly CompositeImplementationStore _testStore;

        public CompositeImplementationStoreTest()
        {
            _handler = new MockTaskHandler();

            _mockStore1 = CreateMock<IImplementationStore>();
            _mockStore2 = CreateMock<IImplementationStore>();

            _testStore = new CompositeImplementationStore(new[] {_mockStore1.Object, _mockStore2.Object});
        }

        #region List all
        [Fact]
        public void ListAll()
        {
            _mockStore1.Setup(x => x.ListAll()).Returns(new[] {_digest1});
            _mockStore2.Setup(x => x.ListAll()).Returns(new[] {_digest2});
            _testStore.ListAll().Should().BeEquivalentTo(new[] {_digest1, _digest2}, because: "Should combine results from all stores");
        }

        [Fact]
        public void ListAllTemp()
        {
            _mockStore1.Setup(x => x.ListAllTemp()).Returns(new[] {"abc"});
            _mockStore2.Setup(x => x.ListAllTemp()).Returns(new[] {"def"});
            _testStore.ListAllTemp().Should().BeEquivalentTo(new[] {"abc", "def"}, because: "Should combine results from all stores");
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

            _mockStore2.Setup(x => x.Add(_digest1, _handler, _archives));
            _testStore.Add(_digest1, _handler, _archives);
        }

        [Fact]
        public void AddSecond()
        {
            _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
            _mockStore2.Setup(x => x.Contains(_digest1)).Returns(false);

            _mockStore2.Setup(x => x.Add(_digest1, _handler, _archives)).Throws(new IOException("Fake IO exception for testing"));
            _mockStore1.Setup(x => x.Add(_digest1, _handler, _archives));
            _testStore.Add(_digest1, _handler, _archives);
        }

        [Fact]
        public void AddFail()
        {
            _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
            _mockStore2.Setup(x => x.Contains(_digest1)).Returns(false);

            _mockStore2.Setup(x => x.Add(_digest1, _handler, _archives)).Throws(new IOException("Fake IO exception for testing"));
            _mockStore1.Setup(x => x.Add(_digest1, _handler, _archives)).Throws(new IOException("Fake IO exception for testing"));
            Assert.Throws<IOException>(() => _testStore.Add(_digest1, _handler, _archives));
        }

        [Fact]
        public void AddFailAlreadyInStore()
        {
            _mockStore1.Setup(x => x.Contains(_digest1)).Returns(true);

            Assert.Throws<ImplementationAlreadyInStoreException>(() => _testStore.Add(_digest1, _handler, _archives));
        }
        #endregion

        #region Remove
        [Fact]
        public void RemoveTwo()
        {
            _mockStore1.Setup(x => x.Remove(_digest1, _handler)).Returns(true);
            _mockStore2.Setup(x => x.Remove(_digest1, _handler)).Returns(true);
            _testStore.Remove(_digest1, _handler).Should().BeTrue();
        }

        [Fact]
        public void RemoveOne()
        {
            _mockStore1.Setup(x => x.Remove(_digest1, _handler)).Returns(false);
            _mockStore2.Setup(x => x.Remove(_digest1, _handler)).Returns(true);
            _testStore.Remove(_digest1, _handler).Should().BeTrue();
        }

        [Fact]
        public void RemoveNone()
        {
            _mockStore1.Setup(x => x.Remove(_digest1, _handler)).Returns(false);
            _mockStore2.Setup(x => x.Remove(_digest1, _handler)).Returns(false);
            _testStore.Remove(_digest1, _handler).Should().BeFalse();
        }
        #endregion

        #region Verify
        [Fact]
        public void Verify()
        {
            _mockStore1.Setup(x => x.Contains(_digest1)).Returns(false);
            _mockStore2.Setup(x => x.Contains(_digest1)).Returns(true);
            _mockStore2.Setup(x => x.Verify(_digest1, _handler));

            _testStore.Verify(_digest1, _handler);
        }
        #endregion
    }
}
