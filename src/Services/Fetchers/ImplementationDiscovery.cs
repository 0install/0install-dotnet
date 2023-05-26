// Copyright Bastian Eicher et al.
// Licensed under the GNU Lesser Public License

using Makaretu.Dns;
using NanoByte.Common.Threading;
using ZeroInstall.Archives;

namespace ZeroInstall.Services.Fetchers;

/// <summary>
/// Discovers implementations in implementation stores on other machines in the local network.
/// </summary>
public class ImplementationDiscovery : IImplementationDiscovery, IDisposable
{
    private readonly ServiceDiscovery _serviceDiscovery = new();
    private readonly Timer _queryTimer;
    private readonly ConcurrentSet<ImplementationDiscoveryInstance> _instances = new();

    private event Action<ImplementationDiscoveryInstance> InstanceDiscovered;

    /// <summary>
    /// Starts discovering implementation stores on other machines in the local network.
    /// </summary>
    public ImplementationDiscovery()
    {
        InstanceDiscovered += _instances.Add;
        _serviceDiscovery.ServiceInstanceDiscovered += OnInstanceDiscovered;
        _queryTimer = new(_ =>
        {
            try
            {
                _serviceDiscovery.QueryServiceInstances(ImplementationServer.DnsServiceName);
            }
            #region Error handling
            catch
            {
                // _serviceDiscovery might already be disposed
            }
            #endregion
        }, null, TimeSpan.Zero, period: TimeSpan.FromSeconds(2));
    }

    /// <summary>
    /// Stops discovering implementation stores.
    /// </summary>
    public void Dispose()
    {
        _queryTimer.Dispose();
        _serviceDiscovery.Dispose();
    }

    /// <summary>
    /// Controls whether implementation stores hosted on the local machine should be excluded from discovery.
    /// </summary>
    internal static bool ExcludeLocalMachine = true;

    private void OnInstanceDiscovered(object? sender, ServiceInstanceDiscoveryEventArgs e)
    {
        if (!e.ServiceInstanceName.ToString().EndsWith(ImplementationServer.DnsServiceName + ".local")
         || e.Message.AdditionalRecords.OfType<SRVRecord>().FirstOrDefault() is not {Port: var port}) return;

        var ips = e.Message.AdditionalRecords.OfType<AddressRecord>().Select(x => x.Address);
        if (ExcludeLocalMachine) ips = ips.Except(MulticastService.GetIPAddresses());

        ips = ips.ToList();
        if (ips.Any()) InstanceDiscovered(new(port, ips, e.ServiceInstanceName));
    }

    /// <inheritdoc/>
    public Uri GetImplementation(ManifestDigest manifestDigest, CancellationToken cancellationToken)
    {
        var racer = new ResultRacer<Uri>(cancellationToken);

        void FindImplementation(ImplementationDiscoveryInstance instance)
            => racer.TrySetResultAsync(innerCancellationToken => instance.GetImplementationAsync(manifestDigest, innerCancellationToken));

        try
        {
            InstanceDiscovered += FindImplementation;

            foreach (var instance in _instances.AsEnumerable())
                Task.Run(() => FindImplementation(instance), cancellationToken);

            return racer.GetResult();
        }
        finally
        {
            InstanceDiscovered -= FindImplementation;
        }
    }
}
