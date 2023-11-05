// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using NanoByte.Common.Native;
using NanoByte.Common.Net;
using NanoByte.Common.Streams;
using NanoByte.Common.Threading;
using ZeroInstall.Store.Configuration;
using Drawing = System.Drawing;

namespace ZeroInstall.Store.Icons;

/// <summary>
/// Stores icon files downloaded from the web as local files.
/// </summary>
/// <remarks>This class is immutable and thread-safe.</remarks>
public sealed class IconStore(string path, Config config, ITaskHandler handler) : IIconStore
{
    /// <summary>
    /// The maximum number of bytes to download for a single icon.
    /// </summary>
    private const long MaximumIconSize = 2 * 1024 * 1024; // 2MiB

    /// <inheritdoc/>
    public string? GetCached(Icon icon, out bool shouldRefresh)
    {
        #region Sanity checks
        if (icon == null) throw new ArgumentNullException(nameof(icon));
        if (icon.Href == null) throw new ArgumentException("Missing href.", nameof(icon));
        #endregion

        string path = GetPath(icon);

        if (File.Exists(path))
        {
            shouldRefresh = config.EffectiveNetworkUse == NetworkLevel.Full
                         && DateTime.UtcNow - File.GetLastWriteTimeUtc(path) > config.Freshness;
            return path;
        }
        else
        {
            shouldRefresh = false;
            return null;
        }
    }

    /// <inheritdoc/>
    public string Get(Icon icon, out bool shouldRefresh)
    {
        using (new MutexLock("ZeroInstall.Model.Icon." + GetPath(icon).GetHashCode()))
            return GetCached(icon, out shouldRefresh) ?? Download(icon);
    }

    private readonly ConcurrentSet<Uri> _updatedIcons = new();

    /// <inheritdoc/>
    public string GetFresh(Icon icon)
    {
        string path = Get(icon, out bool shouldRefresh);
        if (shouldRefresh && _updatedIcons.AddIfNew(icon.Href))
        {
            try
            {
                Download(icon);
            }
            catch (OperationCanceledException)
            {}
            catch (Exception ex) when (ex is WebException or InvalidDataException)
            {
                Log.Info(ex);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warn($"Problem storing new version of {icon}", ex);
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
        if (icon.Href.Scheme.ToLowerInvariant() is not ("http" or "https"))
            throw new WebException(string.Format(Resources.InvalidIconUrl, icon.Href));
        if (config.NetworkUse == NetworkLevel.Offline)
            throw new WebException(string.Format(Resources.NoDownloadInOfflineMode, icon.Href));

        string path = GetPath(icon);

        using var atomic = new AtomicWrite(path);
        handler.RunTask(new DownloadFile(icon.Href, atomic.WritePath) {BytesMaximum = MaximumIconSize});
        Validate(icon, atomic.WritePath);
        atomic.Commit();

        return path;
    }

    /// <inheritdoc/>
    public void Import(Icon icon, Stream stream)
    {
        string path = GetPath(icon);

        using var atomic = new AtomicWrite(path);
        stream.CopyToFile(atomic.WritePath);
        Validate(icon, atomic.WritePath);
        atomic.Commit();
    }

    private string GetPath(Icon icon)
        => Path.Combine(path, GetFileName(icon));

    /// <summary>
    /// Gets a file name suitable for storing an <see cref="Icon"/> on the disk.
    /// </summary>
    public static string GetFileName(Icon icon)
    {
        string name = FeedUri.Escape(icon.Href.AbsoluteUri);

        void EnsureExtension(string mimeType, string extension)
        {
            if (icon.MimeType == mimeType && !StringUtils.EqualsIgnoreCase(Path.GetExtension(name), extension))
                name += extension;
        }

        EnsureExtension(Icon.MimeTypePng, ".png");
        EnsureExtension(Icon.MimeTypeIco, ".ico");
        EnsureExtension(Icon.MimeTypeSvg, ".svg");
        EnsureExtension(Icon.MimeTypeIcns, ".icns");

        return name;
    }

    private static void Validate(Icon icon, string path)
    {
        // Icon validation currently uses GDI+ which is only available on Windows
        if (!WindowsUtils.IsWindows) return;

        try
        {
            switch (icon.MimeType)
            {
                case Icon.MimeTypePng:
                    using (var stream = File.OpenRead(path))
                        Drawing.Image.FromStream(stream).Dispose();
                    break;

                case Icon.MimeTypeIco:
                    new Drawing.Icon(path).Dispose();
                    break;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or Win32Exception)
        {
            // Wrap exception to add context and since only certain exception types are allowed
            throw new InvalidDataException(string.Format(Resources.InvalidIcon, icon.Href, icon.MimeType), ex);
        }
    }
}
