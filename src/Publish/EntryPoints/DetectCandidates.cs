// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Storage;
using ZeroInstall.Publish.Properties;

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Detects entry point <see cref="Candidate"/>s in a file system directory.
/// </summary>
public class DetectCandidates : ReadDirectoryBase
{
    private static readonly List<Func<Candidate>> _candidateCreators = new()
    {
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
    };

    /// <inheritdoc/>
    public DetectCandidates(string path)
        : base(path)
    {}

    /// <inheritdoc/>
    public override string Name => Resources.DetectingCandidates;

    private readonly List<Candidate> _candidates = new();

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

        var candidate = _candidateCreators.Select(x => x()).FirstOrDefault(x => x.Analyze(Source, file));
        if (candidate != null) _candidates.Add(candidate);
    }
}
