// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using System;
using System.IO;

namespace ZeroInstall.Store.Implementations
{
    /// <summary>
    /// Provides extensions methods for <see cref="IImplementationBuilder"/>s.
    /// </summary>
    public static class ImplementationBuilderExtensions
    {
        /// <summary>
        /// Adds a subdirectory to the implementation and returns a wrapped <see cref="IImplementationBuilder"/> to elements inside this subdirectory.
        /// </summary>
        /// <param name="builder">The implementation builder.</param>
        /// <param name="path">The path  of the directory to create relative to the implementation root.</param>
        /// <exception cref="UnauthorizedAccessException">Access to a resource was denied.</exception>
        /// <exception cref="IOException">An IO operation failed.</exception>
        /// <returns>An <see cref="IImplementationBuilder"/> wrapped around <paramref name="builder"/> that prepends <paramref name="path"/> to paths.</returns>
        public static IImplementationBuilder BuildDirectory(this IImplementationBuilder builder, string path)
        {
            builder.AddDirectory(path);
            return new PrefixImplementationBuilder(builder, path);
        }
    }
}
