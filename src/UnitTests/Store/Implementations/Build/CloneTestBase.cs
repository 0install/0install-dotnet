// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using NanoByte.Common.Storage;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Common base class for tests that compare two directories.
    /// </summary>
    public abstract class CloneTestBase : IDisposable
    {
        protected readonly TemporaryDirectory SourceDirectory = new("0install-test-source");
        protected readonly TemporaryDirectory TargetDirectory = new("0install-test-target");

        public void Dispose()
        {
            SourceDirectory.Dispose();
            TargetDirectory.Dispose();
        }
    }
}
