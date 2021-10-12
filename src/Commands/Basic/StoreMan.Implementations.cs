// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using NanoByte.Common;
using NanoByte.Common.Storage;
using NDesk.Options;
using ZeroInstall.Archives.Builders;
using ZeroInstall.Archives.Extractors;
using ZeroInstall.Commands.Properties;
using ZeroInstall.Model;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

namespace ZeroInstall.Commands.Basic
{
    partial class StoreMan
    {
        public class Add : StoreSubCommand
        {
            public const string Name = "add";
            public override string Description => Resources.DescriptionStoreAdd;
            public override string Usage => "DIGEST (DIRECTORY | (ARCHIVE [EXTRACT [MIME-TYPE [...]]))";
            protected override int AdditionalArgsMin => 2;

            public Add(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                var manifestDigest = new ManifestDigest(AdditionalArgs[0]);
                string path = AdditionalArgs[1];
                try
                {
                    if (File.Exists(path))
                    { // One or more archives (combined/overlay)
                        ImplementationStore.Add(manifestDigest, BuildImplementation);
                        return ExitCode.OK;
                    }
                    else if (Directory.Exists(path))
                    { // A single directory
                        if (AdditionalArgs.Count > 2) throw new OptionException(Resources.TooManyArguments + Environment.NewLine + AdditionalArgs.Skip(2).JoinEscapeArguments(), null);
                        ImplementationStore.Add(manifestDigest, builder => Handler.RunTask(new ReadDirectory(Path.GetFullPath(path), builder)));
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

            private void BuildImplementation(IBuilder builder)
            {
                for (int i = 0; i < (AdditionalArgs.Count + 1) / 3; i++)
                {
                    string path = AdditionalArgs[i * 3 + 1];
                    string mimeType = (AdditionalArgs.Count > i * 3 + 3)
                        ? AdditionalArgs[i * 3 + 3]
                        : Archive.GuessMimeType(AdditionalArgs[i * 3 + 1]);
                    string? subDir = (AdditionalArgs.Count > i * 3 + 2) ? AdditionalArgs[i * 3 + 2] : null;

                    var extractor = ArchiveExtractor.For(mimeType, Handler);
                    Handler.RunTask(new ReadFile(path, stream => extractor.Extract(builder, stream, subDir)));
                }
            }
        }

        public class Copy : StoreSubCommand
        {
            public const string Name = "copy";
            public override string Description => Resources.DescriptionStoreCopy;
            public override string Usage => "DIRECTORY [CACHE]";
            protected override int AdditionalArgsMin => 1;
            protected override int AdditionalArgsMax => 2;

            public Copy(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                ManifestDigest digest;
                string path = Path.GetFullPath(AdditionalArgs[0]).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string id = Path.GetFileName(path);
                try
                {
                    digest = new ManifestDigest(id);
                }
                catch (NotSupportedException ex)
                {
                    Log.Error(ex);
                    return ExitCode.NotSupported;
                }

                var store = (AdditionalArgs.Count == 2) ? new ImplementationStore(AdditionalArgs[1]) : ImplementationStore;
                try
                {
                    store.Add(digest, builder => Handler.RunTask(new ReadDirectory(path, builder)));
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
            public const string Name = "export";
            public override string Description => Resources.DescriptionStoreExport;
            public override string Usage => "DIGEST OUTPUT-ARCHIVE [MIME-TYPE]";
            protected override int AdditionalArgsMin => 2;
            protected override int AdditionalArgsMax => 3;

            public Export(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                string outputArchive = AdditionalArgs[1];
                string mimeType = (AdditionalArgs.Count == 3) ? AdditionalArgs[3] : Archive.GuessMimeType(outputArchive);

                var digest = new ManifestDigest(AdditionalArgs[0]);
                string? sourceDirectory = ImplementationStore.GetPath(digest);
                if (sourceDirectory == null)
                    throw new ImplementationNotFoundException(digest);

                using var builder = ArchiveBuilder.Create(outputArchive, mimeType);
                Handler.RunTask(new ReadDirectory(sourceDirectory, builder));

                return ExitCode.OK;
            }
        }

        public class Find : StoreSubCommand
        {
            public const string Name = "find";
            public override string Description => Resources.DescriptionStoreFind;
            public override string Usage => "DIGEST";
            protected override int AdditionalArgsMin => 1;
            protected override int AdditionalArgsMax => 1;

            public Find(ICommandHandler handler)
                : base(handler)
            {}

            public override ExitCode Execute()
            {
                var digest = new ManifestDigest(AdditionalArgs[0]);

                string? path = ImplementationStore.GetPath(digest);
                if (path == null) throw new ImplementationNotFoundException(digest);
                Handler.Output(string.Format(Resources.LocalPathOf, AdditionalArgs[0]), path);
                return ExitCode.OK;
            }
        }

        public class Remove : StoreSubCommand
        {
            public const string Name = "remove";
            public override string Description => Resources.DescriptionStoreRemove;
            public override string Usage => "DIGEST+";
            protected override int AdditionalArgsMin => 1;

            public Remove(ICommandHandler handler)
                : base(handler)
            {}

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
            public const string Name = "verify";
            public override string Description => Resources.DescriptionStoreVerify;
            public override string Usage => "[DIRECTORY] DIGEST";
            protected override int AdditionalArgsMin => 1;
            protected override int AdditionalArgsMax => 2;

            public Verify(ICommandHandler handler)
                : base(handler)
            {}

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
                            ImplementationStoreUtils.Verify(AdditionalArgs[0], new ManifestDigest(AdditionalArgs[1]), Handler);
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
