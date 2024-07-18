// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Info;

namespace ZeroInstall.Model;

/// <summary>
/// Provides utility methods for interface and feed URIs.
/// </summary>
public static class ModelUtils
{
    /// <summary>
    /// The version of Zero Install feed model (used for compatibility checks).
    /// </summary>
    public static ImplementationVersion Version { get; } = new(AppInfo.CurrentLibrary.Version ?? "1.0.0-pre");

    /// <summary>
    /// Determines whether a string contains a template variable (a substring enclosed in curly brackets, e.g {var}).
    /// </summary>
    public static bool ContainsTemplateVariables(string value)
    {
        #region Sanity checks
        if (value == null) throw new ArgumentNullException(nameof(value));
        #endregion

        int openingBracket = value.IndexOf('{');
        if (openingBracket == -1) return false;
        return (value.IndexOf('}', openingBracket) != -1);
    }

    /// <summary>
    /// Turns a relative path into an absolute one, using the file containing the reference as the base.
    /// </summary>
    /// <param name="path">The potentially relative path; will remain untouched if absolute.</param>
    /// <param name="source">The file containing the reference; can be <c>null</c>.</param>
    /// <returns>An absolute path.</returns>
    /// <exception cref="UriFormatException"><paramref name="path"/> is a relative URI that cannot be resolved.</exception>
    public static string GetAbsolutePath(string path, FeedUri? source = null)
    {
        #region Sanity checks
        if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
        #endregion

        if (Path.IsPathRooted(path)) return path;
        if (source?.IsFile is null or false) throw new UriFormatException(string.Format(Resources.RelativePathUnresolvable, path));
        return Path.Combine(Path.GetDirectoryName(source.LocalPath) ?? "", path.ToNativePath());
    }

    /// <summary>
    /// Turns a relative path into an absolute one, using the file containing the reference as the base.
    /// </summary>
    /// <param name="path">The potentially relative path; will remain untouched if absolute.</param>
    /// <param name="source">The file containing the reference; can be <c>null</c>.</param>
    /// <returns>An absolute path.</returns>
    /// <exception cref="UriFormatException"><paramref name="path"/> is a relative URI that cannot be resolved.</exception>
    public static string GetAbsolutePath(string path, string? source)
        => GetAbsolutePath(path, source.EmptyAsNull()?.To(x => new FeedUri(x)));

    /// <summary>
    /// Turns a relative HREF into an absolute one, using the file containing the reference as the base.
    /// </summary>
    /// <param name="href">The potentially relative HREF; will remain untouched if absolute.</param>
    /// <param name="source">The file containing the reference; can be <c>null</c>.</param>
    /// <returns>An absolute HREF.</returns>
    /// <exception cref="UriFormatException"><paramref name="href"/> is a relative URI that cannot be resolved.</exception>
    public static Uri GetAbsoluteHref(Uri href, FeedUri? source = null)
    {
        #region Sanity checks
        if (href == null) throw new ArgumentNullException(nameof(href));
        #endregion

        if (href.IsAbsoluteUri) return href;
        if (source?.IsFile is null or false) throw new UriFormatException(string.Format(Resources.RelativeUriUnresolvable, href));
        return new(new(source.LocalPath, UriKind.Absolute), href);
    }

    /// <summary>
    /// Turns a relative HREF into an absolute one, using the file containing the reference as the base.
    /// </summary>
    /// <param name="href">The potentially relative HREF; will remain untouched if absolute.</param>
    /// <param name="source">The file containing the reference; can be <c>null</c>.</param>
    /// <returns>An absolute HREF.</returns>
    /// <exception cref="UriFormatException"><paramref name="href"/> is a relative URI that cannot be resolved.</exception>
    public static Uri GetAbsoluteHref(Uri href, string? source)
        => GetAbsoluteHref(href, source.EmptyAsNull()?.To(x => new FeedUri(x)));
}
