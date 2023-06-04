// Copyright Bastian Eicher
// Licensed under the MIT License

using System.Net.Sockets;
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
public class ImplementationServer : IDisposable
{
    private readonly IImplementationStore _implementationStore;
    private readonly HttpListener _listener;

    /// <summary>
    /// The TCP port implementations are served on.
    /// </summary>
    public ushort Port { get; }

    /// <summary>
    /// Starts serving implementations as archives via HTTP.
    /// </summary>
    /// <param name="implementationStore">The implementation store to serve implementations from.</param>
    /// <param name="port">The TCP port to serve on; <c>null</c> to automatically pick available port.</param>
    /// <exception cref="WebException">Unable to serve on the specified <paramref name="port"/>.</exception>
    /// <exception cref="NotAdminException">Needs admin rights to serve HTTP requests.</exception>
    public ImplementationServer(IImplementationStore implementationStore, ushort? port = null)
    {
        _implementationStore = implementationStore ?? throw new ArgumentNullException(nameof(implementationStore));

        Port = port ?? FreePort();
        _listener = new() {Prefixes = {$"http://+:{Port}/"}};
        try
        {
            _listener.Start();
        }
        #region Error handling
        catch (HttpListenerException ex) when (WindowsUtils.IsWindowsNT && ex.NativeErrorCode == 5)
        {
            throw new NotAdminException(ex.Message, ex);
        }
        catch (HttpListenerException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new WebException(ex.Message, ex);
        }
        #endregion

        _ = ServeAsync();
    }

    private static ushort FreePort()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        try
        {
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return (ushort)port;
        }
        #region Error handling
        catch (SocketException ex)
        {
            // Wrap exception since only certain exception types are allowed
            throw new WebException(ex.Message, ex);
        }
        #endregion
    }

    /// <summary>
    /// Stops serving implementations.
    /// </summary>
    public void Dispose()
    {
        _listener.Stop();
        _listener.Close();
    }

    private async Task ServeAsync()
    {
        Log.Info($"Serving implementations on port {Port}");

        try
        {
            while (_listener.IsListening)
            {
                var context = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleRequest(context));
            }
        }
        #region Error handling
        catch (HttpListenerException)
        {} // Shutdown
        #endregion
    }

    private void HandleRequest(HttpListenerContext context)
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
                        new ReadDirectory(path, builder).Run();
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
