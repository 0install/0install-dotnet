// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Publish.EntryPoints;

/// <summary>
/// Contains test methods for <see cref="DetectCandidates"/>.
/// </summary>
public class DetectionTest : CandidateTest
{
    [Fact]
    public void ListCandidates()
    {
        Deploy(DotNetExeTest.Reference.RelativePath![..^4] + ".runtimeconfig.json");
        Deploy(DotNetDllTest.Reference);
        Deploy(DotNetFrameworkExeTest.Reference);
        Deploy(PythonScriptTest.Reference, xbit: true);
        Deploy(PosixScriptTest.Reference, xbit: true);
        Deploy(PosixBinaryTest.Reference32, xbit: true);

        var detect = new DetectCandidates(Directory.FullName);
        detect.Run();

        detect.Candidates.Should().BeEquivalentTo(new Candidate[]
        {
            DotNetDllTest.Reference,
            DotNetFrameworkExeTest.Reference,
            PythonScriptTest.Reference,
            PosixScriptTest.Reference,
            PosixBinaryTest.Reference32
        });
    }

    [Fact] // Should not fail on empty files
    public void TestEmpty()
    {
        FileUtils.Touch(Path.Combine(Directory.FullName, "empty"));

        var detect = new DetectCandidates(Directory.FullName);
        detect.Run();

        detect.Candidates.Should().BeEmpty();
    }
}
