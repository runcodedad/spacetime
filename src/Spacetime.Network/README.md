# Spacetime.Network

## Overview

**Spacetime.Network** provides the peer-to-peer (P2P) networking layer for the Spacetime blockchain. It implements TCP-based connection management, message framing, peer discovery, and connection pooling with optional TLS encryption.

## Features

- ✅ **TCP Connection Management** - Asynchronous TCP client/server connections
- ✅ **Message Framing Protocol** - Length-prefixed binary message format
- ✅ **Peer Discovery & Management** - Maintain and score known peers
- ✅ **Connection Pooling** - Automatically maintain N active peer connections
- ✅ **Peer Reputation System** - Track peer behavior and blacklist misbehaving nodes
- ✅ **Handshake Protocol** - Exchange node information on connection
- ✅ **TLS Support** - Optional encrypted connections (TLS 1.2/1.3)
- ✅ **Graceful Connection Handling** - Proper connection shutdown and error recovery

## Architecture

### Core Components

#### Message Types
The network supports various message types for blockchain operations:
- `Handshake` / `HandshakeAck` - Connection establishment
- `Heartbeat` - Keep-alive messages
- `GetPeers` / `Peers` - Peer discovery
- `GetHeaders` / `Headers` / `GetBlock` / `Block` - Blockchain synchronization
- `Transaction` / `NewBlock` - Transaction and block propagation
- `NewChallenge` / `ProofSubmission` - Consensus messages

#### Key Interfaces

##### `IMessageCodec`
Handles encoding and decoding of network messages.

```csharp
public interface IMessageCodec
{
    byte[] Encode(NetworkMessage message);
    Task<NetworkMessage?> DecodeAsync(Stream stream, CancellationToken cancellationToken = default);
}
```

##### `IPeerConnection`
Represents a connection to a remote peer.

```csharp
public interface IPeerConnection : IAsyncDisposable
{
    PeerInfo PeerInfo { get; }
    bool IsConnected { get; }
    Task SendAsync(NetworkMessage message, CancellationToken cancellationToken = default);
    Task<NetworkMessage?> ReceiveAsync(CancellationToken cancellationToken = default);
    Task CloseAsync(CancellationToken cancellationToken = default);
}
```

##### `IPeerManager`
Manages the list of known peers and their reputation scores.

```csharp
public interface IPeerManager
{
    IReadOnlyList<PeerInfo> KnownPeers { get; }
    IReadOnlyList<PeerInfo> ConnectedPeers { get; }
    bool AddPeer(PeerInfo peerInfo);
    bool RemovePeer(string peerId);
    PeerInfo? GetPeer(string peerId);
    void RecordSuccess(string peerId);
    void RecordFailure(string peerId);
    IReadOnlyList<PeerInfo> GetBestPeers(int count);
    bool ShouldBlacklist(string peerId);
}
```

##### `IConnectionManager`
Manages TCP connections and maintains the connection pool.

```csharp
public interface IConnectionManager : IAsyncDisposable
{
    int MaxConnections { get; }
    int ActiveConnectionCount { get; }
    Task StartAsync(IPEndPoint listenEndPoint, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    Task<IPeerConnection?> ConnectAsync(IPEndPoint endPoint, CancellationToken cancellationToken = default);
    IReadOnlyList<IPeerConnection> GetActiveConnections();
    Task DisconnectAsync(string peerId, CancellationToken cancellationToken = default);
}
```

### Message Protocol

Messages use a simple length-prefixed binary format:

```
[4 bytes: message length (little-endian)]
[1 byte: message type]
[N bytes: payload]
```

- Maximum message size: 16 MB
- All numeric values use little-endian byte order
- Efficient for streaming protocols

## Usage

### Basic Server Setup

```csharp
using Spacetime.Network;
using System.Net;

// Create components
var codec = new LengthPrefixedMessageCodec();
var peerManager = new PeerManager(blacklistThreshold: -10, maxFailures: 5);
var connectionManager = new TcpConnectionManager(
    codec, 
    peerManager, 
    maxConnections: 50, 
    useTls: false);

// Start listening for connections
var listenEndPoint = new IPEndPoint(IPAddress.Any, 8333);
await connectionManager.StartAsync(listenEndPoint);

// Server is now accepting connections
```

### Connecting to a Peer

```csharp
var peerEndPoint = new IPEndPoint(IPAddress.Parse("192.168.1.100"), 8333);
var connection = await connectionManager.ConnectAsync(peerEndPoint);

if (connection != null && connection.IsConnected)
{
    // Send a handshake message
    var handshake = new HandshakeMessage(
        protocolVersion: 1,
        nodeId: "my-node-id",
        userAgent: "Spacetime/1.0.0",
        timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    var message = new NetworkMessage(MessageType.Handshake, handshake.Serialize());
    await connection.SendAsync(message);

    // Receive response
    var response = await connection.ReceiveAsync();
    if (response != null && response.Type == MessageType.HandshakeAck)
    {
        var ackHandshake = HandshakeMessage.Deserialize(response.Payload);
        Console.WriteLine($"Connected to: {ackHandshake.NodeId}");
    }
}
```

### Peer Discovery

```csharp
// Add known peers
var peer = new PeerInfo(
    id: "peer-123",
    endPoint: new IPEndPoint(IPAddress.Parse("192.168.1.100"), 8333),
    protocolVersion: 1);

peerManager.AddPeer(peer);

// Get best peers to connect to
var bestPeers = peerManager.GetBestPeers(count: 10);

foreach (var p in bestPeers)
{
    var conn = await connectionManager.ConnectAsync(p.EndPoint);
    if (conn != null)
    {
        peerManager.RecordSuccess(p.Id);
    }
    else
    {
        peerManager.RecordFailure(p.Id);
    }
}
```

### Reputation Management

The peer manager automatically tracks peer behavior:

- **Success**: Increments reputation by 1, resets failure count
- **Failure**: Decrements reputation by 2, increments failure count
- **Blacklisting**: Peers are blacklisted when:
  - Reputation score falls below threshold (default: -10)
  - Consecutive failures exceed maximum (default: 5)

```csharp
// Check if peer should be blacklisted
if (peerManager.ShouldBlacklist(peerId))
{
    await connectionManager.DisconnectAsync(peerId);
    peerManager.RemovePeer(peerId);
}
```

## TLS Encryption

Enable TLS encryption for secure communications:

```csharp
var connectionManager = new TcpConnectionManager(
    codec, 
    peerManager, 
    maxConnections: 50, 
    useTls: true); // Enable TLS

await connectionManager.StartAsync(listenEndPoint);
```

**Note**: The current TLS implementation accepts any certificate. In production, implement proper certificate validation.

## Thread Safety

All components are designed for concurrent use:

- `PeerManager` uses `ConcurrentDictionary` for thread-safe peer storage
- `TcpConnectionManager` uses `ConcurrentDictionary` for connection tracking
- `TcpPeerConnection` uses `SemaphoreSlim` to serialize send operations
- All async operations support cancellation tokens

## Error Handling

The networking layer handles common failure scenarios:

- **Connection Timeouts**: Configurable timeout (default: 10 seconds)
- **Connection Failures**: Return null instead of throwing exceptions
- **Network Errors**: Gracefully close connections on I/O errors
- **Invalid Messages**: Throw `InvalidDataException` for malformed data

## Testing

The project includes comprehensive tests:

### Unit Tests (`Spacetime.Network.Tests`)
- Message codec encoding/decoding
- Peer manager operations
- Handshake message serialization
- Reputation scoring

### Integration Tests (`Spacetime.Network.IntegrationTests`)
- Multi-node connections
- Message exchange between peers
- Connection pooling limits
- Handshake protocol
- Connection failure handling

Run tests:
```bash
# Unit tests
dotnet test tests/Spacetime.Network.Tests

# Integration tests
dotnet test tests/Spacetime.Network.IntegrationTests
```

## Dependencies

- .NET 10.0
- No external dependencies (uses built-in networking APIs)

## Future Enhancements

Potential improvements for future releases:

- **WebSocket Support** - Alternative to TCP for browser compatibility
- **NAT Traversal** - UPnP or STUN/TURN support for NAT hole punching
- **Bandwidth Throttling** - Rate limiting for connections
- **Protocol Negotiation** - Support multiple protocol versions
- **Connection Metrics** - Track bandwidth, latency, and throughput
- **Peer Exchange (PEX)** - Automatic peer discovery via connected peers
- **Certificate Pinning** - Enhanced TLS security
- **Compression** - Optional message compression for large payloads

## Contributing

Follow the project's coding standards:
- Use async/await for all I/O operations
- Always validate inputs with `ArgumentNullException.ThrowIfNull()`
- Use `ReadOnlyMemory<byte>` for binary data
- Include XML documentation for public APIs
- Follow the naming conventions (PascalCase, `_camelCase` for fields)
- Write comprehensive unit and integration tests

## License

This project is part of the Spacetime blockchain implementation.
