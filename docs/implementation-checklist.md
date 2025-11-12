# 🧱 Proof of Space-Time Blockchain — Implementation Checklist

A structured roadmap for building a low-energy, fair, mineable blockchain using a Proof of Space-Time (PoST) consensus mechanism.

---

## Phase 0 — Project Setup & Foundations

- [ ] **Create Repository**
  - Initialize Git repo with folders:
    - `/src` — core codebase
    - `/docs` — design specs, notes
    - `/tests` — unit/integration tests
    - `/plots` — local storage for miner plots
    - `/data` — blockchain data (blocks, chainstate)

- [ ] **Define Core Dependencies**
  - Choose .NET version (>= .NET 10 recommended)
  - Add references for cryptography, serialization, and networking
  - Configure build & linting pipeline (CI/CD optional)

- [ ] **Define Project Goals**
  - Document objectives: low-energy mining, fair participation, educational focus
  - Record design constraints: decentralization, simplicity, deterministic behavior

---

## Phase 1 — Core Blockchain Layer

### Data Structures
- [ ] Define `BlockHeader` fields:
  - version, previous_hash, merkle_root, timestamp, height, challenge, miner_id, proof
- [ ] Define `Block` structure:
  - header, transaction list, block size limit
- [ ] Define `Transaction` format:
  - inputs, outputs, signature, fee
- [ ] Implement `MerkleTree` utility for transactions
- [ ] Define `ChainState` model:
  - account balances, UTXO set, block height, best chain hash

### Serialization & Storage
- [ ] Implement binary serialization for blocks and transactions
- [ ] Implement block persistence (append-only file or lightweight database)
- [ ] Add index for fast lookups by hash and height

### Hashing & Signatures
- [ ] Standardize hashing function (SHA256 or Blake2b)
- [ ] Add ECDSA key pair utilities for wallet and miner identity
- [ ] Verify all blocks and transactions are deterministic across nodes

---

## Phase 2 — Plotting System (Proof of Space)

### Plot File Format
- [ ] Design deterministic plot file structure:
  - header (plot ID, miner public key, seed, creation time)
  - entries (fixed-size binary records)
  - Merkle root of entries for proof verification
- [ ] Implement plot generation (“plotting”)
  - Derive plot seed = `H(minerPubKey || salt)`
  - Generate entries and store sequentially
  - Compute Merkle root and store in header

### Plot Verification
- [ ] Implement reading and indexing of plot files
- [ ] Implement Merkle path generation for a specific entry
- [ ] Implement verification of entry existence using Merkle proof
- [ ] Add basic checksum or version field to detect corruption

### Miner Configuration
- [ ] Add miner config file (path to plots, miner keypair)
- [ ] Support multiple plots and concurrent access

---

## Phase 3 — Challenge & Consensus Core

### Challenge Derivation
- [ ] Define rule for deterministic challenge generation:
  - `challenge_N = H(blockHash_{N-1} || randomness_seed)`
- [ ] Store `randomness_seed` in each block (derived from beacon or rolling hash)
- [ ] Ensure all nodes independently compute identical challenges

### Mining Logic
- [ ] Implement mining loop:
  - Retrieve current challenge
  - Search through plots for valid proof
  - Select best proof (lowest hash score)
  - Build candidate block
- [ ] Implement difficulty adjustment based on block interval averages
- [ ] Include miner public key and proof in block header
- [ ] Add coinbase transaction for block reward

### Block Validation
- [ ] Validate:
  - previous_hash linkage
  - proof correctness (challenge → proof → plot entry)
  - Merkle proof validity
  - block signature
- [ ] Reject invalid or duplicate blocks

---

## Phase 4 — Proof of Time (Space-Time Extension)

### Epoch & Timing Model
- [ ] Define epoch duration (e.g., 30 seconds or 1 minute)
- [ ] Track time since last valid proof for each miner
- [ ] Require miners to respond to periodic challenges to maintain eligibility

### Proof Retention
- [ ] Record proof timestamps and miner activity in node state
- [ ] Implement “decay” logic:
  - miners lose weight or eligibility after missing N epochs
- [ ] Optionally add small bonded stake per miner (anti-Sybil measure)

### Challenge Rotation
- [ ] Generate new epoch challenge:
  - `epoch_challenge = H(last_block_hash || global_beacon)`
- [ ] Broadcast epoch transitions (optional; can be derived locally)
- [ ] Allow late proofs within small tolerance window

---

## Phase 5 — Networking Layer

### Node-to-Node Communication
- [ ] Implement P2P message types:
  - `version`, `getblocks`, `inv`, `block`, `tx`, `challenge`, `proof`
- [ ] Establish peer discovery mechanism (config file or DNS seeds)
- [ ] Implement block and transaction gossip
- [ ] Add basic DoS protection (rate limiting, signature checks)

### Sync & Consensus
- [ ] Implement longest-chain (or heaviest-chain) rule
- [ ] Resolve forks deterministically
- [ ] Detect and ban peers sending invalid blocks
- [ ] Allow reorg within limited depth (for recent epochs only)

---

## Phase 6 — Wallet & Transactions

### Wallet Management
- [ ] Implement key generation, import/export, and encryption
- [ ] Track unspent outputs (UTXOs) or account balances
- [ ] Add transaction builder with fee estimation

### Transaction Handling
- [ ] Implement transaction validation rules
  - signature verification, double-spend prevention
- [ ] Maintain mempool with fee prioritization
- [ ] Include pending transactions in next mined block

---

## Phase 7 — Node Lifecycle & CLI Tools

### Node Operation
- [ ] Implement node start/stop and graceful shutdown
- [ ] Implement background mining process
- [ ] Add local HTTP or CLI interface for monitoring

### CLI Utilities
- [ ] Command to create plots
- [ ] Command to list plots and verify integrity
- [ ] Command to start miner
- [ ] Command to view blockchain height, peers, and stats

---

## Phase 8 — Testing & Validation

### Unit Testing
- [ ] Write tests for:
  - hashing utilities
  - Merkle tree generation and verification
  - plot creation and proof validation
  - block validation logic

### Simulation Testing
- [ ] Simulate multiple miners locally
- [ ] Measure block time stability and fairness
- [ ] Test plot corruption and recovery

### Network Testing
- [ ] Spin up multi-node testnet
- [ ] Validate peer synchronization and fork resolution
- [ ] Test mining under network delay and high latency

---

## Phase 9 — Economics & Fairness

- [ ] Define block reward schedule and halving logic
- [ ] Define transaction fee model
- [ ] Implement difficulty retarget algorithm
- [ ] Implement anti-Sybil measures (optional stake or identity layer)
- [ ] Design reward decay for inactive miners

---

## Phase 10 — Monitoring & Optimization

- [ ] Add logging and telemetry
- [ ] Measure disk I/O and memory footprint
- [ ] Optimize plot reading (use memory-mapped files)
- [ ] Optimize proof search (parallel reads)
- [ ] Cache Merkle trees for faster verification

---

## Phase 11 — Finalization Layer (Optional)

- [ ] Implement lightweight finality mechanism:
  - rotating validators or BFT committee
- [ ] Define checkpoint interval for immutable blocks
- [ ] Enable rapid re-sync via checkpoints

---

## Phase 12 — Documentation & Community

- [ ] Write developer docs (API, data formats, protocol rules)
- [ ] Write architecture overview diagram
- [ ] Document build/run instructions
- [ ] Create tutorial for plotting and mining
- [ ] Publish testnet and genesis configuration
- [ ] Collect community feedback on parameters and fairness

---

✅ **Completion Criteria**
- Network achieves stable block production
- Nodes reach consensus independently
- Energy usage remains low (disk-based mining)
- Fairness verified via simulation (distribution of block winners)
- All core phases tested locally

---

### 📘 Notes
This checklist assumes:
- Single-threaded block validation (can parallelize later)
- Deterministic cryptographic primitives
- Simple economic model initially
- Progressive iteration (each phase should be working before moving on)

---
