// Copyright Bastian Eicher
// Licensed under the MIT License

using System.Text;
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
    /// Serves implementations as archives via HTTP. Blocks until <paramref name="cancellationToken"/> is triggered.
    /// </summary>
    /// <param name="port">The TCP port to serve on..</param>
    /// <param name="cancellationToken">Used to stop serving.</param>
    /// <exception cref="WebException">Unable to serve on the specified <paramref name="port"/>.</exception>
    /// <exception cref="NotAdminException">Needs admin rights to serve HTTP requests.</exception>
    public void Serve(int port, CancellationToken cancellationToken)
    {
        using var listener = new HttpListener {Prefixes = {$"http://+:{port}/"}};
        using var _ = cancellationToken.Register(listener.Stop);

        try
        {
            listener.Start();
        }
        catch (HttpListenerException ex) when (ex.NativeErrorCode == 5)
        {
            throw new NotAdminException(ex.Message, ex);
        }
        catch (Exception ex) when (ex is HttpListenerException or ArgumentException)
        {
            throw new WebException(ex.Message, ex);
        }

        try
        {
            while (listener.IsListening && !cancellationToken.IsCancellationRequested)
                HandleRequest(listener.GetContext(), cancellationToken);
        }
        catch (Exception ex) when (ex is HttpListenerException or InvalidOperationException or OperationCanceledException)
        {}
    }

    private void HandleRequest(HttpListenerContext context, CancellationToken cancellationToken)
    {
        void ReturnError(HttpStatusCode statusCode, Exception exception)
        {
            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "text/plain";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.OutputStream.Write(Encoding.UTF8.GetBytes(exception.GetMessageWithInner()));
        }

        try
        {
            (var manifestDigest, string mimeType) = ParseFileName(context.Request.Url!.LocalPath[1..]);
            string path = _implementationStore.GetPath(manifestDigest)
                       ?? throw new ImplementationNotFoundException(manifestDigest);

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    context.Response.ContentType = mimeType;
                    using (var builder = ArchiveBuilder.Create(context.Response.OutputStream, mimeType, fast: true))
                        new ReadDirectory(path, builder).Run(cancellationToken);
                    break;

                case "HEAD":
                    break;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    break;
            }
        }
        catch (NotSupportedException ex)
        {
            ReturnError(HttpStatusCode.BadRequest, ex);
        }
        catch (ImplementationNotFoundException ex)
        {
            ReturnError(HttpStatusCode.NotFound, ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            ReturnError(HttpStatusCode.Unauthorized, ex);
        }
        context.Response.Close();
    }

    private static (ManifestDigest manifestDigest, string mimeType) ParseFileName(string fileName)
        => (manifestDigest: new ManifestDigest(fileName.GetLeftPartAtFirstOccurrence('.')), mimeType: Archive.GuessMimeType(fileName));
}
