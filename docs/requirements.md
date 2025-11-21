# Spacetime Blockchain Implementation Summary
*Assuming the Merkle Tree library is already implemented.*

This document outlines, in full detail, all components required to implement the Spacetime blockchain. It assumes the Merkle Tree library (plot construction, Merkle proofs, verification, and caching) already exists and is referenced by the miner. The components described below complete the consensus engine, networking layer, chain logic, and node behavior.

---

# 1. High-Level Architecture

Spacetime consists of:

1. **Full Node**
   - Maintains chain state
   - Validates blocks, transactions, and proofs
   - Communicates via P2P networking

2. **Miner (Prover)**
   - Generates plots (large binary files)
   - Produces proofs in response to network challenge announcements
   - Submits block proposals when winning proofs occur

3. **Merkle Tree Library (already implemented)**
   - Used by miners to build plots
   - Used by full nodes to verify proofs
   - Used when constructing blocks (transaction Merkle roots)

---

# 2. Plot System Requirements

The miner must support:

### 2.1 Plot Creation
- Deterministic generation of N leaf values using:
  - Miner public key
  - Plot seed
  - Nonce
- Streamed Merkle tree construction for large files
- Writing:
  - Header (magic, version, metadata, Merkle root, sizes)
  - Level-0 leaves
  - Optional Merkle cache

### 2.2 Plot Loading
- Reading header
- Validating header checksum and root
- Exposing:
  - Leaf count
  - Entry size
  - Root hash
  - Cache offsets
  - File handles for efficient random access

### 2.3 Plot Proof Generation
Given a network challenge:
- Compute score = H(challenge || leaf) for each leaf or subset (depending on scanning strategy)
- Track the best/lowest score
- For the leaf with best score:
  - Generate Merkle proof using the Merkle library
  - Submit proof and score to network

---

# 3. Consensus Mechanism (Proof-of-Spacetime)

Spacetime uses **Proof-of-Spacetime with timed epochs and challenge windows**.

### 3.1 Consensus Flow
1. Network enters a new epoch.
2. Full nodes derive the epoch challenge from the previous block’s hash.
3. Miners compute proofs against their plots.
4. A fixed **challenge window** occurs (e.g., 10 seconds).
5. Nodes collect submitted proofs.
6. The **lowest score** wins.
7. The winner produces the **block proposal**.
8. Full nodes validate:
   - Proof correctness
   - Merkle paths
   - Root correctness
   - Difficulty target
   - Timestamp rules
9. Block becomes the next canonical block.

### 3.2 Proof Scoring
- Score = H(challenge || leaf)
- Lower score = better
- Nodes accept block only if score < difficulty_target

### 3.3 Difficulty Adjustment
- Every `X` blocks, calculate rolling average block time.
- If average block time < target, make difficulty harder.
- If average block time > target, reduce difficulty.
- Difficulty affects **acceptable maximum score**.

---

# 4. Block Structure

A block contains:

### 4.1 Header
- Previous block hash
- Timestamp
- Epoch number
- Difficulty target
- Miner public key
- Plot ID
- Proof score
- Proof Merkle root (from the plot)
- Transaction Merkle root
- Block signature

### 4.2 Proof Object
- Leaf value
- Leaf index
- Sibling hashes
- Orientation bits
- Root (should match plot header root)
- Challenge used

### 4.3 Transactions
- A list of transactions (optional in early versions)
- Transaction Merkle tree (using Merkle lib)

### 4.4 Optional Metadata
- Versioning
- Extra proof data
- Node software version

---

# 5. Block Validation Logic

Full nodes validate:

### 5.1 Header Validation
- Timestamp within allowed skew
- Previous block hash matches known chain tip
- Difficulty target computed correctly
- Epoch/challenge correctness
- Block signature valid

### 5.2 Proof Validation
Using Merkle library:
- Check leaf value size and structure
- Verify Merkle path matches plot root
- Verify plot root matches the plot’s registered identity
- Recompute score = H(challenge || leaf)
- Confirm score < difficulty target

### 5.3 Transaction Validation
- Verify transaction signatures
- Check balances/state updates
- Confirm transaction Merkle root matches block header

If any check fails, block is rejected.

---

# 6. Node Storage and Chain Management

Nodes must store:

### 6.1 Block Storage
- Full block history
- LevelDB / RocksDB structure:
  - Block headers
  - Block bodies
  - Index by height, hash, and timestamp

### 6.2 UTXO/Account State (your choice)
- Could be UTXO-style like Bitcoin
- Or account-style like Ethereum
- State transitions validated per block

### 6.3 Chain Reorganization Rules
- Longest chain by cumulative difficulty
- Save orphan blocks
- If reorg occurs:
  - Roll back state
  - Replay correct chain

---

# 7. Networking Layer

Nodes communicate via P2P with the following message types:

### 7.1 Discovery Messages
- `HELLO`
- `PEER_LIST`
- `REQUEST_PEERS`

### 7.2 Synchronization Messages
- `GET_HEADERS`
- `HEADERS`
- `GET_BLOCK`
- `BLOCK`
- `PING` / `PONG`

### 7.3 Consensus Messages
- `NEW_CHALLENGE`
- `PROOF_SUBMISSION`
- `BLOCK_PROPOSAL`
- `BLOCK_ACCEPTED`

### 7.4 Transaction Relay Messages
- `TX`
- `TX_POOL_REQUEST`

Nodes must:
- Maintain peer lists
- Throttle bandwidth
- Validate incoming blocks
- Relay valid messages

---

# 8. Mining (Prover) Node Behavior

Miners perform:

### 8.1 Challenge Handling
- Receive new epoch challenge from network
- Begin scanning plots using rollover timers

### 8.2 Proof Search
- Iterate through plots (possibly in parallel)
- Compute score for each leaf or subset
- Track best result

### 8.3 Proof Submission
- Submit proof and score to network
- If winner:
  - Build block proposal
  - Broadcast block

### 8.4 Plot Management
- Maintain multiple plots
- Track metadata (IDs, space allocated, roots)
- Handle corrupted plots gracefully

---

# 9. Difficulty and Epoch Management

### 9.1 Epoch Rules
- Fixed length (e.g., 10 seconds)
- Challenge derived from previous block hash

### 9.2 Difficulty Target
- Stored in block header
- Adjusted every N blocks
- Controls acceptable proof score threshold

### 9.3 Probability Model
Difficulty must ensure:
- Consistent average block time
- Fair win probability proportional to disk space

---

# 10. Transaction System (Optional Early)

Transactions may follow:

### 10.1 UTXO Model
- Simpler to implement
- Easy Merkle tree integration

### 10.2 Account Model
- More extensible (smart contracts later)

Each transaction includes:
- Sender
- Recipient
- Amount
- Nonce
- Signature

State updates applied at block acceptance.

---

# 11. Security Considerations

- All block headers signed by miner public key
- Plot roots bound to miner identity via plot header
- Anti-replay protections with challenge uniqueness
- Difficulty ensures no trivial challenge exploitation
- Network messages must guard against spam/flood

---

# 12. Modular Design Strategy

The system should be split into clear modules:

1. **Merkle library** (completed)  
2. **Plot system**  
3. **Proof generator + score calculator**  
4. **Proof verifier**  
5. **Block builder/validator**  
6. **Chain manager (storage, reorg rules)**  
7. **Networking/P2P**  
8. **Consensus manager (epochs + challenges)**  
9. **Transaction/state layer**  
10. **Node configuration system**

Each module should be testable independently.

---

# 13. Full Node Lifecycle Summary

1. Node starts
2. Loads chain from disk
3. Connects to peers
4. Syncs headers & blocks
5. Enters steady-state consensus loop:
   - Receive `NEW_CHALLENGE`
   - Validate and relay proofs
   - Validate and relay blocks
   - Update chain and state

---

# 14. Miner (Prover) Lifecycle Summary

1. Boot
2. Load plots
3. Register with full node or local validator
4. For each epoch:
   - Receive challenge
   - Scan plots
   - Find best score
   - Generate Merkle proof using Merkle library
   - Submit proof
   - If winner → build & broadcast block

---

# 15. What Is *Not* Needed Initially (but planned)

- Smart contracts
- Internal VM
- Advanced fee markets
- Zero-knowledge proofs
- Plot compression
- Optimistic concurrency for state updates

---

# Conclusion

This summary defines everything required to implement the Spacetime blockchain now that the Merkle Tree library is complete. All remaining work revolves around:

- Plot generation and access
- Proof-of-Spacetime consensus runtime
- Block validation and chain maintenance
- Network messaging and peer discovery
- Miner event loop and block construction
- Difficulty and epoch management

This provides the full blueprint for a working end-to-end blockchain implementation.