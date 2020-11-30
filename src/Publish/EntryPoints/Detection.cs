// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Storage;

namespace ZeroInstall.Publish.EntryPoints
{
    /// <summary>
    /// Provides automatic detection of entry point <see cref="Candidate"/>s.
    /// </summary>
    public static class Detection
    {
        private static readonly List<Func<Candidate>> _candidateCreators = new()
        {
            () => new JavaClass(),
            () => new JavaJar(),
            () => new DotNetCoreApp(),
            () => new DotNetExe(),
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

        /// <summary>
        /// Returns a list of entry point <see cref="Candidate"/>s in a directory.
        /// </summary>
        /// <param name="baseDirectory">The base directory to scan for entry points.</param>
        public static List<Candidate> ListCandidates(DirectoryInfo baseDirectory)
        {
            var candidates = new List<Candidate>();
            baseDirectory.Walk(fileAction: file =>
            {
                // Ignore uninstallers
                if (file.Name.ContainsIgnoreCase("uninstall") || file.Name.ContainsIgnoreCase("unins0")) return;

                var candidate = _candidateCreators.Select(x => x()).FirstOrDefault(x => x.Analyze(baseDirectory, file));
                if (candidate != null) candidates.Add(candidate);
            });
            return candidates;
        }
    }
}
