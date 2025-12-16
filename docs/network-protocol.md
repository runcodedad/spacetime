# Spacetime Network Protocol

## Overview

The Spacetime network protocol enables peer-to-peer communication between nodes in the blockchain network. It uses TCP connections with a simple length-prefixed binary message format.

## Message Format

All messages use a consistent binary format:

```
[4 bytes: message length (little-endian)]
[1 byte: message type]
[N bytes: payload]
```

- **Maximum message size**: 16 MB (configurable via `MessageValidator.MaxPayloadSize`)
- **Byte order**: Little-endian for all numeric values
- **Encoding**: Binary serialization (not Protocol Buffers)

### NetworkMessage Architecture

All protocol messages inherit from the abstract `NetworkMessage` base class, which provides:

- **Type Property**: Each message declares its `MessageType` enum value
- **Payload Property**: Lazy-loaded, cached serialized payload data (`ReadOnlyMemory<byte>`)
- **Serialize Method**: Abstract method each message implements for its specific serialization
- **Deserialize Method**: Static factory method that routes deserialization to the appropriate message class
- **Caching**: Serialized payload is cached to avoid repeated serialization

**Implementation Pattern:**
```csharp
public sealed class ExampleMessage : NetworkMessage
{
    public override MessageType Type => MessageType.Example;
    
    // Message-specific properties
    public string Data { get; }
    
    // Constructor with validation
    public ExampleMessage(string data)
    {
        ArgumentNullException.ThrowIfNull(data);
        Data = data;
    }
    
    // Serialization logic
    protected override byte[] Serialize() { /* ... */ }
    
    // Deserialization logic
    internal static ExampleMessage Deserialize(ReadOnlyMemory<byte> data) { /* ... */ }
}
```

## Message Types

All messages inherit from the abstract `NetworkMessage` base class. Each message type implements its own serialization/deserialization logic.

**Empty Messages:** Some message types (GetPeers, Heartbeat, HandshakeAck) carry no payload and are represented internally by an `EmptyMessage` implementation.

### Message Type Reference

| Type | Hex  | Class Name | Category | Description |
|------|------|------------|----------|-------------|
| Handshake | 0x01 | HandshakeMessage | Discovery | Connection establishment with node info |
| HandshakeAck | 0x02 | EmptyMessage | Discovery | Response to handshake (empty) |
| Heartbeat | 0x03 | EmptyMessage | Discovery | Keep-alive (legacy, use Ping/Pong) |
| Ping | 0x04 | PingPongMessage | Discovery | Connection liveness check with nonce |
| Pong | 0x05 | PingPongMessage | Discovery | Response to Ping with same nonce |
| GetPeers | 0x10 | EmptyMessage | Discovery | Request for peer list (empty) |
| Peers | 0x11 | PeerListMessage | Discovery | Response containing peer addresses |
| GetHeaders | 0x20 | GetHeadersMessage | Synchronization | Request block headers |
| Headers | 0x21 | HeadersMessage | Synchronization | Response with block headers |
| GetBlock | 0x22 | GetBlockMessage | Synchronization | Request complete block |
| Block | 0x23 | BlockMessage | Synchronization | Response with complete block |
| Transaction | 0x30 | TransactionMessage | Transaction | Broadcast transaction |
| NewBlock | 0x31 | BlockProposalMessage | Transaction | Broadcast new block proposal |
| TxPoolRequest | 0x32 | TxPoolRequestMessage | Transaction | Request mempool contents |
| ProofSubmission | 0x40 | ProofSubmissionMessage | Consensus | Submit PoST proof |
| BlockAccepted | 0x41 | BlockAcceptedMessage | Consensus | Notify block validation success |
| Error | 0xFF | (Not Implemented) | Error | Generic error message |

### Discovery Messages (0x01-0x11)

#### 0x01 - Handshake
Sent when establishing a new connection.

**Payload Format:**
```
[4 bytes: protocol version]
[8 bytes: timestamp]
[4 bytes: node ID length]
[N bytes: node ID (UTF-8)]
[4 bytes: user agent length]
[N bytes: user agent (UTF-8)]
```

**Example:**
```csharp
var handshake = new HandshakeMessage(
    protocolVersion: 1,
    nodeId: "node-abc123",
    userAgent: "Spacetime/1.0.0",
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
```

#### 0x02 - HandshakeAck
Response to handshake with same format as Handshake.

#### 0x03 - Heartbeat
Legacy keep-alive message. Can be empty or contain optional data (max 1024 bytes).

**Note:** Use Ping/Pong for new implementations.

#### 0x04 - Ping
Connection liveness check with nonce.

**Payload Format:**
```
[8 bytes: nonce]
[8 bytes: timestamp]
```

**Example:**
```csharp
var ping = new PingPongMessage(
    nonce: Random.Shared.NextInt64(),
    timestamp: DateTimeOffset.UtcNow.ToUnixTimeSeconds());
```

#### 0x05 - Pong
Response to Ping, must echo back the same nonce.

**Payload Format:** Same as Ping

#### 0x10 - GetPeers
Request for known peer addresses. No payload (empty message).

#### 0x11 - Peers
Response containing list of peer addresses.

**Payload Format:**
```
[4 bytes: peer count]
For each peer:
  [4 bytes: IP address length]
  [N bytes: IP address bytes]
  [4 bytes: port]
```

**Limits:** Maximum 1000 peers per message

**Example:**
```csharp
var peers = new List<IPEndPoint>
{
    new IPEndPoint(IPAddress.Parse("192.168.1.100"), 8333)
};
var peerList = new PeerListMessage(peers);
```

### Synchronization Messages (0x20-0x23)

#### 0x20 - GetHeaders
Request block headers starting from a specific hash.

**Payload Format:**
```
[32 bytes: locator hash]
[1 byte: has stop hash (0 or 1)]
[0 or 32 bytes: stop hash (if has stop hash = 1)]
[4 bytes: max headers to return]
```

**Example:**
```csharp
var getHeaders = new GetHeadersMessage(
    locatorHash: lastKnownBlockHash,
    stopHash: ReadOnlyMemory<byte>.Empty,
    maxHeaders: 2000);
```

#### 0x21 - Headers
Response containing block headers.

**Payload Format:**
```
[4 bytes: header count]
For each header:
  [4 bytes: serialized header length]
  [N bytes: serialized header data]
```

**Limits:** Maximum 2000 headers per message

**Example:**
```csharp
var headers = blockHeaders.Select(h => 
    new ReadOnlyMemory<byte>(h.Serialize())).ToList();
var headersMsg = new HeadersMessage(headers);
```

#### 0x22 - GetBlock
Request a complete block by its hash.

**Payload Format:**
```
[32 bytes: block hash]
```

**Example:**
```csharp
var getBlock = new GetBlockMessage(blockHash);
```

#### 0x23 - Block
Response containing a complete block.

**Payload Format:**
```
[N bytes: serialized block data]
```

**Limits:** Maximum 16 MB per block

**Example:**
```csharp
var blockData = block.Serialize();
var blockMsg = new BlockMessage(blockData);
```

### Transaction Messages (0x30-0x32)

#### 0x30 - Transaction
Broadcast a transaction to the network.

**Payload Format:**
```
[N bytes: serialized transaction data]
```

**Limits:** Maximum 1 MB per transaction

**Example:**
```csharp
var txData = transaction.Serialize();
var txMsg = new TransactionMessage(txData);
```

#### 0x31 - NewBlock
Broadcast a newly proposed block to the network.

**Payload Format:** Same as Block message (complete serialized block data)

**Limits:** Maximum 16 MB per block

**Note:** This message type is implemented by `BlockProposalMessage` in the code.

#### 0x32 - TxPoolRequest
Request contents of a node's transaction pool (mempool).

**Payload Format:**
```
[4 bytes: max transactions to return]
[1 byte: include transaction data (0=hashes only, 1=full data)]
```

**Example:**
```csharp
var request = new TxPoolRequestMessage(
    maxTransactions: 1000,
    includeTransactionData: true);
```

### Consensus Messages (0x40-0x41)

#### 0x40 - ProofSubmission
Submit a Proof-of-Space-Time proof.

**Payload Format:**
```
[8 bytes: block height]
[33 bytes: miner ID (compressed public key)]
[4 bytes: proof data length]
[N bytes: serialized proof data]
```

**Limits:** Maximum 1 MB per proof

**Example:**
```csharp
var proofData = blockProof.Serialize();
var proofMsg = new ProofSubmissionMessage(
    proofData: proofData,
    minerId: minerPublicKey,
    blockHeight: nextHeight);
```

#### 0x41 - BlockAccepted
Notify that a block has been validated and accepted.

**Payload Format:**
```
[32 bytes: block hash]
[8 bytes: block height]
```

**Example:**
```csharp
var accepted = new BlockAcceptedMessage(blockHash, blockHeight);
```

### Error Messages (0xFF)

#### 0xFF - Error
Generic error message for protocol violations or failures.

**Payload Format:** Freeform (typically UTF-8 error message)

## Protocol Flows

### Initial Connection

1. Node A connects to Node B via TCP
2. Node A sends **Handshake** message
3. Node B validates protocol version and node info
4. Node B responds with **HandshakeAck**
5. Optional: Exchange **Ping/Pong** to measure latency
6. Connection is established

### Peer Discovery

1. Node A sends **GetPeers** to Node B
2. Node B responds with **Peers** containing up to 1000 known peers
3. Node A evaluates peer list and may connect to new peers
4. Process repeats periodically or on-demand

### Block Synchronization

1. Node A (behind) sends **GetHeaders** with last known block hash
2. Node B (current) responds with **Headers** containing newer headers
3. Node A validates headers and identifies missing blocks
4. For each missing block:
   - Node A sends **GetBlock** with block hash
   - Node B responds with **Block** containing full block data
5. Node A validates and adds blocks to its chain

### Block Production & Propagation

1. Miner finds a valid proof
2. Miner sends **ProofSubmission** to network
3. Nodes validate the proof
4. If valid, nodes respond with **BlockAccepted**
5. Miner creates complete block
6. Miner broadcasts **NewBlock** (BlockProposal)
7. Nodes validate and add block to chain
8. Nodes propagate **BlockAccepted** to their peers

### Transaction Propagation

1. Wallet creates and signs transaction
2. Wallet sends **Transaction** to connected node
3. Node validates transaction
4. If valid, node adds to mempool
5. Node propagates **Transaction** to its peers
6. Nodes can request mempool contents via **TxPoolRequest**

## Message Validation

All messages should be validated before processing:

```csharp
var message = await connection.ReceiveAsync();
if (message != null && MessageValidator.ValidateMessage(message))
{
    // Process message based on type
}
else
{
    // Invalid message - close connection or send Error
}
```

### Validation Rules

Each message class implements validation at two levels:

**Constructor Validation** (during message creation):
- Check for null parameters with `ArgumentNullException.ThrowIfNull()`
- Validate string parameters are not empty
- Verify numeric ranges (e.g., block height >= 0)
- Enforce size constraints (e.g., hash = 32 bytes, miner ID = 33 bytes)
- Check maximum collection sizes

**Deserialization Validation** (when receiving messages):
- Verify payload length matches expected format
- Check for buffer underruns/overruns
- Validate data ranges and constraints
- Throw `InvalidDataException` for malformed data

**Network-Level Validation**:
1. **Size Limits**: Enforce maximum payload sizes per message type
2. **Format Validation**: Verify correct serialization format
3. **Semantic Validation**: Check business logic constraints (via validators)
4. **Rate Limiting**: Prevent message flooding per peer
5. **Blacklisting**: Track and ban misbehaving peers

## Security Considerations

### Connection Security

- **TLS Support**: Optional TLS 1.2/1.3 encryption
- **Certificate Validation**: Implement proper certificate verification in production
- **Connection Limits**: Enforce maximum concurrent connections

### Message Security

- **Input Validation**: Always validate all message fields
- **Size Limits**: Enforce strict size limits to prevent DoS
- **Rate Limiting**: Limit message frequency per peer
- **Nonce Tracking**: Track nonces to prevent replay attacks

### Peer Management

- **Reputation System**: Track peer behavior (success/failure counts)
- **Blacklisting**: Automatically ban misbehaving peers
- **Whitelist Mode**: Optional whitelist-only connections

## Implementation Guidelines

### Best Practices

1. **Always use async/await** for all I/O operations
2. **Validate all inputs** before deserializing or processing
3. **Use CancellationToken** for graceful shutdown
4. **Implement timeout handling** for all network operations
5. **Log protocol violations** for debugging and security analysis
6. **Use connection pooling** to manage peer connections efficiently
7. **Implement backoff strategies** for failed connections

### Error Handling

```csharp
try
{
    var message = await connection.ReceiveAsync(cancellationToken);
    if (message == null)
    {
        // Connection closed gracefully
        return;
    }

    if (!MessageValidator.ValidateMessage(message))
    {
        // Send error and close connection
        await connection.SendAsync(
            new NetworkMessage(MessageType.Error, 
                Encoding.UTF8.GetBytes("Invalid message format")));
        await connection.CloseAsync();
        return;
    }

    // Process valid message
}
catch (OperationCanceledException)
{
    // Shutdown requested
}
catch (IOException ex)
{
    // Network error - log and cleanup
    logger.LogWarning(ex, "Network I/O error");
    await connection.CloseAsync();
}
```

### Testing

- Write unit tests for each message type's serialization/deserialization
- Test round-trip serialization to ensure data preservation
- Test edge cases (empty lists, maximum sizes, invalid data)
- Write integration tests for complete protocol flows
- Test error conditions and recovery

## Version Compatibility

### Current Version: 1

The protocol version is exchanged during the handshake. Nodes should:

1. Check protocol version compatibility
2. Reject connections with incompatible versions
3. Support graceful degradation where possible

### Future Version Support

When introducing breaking changes:

1. Increment protocol version number
2. Support multiple versions during transition period
3. Document version differences
4. Provide migration guide

## Message Implementation Summary

The Spacetime network protocol provides 13 message types across 4 categories:

- **7 Discovery Messages**: Connection establishment, peer discovery, liveness checking
- **4 Synchronization Messages**: Block header and full block exchange
- **3 Transaction Messages**: Transaction and mempool management, block proposals
- **2 Consensus Messages**: Proof submission and block acceptance notifications

All messages follow a consistent pattern:
1. Inherit from `NetworkMessage` abstract base class
2. Implement `Type` property and `Serialize()` method
3. Provide static `Deserialize()` method for reconstruction
4. Validate inputs in constructor (throw `ArgumentException`/`ArgumentNullException`)
5. Validate format in deserialization (throw `InvalidDataException`)
6. Use little-endian byte order for all numeric values
7. Cache serialized payload to avoid repeated serialization

## References

- [Spacetime.Network Implementation](../src/Spacetime.Network/)
- [Spacetime.Network README](../src/Spacetime.Network/README.md)
- [Network Tests](../tests/Spacetime.Network.Tests/)
- [Network Integration Tests](../tests/Spacetime.Network.IntegrationTests/)
- [Core Types Documentation](../src/Spacetime.Core/)
