// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using NanoByte.Common.Storage;
using NanoByte.Common.Tasks;
using ZeroInstall.Model;
using ZeroInstall.Store.Implementations.Archives;
using ZeroInstall.Store.Properties;

namespace ZeroInstall.Store.Implementations.Build
{
    /// <summary>
    /// Provides helper methods for dealing with <see cref="Recipe"/>s.
    /// </summary>
    public static class RecipeUtils
    {
        /// <summary>
        /// Applies a <see cref="Recipe"/> to a <see cref="TemporaryDirectory"/>.
        /// </summary>
        /// <param name="recipe">The <see cref="Recipe"/> to apply.</param>
        /// <param name="downloadedFiles">Files downloaded for the the <paramref name="recipe"/>. Must be in same order as <see cref="DownloadRetrievalMethod"/> elements in <paramref name="recipe"/>.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        /// <param name="tag">A tag used to associate composite task with a specific operation; can be null.</param>
        /// <returns>A <see cref="TemporaryDirectory"/> with the resulting directory content.</returns>
        /// <exception cref="ArgumentException">The <see cref="Archive"/>s in <paramref name="recipe"/> and the files in <paramref name="downloadedFiles"/> do not match up.</exception>
        /// <exception cref="NotSupportedException"><paramref name="recipe"/> contains unknown step types.</exception>
        [SuppressMessage("Microsoft.Usage", "CA2208:InstantiateArgumentExceptionsCorrectly", Justification = "False positivie due to usage inside lamda")]
        public static TemporaryDirectory Apply(this Recipe recipe, IEnumerable<TemporaryFile> downloadedFiles, ITaskHandler handler, object? tag = null)
        {
            #region Sanity checks
            if (recipe == null) throw new ArgumentNullException(nameof(recipe));
            if (downloadedFiles == null) throw new ArgumentNullException(nameof(downloadedFiles));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (recipe.UnknownElements != null && recipe.UnknownElements.Length != 0)
                throw new NotSupportedException(string.Format(Resources.UnknownRecipeStepType, recipe.UnknownElements[0].Name));

            var workingDir = new TemporaryDirectory("0install-recipe");

            try
            {
                using var downloadedEnum = downloadedFiles.GetEnumerator();
                foreach (var step in recipe.Steps)
                {
                    switch (step)
                    {
                        case Archive archive:
                            downloadedEnum.MoveNext();
                            if (downloadedEnum.Current == null) throw new ArgumentException(Resources.RecipeFileNotDownloaded, nameof(downloadedFiles));
                            archive.Apply(downloadedEnum.Current, workingDir, handler, tag);
                            break;
                        case SingleFile file:
                            downloadedEnum.MoveNext();
                            if (downloadedEnum.Current == null) throw new ArgumentException(Resources.RecipeFileNotDownloaded, nameof(downloadedFiles));
                            file.Apply(downloadedEnum.Current, workingDir);
                            break;
                        case RemoveStep x:
                            x.Apply(workingDir);
                            break;
                        case RenameStep x:
                            x.Apply(workingDir);
                            break;
                        case CopyFromStep x:
                            x.Apply(workingDir, handler, tag);
                            break;
                        default: throw new NotSupportedException($"Unknown recipe step: ${step}");
                    }
                }
                return workingDir;
            }
            #region Error handling
            catch
            {
                workingDir.Dispose();
                throw;
            }
            #endregion
        }

        /// <summary>
        /// Applies a <see cref="Archive"/> to a <see cref="TemporaryDirectory"/>.
        /// </summary>
        /// <param name="step">The <see cref="Archive"/> to apply.</param>
        /// <param name="localPath">The local path of the archive.</param>
        /// <param name="workingDir">The <see cref="TemporaryDirectory"/> to apply the changes to.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        /// <param name="tag">A tag used to associate composite task with a specific operation; can be null.</param>
        /// <exception cref="IOException">A path specified in <paramref name="step"/> is illegal.</exception>
        /// <exception cref="ArgumentException"><see cref="Archive.Normalize"/> was not called for <paramref name="step"/>.</exception>
        public static void Apply(this Archive step, string localPath, TemporaryDirectory workingDir, ITaskHandler handler, object? tag = null)
        {
            #region Sanity checks
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (string.IsNullOrEmpty(localPath)) throw new ArgumentNullException(nameof(localPath));
            if (workingDir == null) throw new ArgumentNullException(nameof(workingDir));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            if (!string.IsNullOrEmpty(step.Destination))
            {
                string destination = FileUtils.UnifySlashes(step.Destination);
                if (FileUtils.IsBreakoutPath(destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, destination));
            }

            using var extractor = ArchiveExtractor.Create(
                localPath,
                workingDir,
                step.MimeType ?? throw new ArgumentException($"{nameof(step.Normalize)} was not called.", nameof(step)),
                step.StartOffset);
            extractor.Extract = step.Extract;
            extractor.TargetSuffix = FileUtils.UnifySlashes(step.Destination);
            extractor.Tag = tag;
            handler.RunTask(extractor);
        }

        /// <summary>
        /// Applies a <see cref="SingleFile"/> to a <see cref="TemporaryDirectory"/>.
        /// </summary>
        /// <param name="step">The <see cref="Archive"/> to apply.</param>
        /// <param name="localPath">The local path of the file.</param>
        /// <param name="workingDir">The <see cref="TemporaryDirectory"/> to apply the changes to.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        /// <exception cref="IOException">A path specified in <paramref name="step"/> is illegal.</exception>
        public static void Apply(this SingleFile step, string localPath, TemporaryDirectory workingDir, ITaskHandler handler)
        {
            #region Sanity checks
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (string.IsNullOrEmpty(localPath)) throw new ArgumentNullException(nameof(localPath));
            if (workingDir == null) throw new ArgumentNullException(nameof(workingDir));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            // Use a copy of the original file because the source file is moved
            using var tempFile = new TemporaryFile("0install");
            // ReSharper disable once AccessToDisposedClosure
            handler.RunTask(new SimpleTask(Resources.CopyFiles, () => File.Copy(localPath, tempFile, overwrite: true)));
            step.Apply(tempFile, workingDir);
        }

        /// <summary>
        /// Applies a <see cref="SingleFile"/> to a <see cref="TemporaryDirectory"/>.
        /// </summary>
        /// <param name="step">The <see cref="Archive"/> to apply.</param>
        /// <param name="downloadedFile">The file downloaded from <see cref="DownloadRetrievalMethod.Href"/>.</param>
        /// <param name="workingDir">The <see cref="TemporaryDirectory"/> to apply the changes to.</param>
        /// <exception cref="IOException">A path specified in <paramref name="step"/> is illegal.</exception>
        [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "tag", Justification = "Number of method parameters must match overloaded method to ensure proper type-based compiler selection")]
        public static void Apply(this SingleFile step, TemporaryFile downloadedFile, TemporaryDirectory workingDir)
        {
            #region Sanity checks
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (downloadedFile == null) throw new ArgumentNullException(nameof(downloadedFile));
            if (workingDir == null) throw new ArgumentNullException(nameof(workingDir));
            #endregion

            if (string.IsNullOrEmpty(step.Destination)) throw new IOException(Resources.FileMissingDest);

            var builder = new DirectoryBuilder(workingDir);
            string destinationPath = builder.NewFilePath(
                FileUtils.UnifySlashes(step.Destination),
                FileUtils.FromUnixTime(0),
                step.Executable);
            FileUtils.Replace(downloadedFile, destinationPath);
            builder.CompletePending();
        }

        /// <summary>
        /// Applies a <see cref="RemoveStep"/> to a <see cref="TemporaryDirectory"/>.
        /// </summary>
        /// <param name="step">The <see cref="RemoveStep"/> to apply.</param>
        /// <param name="workingDir">The <see cref="TemporaryDirectory"/> to apply the changes to.</param>
        /// <exception cref="IOException">A path specified in <paramref name="step"/> is illegal.</exception>
        public static void Apply(this RemoveStep step, TemporaryDirectory workingDir)
        {
            #region Sanity checks
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (workingDir == null) throw new ArgumentNullException(nameof(workingDir));
            #endregion

            if (string.IsNullOrEmpty(step.Path)) throw new IOException(string.Format(Resources.RecipeInvalidPath, "(empty)"));
            string path = FileUtils.UnifySlashes(step.Path);
            if (FileUtils.IsBreakoutPath(path)) throw new IOException(string.Format(Resources.RecipeInvalidPath, path));

            string absolutePath = Path.Combine(workingDir, path);
            if (Directory.Exists(absolutePath)) Directory.Delete(absolutePath, recursive: true);
            else File.Delete(absolutePath);

            // Update in flag files as well
            FlagUtils.Remove(Path.Combine(workingDir, FlagUtils.XbitFile), path);
            FlagUtils.Remove(Path.Combine(workingDir, FlagUtils.SymlinkFile), path);
        }

        /// <summary>
        /// Applies a <see cref="RenameStep"/> to a <see cref="TemporaryDirectory"/>.
        /// </summary>
        /// <param name="step">The <see cref="RenameStep"/> to apply.</param>
        /// <param name="workingDir">The <see cref="TemporaryDirectory"/> to apply the changes to.</param>
        /// <exception cref="IOException">A path specified in <paramref name="step"/> is illegal.</exception>
        public static void Apply(this RenameStep step, TemporaryDirectory workingDir)
        {
            #region Sanity checks
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (workingDir == null) throw new ArgumentNullException(nameof(workingDir));
            #endregion

            if (string.IsNullOrEmpty(step.Source)) throw new IOException(string.Format(Resources.RecipeInvalidPath, "(empty)"));
            if (string.IsNullOrEmpty(step.Destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, "(empty)"));
            string source = FileUtils.UnifySlashes(step.Source);
            string destination = FileUtils.UnifySlashes(step.Destination);
            if (FileUtils.IsBreakoutPath(source)) throw new IOException(string.Format(Resources.RecipeInvalidPath, source));
            if (FileUtils.IsBreakoutPath(destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, destination));

            string sourcePath = Path.Combine(workingDir, source);
            string destinationPath = Path.Combine(workingDir, destination);
            string parentDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

            if (Directory.Exists(sourcePath)) Directory.Move(sourcePath, destinationPath);
            else File.Move(sourcePath, destinationPath);

            // Update in flag files as well
            FlagUtils.Rename(Path.Combine(workingDir, FlagUtils.XbitFile), source, destination);
            FlagUtils.Rename(Path.Combine(workingDir, FlagUtils.SymlinkFile), source, destination);
        }

        /// <summary>
        /// Applies a <see cref="CopyFromStep"/> to a <see cref="TemporaryDirectory"/>. <see cref="FetchHandle.Register"/> must be called first on the same thread.
        /// </summary>
        /// <param name="step">The <see cref="Archive"/> to apply.</param>
        /// <param name="workingDir">The <see cref="TemporaryDirectory"/> to apply the changes to.</param>
        /// <param name="handler">A callback object used when the the user needs to be informed about progress.</param>
        /// <param name="tag">A tag used to associate composite task with a specific operation; can be null.</param>
        /// <exception cref="IOException">A path specified in <paramref name="step"/> is illegal.</exception>
        /// <exception cref="ArgumentException"><see cref="CopyFromStep.Implementation"/> is <c>null</c>. Please call <see cref="Feed.ResolveInternalReferences"/> first.</exception>
        /// <exception cref="InvalidOperationException"><see cref="FetchHandle.Register"/> was not called first.</exception>
        public static void Apply(this CopyFromStep step, TemporaryDirectory workingDir, ITaskHandler handler, object? tag = null)
        {
            #region Sanity checks
            if (step == null) throw new ArgumentNullException(nameof(step));
            if (workingDir == null) throw new ArgumentNullException(nameof(workingDir));
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            #endregion

            string source = FileUtils.UnifySlashes(step.Source ?? "");
            string destination = FileUtils.UnifySlashes(step.Destination ?? "");
            if (FileUtils.IsBreakoutPath(source)) throw new IOException(string.Format(Resources.RecipeInvalidPath, source));
            if (FileUtils.IsBreakoutPath(destination)) throw new IOException(string.Format(Resources.RecipeInvalidPath, destination));

            if (step.Implementation == null) throw new ArgumentException(string.Format(Resources.UnableToResolveRecipeReference, step, ""));

            string sourcePath = Path.Combine(FetchHandle.Use(step.Implementation), source);
            if (Directory.Exists(sourcePath))
            {
                handler.RunTask(new CloneDirectory(sourcePath, workingDir)
                {
                    TargetSuffix = destination,
                    UseHardlinks = true,
                    Tag = tag
                });
            }
            else if (File.Exists(sourcePath))
            {
                if (string.IsNullOrEmpty(destination)) throw new IOException(string.Format(Resources.RecipeCopyFromDestinationMissing, step));
                handler.RunTask(new CloneFile(sourcePath, workingDir)
                {
                    TargetSuffix = Path.GetDirectoryName(destination),
                    TargetFileName = Path.GetFileName(destination),
                    UseHardlinks = true,
                    Tag = tag
                });
            }
            else throw new IOException(string.Format(Resources.RecipeCopyFromSourceMissing, step));
        }
    }
}
