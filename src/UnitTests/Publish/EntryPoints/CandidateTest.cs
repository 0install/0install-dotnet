// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Streams;
using ZeroInstall.Store.FileSystem;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Base class for test fixtures that test <see cref="Candidate"/>s.
/// </summary>
public abstract class CandidateTest : IDisposable
{
    private readonly TemporaryDirectory _temporaryDirectory = new("unit-tests");
    public void Dispose() => _temporaryDirectory.Dispose();

    protected readonly DirectoryInfo Directory;

    protected CandidateTest()
    {
        Directory = new DirectoryInfo(_temporaryDirectory);
    }

    /// <summary>
    /// Ensures <see cref="Candidate.Analyze"/> correctly identifies a reference file.
    /// </summary>
    /// <param name="reference">Baseline to compare a new <see cref="Candidate"/> against. Also used to determine the reference file to <see cref="Deploy(ZeroInstall.Publish.EntryPoints.Candidate,bool)"/>.</param>
    /// <param name="executable">Set to <c>true</c> to mark the file as Unix executable.</param>
    protected void TestAnalyze<T>(T reference, bool executable = false) where T : Candidate, new()
    {
        var candidate = new T();
        candidate.Analyze(baseDirectory: Directory, file: Deploy(reference, executable)).Should().BeTrue();
        candidate.Should().Be(reference);
    }

    /// <summary>
    /// Deploys a reference file for a <see cref="Candidate"/> from an internal resource.
    /// </summary>
    /// <param name="reference">Uses <see cref="Candidate.RelativePath"/> as the resource name.</param>
    /// <param name="xbit">Set to <c>true</c> to mark the file as Unix executable.</param>
    /// <returns></returns>
    protected FileInfo Deploy(Candidate reference, bool xbit = false)
        => Deploy(reference.RelativePath!, xbit);

    /// <summary>
    /// Deploys a reference file from an internal resource.
    /// </summary>
    /// <param name="path">The relative path of the resource.</param>
    /// <param name="xbit">Set to <c>true</c> to mark the file as Unix executable.</param>
    /// <returns></returns>
    protected FileInfo Deploy(string path, bool xbit = false)
    {
        var file = new FileInfo(Path.Combine(Directory.FullName, path));

        typeof(CandidateTest).CopyEmbeddedToFile(path, file.FullName);

        if (xbit) ImplFileUtils.SetExecutable(file.FullName);

        return file;
    }
}
