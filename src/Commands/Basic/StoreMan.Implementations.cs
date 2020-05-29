// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NDesk.Options;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Store.Implementations;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Model;

namespace ZeroInstall.Commands.Basic
{
    partial class StoreMan
    {
        // ReSharper disable MemberHidesStaticFromOuterClass

        public class Add : StoreSubCommand
        {
            #region Metadata
            public const string Name = "add";

            public override string Description => Resources.DescriptionStoreAdd;

            public override string Usage => "DIGEST (DIRECTORY | (ARCHIVE [EXTRACT [MIME-TYPE [...]]))";

            protected override int AdditionalArgsMin => 2;

            public Add(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                var manifestDigest = new ManifestDigest(AdditionalArgs[0]);
                string path = AdditionalArgs[1];
                try
                {
                    if (File.Exists(path))
                    { // One or more archives (combined/overlay)
                        ImplementationStore.AddArchives(GetArchiveFileInfos(), manifestDigest, Handler);
                        return ExitCode.OK;
                    }
                    else if (Directory.Exists(path))
                    { // A single directory
                        if (AdditionalArgs.Count > 2) throw new OptionException(Resources.TooManyArguments + Environment.NewLine + AdditionalArgs.Skip(2).JoinEscapeArguments(), null);
                        ImplementationStore.AddDirectory(Path.GetFullPath(path), manifestDigest, Handler);
                        return ExitCode.OK;
                    }
                    else throw new FileNotFoundException(string.Format(Resources.FileOrDirNotFound, path), path);
                }
                catch (ImplementationAlreadyInStoreException ex)
                {
                    Log.Warn(ex);
                    return ExitCode.NoChanges;
                }
            }

            private IEnumerable<ArchiveFileInfo> GetArchiveFileInfos()
            {
                var archives = new ArchiveFileInfo[(AdditionalArgs.Count + 1) / 3];
                for (int i = 0; i < archives.Length; i++)
                {
                    archives[i] = new ArchiveFileInfo(
                        path: Path.GetFullPath(AdditionalArgs[i * 3 + 1]),
                        mimeType: (AdditionalArgs.Count > i * 3 + 3) ? AdditionalArgs[i * 3 + 3] : Archive.GuessMimeType(AdditionalArgs[i * 3 + 1]));
                    if (AdditionalArgs.Count > i * 3 + 2)
                        archives[i].Extract = AdditionalArgs[i * 3 + 2];
                }
                return archives;
            }
        }

        public class Copy : StoreSubCommand
        {
            #region Metadata
            public const string Name = "copy";

            public override string Description => Resources.DescriptionStoreCopy;

            public override string Usage => "DIRECTORY [CACHE]";

            protected override int AdditionalArgsMin => 1;

            protected override int AdditionalArgsMax => 2;

            public Copy(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                ManifestDigest manifestDigest;
                string path = Path.GetFullPath(AdditionalArgs[0]).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                try
                {
                    manifestDigest = new ManifestDigest(Path.GetFileName(path));
                }
                #region Error handling
                catch (ArgumentException ex)
                {
                    // Wrap exception since only certain exception types are allowed
                    throw new IOException(ex.Message);
                }
                #endregion

                var store = (AdditionalArgs.Count == 2) ? new DiskImplementationStore(AdditionalArgs[1]) : ImplementationStore;
                try
                {
                    store.AddDirectory(path, manifestDigest, Handler);
                    return ExitCode.OK;
                }
                catch (ImplementationAlreadyInStoreException ex)
                {
                    Log.Warn(ex);
                    return ExitCode.NoChanges;
                }
            }
        }

        public class Export : StoreSubCommand
        {
            #region Metadata
            public const string Name = "export";

            public override string Description => Resources.DescriptionStoreExport;

            public override string Usage => "DIGEST OUTPUT-ARCHIVE [MIME-TYPE]";

            protected override int AdditionalArgsMin => 2;

            protected override int AdditionalArgsMax => 3;

            public Export(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                var manifestDigest = new ManifestDigest(AdditionalArgs[0]);

                string outputArchive = AdditionalArgs[1];
                Debug.Assert(outputArchive != null);

                string? sourceDirectory = ImplementationStore.GetPath(manifestDigest);
                if (sourceDirectory == null)
                    throw new ImplementationNotFoundException(manifestDigest);

                string mimeType = (AdditionalArgs.Count == 3) ? AdditionalArgs[3] : Archive.GuessMimeType(outputArchive);

                using var generator = ArchiveGenerator.Create(sourceDirectory, outputArchive, mimeType);
                Handler.RunTask(generator);
                return ExitCode.OK;
            }
        }

        public class Find : StoreSubCommand
        {
            #region Metadata
            public const string Name = "find";

            public override string Description => Resources.DescriptionStoreFind;

            public override string Usage => "DIGEST";

            protected override int AdditionalArgsMin => 1;

            protected override int AdditionalArgsMax => 1;

            public Find(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                var manifestDigest = new ManifestDigest(AdditionalArgs[0]);

                string? path = ImplementationStore.GetPath(manifestDigest);
                if (path == null) throw new ImplementationNotFoundException(manifestDigest);
                Handler.Output(string.Format(Resources.LocalPathOf, AdditionalArgs[0]), path);
                return ExitCode.OK;
            }
        }

        public class Remove : StoreSubCommand
        {
            #region Metadata
            public const string Name = "remove";

            public override string Description => Resources.DescriptionStoreRemove;

            public override string Usage => "DIGEST+";

            protected override int AdditionalArgsMin => 1;

            public Remove(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                foreach (var digest in AdditionalArgs.Select(x => new ManifestDigest(x)))
                {
                    if (!ImplementationStore.Remove(digest, Handler))
                        throw new ImplementationNotFoundException(digest);
                }
                return ExitCode.OK;
            }
        }

        public class Verify : StoreSubCommand
        {
            #region Metadata
            public const string Name = "verify";

            public override string Description => Resources.DescriptionStoreVerify;

            public override string Usage => "[DIRECTORY] DIGEST";

            protected override int AdditionalArgsMin => 1;

            protected override int AdditionalArgsMax => 2;

            public Verify(ICommandHandler handler)
                : base(handler)
            {}
            #endregion

            public override ExitCode Execute()
            {
                try
                {
                    switch (AdditionalArgs.Count)
                    {
                        case 1:
                            // Verify a directory inside the store
                            ImplementationStore.Verify(new ManifestDigest(AdditionalArgs[0]), Handler);
                            break;

                        case 2:
                            // Verify an arbitrary directory
                            DiskImplementationStore.VerifyDirectory(AdditionalArgs[0], new ManifestDigest(AdditionalArgs[1]), Handler);
                            break;
                    }
                }
                catch (DigestMismatchException ex)
                {
                    Handler.Output(Resources.VerifyImplementation, ex.LongMessage);
                    return ExitCode.DigestMismatch;
                }

                return ExitCode.OK;
            }
        }
    }
}
