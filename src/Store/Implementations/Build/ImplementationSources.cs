// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using NanoByte.Common.Storage;
using ZeroInstall.Model;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Factory methods for building <see cref="IImplementationSource"/>s.
    /// </summary>
    public static class ImplementationSources
    {
        public static IImplementationSource[] GetImplementationSources(this Recipe recipe, [InstantHandle] Func<DownloadRetrievalMethod, string> download, [InstantHandle] Func<Implementation, string?>? implementationLookup = null)
            => recipe.Steps.Select(step => step switch
            {
                DownloadRetrievalMethod retrievalMethod => retrievalMethod.GetImplementationSource(download(retrievalMethod)),
                RemoveStep remove => remove.GetImplementationSource(),
                RenameStep rename => rename.GetImplementationSource(),
                CopyFromStep copyFrom => copyFrom.GetImplementationSource(implementationLookup?.Invoke(copyFrom.Implementation) ?? throw new IOException(Resources.RecipeCopyFromSourceMissing)),
                _ => throw new NotSupportedException($"Unknown recipe step: ${step}")
            }).ToArray();

        public static IImplementationSource GetImplementationSource(this DownloadRetrievalMethod retrievalMethod, string path)
            => retrievalMethod switch
            {
                Archive archive => archive.GetImplementationSource(path),
                SingleFile singleFile => singleFile.GetImplementationSource(path),
                _ => throw new NotSupportedException($"Unknown retrieval method: ${retrievalMethod}")
            };

        public static IImplementationSource GetImplementationSource(this Archive archive, string path)
        {
            if (FileUtils.IsBreakoutPath(archive.Destination))
                throw new IOException(string.Format(Resources.RecipeInvalidPath, archive.Destination));

            return new ArchiveImplementationSource(
                path,
                archive.MimeType ?? throw new ArgumentException($"{nameof(Recipe.Normalize)}() was not called.", nameof(archive)))
            {
                Extract = archive.Extract,
                Destination = FileUtils.UnifySlashes(archive.Destination)
            };
        }

        public static IImplementationSource GetImplementationSource(this SingleFile singleFile, string path)
        {
            if (string.IsNullOrEmpty(singleFile.Destination))throw new IOException(Resources.FileMissingDest);
            if (FileUtils.IsBreakoutPath(singleFile.Destination))
                throw new IOException(string.Format(Resources.RecipeInvalidPath, singleFile.Destination));

            return new FileImplementationSource(path, FileUtils.UnifySlashes(singleFile.Destination))
            {
                MakeExecutable = singleFile.Executable,
                ResetTimestamp = true
            };
        }

        public static IImplementationSource GetImplementationSource(this RemoveStep remove)
        {
            if (string.IsNullOrEmpty(remove.Path)) throw new IOException(string.Format(Resources.RecipeInvalidPath, "(empty)"));
            if (FileUtils.IsBreakoutPath(remove.Path)) throw new IOException(string.Format(Resources.RecipeInvalidPath, remove.Path));

            return new RemoveImplementationSource(remove.Path);
        }

        public static IImplementationSource GetImplementationSource(this RenameStep rename)
        {
            if (string.IsNullOrEmpty(rename.Source)) throw new IOException(string.Format(Resources.RecipeInvalidPath, "(empty)"));
            if (FileUtils.IsBreakoutPath(rename.Source)) throw new IOException(string.Format(Resources.RecipeInvalidPath, rename.Source));
            if (string.IsNullOrEmpty(rename.Destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, "(empty)"));
            if (FileUtils.IsBreakoutPath(rename.Destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, rename.Destination));

            var implementationSource = new RenameImplementationSource(rename.Source, rename.Destination);
            return implementationSource;
        }

        public static IImplementationSource GetImplementationSource(this CopyFromStep copyFrom, string path)
        {
            string source = FileUtils.UnifySlashes(copyFrom.Source ?? "");
            string destination = FileUtils.UnifySlashes(copyFrom.Destination ?? "");
            if (FileUtils.IsBreakoutPath(source)) throw new IOException(string.Format(Resources.RecipeInvalidPath, source));
            if (FileUtils.IsBreakoutPath(destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, destination));

            if (copyFrom.Implementation == null) throw new ArgumentException(string.Format(Resources.UnableToResolveRecipeReference, copyFrom, ""));

            string sourcePath = Path.Combine(path, source);
            if (Directory.Exists(sourcePath))
                return new DirectoryImplementationSource(sourcePath, destination);
            else if (File.Exists(sourcePath))
            {
                if (string.IsNullOrEmpty(destination)) throw new IOException(string.Format(Resources.RecipeCopyFromDestinationMissing, copyFrom));
                return new FileImplementationSource(sourcePath, destination);
            }
            else throw new IOException(string.Format(Resources.RecipeCopyFromSourceMissing, copyFrom));
        }
    }
}
