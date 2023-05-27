// Copyright Bastian Eicher
// Licensed under the MIT License

using System.Text;
using NanoByte.Common.Native;
using ZeroInstall.Archives.Builders;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

#if NETFRAMEWORK
using NanoByte.Common.Streams;
#endif

namespace ZeroInstall.Archives;

/// <summary>
/// Simple HTTP web server that serves implementations as on-demand generated archives.
/// </summary>
[PrimaryConstructor]
public partial class ImplementationServer
{
    private readonly IImplementationStore _implementationStore;

    /// <summary>
    /// Serves implementations as archives via HTTP. Automatically picks a free port.
    /// Blocks until <paramref name="cancellationToken"/> is triggered.
    /// </summary>
    /// <param name="cancellationToken">Used to stop serving.</param>
    /// <exception cref="WebException">Unable to find a free port.</exception>
    /// <exception cref="NotAdminException">Needs admin rights to serve HTTP requests.</exception>
    public void Serve(CancellationToken cancellationToken)
    {
        for (ushort port = 49152; port < ushort.MaxValue; port++) // Private ports
        {
            try
            {
                Serve(port, cancellationToken);
                return;
            }
            catch (WebException) {}
        }
    }

    /// <summary>
    /// Serves implementations as archives via HTTP.
    /// Blocks until <paramref name="cancellationToken"/> is triggered.
    /// </summary>
    /// <param name="port">The TCP port to serve on.</param>
    /// <param name="cancellationToken">Used to stop serving.</param>
    /// <exception cref="WebException">Unable to serve on the specified <paramref name="port"/>.</exception>
    /// <exception cref="NotAdminException">Needs admin rights to serve HTTP requests.</exception>
    public void Serve(ushort port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener {Prefixes = {$"http://+:{port}/"}};
        using var _ = cancellationToken.Register(listener.Stop);

        try
        {
            listener.Start();
        }
        #region Error handling
        catch (HttpListenerException ex)
        {
            if (WindowsUtils.IsWindowsNT && ex.NativeErrorCode == 5) throw new NotAdminException(ex.Message, ex);
            throw new WebException(ex.Message, ex);
        }
        #endregion

        try
        {
            while (listener.IsListening && !cancellationToken.IsCancellationRequested)
                HandleRequest(listener.GetContext(), cancellationToken);
        }
        #region Error handling
        catch (Exception ex) when (ex is HttpListenerException or OperationCanceledException)
        {} // Shutdown
        #endregion
    }

    private void HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
    {
        if (context.Request.Url is not {} url) return;

        void ReturnError(HttpStatusCode statusCode, Exception exception)
        {
            Log.Info($"Returning {statusCode} for request: {context.Request.HttpMethod} {url.PathAndQuery}", exception);
            try
            {
                context.Response.StatusCode = (int)statusCode;
                if (context.Request.HttpMethod != "HEAD")
                {
                    context.Response.ContentType = "text/plain";
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(exception.GetMessageWithInner()));
                }
            }
            #region Error handling
            catch (HttpListenerException)
            {} // Connection may already be gone
            #endregion
        }

        Log.Debug($"Incoming request: {context.Request.HttpMethod} {url.PathAndQuery}");
        try
        {
            (var manifestDigest, string mimeType) = ParseFileName(url.LocalPath[1..]);
            string path = _implementationStore.GetPath(manifestDigest)
                       ?? throw new ImplementationNotFoundException(manifestDigest);

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    Log.Info($"Serving implementation {manifestDigest} as {mimeType}");
                    context.Response.ContentType = mimeType;
                    using (var builder = ArchiveBuilder.Create(context.Response.OutputStream, mimeType, fast: true))
                        new ReadDirectory(path, builder).Run(cancellationToken);
                    Log.Info($"Finished serving implementation {manifestDigest}");
                    break;

                case "HEAD":
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    break;
            }
        }
        #region Error handling
        catch (HttpListenerException ex) { Log.Info($"Request cancelled: {context.Request.HttpMethod} {url.PathAndQuery}", ex); }
        catch (NotSupportedException ex) { ReturnError(HttpStatusCode.BadRequest, ex); }
        catch (ImplementationNotFoundException ex) { ReturnError(HttpStatusCode.NotFound, ex); }
        catch (UnauthorizedAccessException ex) { ReturnError(HttpStatusCode.Unauthorized, ex); }
        catch (IOException ex) { ReturnError(HttpStatusCode.ServiceUnavailable, ex); }
        catch (Exception ex) { ReturnError(HttpStatusCode.InternalServerError, ex); }
        #endregion

        context.Response.Close();
    }

    private static (ManifestDigest manifestDigest, string mimeType) ParseFileName(string fileName)
        => (manifestDigest: new ManifestDigest(fileName.GetLeftPartAtFirstOccurrence('.')), mimeType: Archive.GuessMimeType(fileName));
}
