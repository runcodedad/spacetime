using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace Spacetime.Network;

/// <summary>
/// Manages a collection of peer addresses with validation, diversity tracking, and persistence.
/// </summary>
public sealed class PeerAddressBook : IPeerAddressBook
{
    private readonly ConcurrentDictionary<string, PeerAddress> _addresses = new();
    private readonly int _maxAddresses;
    private readonly bool _allowPrivateAddresses;
    private readonly string? _persistencePath;
    private readonly int _maxAddressesPerSubnet;

    /// <summary>
    /// Initializes a new instance of the <see cref="PeerAddressBook"/> class.
    /// </summary>
    /// <param name="maxAddresses">Maximum number of addresses to store. Default is 10000.</param>
    /// <param name="allowPrivateAddresses">Whether to allow private/local addresses. Default is false.</param>
    /// <param name="persistencePath">Optional path for saving/loading the address book.</param>
    /// <param name="maxAddressesPerSubnet">Maximum addresses per /24 subnet. Default is 10.</param>
    public PeerAddressBook(
        int maxAddresses = 10000,
        bool allowPrivateAddresses = false,
        string? persistencePath = null,
        int maxAddressesPerSubnet = 10)
    {
        if (maxAddresses < 1)
        {
            throw new ArgumentException("Max addresses must be at least 1.", nameof(maxAddresses));
        }

        if (maxAddressesPerSubnet < 1)
        {
            throw new ArgumentException("Max addresses per subnet must be at least 1.", nameof(maxAddressesPerSubnet));
        }

        _maxAddresses = maxAddresses;
        _allowPrivateAddresses = allowPrivateAddresses;
        _persistencePath = persistencePath;
        _maxAddressesPerSubnet = maxAddressesPerSubnet;
    }

    /// <inheritdoc/>
    public int Count => _addresses.Count;

    /// <inheritdoc/>
    public IReadOnlyList<PeerAddress> GetAllAddresses()
    {
        return _addresses.Values.ToList();
    }

    /// <inheritdoc/>
    public bool AddAddress(PeerAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Validate address
        if (!IsValidAddress(address.EndPoint))
        {
            return false;
        }

        var key = GetKey(address.EndPoint);

        // Check if already exists
        if (_addresses.ContainsKey(key))
        {
            return false;
        }

        // Check subnet diversity before adding
        if (!CheckSubnetDiversity(address.EndPoint))
        {
            return false;
        }

        // Check capacity and evict if necessary
        if (_addresses.Count >= _maxAddresses)
        {
            EvictLowestQualityAddress();
        }

        return _addresses.TryAdd(key, address);
    }

    /// <inheritdoc/>
    public bool RemoveAddress(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        return _addresses.TryRemove(GetKey(endPoint), out _);
    }

    /// <inheritdoc/>
    public PeerAddress? GetAddress(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        return _addresses.TryGetValue(GetKey(endPoint), out var address) ? address : null;
    }

    /// <inheritdoc/>
    public void UpdateLastSeen(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        
        var key = GetKey(endPoint);
        if (_addresses.TryGetValue(key, out var address))
        {
            _addresses[key] = address.WithUpdatedLastSeen();
        }
    }

    /// <inheritdoc/>
    public void RecordSuccess(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        
        var key = GetKey(endPoint);
        if (_addresses.TryGetValue(key, out var address))
        {
            _addresses[key] = address.WithRecordedSuccess();
        }
    }

    /// <inheritdoc/>
    public void RecordFailure(IPEndPoint endPoint)
    {
        ArgumentNullException.ThrowIfNull(endPoint);
        
        var key = GetKey(endPoint);
        if (_addresses.TryGetValue(key, out var address))
        {
            _addresses[key] = address.WithRecordedFailure();
        }
    }

    /// <inheritdoc/>
    public IReadOnlyList<PeerAddress> GetBestAddresses(int count, IEnumerable<IPEndPoint>? excludeEndPoints = null)
    {
        var excludeKeys = excludeEndPoints?.Select(GetKey).ToHashSet() ?? new HashSet<string>();
        
        return _addresses.Values
            .Where(a => !excludeKeys.Contains(GetKey(a.EndPoint)))
            .OrderByDescending(a => a.QualityScore)
            .ThenByDescending(a => a.LastSeen)
            .Take(count)
            .ToList();
    }

    /// <inheritdoc/>
    public int RemoveStaleAddresses(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var staleAddresses = _addresses.Values
            .Where(a => a.LastSeen < cutoff)
            .Select(a => GetKey(a.EndPoint))
            .ToList();

        var removed = 0;
        foreach (var key in staleAddresses)
        {
            if (_addresses.TryRemove(key, out _))
            {
                removed++;
            }
        }

        return removed;
    }

    /// <inheritdoc/>
    public int RemovePoorQualityAddresses(double minQualityScore, int minAttempts = 5)
    {
        var poorAddresses = _addresses.Values
            .Where(a =>
            {
                var totalAttempts = a.SuccessCount + a.FailureCount;
                return totalAttempts >= minAttempts && a.QualityScore < minQualityScore;
            })
            .Select(a => GetKey(a.EndPoint))
            .ToList();

        var removed = 0;
        foreach (var key in poorAddresses)
        {
            if (_addresses.TryRemove(key, out _))
            {
                removed++;
            }
        }

        return removed;
    }

    /// <inheritdoc/>
    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_persistencePath))
        {
            return;
        }

        // Convert to serializable format
        var data = _addresses.Values.Select(a => new
        {
            Address = a.EndPoint.Address.ToString(),
            Port = a.EndPoint.Port,
            FirstSeen = a.FirstSeen,
            LastSeen = a.LastSeen,
            LastAttempt = a.LastAttempt,
            SuccessCount = a.SuccessCount,
            FailureCount = a.FailureCount,
            Source = a.Source
        }).ToList();

        var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(_persistencePath, json, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_persistencePath) || !File.Exists(_persistencePath))
        {
            return;
        }

        var json = await File.ReadAllTextAsync(_persistencePath, cancellationToken).ConfigureAwait(false);
        var data = JsonSerializer.Deserialize<List<JsonElement>>(json);

        if (data != null)
        {
            _addresses.Clear();
            foreach (var item in data)
            {
                try
                {
                    var addressStr = item.GetProperty("Address").GetString();
                    var port = item.GetProperty("Port").GetInt32();
                    if (addressStr != null && IPAddress.TryParse(addressStr, out var address))
                    {
                        var endPoint = new IPEndPoint(address, port);
                        var peerAddress = new PeerAddress(endPoint, item.GetProperty("Source").GetString() ?? "unknown")
                        {
                            FirstSeen = item.GetProperty("FirstSeen").GetDateTimeOffset(),
                            LastSeen = item.GetProperty("LastSeen").GetDateTimeOffset(),
                            LastAttempt = item.GetProperty("LastAttempt").GetDateTimeOffset(),
                            SuccessCount = item.GetProperty("SuccessCount").GetInt32(),
                            FailureCount = item.GetProperty("FailureCount").GetInt32()
                        };
                        AddAddress(peerAddress);
                    }
                }
                catch
                {
                    // Skip invalid entries
                }
            }
        }
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _addresses.Clear();
    }

    private static string GetKey(IPEndPoint endPoint)
    {
        return $"{endPoint.Address}:{endPoint.Port}";
    }

    private bool IsValidAddress(IPEndPoint endPoint)
    {
        // Check port range
        if (endPoint.Port < 1 || endPoint.Port > 65535)
        {
            return false;
        }

        var address = endPoint.Address;

        // Check for invalid addresses
        if (IPAddress.IsLoopback(address) && !_allowPrivateAddresses)
        {
            return false;
        }

        // Check for private addresses (RFC 1918)
        if (!_allowPrivateAddresses && IsPrivateAddress(address))
        {
            return false;
        }

        // Check for link-local addresses
        if (address.IsIPv6LinkLocal && !_allowPrivateAddresses)
        {
            return false;
        }

        return true;
    }

    private static bool IsPrivateAddress(IPAddress address)
    {
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // Check for IPv6 private addresses (ULA: fc00::/7)
            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC;
        }

        // Check for IPv4 private addresses
        var addressBytes = address.GetAddressBytes();
        
        // 10.0.0.0/8
        if (addressBytes[0] == 10)
        {
            return true;
        }

        // 172.16.0.0/12
        if (addressBytes[0] == 172 && (addressBytes[1] >= 16 && addressBytes[1] <= 31))
        {
            return true;
        }

        // 192.168.0.0/16
        if (addressBytes[0] == 192 && addressBytes[1] == 168)
        {
            return true;
        }

        return false;
    }

    private bool CheckSubnetDiversity(IPEndPoint endPoint)
    {
        var subnet = GetSubnet24(endPoint.Address);
        var addressesInSubnet = _addresses.Values
            .Count(a => GetSubnet24(a.EndPoint.Address) == subnet);

        return addressesInSubnet < _maxAddressesPerSubnet;
    }

    private static string GetSubnet24(IPAddress address)
    {
        if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            // For IPv6, use /48 prefix
            var bytes = address.GetAddressBytes();
            return $"{bytes[0]:X2}{bytes[1]:X2}:{bytes[2]:X2}{bytes[3]:X2}:{bytes[4]:X2}{bytes[5]:X2}";
        }

        // For IPv4, use /24 prefix
        var addressBytes = address.GetAddressBytes();
        return $"{addressBytes[0]}.{addressBytes[1]}.{addressBytes[2]}";
    }

    private void EvictLowestQualityAddress()
    {
        var lowestQuality = _addresses.Values
            .OrderBy(a => a.QualityScore)
            .ThenBy(a => a.LastSeen)
            .FirstOrDefault();

        if (lowestQuality != null)
        {
            _addresses.TryRemove(GetKey(lowestQuality.EndPoint), out _);
        }
    }
}
