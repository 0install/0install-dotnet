// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using NanoByte.Common.Storage;
using Xunit;
using ZeroInstall.FileSystem;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Contains test methods for <see cref="CloneFile"/>.
    /// </summary>
    public class CloneFileTest : CloneTestBase
    {
        [Fact]
        public void CopyFile()
        {
            new TestRoot
            {
                new TestFile("fileA"),
                new TestFile("decoy") {Contents = "wrong"}
            }.Build(SourceDirectory);

            new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetFileName = "fileB"}.Run();

            new TestRoot {new TestFile("fileB")}.Verify(TargetDirectory);
        }

        [Fact]
        public void CopyFileSuffix()
        {
            new TestRoot {new TestFile("fileA")}.Build(SourceDirectory);

            new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetSuffix = "suffix", TargetFileName = "fileB"}.Run();

            new TestRoot {new TestDirectory("suffix") {new TestFile("fileB")}}.Verify(TargetDirectory);
        }

        [Fact]
        public void HardlinkFile()
        {
            new TestRoot {new TestFile("fileA")}.Build(SourceDirectory);

            FileUtils.EnableWriteProtection(SourceDirectory); // Hard linking logic should work around write-protection by temporarily removing it
            try
            {
                new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetFileName = "fileB", UseHardlinks = true}.Run();
            }
            finally
            {
                FileUtils.DisableWriteProtection(SourceDirectory);
            }

            new TestRoot {new TestFile("fileB")}.Verify(TargetDirectory);
            FileUtils.AreHardlinked(Path.Combine(SourceDirectory, "fileA"), Path.Combine(TargetDirectory, "fileB"));
        }

        [Fact]
        public void CopySymlink()
        {
            new TestRoot {new TestSymlink("fileA", "target")}.Build(SourceDirectory);

            new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetFileName = "fileB"}.Run();

            new TestRoot {new TestSymlink("fileB", "target")}.Verify(TargetDirectory);
        }

        [Fact]
        public void OverwriteFile()
        {
            new TestRoot {new TestFile("fileA")}.Build(SourceDirectory);
            new TestRoot {new TestFile("fileB") {LastWrite = new DateTime(2000, 2, 2), Contents = "wrong", IsExecutable = true}}.Build(TargetDirectory);

            new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetFileName = "fileB"}.Run();

            new TestRoot {new TestFile("fileB")}.Verify(TargetDirectory);
        }

        [Fact]
        public void OverwriteSymlink()
        {
            new TestRoot {new TestFile("fileA")}.Build(SourceDirectory);
            new TestRoot {new TestSymlink("fileB", "target")}.Build(TargetDirectory);

            new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetFileName = "fileB"}.Run();

            new TestRoot {new TestFile("fileB")}.Verify(TargetDirectory);
        }

        [Fact]
        public void OverwriteWithSymlink()
        {
            new TestRoot {new TestSymlink("fileA", "target")}.Build(SourceDirectory);
            new TestRoot {new TestFile("fileB")}.Build(TargetDirectory);

            new CloneFile(Path.Combine(SourceDirectory, "fileA"), TargetDirectory) {TargetFileName = "fileB"}.Run();

            new TestRoot {new TestSymlink("fileB", "target")}.Verify(TargetDirectory);
        }
    }
}
