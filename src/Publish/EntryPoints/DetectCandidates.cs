// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Detects entry point <see cref="Candidate"/>s in a file system directory.
/// </summary>
public class DetectCandidates(string path) : ReadDirectoryBase(path)
{
    private static readonly List<Func<Candidate>> _candidateCreators =
    [
        () => new JavaClass(),
        () => new JavaJar(),
        () => new DotNetDll(),
        () => new DotNetExe(),
        () => new DotNetFrameworkExe(),
        () => new WindowsExe(),
        () => new WindowsBatch(),
        () => new MacOSApp(),
        () => new PowerShellScript(),
        () => new PythonScript(),
        () => new PhpScript(),
        () => new PerlScript(),
        () => new RubyScript(),
        () => new BashScript(),
        () => new PosixScript(),
        () => new PosixBinary()
    ];

    /// <inheritdoc/>
    public override string Name => Resources.DetectingCandidates;

    private readonly List<Candidate> _candidates = [];

    /// <summary>
    /// The list of detected candidates.
    /// </summary>
    public IReadOnlyList<Candidate> Candidates => _candidates;

    /// <inheritdoc/>
    protected override void HandleDirectory(DirectoryInfo directory)
    {}

    /// <inheritdoc/>
    protected override void HandleFile(FileInfo file, FileInfo? hardlinkTarget = null)
    {
        // Ignore uninstallers
        if (file.Name.ContainsIgnoreCase("uninstall") || file.Name.ContainsIgnoreCase("unins0")) return;

        if (_candidateCreators.Select(x => x()).FirstOrDefault(x => x.Analyze(Source, file)) is {} candidate)
            _candidates.Add(candidate);
    }
}
