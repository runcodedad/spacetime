# Peer addresses vs. connections — roles and integration

## Overview

This document explains the difference between the address/discovery layer (`PeerAddress`, `PeerAddressBook`, peer exchange and gossiping) and the runtime connection/peer-management layer (`IPeerConnection`, `IPeerManager`, `IConnectionManager`). It describes responsibilities, lifecycle, and how components should interact.

## High-level summary

- `PeerAddress` / `PeerAddressBook`: persistent, endpoint-centric discovery and quality-tracking. Stores `IPEndPoint` plus metadata (first/last seen, attempts, success/failure counts, source) and implements validation, IP diversity, pruning and persistence.
- `IPeerConnection` / `IConnectionManager`: short-lived runtime socket abstractions and connection pool. Handles message I/O, handshakes and the actual network transport.
- `IPeerManager`: identity-centric runtime peer bookkeeping and reputation. Tracks `PeerInfo` (peer id, endpoint, score), connected peers and blacklisting decisions.

## Responsibilities

- `PeerAddressBook`:
  - Validate discovered endpoints (reject private/link-local unless allowed).
  - Enforce IP diversity (per-subnet limits).
  - Score endpoints by `QualityScore` (success/failure ratio).
  - Persist addresses and load them on startup.
  - Evict low-quality or stale addresses when capacity limits are reached.

- `IPeerManager`:
  - Maintain known peers by identifier (`PeerInfo`).
  - Track per-peer reputation and failure counts for blacklisting.
  - Provide lists of best peers by reputation for connection attempts.

- `IPeerConnection` / `IConnectionManager`:
  - Open/close TCP connections (optionally TLS).
  - Send/receive framed `NetworkMessage` instances.
  - Maintain connection state (`IsConnected`) and active connection list.

## Data model differences

- `PeerAddress` is keyed by `IPEndPoint` and stores endpoint metadata; its `QualityScore` is computed from `SuccessCount` / `FailureCount`.
- `PeerInfo` (used by `IPeerManager`/`IPeerConnection`) is identity-first (peer id) and may include one or more endpoints, protocol/version info, and runtime reputation.

## Lifecycle and flow

1. Discovery: seeds, gossip or peer exchange populate `PeerAddressBook` with `PeerAddress` entries.
2. Selection: the connection manager or a connector service queries `PeerAddressBook.GetBestAddresses()` to pick endpoints to attempt.
3. Connection attempt: `IConnectionManager.ConnectAsync(endPoint)` creates an `IPeerConnection`.
4. Handshake: on successful handshake the remote node's `PeerInfo` is learned; `IPeerManager.AddPeer(peerInfo)` is called and the `PeerAddressBook.RecordSuccess(endPoint)` is updated.
5. Runtime: `IPeerConnection` handles message I/O; `IPeerManager` tracks reputation and connected peers; `PeerAddressBook` continues to record successes/failures and `LastSeen` updates.
6. Failure/blacklisting: repeated failures update both endpoint quality and peer reputation; blacklisted peers are disconnected via `IConnectionManager` and removed/marked by `IPeerManager`.

## Overlap and why both exist

- Both layers track successes/failures and produce “best” candidates, but at different granularities: one is endpoint-focused and persisted across restarts (`PeerAddressBook`), the other is peer-identity focused and runtime-only (`IPeerManager`).
- Address book mitigates Sybil-style attacks via subnet diversity and capacity management; peer manager enforces runtime policies like blacklisting by behavior.

## Integration guidance (practical patterns)

- On startup:
  - Load addresses: `await addressBook.LoadAsync()`
  - Populate initial peer list in `IPeerManager` with lightweight `PeerInfo` entries derived from addresses (optionally generate deterministic ids if not known).

- When receiving a `PeerListMessage` / gossip:
  - Convert received `IPEndPoint`s to `PeerAddress` and call `addressBook.AddAddress()`.
  - Do not immediately convert every address to a `PeerInfo`; instead use `GetBestAddresses()` when attempting connections.

- When a connection succeeds:
  - Create or update `PeerInfo` in `IPeerManager` and call `addressBook.RecordSuccess(endPoint)`.
  - Update `IPeerManager.RecordSuccess(peerId)` so reputation reflects identity-level behavior.

- When a connection fails:
  - Call `addressBook.RecordFailure(endPoint)` and `IPeerManager.RecordFailure(peerId)` (if a `peerId` is known).

## Example (pseudo-C# flow)

```csharp
// pick endpoints to try
var endpoints = addressBook.GetBestAddresses(8);

foreach (var pa in endpoints)
{
    var conn = await connectionManager.ConnectAsync(pa.EndPoint);
    if (conn != null && conn.IsConnected)
    {
        var handshake = await DoHandshake(conn);
        var peerInfo = new PeerInfo(handshake.NodeId, pa.EndPoint, handshake.ProtocolVersion);
        peerManager.AddPeer(peerInfo);
        addressBook.RecordSuccess(pa.EndPoint);
        peerManager.RecordSuccess(peerInfo.Id);
    }
    else
    {
        addressBook.RecordFailure(pa.EndPoint);
    }
}
```

## Where to read more

- Peer address book implementation: `src/Spacetime.Network/PeerAddressBook.cs`
- Runtime peer management: `src/Spacetime.Network/IPeerManager.cs`, `src/Spacetime.Network/IPeerConnection.cs`

If you'd like, I can add a small connector service example that demonstrates this glue code in the repository under `src/Spacetime.Network/`.
