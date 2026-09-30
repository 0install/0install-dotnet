// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO.Pipes;
using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using NanoByte.Common.Native;
using NanoByte.Common.Streams;
using ZeroInstall.FileSystem;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Manifests;

namespace ZeroInstall.Store.Implementations;

/// <summary>
/// Tests <see cref="ServiceImplementationSink"/> and <see cref="StoreServiceServer"/> talking to each other via a named pipe.
/// </summary>
[SupportedOSPlatform("windows")]
public class StoreServiceTest : IDisposable
{
    private const string Data = "data";

    private readonly TemporaryDirectory _storeDir = CreateSecureDirectory();
    private readonly string _pipeName = "ZeroInstall.Store.Service.Test." + Guid.NewGuid();
    private StoreServiceServer? _server;

    public StoreServiceTest()
    {
        Assert.SkipUnless(WindowsUtils.IsWindowsNT, "Store Service is only available on Windows");
    }

    /// <summary>
    /// Creates a temporary directory that only the current user, administrators and the system can write to, like a machine-wide store.
    /// </summary>
    internal static TemporaryDirectory CreateSecureDirectory()
    {
        var directory = new TemporaryDirectory("0install-unit-test-store");
        if (WindowsUtils.IsWindowsNT)
        {
            var acl = new DirectorySecurity();
            acl.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
            foreach (var sid in new[] {CurrentUser, new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null), new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null)})
                acl.AddAccessRule(new FileSystemAccessRule(sid, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
            new DirectoryInfo(directory).SetAccessControl(acl);
        }
        return directory;
    }

    public void Dispose()
    {
        _server?.Dispose();
        _storeDir.Dispose();
    }

    private void StartServer(StoreServiceLimits? limits = null)
    {
        _server = new StoreServiceServer([_storeDir], _pipeName, limits ?? new());
        _server.Start();
    }

    private static SecurityIdentifier CurrentUser
    {
        get
        {
            using var identity = WindowsIdentity.GetCurrent();
            return identity.User!;
        }
    }

    private ServiceImplementationSink CreateClient(IReadOnlyCollection<SecurityIdentifier>? trustedOwners = null, TimeSpan? timeout = null)
        => new(_pipeName, trustedOwners ?? StoreServiceSecurity.SelfOwners, timeout ?? TimeSpan.FromSeconds(30));

    private static readonly Action<IBuilder> _build = builder =>
    {
        builder.AddDirectory("dir");
        builder.AddFile(Path.Combine("dir", "file"), Data.ToStream(), modifiedTime: 1337);
        builder.AddFile(Path.Combine("dir", "exe"), Data.ToStream(), modifiedTime: 1337, executable: true);
        builder.AddFile("unknown-length", new NonSeekableStream(Data.ToStream()), modifiedTime: 1337);
        builder.AddFile("to-remove", Data.ToStream(), modifiedTime: 1337);
        builder.Remove("to-remove");
        builder.AddFile("to-rename", Data.ToStream(), modifiedTime: 1337);
        builder.Rename("to-rename", "renamed");
        builder.AddHardlink("hardlink", "renamed");
        builder.AddSymlink("symlink", "renamed");
    };

    private static ManifestDigest CalculateDigest(Action<IBuilder> build)
    {
        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        build(builder);
        return new(builder.Manifest.CalculateDigest());
    }

    [Fact]
    public void AddsImplementation()
    {
        StartServer();
        var digest = CalculateDigest(_build);

        CreateClient().Add(digest, _build);

        new TestRoot
        {
            new TestDirectory("dir")
            {
                new TestFile("file") {Contents = Data, LastWrite = 1337},
                new TestFile("exe") {Contents = Data, LastWrite = 1337, IsExecutable = true}
            },
            new TestFile("unknown-length") {Contents = Data, LastWrite = 1337},
            new TestFile("renamed") {Contents = Data, LastWrite = 1337},
            new TestFile("hardlink") {Contents = Data, LastWrite = 1337},
            new TestSymlink("symlink", "renamed")
        }.Verify(Path.Combine(_storeDir, digest.Best!));
        File.Exists(Path.Combine(_storeDir, digest.Best!, "to-remove")).Should().BeFalse();
        ImplementationStoreUtils.Verify(Path.Combine(_storeDir, digest.Best!), digest, new SilentTaskHandler());
        ListTempDirs().Should().BeEmpty();
    }

    [Fact]
    public void AddsImplementationViaCompositeStore()
    {
        StartServer();
        using var userStoreDir = new TemporaryDirectory("0install-unit-test-user-store");
        var store = new CompositeImplementationStore([new ImplementationStore(userStoreDir, new SilentTaskHandler(), useWriteProtection: false)], [CreateClient()]);
        var digest = CalculateDigest(_build);

        store.Add(digest, _build);

        Directory.Exists(Path.Combine(_storeDir, digest.Best!)).Should().BeTrue(because: "Store Service should be preferred");
        Directory.Exists(Path.Combine(userStoreDir, digest.Best!)).Should().BeFalse();
    }

    [Fact]
    public void CompositeStoreFallsBackIfServiceUnavailable()
    {
        using var userStoreDir = new TemporaryDirectory("0install-unit-test-user-store");
        var store = new CompositeImplementationStore([new ImplementationStore(userStoreDir, new SilentTaskHandler(), useWriteProtection: false)], [CreateClient()]);
        var digest = CalculateDigest(_build);

        store.Add(digest, _build);

        Directory.Exists(Path.Combine(userStoreDir, digest.Best!)).Should().BeTrue();
    }

    [Fact]
    public void ReportsAlreadyInStore()
    {
        StartServer();
        var digest = CalculateDigest(_build);
        CreateClient().Add(digest, _build);

        CreateClient().Invoking(x => x.Add(digest, _build))
                      .Should().Throw<ImplementationAlreadyInStoreException>();
    }

    [Fact]
    public void ReportsDigestMismatch()
    {
        StartServer();
        var digest = CalculateDigest(_build);
        var wrongDigest = CalculateDigest(builder => builder.AddDirectory("other"));

        var ex = CreateClient().Invoking(x => x.Add(wrongDigest, _build))
                               .Should().Throw<DigestMismatchException>().Which;
        ex.ExpectedDigest.Should().Be(wrongDigest.Best);
        ex.ActualDigest.Should().Be(digest.Best);
        ex.ActualManifest.Should().NotBeNull();
        ex.ActualManifest!.CalculateDigest().Should().Be(digest.Best);

        Directory.Exists(Path.Combine(_storeDir, wrongDigest.Best!)).Should().BeFalse();
        WaitForNoTempDirs();
    }

    [Fact]
    public void RejectsWeakDigests()
    {
        StartServer();
        var digest = new ManifestDigest(Sha1New: "da39a3ee5e6b4b0d3255bfef95601890afd80709");

        CreateClient().Invoking(x => x.Add(digest, _ => {}))
                      .Should().Throw<IOException>();
    }

    [Theory]
    [InlineData("../escape")]
    [InlineData("dir/../../escape")]
    [InlineData("file:stream")]
    [InlineData("NUL")]
    [InlineData("trailing.")]
    public void RejectsInvalidPaths(string path)
    {
        StartServer();
        var digest = CalculateDigest(_build);

        CreateClient().Invoking(x => x.Add(digest, builder => builder.AddFile(path.ToNativePath()!, Data.ToStream(), modifiedTime: 0)))
                      .Should().Throw<IOException>();

        Directory.GetFileSystemEntries(Path.GetDirectoryName(_storeDir.Path)!, "escape").Should().BeEmpty();
        Directory.Exists(Path.Combine(_storeDir, digest.Best!)).Should().BeFalse();
        WaitForNoTempDirs();
    }

    [Theory]
    [InlineData(5000, 'a')]
    [InlineData(1, '\uD800')]
    public void ReportsUnsendablePathsImmediately(int length, char c)
    {
        StartServer();
        string path = new string(c, length);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        CreateClient().Invoking(x => x.Add(CalculateDigest(_build), builder => builder.AddDirectory(path)))
                      .Should().Throw<IOException>().WithMessage("*String*");
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(4), because: "should not wait for an error response from the service");
        WaitForNoTempDirs();
    }

    [Fact]
    public void SerializesOnlySmallManifests()
    {
        var builder = new ManifestBuilder(ManifestFormat.Sha256New);
        builder.AddDirectory("dir");
        StoreServiceServer.SerializeManifest(builder.Manifest).Should().Equal(Encoding.UTF8.GetBytes(builder.Manifest.ToString()));

        for (int i = 0; i < 30_000; i++)
            builder.AddDirectory("dir" + i + new string('a', 200));
        StoreServiceServer.SerializeManifest(builder.Manifest).Should().BeEmpty();

        StoreServiceServer.SerializeManifest(null).Should().BeEmpty();
    }

    [Fact]
    public void CleansUpWhenClientFails()
    {
        StartServer();
        var digest = CalculateDigest(_build);

        CreateClient().Invoking(x => x.Add(digest, builder =>
        {
            builder.AddFile("file", Data.ToStream(), modifiedTime: 0);
            throw new OperationCanceledException();
        })).Should().Throw<OperationCanceledException>();

        WaitForNoTempDirs();
        Directory.Exists(Path.Combine(_storeDir, digest.Best!)).Should().BeFalse();
    }

    [Fact]
    public void EnforcesSizeLimit()
    {
        StartServer(new() {MaxTotalBytes = 1024});
        Action<IBuilder> build = builder => builder.AddFile("big", new MemoryStream(new byte[4096]), modifiedTime: 0);

        CreateClient().Invoking(x => x.Add(CalculateDigest(build), build))
                      .Should().Throw<IOException>().WithMessage("*maximum size*");
        WaitForNoTempDirs();
    }

    [Fact]
    public void EnforcesSizeLimitWithUnknownLength()
    {
        StartServer(new() {MaxTotalBytes = 1024});
        Action<IBuilder> build = builder => builder.AddFile("big", new NonSeekableStream(new MemoryStream(new byte[4096])), modifiedTime: 0);

        CreateClient().Invoking(x => x.Add(CalculateDigest(build), build))
                      .Should().Throw<IOException>().WithMessage("*maximum size*");
        WaitForNoTempDirs();
    }

    [Fact]
    public void AddsFilesLargerThanPreallocationLimit()
    {
        StartServer(new() {MaxPreallocatedBytes = 1});
        var digest = CalculateDigest(_build);

        CreateClient().Add(digest, _build);

        ImplementationStoreUtils.Verify(Path.Combine(_storeDir, digest.Best!), digest, new SilentTaskHandler());
    }

    [Theory]
    [InlineData(-1, -1)]
    [InlineData(0, 0)]
    [InlineData(1024, 1024)]
    [InlineData(1025, -1)]
    [InlineData(long.MaxValue, -1)]
    public void ReportsDeclaredLengthOnlyUpToLimit(long declaredLength, long reportedLength)
        => new StoreServiceFileStream(new StoreServiceReader(new MemoryStream()), declaredLength, new(new()), maxReportedLength: 1024)
          .Length.Should().Be(reportedLength);

    [Fact]
    public void EnforcesEntryLimit()
    {
        StartServer(new() {MaxEntries = 3});
        Action<IBuilder> build = builder =>
        {
            for (int i = 0; i < 10; i++)
                builder.AddDirectory("dir" + i);
        };

        CreateClient().Invoking(x => x.Add(CalculateDigest(build), build))
                      .Should().Throw<IOException>().WithMessage("*entries*");
    }

    [Fact]
    public void RejectsUntrustedPipeOwner()
    {
        StartServer();
        var client = CreateClient(trustedOwners: [new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null)]);

        client.Invoking(x => x.Add(CalculateDigest(_build), _build))
              .Should().Throw<IOException>();
        ListImplementations().Should().BeEmpty();
    }

    [Fact]
    public void ReportsServiceNotRunning()
    {
        var client = CreateClient();
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        client.Invoking(x => x.Add(CalculateDigest(_build), _build))
              .Should().Throw<IOException>();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2), because: "should not wait for connect timeout if the pipe does not exist");
    }

    [Fact]
    public void ServesMultipleClientsConcurrently()
    {
        StartServer(new() {MaxConcurrentSessions = 2});

        var digests = Enumerable.Range(0, 6).Select(i =>
        {
            Action<IBuilder> build = builder => builder.AddFile("file" + i, Data.ToStream(), modifiedTime: 0);
            return (digest: CalculateDigest(build), build);
        }).ToList();
        Parallel.ForEach(digests, x => CreateClient().Add(x.digest, x.build));

        foreach (var (digest, _) in digests)
            Directory.Exists(Path.Combine(_storeDir, digest.Best!)).Should().BeTrue();
    }

    [Fact]
    public void TimesOutWaitingForBusyService()
    {
        StartServer(new() {MaxConcurrentSessions = 1});
        using var blockingPipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
        blockingPipe.Connect(5000);
        var blockingWriter = new StoreServiceWriter(blockingPipe);
        blockingWriter.Write(StoreServiceProtocol.Magic, 0, StoreServiceProtocol.Magic.Length);
        blockingWriter.WriteUInt16(StoreServiceProtocol.Version);
        blockingWriter.WriteByte(1);
        blockingWriter.WriteString("sha256new_4OYMIQUY7QOBJGX36TEJS35ZEQT24QPEMSNZGTFESWMRW6CSXBKQ", StoreServiceProtocol.MaxDigestLength);
        new StoreServiceReader(blockingPipe).ReadResponse().Result.Should().Be(StoreServiceResult.Ok);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        CreateClient(timeout: TimeSpan.FromMilliseconds(200))
           .Invoking(x => x.Add(CalculateDigest(_build), _build))
           .Should().Throw<IOException>();
        stopwatch.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(2), because: "the service is busy");
    }

    [Fact]
    public void PipeAclRestrictsAccess()
    {
        StartServer();
        using var pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut);
        pipe.Connect(5000);

        var rules = pipe.GetAccessControl().GetAccessRules(includeExplicit: true, includeInherited: true, typeof(SecurityIdentifier)).Cast<PipeAccessRule>().ToList();

        var authenticatedUsers = rules.Single(x => x.IdentityReference == new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null));
        authenticatedUsers.AccessControlType.Should().Be(AccessControlType.Allow);
        authenticatedUsers.PipeAccessRights.HasFlag(PipeAccessRights.CreateNewInstance).Should().BeFalse();

        var network = rules.Single(x => x.IdentityReference == new SecurityIdentifier(WellKnownSidType.NetworkSid, null));
        network.AccessControlType.Should().Be(AccessControlType.Deny);

        rules.Should().NotContain(x => x.IdentityReference == new SecurityIdentifier(WellKnownSidType.WorldSid, null));
        rules.Should().NotContain(x => x.IdentityReference == new SecurityIdentifier(WellKnownSidType.AnonymousSid, null));
    }

    [Fact]
    public void RefusesInsecureStoreDirectory()
    {
        var acl = new DirectoryInfo(_storeDir).GetAccessControl();
        acl.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null), FileSystemRights.Modify, AccessControlType.Allow));
        new DirectoryInfo(_storeDir).SetAccessControl(acl);

        Action createServer = () => _server = new StoreServiceServer([_storeDir], _pipeName, new());
        createServer.Should().Throw<IOException>();
    }

    private IEnumerable<string> ListTempDirs()
        => Directory.GetDirectories(_storeDir, "0install-extract-*");

    private IEnumerable<string> ListImplementations()
        => Directory.GetDirectories(_storeDir, "sha256*");

    /// <summary>
    /// The server cleans up asynchronously after the client has received the response.
    /// </summary>
    private void WaitForNoTempDirs()
    {
        for (int i = 0; i < 50 && ListTempDirs().Any(); i++)
            Thread.Sleep(100);
        ListTempDirs().Should().BeEmpty();
    }
}
