# Spacetime.Network

## Overview

**Spacetime.Network** provides the peer-to-peer (P2P) networking layer for the Spacetime blockchain. It implements TCP-based connection management, message framing, peer discovery, and connection pooling with optional TLS encryption.

## Features

- ✅ **TCP Connection Management** - Asynchronous TCP client/server connections
- ✅ **Message Framing Protocol** - Length-prefixed binary message format
- ✅ **Automatic Peer Discovery** - Discover peers from seed nodes and peer exchange
- ✅ **Peer Address Book** - Persistent storage of known peer addresses with metadata
- ✅ **Peer Exchange Protocol** - REQUEST_PEERS/PEER_LIST with rate limiting
- ✅ **Peer Address Gossiping** - Periodic broadcasting of peer addresses
- ✅ **Address Validation** - Filter private/local addresses, enforce IP diversity
- ✅ **Peer Management** - Maintain and score known peers
- ✅ **Connection Pooling** - Automatically maintain N active peer connections
- ✅ **Peer Reputation System** - Track peer behavior and blacklist misbehaving nodes
- ✅ **Handshake Protocol** - Exchange node information on connection
- ✅ **TLS Support** - Optional encrypted connections (TLS 1.2/1.3)
- ✅ **Graceful Connection Handling** - Proper connection shutdown and error recovery

## Architecture

### Core Components

#### NetworkMessage Base Class

All protocol messages inherit from the abstract `NetworkMessage` base class, which provides:

- **Type Property**: Each message declares its `MessageType` enum value
- **Payload Property**: Lazy-loaded, cached serialized payload (`ReadOnlyMemory<byte>`)
- **Serialize Method**: Abstract method each message implements for its specific serialization
- **Deserialize Method**: Static factory method that routes to appropriate message class
- **Caching**: Serialized payload is cached to avoid repeated serialization

**Empty Messages**: Some message types (GetPeers, Heartbeat, HandshakeAck) carry no payload and are represented internally by a private `EmptyMessage` implementation.

**Usage Pattern:**
```csharp
// Creating a message (validation happens in constructor)
var message = new HandshakeMessage(
    protocolVersion: 1,
    nodeId: "node-123",
    userAgent: "Spacetime/1.0.0",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());

// Sending a message (serialization happens automatically)
await connection.SendAsync(message);

// Receiving a message (deserialization happens automatically)
var received = await connection.ReceiveAsync();

// Type-based handling
switch (received)
{
    case HandshakeMessage hs:
        Console.WriteLine($"Handshake from {hs.NodeId}");
        break;
    case PingPongMessage pp when received.Type == MessageType.Ping:
        var pong = new PingPongMessage(pp.Nonce, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await connection.SendAsync(pong);
        break;
}
```

#### Message Types

The network supports 13 message types for blockchain operations, organized by category:

**Discovery Messages:**
- `Handshake` (HandshakeMessage) - Connection establishment with node information exchange
- `HandshakeAck` (EmptyMessage) - Response to handshake (no payload)
- `Ping` (PingPongMessage) - Connection liveness checking with nonce matching
- `Pong` (PingPongMessage) - Response to Ping with same nonce
- `GetPeers` (GetPeersMessage) - Request peer list with filter criteria (maxCount, excludeAddresses)
- `Peers` (PeerListMessage) - Peer discovery and exchange (up to 1000 addresses)
- `Heartbeat` (EmptyMessage) - Keep-alive messages (legacy, use Ping/Pong for new implementations)

**Synchronization Messages:**
- `GetHeaders` (GetHeadersMessage) - Request block headers starting from a specific hash
- `Headers` (HeadersMessage) - Response containing multiple block headers
- `GetBlock` (GetBlockMessage) - Request a complete block by hash
- `Block` (BlockMessage) - Response containing a complete block

**Transaction Messages:**
- `Transaction` (TransactionMessage) - Broadcast a transaction to the network
- `TxPoolRequest` (TxPoolRequestMessage) - Request contents of a node's transaction pool (mempool)
- `NewBlock` (BlockProposalMessage) - Broadcast a newly proposed block

**Consensus Messages:**
- `ProofSubmission` (ProofSubmissionMessage) - Submit a Proof-of-Space-Time proof
- `BlockAccepted` (BlockAcceptedMessage) - Notify network that a block has been validated and accepted

**Error Handling:**
- `Error` (Not yet implemented) - Generic error message for reporting protocol violations or failures

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

#### Peer Exchange and Address Gossiping

The network layer implements a comprehensive peer exchange and gossiping system to enable nodes to discover and maintain a healthy set of network peers without relying solely on hardcoded seed nodes.

##### `IPeerAddressBook`
Manages a persistent collection of peer addresses with metadata, validation, and maintenance.

```csharp
public interface IPeerAddressBook
{
    int Count { get; }
    IReadOnlyList<PeerAddress> GetAllAddresses();
    bool AddAddress(PeerAddress address);
    bool RemoveAddress(IPEndPoint endPoint);
    PeerAddress? GetAddress(IPEndPoint endPoint);
    void UpdateLastSeen(IPEndPoint endPoint);
    void RecordSuccess(IPEndPoint endPoint);
    void RecordFailure(IPEndPoint endPoint);
    IReadOnlyList<PeerAddress> GetBestAddresses(int count, IEnumerable<IPEndPoint>? excludeEndPoints = null);
    int RemoveStaleAddresses(TimeSpan maxAge);
    int RemovePoorQualityAddresses(double minQualityScore, int minAttempts = 5);
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task LoadAsync(CancellationToken cancellationToken = default);
}
```

**Features:**
- **Address Validation**: Filters private/local addresses (192.168.x.x, 10.x.x.x, 127.x.x.x) unless explicitly allowed
- **IP Diversity**: Limits addresses per /24 subnet (default: 10) to prevent Sybil attacks
- **Quality Tracking**: Maintains success/failure counts and calculates connection quality scores
- **Staleness Detection**: Removes addresses not seen within configurable time window (default: 24 hours)
- **Capacity Management**: Evicts lowest quality addresses when capacity (default: 10,000) is reached
- **Persistence**: Save/load address book to/from disk in JSON format

**Usage:**
```csharp
var addressBook = new PeerAddressBook(
    maxAddresses: 10000,
    allowPrivateAddresses: false,
    persistencePath: "peers.json",
    maxAddressesPerSubnet: 10);

// Load persisted addresses on startup
await addressBook.LoadAsync();

// Add new address
var address = new PeerAddress(
    new IPEndPoint(IPAddress.Parse("203.0.113.100"), 8333),
    source: "seed");
addressBook.AddAddress(address);

// Track connection attempts
addressBook.RecordSuccess(address.EndPoint);
addressBook.RecordFailure(address.EndPoint);

// Get best peers to connect to
var best = addressBook.GetBestAddresses(count: 8);

// Clean up stale addresses
addressBook.RemoveStaleAddresses(TimeSpan.FromHours(24));
addressBook.RemovePoorQualityAddresses(minQualityScore: 0.3, minAttempts: 5);

// Persist changes
await addressBook.SaveAsync();
```

##### `IPeerExchange`
Implements the peer exchange protocol with rate limiting.

```csharp
public interface IPeerExchange
{
    Task<IReadOnlyList<IPEndPoint>> RequestPeersAsync(
        IPeerConnection connection,
        int maxCount = 100,
        IEnumerable<IPEndPoint>? excludeAddresses = null,
        CancellationToken cancellationToken = default);
    
    PeerListMessage HandlePeerRequest(GetPeersMessage request, string requesterId);
    bool CanRequestPeers(string peerId);
}
```

**Features:**
- **Rate Limiting**: Token bucket rate limiter (default: 1 request per 5 minutes per peer)
- **Request Filtering**: Clients can specify addresses to exclude and max count
- **Response Limits**: Maximum 1000 addresses per response
- **Timeout Handling**: Configurable request timeout (default: 10 seconds)

**Protocol:**
```
Client                           Server
  |                                 |
  |---GetPeersMessage(max=100)---->|
  |    (maxCount, excludeAddresses) |
  |                                 |
  |<---PeerListMessage(peers)-------|
  |    (list of IPEndPoints)        |
  |                                 |
```

**Usage:**
```csharp
var exchange = new PeerExchange(
    addressBook,
    minRequestInterval: TimeSpan.FromMinutes(5),
    requestTimeout: TimeSpan.FromSeconds(10));

// Request peers from a connection
var excludeList = connectedPeers.Select(p => p.EndPoint);
var peers = await exchange.RequestPeersAsync(connection, maxCount: 100, excludeList);

// Handle incoming peer requests
var request = new GetPeersMessage(maxCount: 50);
var response = exchange.HandlePeerRequest(request, "peer-abc123");
await connection.SendAsync(response);
```

##### `IPeerGossiper`
Manages periodic gossiping of peer addresses to maintain network connectivity.

```csharp
public interface IPeerGossiper : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
    void ProcessReceivedAddresses(IEnumerable<PeerAddress> peerAddresses, string sourceId);
}
```

**Features:**
- **Periodic Broadcasting**: Gossips subset of known peers at configurable interval (default: 10 minutes)
- **Address Deduplication**: Tracks recently seen addresses to avoid redundant processing (default: 1 hour window)
- **Automatic Forwarding**: Forwards received addresses (excluding sender) to other connected peers
- **Configurable Size**: Number of addresses per gossip message (default: 20)

**Usage:**
```csharp
var gossiper = new PeerGossiper(
    addressBook,
    peerManager,
    connectionManager,
    gossipInterval: TimeSpan.FromMinutes(10),
    addressesPerGossip: 20,
    addressDeduplicationWindow: TimeSpan.FromHours(1));

// Start gossiping service
await gossiper.StartAsync();

// Process received addresses from gossip
var receivedAddresses = peerListMessage.Peers
    .Select(ep => new PeerAddress(ep, $"gossip:{sourceId}"));
gossiper.ProcessReceivedAddresses(receivedAddresses, sourceId);

// Stop when shutting down
await gossiper.StopAsync();
```

##### PeerAddress Record
Immutable record containing peer address metadata.

```csharp
public sealed record PeerAddress
{
    public IPEndPoint EndPoint { get; init; }
    public DateTimeOffset FirstSeen { get; init; }
    public DateTimeOffset LastSeen { get; init; }
    public DateTimeOffset LastAttempt { get; init; }
    public int SuccessCount { get; init; }
    public int FailureCount { get; init; }
    public string Source { get; init; }
    public double QualityScore { get; }  // SuccessCount / (SuccessCount + FailureCount)
}

See [docs/peer-address-vs-connection.md](docs/peer-address-vs-connection.md) for a short guide that explains the roles and integration between the address book (`PeerAddress` / `PeerAddressBook`) and the runtime peer/connection APIs (`IPeerManager` / `IPeerConnection`).
```

### Message Classes

Each message type has a dedicated class that handles serialization/deserialization:

| Message Class | Type | Purpose | Max Size |
|--------------|------|---------|----------|
| HandshakeMessage | 0x01 | Connection establishment | Variable |
| PingPongMessage | 0x04/0x05 | Liveness checking | 16 bytes |
| GetPeersMessage | 0x10 | Request peer list with filters | Variable (1000 excludes max) |
| PeerListMessage | 0x11 | Peer exchange | 1000 peers max |
| GetHeadersMessage | 0x20 | Request headers | Variable |
| HeadersMessage | 0x21 | Provide headers | 2000 headers max |
| GetBlockMessage | 0x22 | Request block | 32 bytes |
| BlockMessage | 0x23 | Provide block | 16 MB max |
| TransactionMessage | 0x30 | Broadcast transaction | 1 MB max |
| BlockProposalMessage | 0x31 | Propose new block | 16 MB max |
| TxPoolRequestMessage | 0x32 | Request mempool | 5 bytes |
| ProofSubmissionMessage | 0x40 | Submit PoST proof | 1 MB max |
| BlockAcceptedMessage | 0x41 | Notify acceptance | 40 bytes |

All messages implement validation in constructors and throw `ArgumentException` or `ArgumentNullException` for invalid inputs. Deserialization methods throw `InvalidDataException` for malformed data.

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

#### Message Serialization

Each message type has its own serialization format and validation. Messages expose a `Payload` property that returns the cached serialized data:

**HandshakeMessage** - Connection establishment
```csharp
var handshake = new HandshakeMessage(
    protocolVersion: 1,
    nodeId: "node-12345",
    userAgent: "Spacetime/1.0.0",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());

// Payload is automatically serialized and cached
ReadOnlyMemory<byte> payload = handshake.Payload;

// Send via connection
await connection.SendAsync(handshake);
```

**Deserialization** - Receiving messages
```csharp
// Receive raw message type and payload
var messageType = /* read from stream */;
var payloadData = /* read from stream */;

// Deserialize to specific message type
NetworkMessage message = NetworkMessage.Deserialize(messageType, payloadData);

// Handle based on type
switch (message)
{
    case HandshakeMessage handshake:
        Console.WriteLine($"Node: {handshake.NodeId}, Version: {handshake.ProtocolVersion}");
        break;
    case PingPongMessage ping when message.Type == MessageType.Ping:
        var pong = new PingPongMessage(ping.Nonce, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await connection.SendAsync(pong);
        break;
    // ... other message types
}
```

**PingPongMessage** - Liveness checking
```csharp
var ping = new PingPongMessage(
    nonce: Random.Shared.NextInt64(),
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
// Message automatically serializes when accessing Payload property
await connection.SendAsync(ping);

// Responder echoes back with same nonce in a Pong message
var pong = new PingPongMessage(ping.Nonce, DateTimeOffset.UtcNow.ToUnixTimeSeconds());
await connection.SendAsync(pong);
```

**PeerListMessage** - Peer exchange
```csharp
var peers = new List<IPEndPoint>
{
    new IPEndPoint(IPAddress.Parse("192.168.1.100"), 8333),
    new IPEndPoint(IPAddress.Parse("10.0.0.50"), 8333)
};
var peerList = new PeerListMessage(peers);
await connection.SendAsync(peerList);
```

**GetHeadersMessage** - Request block headers
```csharp
var getHeaders = new GetHeadersMessage(
    locatorHash: lastKnownBlockHash,
    stopHash: ReadOnlyMemory<byte>.Empty, // Empty = no stop
    maxHeaders: 2000);
await connection.SendAsync(getHeaders);
```

**HeadersMessage** - Provide block headers
```csharp
var headers = blockHeaders.Select(h => new ReadOnlyMemory<byte>(h.Serialize())).ToList();
var headersMsg = new HeadersMessage(headers);
await connection.SendAsync(headersMsg);
```

**GetBlockMessage** - Request a specific block
```csharp
var getBlock = new GetBlockMessage(blockHash);
await connection.SendAsync(getBlock);
```

**BlockMessage** - Provide a complete block
```csharp
var blockData = block.Serialize();
var blockMsg = new BlockMessage(blockData);
await connection.SendAsync(blockMsg);
```

**TransactionMessage** - Broadcast a transaction
```csharp
var txData = transaction.Serialize();
var txMsg = new TransactionMessage(txData);
await connection.SendAsync(txMsg);
```

**ProofSubmissionMessage** - Submit a PoST proof
```csharp
var proofData = blockProof.Serialize();
var proofMsg = new ProofSubmissionMessage(
    proofData: proofData,
    minerId: minerPublicKey,
    blockHeight: currentHeight + 1);
await connection.SendAsync(proofMsg);
```

**BlockProposalMessage** - Propose a new block
```csharp
var blockData = newBlock.Serialize();
var proposal = new BlockProposalMessage(blockData);
await connection.SendAsync(proposal);
```

**BlockAcceptedMessage** - Notify block acceptance
```csharp
var accepted = new BlockAcceptedMessage(blockHash, blockHeight);
await connection.SendAsync(accepted);
```

**TxPoolRequestMessage** - Request mempool contents
```csharp
var request = new TxPoolRequestMessage(
    maxTransactions: 1000,
    includeTransactionData: true); // false = hashes only
await connection.SendAsync(request);
```

#### Message Validation

Use `MessageValidator` to validate messages before processing:

```csharp
var message = await connection.ReceiveAsync();
if (message != null && MessageValidator.ValidateMessage(message))
{
    // Process valid message
    switch (message.Type)
    {
        case MessageType.Handshake:
            var handshake = HandshakeMessage.Deserialize(message.Payload);
            // Handle handshake...
            break;
        // ... other cases
    }
}
else
{
    // Invalid message - close connection or send error
}
```

### Message Protocol Flows

#### Connection Establishment Flow
```
Node A                          Node B
  |                               |
  |-------- Handshake --------->  |
  |                               | (Validate protocol version, etc.)
  |<------- HandshakeAck -------  |
  |                               |
  |-------- Ping (nonce=N) --->  |
  |<------- Pong (nonce=N) ----  |
  |                               |
  (Connection established)
```

#### Peer Discovery Flow
```
Node A                          Node B
  |                               |
  |-------- GetPeers ---------->  |
  |                               | (Select best peers to share)
  |<------- Peers (list) -------  |
  |                               |
  (Connect to discovered peers)
```

#### Block Synchronization Flow
```
Node A (behind)                 Node B (current)
  |                               |
  |-- GetHeaders (from hash) -->  |
  |                               | (Find headers after hash)
  |<-- Headers (list) ----------  |
  |                               |
  |-- GetBlock (hash) ---------->  |
  |<-- Block (full data) --------  |
  |                               |
  (Repeat for each needed block)
```

#### Block Propagation Flow
```
Miner                           Network Nodes
  |                               |
  |-- ProofSubmission ---------->  |
  |                               | (Validate proof)
  |<-- BlockAccepted -----------  |
  |                               |
  |-- BlockProposal (NewBlock) ->  |
  |                               | (Validate and add to chain)
  |<-- BlockAccepted -----------  |
  |                               |
  (Propagate to other peers)
```

#### Transaction Flow
```
Wallet/Node                     Network Nodes
  |                               |
  |-- Transaction --------------->  |
  |                               | (Validate and add to mempool)
  |                               | (Propagate to peers)
  |                               |
  |-- TxPoolRequest ------------->  |
  |<-- Transactions (list) ------  |
  |                               |
```

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

    // Message is automatically serialized when sent
    await connection.SendAsync(handshake);

    // Receive response
    var response = await connection.ReceiveAsync();
    if (response != null && response.Type == MessageType.HandshakeAck)
    {
        // HandshakeAck uses same format as Handshake
        var ackHandshake = HandshakeMessage.Deserialize(response.Payload);
        Console.WriteLine($"Connected to: {ackHandshake.NodeId}");
    }
}
```

### Peer Discovery

The network layer supports automatic peer discovery from seed nodes:

```csharp
// Create peer discovery service
var peerDiscovery = new PeerDiscovery(connectionManager, peerManager);

// Add seed nodes for bootstrapping
peerDiscovery.AddSeedNode(new IPEndPoint(IPAddress.Parse("seed1.spacetime.io"), 8333));
peerDiscovery.AddSeedNode(new IPEndPoint(IPAddress.Parse("seed2.spacetime.io"), 8333));

// Discover peers from seed nodes
await peerDiscovery.DiscoverPeersAsync();

// Get best peers to connect to
var bestPeers = peerManager.GetBestPeers(count: 10);

foreach (var p in bestPeers)
{
    var conn = await connectionManager.ConnectAsync(p.EndPoint);
    if (conn != null)
    {
        peerManager.RecordSuccess(p.Id);
        
        // Request more peers from this connection
        var morePeers = await peerDiscovery.RequestPeersAsync(conn);
        foreach (var peerEndPoint in morePeers)
        {
            var peerId = $"peer_{peerEndPoint.Address}_{peerEndPoint.Port}";
            var peerInfo = new PeerInfo(peerId, peerEndPoint, 1);
            peerManager.AddPeer(peerInfo);
        }
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

## Message Relay and Broadcasting

The network layer includes a comprehensive message relay system for efficient propagation of blocks, transactions, and proofs across the network.

### Core Components

#### MessageRelay

The main relay service that coordinates message broadcasting with deduplication, rate limiting, and bandwidth management:

```csharp
var relay = new MessageRelay(connectionManager, peerManager);

// Broadcast a block to all peers
var blockMessage = new BlockMessage(blockData);
await relay.BroadcastAsync(blockMessage);

// Relay a received transaction (with validation)
var txMessage = new TransactionMessage(txData);
var relayed = await relay.RelayAsync(txMessage, sourcePeerId: "peer1");
```

**Features:**
- Background worker thread for async message delivery
- Automatic deduplication to prevent relay loops
- Per-peer rate limiting with token bucket algorithm
- Global and per-peer bandwidth management
- Priority-based message queuing
- Validation before relay
- Peer reputation tracking on success/failure

#### MessageTracker

Tracks seen messages to prevent duplicate relays using SHA-256 hashing:

```csharp
var tracker = new MessageTracker(
    messageLifetime: TimeSpan.FromMinutes(5),
    maxTrackedMessages: 100_000);

// Check if message is new
if (tracker.MarkAndCheckIfNew(message))
{
    // First time seeing this message
    await BroadcastToOthers(message);
}
```

**Features:**
- SHA-256 based message hashing
- Sliding window with configurable lifetime (default: 5 minutes)
- Automatic cleanup of old entries
- Capacity limits to prevent memory exhaustion
- Thread-safe concurrent operations

#### RateLimiter

Token bucket rate limiting per peer to prevent spam:

```csharp
var rateLimiter = new RateLimiter(
    maxTokens: 100,
    refillInterval: TimeSpan.FromSeconds(1),
    refillAmount: 10);

// Check if peer can send
if (rateLimiter.TryConsume(peerId, tokens: 1))
{
    // Process message
}
else
{
    // Rate limit exceeded, drop message
}
```

**Features:**
- Token bucket algorithm per peer
- Configurable refill rate and capacity
- Independent limits per peer
- Automatic token refill over time

#### BandwidthMonitor

Tracks and enforces bandwidth limits per peer and globally:

```csharp
var monitor = new BandwidthMonitor(
    maxBytesPerSecondPerPeer: 1_048_576,    // 1 MB/s per peer
    maxTotalBytesPerSecond: 10_485_760);     // 10 MB/s total

// Check before sending
if (monitor.CanSend(peerId, messageSize))
{
    await SendMessage(message);
    monitor.RecordSent(peerId, messageSize);
}
```

**Features:**
- Per-peer bandwidth limits
- Global bandwidth cap across all peers
- Per-second reset for fair distribution
- Real-time bandwidth statistics

#### PriorityMessageQueue

Priority queue with 4 levels for efficient message ordering:

```csharp
var queue = new PriorityMessageQueue(capacity: 1000);

// Enqueue with priority
await queue.EnqueueAsync(message, targetPeerId, MessagePriority.High);

// Dequeue highest priority first
var queued = await queue.DequeueAsync();
```

**Priority Levels:**
- **Critical**: Ping/Pong (network health)
- **High**: Blocks, BlockAccepted (consensus critical)
- **Normal**: Proofs, Headers (synchronization)
- **Low**: Transactions (can be delayed)

**Features:**
- Channel-based implementation for high performance
- FIFO within each priority level
- Bounded capacity with drop-oldest policy
- Async enqueue/dequeue operations

### Message Relay Flow

```
┌─────────────────┐
│  Receive Msg    │
│  from Peer A    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Validate      │
│   Message       │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      NO      ┌─────────────┐
│  Check Rate     │─────────────▶│  Drop Msg   │
│  Limit          │               └─────────────┘
└────────┬────────┘
         │ YES
         ▼
┌─────────────────┐      YES     ┌─────────────┐
│  Check if       │─────────────▶│  Drop Msg   │
│  Duplicate      │               └─────────────┘
└────────┬────────┘
         │ NO
         ▼
┌─────────────────┐
│  Mark as Seen   │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Enqueue for    │
│  Broadcast      │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Background     │
│  Worker         │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      NO      ┌─────────────┐
│  Check          │─────────────▶│  Skip Peer  │
│  Bandwidth      │               └─────────────┘
└────────┬────────┘
         │ YES
         ▼
┌─────────────────┐
│  Send to        │
│  Peer B, C, D   │
│  (except A)     │
└─────────────────┘
```

### Usage Example

```csharp
// Setup components
var connectionManager = new TcpConnectionManager(codec, peerManager, maxConnections: 50);
var relay = new MessageRelay(connectionManager, peerManager);

await connectionManager.StartAsync(new IPEndPoint(IPAddress.Any, 8333));

// Relay a received block
var blockMessage = await connection.ReceiveAsync();
if (blockMessage != null && blockMessage.Type == MessageType.NewBlock)
{
    // Validate block locally first
    var block = Block.Deserialize(((BlockProposalMessage)blockMessage).BlockData);
    if (await ValidateBlock(block))
    {
        // Relay to other peers
        await relay.RelayAsync(blockMessage, connection.PeerInfo.Id);
    }
}

// Broadcast a new transaction
var transaction = CreateTransaction();
var txMessage = new TransactionMessage(transaction.Serialize());
await relay.BroadcastAsync(txMessage);

// Check relay statistics
Console.WriteLine($"Relayed: {relay.TotalMessagesRelayed}");
Console.WriteLine($"Duplicates: {relay.TotalDuplicatesFiltered}");
Console.WriteLine($"Dropped: {relay.TotalMessagesDropped}");
```

### Configuration

Default settings are optimized for typical blockchain networks:

```csharp
// Custom configuration
var tracker = new MessageTracker(
    messageLifetime: TimeSpan.FromMinutes(10),  // Longer tracking
    maxTrackedMessages: 500_000);                // More capacity

var rateLimiter = new RateLimiter(
    maxTokens: 200,                              // More burst capacity
    refillInterval: TimeSpan.FromSeconds(1),
    refillAmount: 20);                           // Faster refill

var bandwidthMonitor = new BandwidthMonitor(
    maxBytesPerSecondPerPeer: 5_242_880,        // 5 MB/s per peer
    maxTotalBytesPerSecond: 52_428_800);         // 50 MB/s total

var relay = new MessageRelay(
    connectionManager,
    peerManager,
    messageTracker: tracker,
    rateLimiter: rateLimiter,
    bandwidthMonitor: bandwidthMonitor);
```

### Performance

Benchmarks on a typical development machine:

| Operation | Messages | Time | Rate |
|-----------|----------|------|------|
| Deduplication | 10,000 | ~5 ms | 2M/s |
| Rate Limiting | 10,000 | ~2 ms | 5M/s |
| Bandwidth Check | 10,000 | ~1 ms | 10M/s |
| Priority Queue | 10,000 | ~15 ms | 666K/s |

Run benchmarks:
```bash
dotnet run -c Release --project benchmarks/Spacetime.Benchmarks -- --filter "*MessageRelay*"
```

## Testing

The project includes comprehensive tests:

### Unit Tests (`Spacetime.Network.Tests`)
- Message codec encoding/decoding
- Peer manager operations
- Handshake message serialization
- Reputation scoring
- Message relay components (212 tests)
  - MessageTracker deduplication
  - RateLimiter token bucket
  - BandwidthMonitor limits
  - PriorityMessageQueue ordering
  - MessageRelay validation

### Integration Tests (`Spacetime.Network.IntegrationTests`)
- Multi-node connections
- Message exchange between peers
- Connection pooling limits
- Handshake protocol
- Connection failure handling
- Message relay propagation (15 tests)
  - Broadcast to multiple peers
  - Duplicate filtering
  - Rate limiting behavior
  - Bandwidth management
  - Priority ordering

Run tests:
```bash
# Unit tests
dotnet test tests/Spacetime.Network.Tests

# Integration tests
dotnet test tests/Spacetime.Network.IntegrationTests
```

## Block Synchronization

The network layer includes a comprehensive block synchronization system for nodes catching up with the network.

### Core Components

#### IBlockSynchronizer

The main synchronization interface that orchestrates initial blockchain download (IBD) and ongoing sync:

```csharp
var synchronizer = new BlockSynchronizer(
    peerManager,
    chainStorage,
    blockValidator,
    bandwidthMonitor,
    config: new SyncConfig
    {
        ParallelDownloads = 4,
        MaxHeadersPerRequest = 2000,
        MaxRetries = 3,
        IbdThresholdBlocks = 1000
    });

// Subscribe to progress updates
synchronizer.ProgressUpdated += (sender, progress) =>
{
    Console.WriteLine($"Sync: {progress.PercentComplete:F2}% - " +
                     $"{progress.CurrentHeight}/{progress.TargetHeight} - " +
                     $"State: {progress.State}");
};

// Start synchronization
await synchronizer.StartAsync();

// Check sync status
if (synchronizer.IsSynchronizing)
{
    Console.WriteLine($"IBD Mode: {synchronizer.IsInitialBlockDownload}");
}
```

#### SyncProgress

Tracks synchronization progress with real-time statistics:

```csharp
var progress = synchronizer.Progress;

Console.WriteLine($"Current Height: {progress.CurrentHeight}");
Console.WriteLine($"Target Height: {progress.TargetHeight}");
Console.WriteLine($"Percent Complete: {progress.PercentComplete:F2}%");
Console.WriteLine($"Blocks Downloaded: {progress.BlocksDownloaded}");
Console.WriteLine($"Blocks Validated: {progress.BlocksValidated}");
Console.WriteLine($"Bytes Downloaded: {progress.BytesDownloaded}");
Console.WriteLine($"Download Rate: {progress.DownloadRate:F0} B/s");
Console.WriteLine($"State: {progress.State}");

if (progress.EstimatedTimeRemaining.HasValue)
{
    Console.WriteLine($"ETA: {progress.EstimatedTimeRemaining}");
}
```

#### SyncState

The synchronization process goes through several states:

- **Idle**: Not synchronizing
- **Discovering**: Finding peers and determining target height
- **DownloadingHeaders**: Header-first synchronization phase
- **DownloadingBlocks**: Parallel block download phase
- **Validating**: Validating and applying downloaded blocks
- **Synced**: Synchronization complete, node is up to date
- **Failed**: Synchronization failed due to error
- **Cancelled**: Synchronization was cancelled

#### SyncConfig

Configuration options for synchronization behavior:

```csharp
var config = new SyncConfig
{
    MaxPeers = 8,                        // Max peers for sync
    ParallelDownloads = 4,               // Parallel downloads
    MaxHeadersPerRequest = 2000,         // Headers per request
    MaxRetries = 3,                      // Retry failed downloads
    DownloadTimeoutSeconds = 30,         // Download timeout
    IbdThresholdBlocks = 1000,           // IBD threshold
    ProgressUpdateIntervalMs = 1000,     // Progress update rate
    EnableBandwidthThrottling = true,    // Enable throttling
    MaxBandwidthBytesPerSecond = 10_485_760  // 10 MB/s max
};
```

### Features

#### Header-First Synchronization

The synchronizer uses header-first sync for efficiency:

1. Download headers from peers (lightweight, fast)
2. Identify missing blocks
3. Download blocks in parallel from multiple peers
4. Validate blocks as they arrive
5. Apply blocks to chain in order

#### Parallel Block Downloads

Multiple blocks are downloaded simultaneously for maximum throughput:

- Configurable parallelism (default: 4)
- Automatic peer selection based on reputation
- Retry logic for failed downloads
- Bandwidth management to prevent network saturation

#### Resume Capability

Synchronization can be interrupted and resumed:

```csharp
// Start synchronization
var syncTask = synchronizer.StartAsync();

// Later, stop synchronization
await synchronizer.StopAsync();

// Resume from where we left off
await synchronizer.ResumeAsync();
```

#### Progress Tracking

Real-time progress updates via events:

```csharp
synchronizer.ProgressUpdated += (sender, progress) =>
{
    // Update UI or log progress
    UpdateProgressBar(progress.PercentComplete);
    
    if (progress.EstimatedTimeRemaining.HasValue)
    {
        ShowETA(progress.EstimatedTimeRemaining.Value);
    }
};
```

#### Bandwidth Throttling

Integrates with `BandwidthMonitor` to respect network limits:

- Per-peer bandwidth limits
- Global bandwidth cap
- Configurable maximum throughput
- Automatic rate adjustment

#### Malicious Peer Handling

Automatically detects and bans peers providing invalid data:

- Block validation during sync
- Peer reputation tracking
- Automatic blacklisting of bad peers
- Retry with different peers

### Synchronization Flow

```
┌─────────────────┐
│   Discovering   │  Find peers, determine target height
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Download Headers│  Header-first sync (lightweight)
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│ Download Blocks │  Parallel downloads from peers
│   (Parallel)    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Validating    │  Validate blocks, check signatures
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│     Synced      │  Up to date with network
└─────────────────┘
```

### Initial Blockchain Download (IBD)

IBD mode is automatically detected when significantly behind:

```csharp
if (synchronizer.IsInitialBlockDownload)
{
    // Node is far behind, in IBD mode
    // Optimize for download speed
    Console.WriteLine("Performing initial blockchain download...");
}
else
{
    // Node is mostly synced, catching up
    Console.WriteLine("Catching up to network tip...");
}
```

IBD threshold is configurable via `SyncConfig.IbdThresholdBlocks` (default: 1000 blocks).

### Usage Example

Complete synchronization example:

```csharp
// Setup components
var peerManager = new PeerManager();
var chainStorage = new RocksDbChainStorage("./data");
var blockValidator = new BlockValidator(chainStorage, signatureVerifier);
var bandwidthMonitor = new BandwidthMonitor();

// Configure synchronization
var config = new SyncConfig
{
    ParallelDownloads = 4,
    MaxPeers = 8,
    EnableBandwidthThrottling = true,
    MaxBandwidthBytesPerSecond = 10_485_760  // 10 MB/s
};

// Create synchronizer
var synchronizer = new BlockSynchronizer(
    peerManager,
    chainStorage,
    blockValidator,
    bandwidthMonitor,
    config);

// Track progress
var progressTimer = new Timer(_ =>
{
    var p = synchronizer.Progress;
    Console.WriteLine($"[{p.State}] {p.PercentComplete:F2}% - " +
                     $"{p.CurrentHeight}/{p.TargetHeight} blocks - " +
                     $"{p.DownloadRate / 1024:F0} KB/s");
}, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

try
{
    // Start synchronization
    await synchronizer.StartAsync();
    Console.WriteLine("Synchronization completed successfully!");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Synchronization was cancelled.");
}
catch (Exception ex)
{
    Console.WriteLine($"Synchronization failed: {ex.Message}");
}
finally
{
    progressTimer.Dispose();
    await synchronizer.DisposeAsync();
}
```

### Performance

Block synchronization performance depends on several factors:

- Number of peers available
- Parallel download configuration
- Network bandwidth
- Storage performance (disk I/O)
- Block validation complexity

Typical performance on a well-connected node:

| Configuration | Blocks/sec | Bandwidth | Time for 10K blocks |
|--------------|------------|-----------|---------------------|
| 1 parallel   | ~20        | 2 MB/s    | ~8 minutes          |
| 4 parallel   | ~60        | 6 MB/s    | ~3 minutes          |
| 8 parallel   | ~80        | 8 MB/s    | ~2 minutes          |

Run synchronization benchmarks:
```bash
dotnet run -c Release --project benchmarks/Spacetime.Benchmarks -- --filter "*BlockSynchronization*"
```

## Dependencies

- .NET 10.0
- Spacetime.Core (Block, BlockValidator)
- Spacetime.Storage (IChainStorage, IBlockStorage)
- No external dependencies (uses built-in networking APIs)

## Future Enhancements

Potential improvements for future releases:

- **WebSocket Support** - Alternative to TCP for browser compatibility
- **NAT Traversal** - UPnP or STUN/TURN support for NAT hole punching
- **Protocol Negotiation** - Support multiple protocol versions
- **Connection Metrics** - Track bandwidth, latency, and throughput
- **Peer Exchange (PEX)** - Automatic peer discovery via connected peers
- **Certificate Pinning** - Enhanced TLS security
- **Compression** - Optional message compression for large payloads
- **Adaptive Rate Limits** - Dynamic rate limiting based on peer behavior
- **Message Batching** - Group small messages to reduce overhead

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
