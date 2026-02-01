using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using NexusHome.IoT.Core.Services.Interfaces;

namespace NexusHome.IoT.Infrastructure.Services;

/// <summary>
/// mDNS-based device discovery service for Matter and other network devices.
/// Uses multicast DNS to discover devices on the local network.
/// </summary>
public class MatterDiscoveryService : IAsyncDisposable
{
    private readonly ILogger<MatterDiscoveryService> _logger;
    private readonly List<DiscoveredDevice> _discoveredDevices = new();
    private UdpClient? _mdnsClient;
    private CancellationTokenSource? _discoveryCts;
    private bool _isDiscovering;

    // mDNS constants
    private static readonly IPAddress MdnsMulticastAddress = IPAddress.Parse("224.0.0.251");
    private const int MdnsPort = 5353;
    private const string MatterServiceType = "_matter._tcp.local";
    private const string MatterCommissionableType = "_matterc._udp.local";

    public event Action<DiscoveredDevice>? OnDeviceDiscovered;
    public event Action<string>? OnDeviceLost;

    public MatterDiscoveryService(ILogger<MatterDiscoveryService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts device discovery on the local network.
    /// </summary>
    public async Task StartDiscoveryAsync(CancellationToken cancellationToken = default)
    {
        if (_isDiscovering)
        {
            _logger.LogWarning("Discovery is already running");
            return;
        }

        _logger.LogInformation("Starting Matter device discovery via mDNS");
        _isDiscovering = true;
        _discoveryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        try
        {
            _mdnsClient = new UdpClient();
            _mdnsClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _mdnsClient.Client.Bind(new IPEndPoint(IPAddress.Any, MdnsPort));
            _mdnsClient.JoinMulticastGroup(MdnsMulticastAddress);

            // Start listening for responses
            _ = Task.Run(() => ListenForResponsesAsync(_discoveryCts.Token), _discoveryCts.Token);

            // Send initial discovery query
            await SendDiscoveryQueryAsync();

            _logger.LogInformation("mDNS discovery started on port {Port}", MdnsPort);
        }
        catch (SocketException ex)
        {
            _logger.LogError(ex, "Failed to start mDNS discovery - socket error");
            _isDiscovering = false;
            throw;
        }
    }

    /// <summary>
    /// Stops device discovery.
    /// </summary>
    public Task StopDiscoveryAsync()
    {
        if (!_isDiscovering)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Stopping Matter device discovery");
        _discoveryCts?.Cancel();
        _mdnsClient?.Close();
        _mdnsClient?.Dispose();
        _mdnsClient = null;
        _isDiscovering = false;

        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets all currently discovered devices.
    /// </summary>
    public IReadOnlyList<DiscoveredDevice> GetDiscoveredDevices()
    {
        lock (_discoveredDevices)
        {
            return _discoveredDevices.ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Performs a one-time discovery scan with timeout.
    /// </summary>
    public async Task<IEnumerable<DiscoveredDevice>> DiscoverAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var discoveredDuringThis = new List<DiscoveredDevice>();
        
        void OnDiscovered(DiscoveredDevice device)
        {
            lock (discoveredDuringThis)
            {
                if (!discoveredDuringThis.Any(d => d.DeviceId == device.DeviceId))
                {
                    discoveredDuringThis.Add(device);
                }
            }
        }

        OnDeviceDiscovered += OnDiscovered;

        try
        {
            await StartDiscoveryAsync(cancellationToken);
            await Task.Delay(timeout, cancellationToken);
            await StopDiscoveryAsync();
        }
        finally
        {
            OnDeviceDiscovered -= OnDiscovered;
        }

        return discoveredDuringThis;
    }

    private async Task SendDiscoveryQueryAsync()
    {
        if (_mdnsClient == null) return;

        try
        {
            // Build mDNS query for Matter devices
            var query = BuildMdnsQuery(MatterServiceType);
            var endpoint = new IPEndPoint(MdnsMulticastAddress, MdnsPort);
            await _mdnsClient.SendAsync(query, query.Length, endpoint);

            // Also query for commissionable devices
            var commissionableQuery = BuildMdnsQuery(MatterCommissionableType);
            await _mdnsClient.SendAsync(commissionableQuery, commissionableQuery.Length, endpoint);

            _logger.LogDebug("Sent mDNS discovery queries");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send mDNS discovery query");
        }
    }

    private async Task ListenForResponsesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested && _mdnsClient != null)
        {
            try
            {
                var result = await _mdnsClient.ReceiveAsync(cancellationToken);
                ProcessMdnsResponse(result.Buffer, result.RemoteEndPoint);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error receiving mDNS response");
            }
        }
    }

    private void ProcessMdnsResponse(byte[] data, IPEndPoint remoteEndpoint)
    {
        try
        {
            var response = ParseMdnsResponse(data);
            if (response == null) return;

            var device = new DiscoveredDevice
            {
                DeviceId = response.InstanceName ?? Guid.NewGuid().ToString(),
                DeviceName = response.DeviceName ?? "Unknown Matter Device",
                Protocol = "Matter",
                Manufacturer = response.VendorName ?? "Unknown",
                Model = response.ProductName ?? "Matter Device",
                DeviceType = response.DeviceType ?? "Smart Device",
                IsCommissioned = !response.IsCommissionable,
                Metadata = new Dictionary<string, string>
                {
                    ["IPAddress"] = remoteEndpoint.Address.ToString(),
                    ["Port"] = response.Port.ToString(),
                    ["VendorId"] = response.VendorId?.ToString() ?? "0",
                    ["ProductId"] = response.ProductId?.ToString() ?? "0",
                    ["Discriminator"] = response.Discriminator?.ToString() ?? ""
                }
            };

            lock (_discoveredDevices)
            {
                var existing = _discoveredDevices.FirstOrDefault(d => d.DeviceId == device.DeviceId);
                if (existing != null)
                {
                    _discoveredDevices.Remove(existing);
                }
                _discoveredDevices.Add(device);
            }

            OnDeviceDiscovered?.Invoke(device);
            _logger.LogInformation("Discovered Matter device: {DeviceName} at {IP}", device.DeviceName, remoteEndpoint.Address);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse mDNS response");
        }
    }

    private static byte[] BuildMdnsQuery(string serviceType)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Transaction ID
        writer.Write((ushort)0);
        
        // Flags: Standard query
        writer.Write(SwapBytes((ushort)0x0000));
        
        // Questions count
        writer.Write(SwapBytes((ushort)1));
        
        // Answer, Authority, Additional counts
        writer.Write((ushort)0);
        writer.Write((ushort)0);
        writer.Write((ushort)0);

        // Write question (service type)
        var parts = serviceType.Split('.');
        foreach (var part in parts)
        {
            writer.Write((byte)part.Length);
            writer.Write(System.Text.Encoding.ASCII.GetBytes(part));
        }
        writer.Write((byte)0); // Null terminator

        // Type: PTR (12)
        writer.Write(SwapBytes((ushort)12));
        
        // Class: IN (1)
        writer.Write(SwapBytes((ushort)1));

        return ms.ToArray();
    }

    private MdnsResponseInfo? ParseMdnsResponse(byte[] data)
    {
        if (data.Length < 12) return null;

        // Skip header, parse response records
        var response = new MdnsResponseInfo();
        var offset = 12;

        // Skip questions
        var questionCount = (data[4] << 8) | data[5];
        for (int i = 0; i < questionCount && offset < data.Length; i++)
        {
            offset = SkipName(data, offset) + 4;
        }

        var answerCount = (data[6] << 8) | data[7];
        for (int i = 0; i < answerCount && offset < data.Length; i++)
        {
            var (name, newOffset) = ReadName(data, offset);
            offset = newOffset;
            
            if (offset + 10 > data.Length) break;
            
            var type = (data[offset] << 8) | data[offset + 1];
            var dataLength = (data[offset + 8] << 8) | data[offset + 9];
            offset += 10;

            if (offset + dataLength > data.Length) break;

            // Parse TXT records for device info
            if (type == 16 && dataLength > 0) // TXT record
            {
                ParseTxtRecord(data, offset, dataLength, response);
            }
            // Parse SRV records for port
            else if (type == 33 && dataLength >= 6) // SRV record
            {
                response.Port = (data[offset + 4] << 8) | data[offset + 5];
            }
            // Parse PTR for instance name
            else if (type == 12) // PTR record
            {
                var (instanceName, _) = ReadName(data, offset);
                response.InstanceName = instanceName.Split('.').FirstOrDefault();
            }

            offset += dataLength;
        }

        return response.InstanceName != null ? response : null;
    }

    private static void ParseTxtRecord(byte[] data, int offset, int length, MdnsResponseInfo response)
    {
        var end = offset + length;
        while (offset < end)
        {
            var txtLength = data[offset++];
            if (offset + txtLength > end) break;

            var txt = System.Text.Encoding.UTF8.GetString(data, offset, txtLength);
            var parts = txt.Split('=', 2);
            if (parts.Length == 2)
            {
                var key = parts[0].ToLowerInvariant();
                var value = parts[1];

                switch (key)
                {
                    case "dn": response.DeviceName = value; break;
                    case "vn": response.VendorName = value; break;
                    case "pn": response.ProductName = value; break;
                    case "dt": response.DeviceType = value; break;
                    case "vi": response.VendorId = ushort.TryParse(value, out var vi) ? vi : null; break;
                    case "pi": response.ProductId = ushort.TryParse(value, out var pi) ? pi : null; break;
                    case "d": response.Discriminator = ushort.TryParse(value, out var disc) ? disc : null; break;
                    case "cm": response.IsCommissionable = value == "1"; break;
                }
            }
            offset += txtLength;
        }
    }

    private static (string Name, int NewOffset) ReadName(byte[] data, int offset)
    {
        var parts = new List<string>();
        while (offset < data.Length)
        {
            var length = data[offset];
            if (length == 0)
            {
                offset++;
                break;
            }
            if ((length & 0xC0) == 0xC0)
            {
                // Compression pointer
                var pointer = ((length & 0x3F) << 8) | data[offset + 1];
                var (compressedName, _) = ReadName(data, pointer);
                parts.Add(compressedName);
                offset += 2;
                break;
            }
            offset++;
            parts.Add(System.Text.Encoding.ASCII.GetString(data, offset, length));
            offset += length;
        }
        return (string.Join(".", parts), offset);
    }

    private static int SkipName(byte[] data, int offset)
    {
        while (offset < data.Length)
        {
            var length = data[offset];
            if (length == 0) return offset + 1;
            if ((length & 0xC0) == 0xC0) return offset + 2;
            offset += length + 1;
        }
        return offset;
    }

    private static ushort SwapBytes(ushort value)
    {
        return (ushort)((value >> 8) | (value << 8));
    }

    public async ValueTask DisposeAsync()
    {
        await StopDiscoveryAsync();
    }

    private class MdnsResponseInfo
    {
        public string? InstanceName { get; set; }
        public string? DeviceName { get; set; }
        public string? VendorName { get; set; }
        public string? ProductName { get; set; }
        public string? DeviceType { get; set; }
        public ushort? VendorId { get; set; }
        public ushort? ProductId { get; set; }
        public ushort? Discriminator { get; set; }
        public int Port { get; set; } = 5540;
        public bool IsCommissionable { get; set; }
    }
}
