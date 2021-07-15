// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Provides extensions methods for <see cref="IBuilder"/>s.
    /// </summary>
    public static class BuilderExtensions
    {
        /// <summary>
        /// Adds a subdirectory to the implementation and returns a wrapped <see cref="IBuilder"/> to elements inside this subdirectory.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="path">The path  of the directory to create relative to the implementation root.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        /// <returns>An <see cref="IBuilder"/> wrapped around <paramref name="builder"/> that prepends <paramref name="path"/> to paths.</returns>
        public static IBuilder BuildDirectory(this IBuilder builder, string? path)
        {
            if (string.IsNullOrEmpty(path)) return builder;
            builder.AddDirectory(path);
            return new PrefixBuilder(builder, path);
        }
    }
}
