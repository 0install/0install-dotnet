// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Net;
using NanoByte.Common.Threading;
using ZeroInstall.Store.Configuration;

namespace ZeroInstall.Store.Icons;

/// <summary>
/// Stores icon files downloaded from the web as local files.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
[PrimaryConstructor]
public sealed partial class IconStore : IIconStore
{
    private readonly string _path;
    private readonly Config _config;
    private readonly ITaskHandler _handler;

    /// <summary>
    /// The maximum number of bytes to download for a single icon.
    /// </summary>
    private const long MaximumIconSize = 2 * 1024 * 1024; // 2MiB

    /// <inheritdoc/>
    public string? GetCached(Icon icon, out bool stale)
    {
        #region Sanity checks
        if (icon == null) throw new ArgumentNullException(nameof(icon));
        if (icon.Href == null) throw new ArgumentException("Missing href.", nameof(icon));
        #endregion

        string path = BuildPath(icon);

        if (File.Exists(path))
        {
            stale = DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > _config.Freshness;
            return path;
        }
        else
        {
            stale = false;
            return null;
        }
    }

    /// <inheritdoc/>
    public string Get(Icon icon, out bool stale)
    {
        using (new MutexLock("ZeroInstall.Model.Icon." + BuildPath(icon).GetHashCode()))
            return GetCached(icon, out stale) ?? Download(icon);
    }

    private readonly ConcurrentSet<Uri> _updatedIcons = new();

    /// <inheritdoc/>
    public string GetFresh(Icon icon)
    {
        string path = Get(icon, out bool stale);
        if (stale && _updatedIcons.AddIfNew(icon.Href))
        {
            try
            {
                Download(icon);
            }
            catch (OperationCanceledException)
            {}
            catch (WebException ex)
            {
                Log.Info(ex.Message, ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"Error storing {icon}", ex);
            }
            catch (Exception ex)
            {
                Log.Error($"Unexpected error downloading {icon}", ex);
            }
        }
        return path;
    }

    private string Download(Icon icon)
    {
        if (_config.NetworkUse == NetworkLevel.Offline)
            throw new WebException(string.Format(Resources.NoDownloadInOfflineMode, icon.Href));

        string path = BuildPath(icon);

        using var atomic = new AtomicWrite(path);
        _handler.RunTask(new DownloadFile(icon.Href, atomic.WritePath) {BytesMaximum = MaximumIconSize});
        atomic.Commit();

        return path;
    }

    internal string BuildPath(Icon icon)
    {
        string path = Path.Combine(_path, FeedUri.Escape(icon.Href.AbsoluteUri));

        void EnsureExtension(string mimeType, string extension)
        {
            if (icon.MimeType == mimeType && !StringUtils.EqualsIgnoreCase(Path.GetExtension(path), extension))
                path += extension;
        }

        EnsureExtension(Icon.MimeTypePng, ".png");
        EnsureExtension(Icon.MimeTypeIco, ".ico");
        EnsureExtension(Icon.MimeTypeSvg, ".svg");
        EnsureExtension(Icon.MimeTypeIcns, ".icns");

        return path;
    }
}
