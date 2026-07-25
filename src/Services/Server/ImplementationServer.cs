// Copyright Bastian Eicher
// Licensed under the MIT License

#if !MINIMAL
using System.Text;
using Makaretu.Dns;
using NanoByte.Common.Native;
using NanoByte.Common.Net;
using WindowsFirewallHelper;
using ZeroInstall.Archives.Builders;
using ZeroInstall.Store.FileSystem;
using ZeroInstall.Store.Implementations;

#if NETFRAMEWORK
using NanoByte.Common.Streams;
#endif

namespace ZeroInstall.Services.Server;

/// <summary>
/// Simple HTTP web server that serves implementations as on-demand generated archives.
/// </summary>
public sealed class ImplementationServer : HttpServer
{
    /// <summary>
    /// DNS name for discovering Zero Install implementation stores.
    /// </summary>
    public const string DnsServiceName = "_0install-store._tcp";

    private readonly IImplementationStore _implementationStore;
    private string? _firewallRuleName;
    private ServiceDiscovery? _serviceDiscovery;

    /// <summary>
    /// Starts serving implementations as archives via HTTP.
    /// </summary>
    /// <param name="implementationStore">The implementation store to serve implementations from.</param>
    /// <param name="port">The TCP port to listen on; <c>0</c> to automatically pick free port.</param>
    /// <param name="localOnly"><c>true</c> to only respond to requests from the local machine instead of the network. Useful for unit tests.</param>
    /// <exception cref="WebException">Unable to serve on the specified <paramref name="port"/>.</exception>
    /// <exception cref="NotAdminException">Needs admin rights to serve HTTP requests.</exception>
    public ImplementationServer(IImplementationStore implementationStore, ushort port = 0, bool localOnly = false)
        : base(port, localOnly)
    {
        _implementationStore = implementationStore ?? throw new ArgumentNullException(nameof(implementationStore));

        if (!localOnly)
        {
            AddFirewallRule();
            AdvertiseService();
        }

        StartHandlingRequests();
    }

    private void AddFirewallRule()
    {
        if (!WindowsUtils.IsWindowsVista || !WindowsUtils.IsAdministrator) return;

        try
        {
            var firewall = FirewallWAS.Instance;
            var rule = firewall.CreatePortRule(
                FirewallProfiles.Private | FirewallProfiles.Domain,
                name: $"Zero Install - Implementation sharing (Port {Port})",
                FirewallAction.Allow, FirewallDirection.Inbound,
                Port, FirewallProtocol.TCP);
            rule.Grouping = "Zero Install";
            firewall.Rules.Remove(rule.Name); // Overwrite existing rule if present
            firewall.Rules.Add(rule);
            _firewallRuleName = rule.Name;
        }
        #region Error handling
        catch (Exception ex)
        {
            Log.Warn("Failed to create Windows Firewall rule", ex);
        }
        #endregion
    }

    private void AdvertiseService()
    {
        try
        {
            _serviceDiscovery = new();
            _serviceDiscovery.Advertise(new(Guid.NewGuid().ToString(), DnsServiceName, Port));
        }
        #region Error handling
        catch (Exception ex)
        {
            Log.Warn("Failed to initialize service advertisement", ex);
        }
        #endregion
    }

    /// <summary>
    /// Stops serving implementations.
    /// </summary>
    public override void Dispose()
    {
        try
        {
            base.Dispose();
        }
        finally
        {
            _serviceDiscovery?.Unadvertise();
            _serviceDiscovery?.Dispose();

            if (_firewallRuleName != null)
                FirewallWAS.Instance.Rules.Remove(_firewallRuleName);
        }
    }

    protected override void HandleRequest(HttpListenerContext context)
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

        ManifestDigest manifestDigest;
        string path;
        IArchiveBuilder builder;
        try
        {
            (manifestDigest, string mimeType) = ParseFileName(url.LocalPath[1..]);
            path = _implementationStore.GetPath(manifestDigest)
                ?? throw new ImplementationNotFoundException(manifestDigest);

            switch (context.Request.HttpMethod)
            {
                case "GET":
                    break;

                case "HEAD":
                    return;

                default:
                    context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                    return;
            }

            Log.Info($"Serving implementation {manifestDigest} as {mimeType}");
            context.Response.ContentType = mimeType;
            builder = ArchiveBuilder.Create(context.Response.OutputStream, mimeType, fast: true);
        }
        #region Error handling
        catch (HttpListenerException ex) { Log.Info($"Request cancelled: {context.Request.HttpMethod} {url.PathAndQuery}", ex); return; }
        catch (NotSupportedException ex) { ReturnError(HttpStatusCode.BadRequest, ex); return; }
        catch (ImplementationNotFoundException ex) { ReturnError(HttpStatusCode.NotFound, ex); return; }
        catch (UnauthorizedAccessException ex) { ReturnError(HttpStatusCode.Unauthorized, ex); return; }
        catch (IOException ex) { ReturnError(HttpStatusCode.ServiceUnavailable, ex); return; }
        catch (Exception ex) { ReturnError(HttpStatusCode.InternalServerError, ex); return; }
        #endregion

        new ReadDirectory(path, builder).Run();

        // Deliberately only disposed if the archive was generated completely.
        // Disposing writes the archive's end marker and closes the response stream, terminating the chunked transfer. Doing that after a failure would present a partial archive to the client as a complete one.
        builder.Dispose();

        Log.Info($"Finished serving implementation {manifestDigest}");
    }

    private static (ManifestDigest manifestDigest, string mimeType) ParseFileName(string fileName)
        => (manifestDigest: new ManifestDigest(fileName.GetLeftPartAtFirstOccurrence('.')), mimeType: Archive.GuessMimeType(fileName));
}
#endif
