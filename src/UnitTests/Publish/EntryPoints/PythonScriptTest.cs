// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="PythonScript"/>.
/// </summary>
public class PythonScriptTest : CandidateTest
{
    public static readonly PythonScript Reference = new() {RelativePath = "python", Name = "python", NeedsTerminal = true};
    public static readonly PythonScript ReferenceWithExtension = new() {RelativePath = "python.py", Name = "python", NeedsTerminal = true};
    public static readonly PythonScript ReferenceWithExtensionWindows = new() {RelativePath = "python.pyw", Name = "python", NeedsTerminal = false};

    [Fact]
    public void NoExtension() => TestAnalyze(Reference, executable: true);

    [Fact]
    public void NotExecutable()
        => new PythonScript().Analyze(baseDirectory: Directory, file: Deploy(Reference, xbit: false))
                             .Should().BeFalse();

    [Fact]
    public void WithExtension() => TestAnalyze(ReferenceWithExtension);

    [Fact]
    public void WithExtensionWindows() => TestAnalyze(ReferenceWithExtensionWindows);
}
