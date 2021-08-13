// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System.IO;
using System.Linq;
using FluentAssertions;
using NanoByte.Common.Storage;
using Xunit;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Contains test methods for <see cref="Detection"/>.
    /// </summary>
    public class DetectionTest : CandidateTest
    {
        [Fact]
        public void ListCandidates()
        {
            Deploy(DotNetExeTest.Reference, xbit: false);
            Deploy(PythonScriptTest.Reference, xbit: true);
            Deploy(PosixScriptTest.Reference, xbit: true);
            Deploy(PosixBinaryTest.Reference32, xbit: true);

            var candidates = Detection.ListCandidates(Directory).ToList();
            candidates.Should().BeEquivalentTo(new Candidate[]
            {
                DotNetExeTest.Reference,
                PythonScriptTest.Reference,
                PosixScriptTest.Reference,
                PosixBinaryTest.Reference32
            });
        }

        [Fact] // Should not fail on empty files
        public void TestEmpty()
        {
            FileUtils.Touch(Path.Combine(Directory.FullName, "empty"));
            Detection.ListCandidates(Directory).Should().BeEmpty();
        }
    }
}
