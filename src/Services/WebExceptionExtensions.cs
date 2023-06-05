// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

namespace ZeroInstall.Services;

/// <summary>
/// Provides extension methods for <see cref="WebException"/>s.
/// </summary>
public static class WebExceptionExtensions
{
    /// <summary>
    /// Determines a download from a specific <paramref name="uri"/> that failed with an <paramref name="exception"/> is should be retried using a mirror location.
    /// </summary>
    public static bool ShouldTryMirror(this WebException exception, Uri uri)
        => uri is {Scheme: "http" or "https", HostNameType: UriHostNameType.Dns, IsLoopback: false}
        && exception is not {
#if NET
               InnerException: HttpRequestException
#else
               Response: HttpWebResponse
#endif
               {StatusCode: HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden}
           };
}
