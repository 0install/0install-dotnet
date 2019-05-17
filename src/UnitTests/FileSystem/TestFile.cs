// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Text;
using FluentAssertions;
using JetBrains.Annotations;
using NanoByte.Common.Native;
using NanoByte.Common.Storage;
using ZeroInstall.Store.Implementations.Build;

namespace ZeroInstall.FileSystem
{
    /// <summary>
    /// Represents a file used for testing file system operations.
    /// It can either be realized on-disk or compared against an existing on-disk file.
    /// </summary>
    /// <seealso cref="TestRoot"/>
    public class TestFile : TestElement
    {
        /// <summary>
        /// The default value for <see cref="LastWrite"/>.
        /// </summary>
        public static readonly DateTime DefaultLastWrite = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// The last write time of the file.
        /// </summary>
        public DateTime LastWrite { get; set; } = DefaultLastWrite;

        /// <summary>
        /// The default value for <see cref="Contents"/>.
        /// </summary>
        public const string DefaultContents = "AAA";

        /// <summary>
        /// The contents of the file encoded in UTF8 without a BOM.
        /// </summary>
        public string Contents { get; set; } = DefaultContents;

        /// <summary>
        /// Is the file marked as executable.
        /// </summary>
        public bool IsExecutable { get; set; }

        /// <summary>
        /// Creates a new test file.
        /// </summary>
        /// <param name="name">The name of the file.</param>
        public TestFile([NotNull] string name)
            : base(name)
        {}

        public override void Build(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            File.WriteAllText(path, Contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.SetLastWriteTimeUtc(path, LastWrite);

            if (IsExecutable)
            {
                if (UnixUtils.IsUnix) FileUtils.SetExecutable(path, true);
                else FlagUtils.SetAuto(FlagUtils.XbitFile, path);
            }
        }

        public override void Verify(string parentPath)
        {
            string path = Path.Combine(parentPath, Name);
            File.Exists(path).Should().BeTrue(because: $"File '{path}' should exist.");
            File.GetLastWriteTimeUtc(path).Should().Be(LastWrite, because: $"File '{path}' should have correct last-write time.");

            if (IsExecutable)
            {
                bool isExecutable = UnixUtils.IsUnix
                    ? FileUtils.IsExecutable(path)
                    : FlagUtils.IsFlagged(FlagUtils.XbitFile, path);
                isExecutable.Should().BeTrue(because: $"File '{path}' should be executable.");
            }
        }
    }
}
