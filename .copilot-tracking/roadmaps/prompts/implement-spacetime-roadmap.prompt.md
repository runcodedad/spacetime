---
agent: roadmap-manager
model: GPT-5 mini (copilot)
---

<!-- markdownlint-disable-file -->

# Implementation Prompt: Spacetime Roadmap (Updated 2025-12-12)

## Overview

This roadmap guides implementation of the Spacetime blockchain from its current state (Phase 1-2 complete) through production-ready deployment. Focus on Phase 3 (Networking) as the critical path, with parallel work on Phase 7 (Emission), Phase 4 (Miner), and Phase 6 (State Root).

## Implementation Priorities

### Tier 1: Critical Path & Security (Start Immediately)

1. **#18 - P2P Network Foundation** (Priority: 93)
   - **Critical path blocker** for Phase 5 (Full Node)
   - Start immediately as highest technical priority
   
2. **#32 - Security Measures** (Priority: 96) **HIGHEST OVERALL PRIORITY**
   - Address critical items incrementally:
     - Input validation in consensus (blocks, transactions, proofs)
     - Crypto safety (secure key generation, signature verification)
     - DoS protection (rate limiting, message size limits, peer banning)
   - Schedule full security audit for Q1 2026

3. **#67 - Emission Curve** (Priority: 92)
   - Foundation for economic model
   - Blocks all other emission work (#68-#73)
   - Can start in parallel with #18

### Tier 2: Parallel High-Value Work

4. **#64 - State Root & Light Client Support** (Priority: 88)
   - Enables light clients and SPV validation
   - Can start immediately (dependencies #13 ✅, #11 ✅ complete)

5. **#20 - Plot Scanning Strategies** (Priority: 84)
   - Foundation for miner implementation
   - Can start immediately (plot system complete)

6. **#28 - Integration Test Suite** (Priority: 75)
   - Critical for catching issues early
   - Start alongside Phase 3 development

## Phase-by-phase Execution

### Phase 3 — P2P Networking Layer (8-12 weeks)

**Critical Path: Complete in order**

#### Issue #18: Implement P2P Network Foundation (4-6 weeks)

**Prerequisites:**
- None - all dependencies complete

**Implementation Notes:**
- Start with TCP connection management (accept, connect, disconnect)
- Implement peer discovery mechanism (seed nodes, peer exchange)
- Add peer list management (maintain N active peers)
- Implement handshake protocol (version negotiation, network ID)
- Add connection encryption (TLS or Noise protocol)
- Implement peer reputation/scoring system
- Handle connection failures gracefully (auto-reconnect with backoff)

**Acceptance Criteria:**
- Successfully connect to seed nodes
- Maintain stable peer connections (target: 8-32 peers)
- Automatic peer discovery working
- Encrypted connections established
- Peer reputation prevents spam/attack
- Unit tests for connection handling
- Integration tests with multiple nodes

**Stop Point:** Validate peer connectivity and stability before proceeding to #17

---

#### Issue #17: Implement Network Message Types (3-4 weeks)

**Prerequisites:**
- #18 (P2P Network Foundation) complete

**Implementation Notes:**
- Define Protocol Buffers schemas (or chosen serialization format)
- Implement message framing (length-prefixed messages)
- Discovery messages:
  - HELLO/HANDSHAKE (version, network ID, capabilities)
  - PEER_LIST (advertise known peers)
  - REQUEST_PEERS (ask for more peers)
- Synchronization messages:
  - GET_HEADERS (request block headers by height range)
  - HEADERS (respond with headers)
  - GET_BLOCK (request full block by hash)
  - BLOCK (respond with full block)
  - PING/PONG (keepalive)
- Consensus messages:
  - NEW_CHALLENGE (broadcast new epoch challenge)
  - PROOF_SUBMISSION (miner submits winning proof)
  - BLOCK_PROPOSAL (broadcast new block)
  - BLOCK_ACCEPTED (ack block validation)
- Transaction messages:
  - TX (broadcast transaction)
  - TX_POOL_REQUEST (request mempool transactions)
- Add message validation (size limits, format checks)
- Implement serialization/deserialization with error handling

**Acceptance Criteria:**
- All message types defined and documented
- Serialization/deserialization working (roundtrip tests)
- Message validation rejects malformed messages
- Unit tests for each message type
- Integration tests with message exchange between nodes

**Stop Point:** Validate message exchange between 2+ nodes before proceeding to sync

---

#### Issue #16: Implement Block Synchronization (4-5 weeks)

**Prerequisites:**
- #18 (P2P Foundation) complete
- #17 (Message Types) complete
- #14 (Storage) ✅, #11 (Validation) ✅, #12 (Reorg) ✅

**Implementation Notes:**
- Implement Initial Blockchain Download (IBD) mode detection
- Header-first synchronization:
  1. Request headers from peers (batch of 2000)
  2. Verify header chain (difficulty, timestamps)
  3. Download full blocks for headers
  4. Validate and apply blocks
- Parallel block downloads (request from multiple peers)
- Resume capability (track last synced height, resume on restart)
- Progress tracking and reporting (% synced, ETA)
- Bandwidth throttling (configurable download rate)
- Handle malicious peers:
  - Detect invalid blocks/headers
  - Ban peers serving bad data
  - Request from alternative peers
- Integration with #12 (reorg handling during sync)

**Acceptance Criteria:**
- Node successfully syncs from genesis to current tip
- Fast synchronization with parallel downloads
- Handles network interruptions gracefully (resume)
- Validates all blocks during sync
- Progress reporting working (logs/metrics)
- Malicious peer detection and banning
- Unit tests for sync logic
- Integration tests: sync 1000+ block chain
- Performance benchmark: target 100+ blocks/sec sync

**Stop Point:** Validate full sync from genesis on test network

---

#### Issue #15: Implement Message Relay and Broadcasting (3-4 weeks)

**Prerequisites:**
- #18 (P2P Foundation) complete
- #17 (Message Types) complete

**Implementation Notes:**
- Implement efficient broadcast algorithm:
  - Gossip protocol (send to subset of peers)
  - Avoid duplicate sends (track which peers have seen message)
- Message deduplication (track recent message hashes, LRU cache)
- Flood control and rate limiting:
  - Per-peer rate limits (messages/second)
  - Per-message-type limits
  - Global bandwidth limits
- Priority queuing:
  - High priority: NEW_CHALLENGE, BLOCK
  - Medium priority: PROOF_SUBMISSION
  - Low priority: TX
- Relay validation (don't relay invalid data):
  - Quick validation before relay (signatures, basic format)
  - Drop and ban on invalid relay attempts
- Bandwidth management (throttle if exceeding limits)

**Acceptance Criteria:**
- Messages propagate quickly across network (target: 95% nodes in 5 seconds)
- No duplicate message sends
- Bandwidth usage optimized (minimal redundancy)
- Invalid messages not relayed
- Rate limiting prevents flooding
- Unit tests for relay logic
- Network simulation tests (10+ nodes, measure propagation time)
- Performance benchmarks (blocks/sec, txs/sec)

**Stop Point:** Validate message propagation in 10-node simulated network

---

### Phase 7 — Emission & Economic Model (6-8 weeks, parallel with Phase 3)

**Execute in order, parallel with Phase 3**

#### Issue #67: Implement Emission Curve and Block Reward Schedule (2-3 weeks)

**Prerequisites:**
- None - can start immediately

**Implementation Notes:**
- Define EmissionConfig structure:
  - initial_reward (in smallest units)
  - schedule_type (enum: Halving, ExponentialDecay, LinearDecay, Fixed, CustomTable)
  - schedule_parameters (type-specific config)
  - emission_cap (optional max supply)
  - reward_floor (minimum non-zero reward)
- Implement CalculateBlockReward(height) function:
  - Use **integer-only arithmetic** (no floating point)
  - Define explicit rounding rules (truncate/floor)
  - Support all schedule types
  - Performance: O(1) or O(log n)
- Schedule implementations:
  - **Halving**: reward = initial_reward / (2 ^ (height / halving_interval))
  - **Exponential decay**: reward = initial_reward * exp(-decay_rate * height)
  - **Linear decay**: reward = initial_reward - (height * decay_per_block)
  - **Fixed**: reward = fixed_reward
  - **Custom table**: lookup by height range
- Supply tracking integration with #13 (chain state)
- Versioning support for future changes

**Acceptance Criteria:**
- All schedule types implemented and tested
- Integer-only arithmetic throughout
- Rounding rules documented and consistent
- CalculateBlockReward(height) deterministic (same input → same output)
- Supply tracking working
- Emission cap enforcement (if configured)
- Unit tests for all schedule types
- Boundary tests (halving triggers, emission cap)
- Property tests: cumulative emission ≤ cap
- Performance benchmark: target 1M+ reward calculations/sec
- Documentation of emission formulas with examples

**Stop Point:** Validate emission schedules match expected supply curves

---

#### Issue #68: Implement Coinbase Transaction Structure (1-2 weeks)

**Prerequisites:**
- #67 (Emission Curve) complete
- #9 (Transaction Structure) ✅

**Implementation Notes:**
- Define coinbase transaction type (distinct from regular transactions)
- Fields:
  - tx_type = coinbase (or implicit from no inputs)
  - block_height (explicit, prevents ambiguity)
  - outputs[] (address + amount for each recipient)
  - miner_id or miner_pubkey (block producer identity)
  - extra_nonce / coinbase_data (arbitrary data, size-limited to 100 bytes)
  - signature (miner signature if not in block header)
- Coinbase must be first transaction in block
- Support split rewards (multiple outputs: miner, pool, dev fund, etc.)
- Total outputs must equal block_reward + collected_fees
- Canonical serialization (deterministic, documented)
- Link to miner identity from block header

**Acceptance Criteria:**
- Coinbase transaction structure defined
- All required fields implemented
- Serialization/deserialization working (roundtrip tests)
- Canonical encoding rules documented
- Support for multiple outputs (split rewards)
- Miner identity validation
- Size limits enforced on coinbase_data
- Unit tests for serialization
- Unit tests for field validation
- Integration with transaction system
- Documentation of transaction format

**Stop Point:** Validate coinbase transaction serialization and multi-output support

---

#### Issue #69: Implement Coinbase Maturity Rules (1-2 weeks)

**Prerequisites:**
- #68 (Coinbase Transaction) complete
- #13 (Chain State) ✅, #12 (Reorg Logic) ✅

**Implementation Notes:**
- Define coinbase_maturity configuration parameter (default: 100 blocks)
- Store in genesis block or chain config
- Track creation height for each coinbase UTXO
- Validation rule: reject transactions spending immature outputs
  - Age = current_height - coinbase_height
  - Mature if: age ≥ coinbase_maturity
- Handle maturity during chain reorgs:
  - Immature outputs become invalid if block orphaned
  - Re-check maturity after reorg
- API: GetMatureBalance(address) vs GetTotalBalance(address)
- Wallet integration: display mature vs immature balances

**Acceptance Criteria:**
- Maturity configuration parameter defined
- Coinbase UTXO tracking includes creation height
- Validation rejects spending of immature outputs
- Maturity calculation correct across reorgs
- API for checking output maturity status
- Wallet shows mature/immature separately
- Unit tests for maturity enforcement
- Reorg scenario tests (orphan block with coinbase)
- Edge case: spending at exact maturity height
- Documentation of maturity rules

**Stop Point:** Validate maturity enforcement across reorg scenarios

---

#### Issue #70: Implement Coinbase Validation in Block Validation (2-3 weeks)

**Prerequisites:**
- #67 (Emission Curve) complete
- #68 (Coinbase Transaction) complete
- #69 (Coinbase Maturity) complete
- #11 (Block Validation) ✅

**Implementation Notes:**
- Integrate into block validation pipeline (extend #11):
  1. Identify coinbase transaction (must be first tx in block)
  2. Compute expected_reward = CalculateBlockReward(block.height)
  3. Calculate collected_fees = Σ(tx_inputs - tx_outputs) for non-coinbase txs
  4. Sum coinbase_total = Σ(coinbase.outputs.amounts)
  5. Enforce: coinbase_total ≤ expected_reward + collected_fees
  6. Validate coinbase output addresses/formats
  7. Validate miner_id matches block signature
  8. Validate coinbase maturity for any referenced UTXOs
  9. Update state: add coinbase outputs to UTXO set
- Consensus enforcement:
  - Reject: coinbase exceeds allowed reward + fees
  - Reject: multiple coinbase transactions
  - Reject: coinbase not in first position
  - Reject: invalid coinbase structure
  - Reject: exceeds emission cap (if configured)
- Detailed error messages for each validation failure

**Acceptance Criteria:**
- Coinbase validation integrated into block validation
- All validation rules enforced
- Blocks with invalid coinbase rejected
- Correct calculation of expected reward and fees
- Emission cap enforcement (if configured)
- State correctly updated with coinbase outputs
- Detailed validation error messages
- Unit tests for each validation rule
- Integration tests with valid blocks
- Integration tests with invalid coinbase (various violations)
- Edge case tests (zero fees, max reward, emission cap hit)

**Stop Point:** Validate coinbase enforcement on test chain with various scenarios

---

#### Issues #71, #72, #73 (Supply Tracking, Genesis Config, Testing)

Execute after #67-#70 complete. See roadmap details file for implementation notes.

---

### Phase 4 — Miner Implementation (parallel, start after Phase 3 begins)

#### Issue #20: Implement Plot Scanning Strategies (3-4 weeks)

**Prerequisites:**
- Plot system complete (#2, #3, #4, #5 ✅)

**Implementation Notes:**
- Implement multiple scanning strategies:
  - **Full scan**: iterate all leaves (for small plots)
  - **Sampling**: random sample of leaves (for large plots)
  - **Adaptive**: switch strategy based on plot size
- Parallel scanning across multiple plots (thread pool)
- Configurable scan parameters:
  - sample_size (for sampling strategy)
  - max_parallel_plots (thread limit)
  - scan_timeout (early termination if no winning proof)
- Early termination if winning proof found (score < target)
- Cache-friendly access patterns (sequential reads where possible)
- Performance profiling (measure scan time per strategy)

**Acceptance Criteria:**
- Multiple scanning strategies implemented
- Parallel scanning works correctly (no data races)
- Performance benchmarks for each strategy
- Configurable via config file
- Early termination working
- Unit tests for each strategy
- Integration tests with real plots
- Performance comparison (full vs sampling)
- Documentation of tradeoffs

**Stop Point:** Benchmark scanning performance, validate parallel scanning

---

#### Issue #21: Implement Miner Event Loop (3-4 weeks)

**Prerequisites:**
- #20 (Plot Scanning) complete
- #10 (Block Builder) ✅
- #18 (P2P Network) complete (for proof submission)

**Implementation Notes:**
- Boot sequence:
  1. Load configuration
  2. Load all plots via PlotManager
  3. Connect to full node or validator
  4. Subscribe to NEW_CHALLENGE messages
- Epoch loop (runs concurrently):
  1. Listen for NEW_CHALLENGE message
  2. Trigger proof search across all plots (#20)
  3. Track best proof found
  4. If winning proof (score < target):
     - Submit proof to network via PROOF_SUBMISSION
     - Build block via #10 (Block Builder)
     - Broadcast block via BLOCK_PROPOSAL
  5. Handle concurrent epochs (overlap if block time < scan time)
  6. Performance monitoring (scan time, proof quality)
  7. Error recovery (network disconnects, plot errors)

**Acceptance Criteria:**
- Miner boots successfully and connects to node
- Responds to challenges correctly
- Generates valid proofs
- Submits proofs to network
- Builds blocks when winning
- Handles concurrent epochs
- Performance monitoring (logs/metrics)
- Unit tests for event loop logic
- Integration tests with full node
- End-to-end test: miner wins epoch and produces block

**Stop Point:** Validate end-to-end mining on local test network

---

### Phase 6 — State Root & Light Client Support (parallel, 3-4 weeks)

#### Issue #64: Implement State Root and Light Client Support

**Prerequisites:**
- #13 (Chain State) ✅
- #11 (Block Validation) ✅

**Implementation Notes:**
- Choose authenticated data structure:
  - **Option A**: Merkle Patricia Trie (Ethereum-style)
  - **Option B**: Sparse Merkle Tree
  - **Option C**: Jellyfish Merkle Tree (Aptos-style)
  - **Recommendation**: Start with Sparse Merkle Tree (simpler, efficient)
- Add state_root field to block header (update #8 Block Structure)
- Compute state root after applying all block transactions
- Incremental updates (don't rebuild entire tree per block)
- State proof generation:
  - Generate Merkle proof for account existence/balance
  - Generate proof of non-existence
  - Efficient proof serialization (target: < 1KB per proof)
- State proof verification (light client):
  - Verify proof against state root from block header
  - Return verified value from proof
- Integration with #13 (chain state management):
  - Update state root on each block
  - Rollback state root on reorg

**Acceptance Criteria:**
- Authenticated data structure chosen and implemented
- state_root field added to block header
- State root computed correctly after each block
- State proof generation implemented
- State proof verification implemented
- Proof size benchmarked (should be O(log n) in state size)
- Integration with block validation
- Light client verification example/test
- Unit tests for proof generation/verification
- Performance tests for large state (1M+ accounts)
- Documentation for light client developers

**Stop Point:** Validate light client can verify account state using proofs

---

## Testing & Quality Assurance (ongoing)

### Issue #28: Implement Integration Test Suite (start immediately, expand continuously)

**Prerequisites:**
- Start with completed Phase 1-2 components
- Expand as Phase 3-7 complete

**Implementation Notes:**
- Set up test harness for multi-node network simulation
- End-to-end test scenarios:
  - Genesis to multi-block chain (10+ blocks)
  - Plot creation → proof generation → block production
  - Full node and miner interaction
  - Block propagation across 10-node network
  - Chain reorganization (2-block reorg)
  - Transaction lifecycle (create → broadcast → include → confirm)
  - Multi-node consensus (3+ nodes producing blocks)
- Test network configurations:
  - Local network (localhost, multiple processes)
  - Simulated latency/packet loss
  - Attack scenarios (malicious peers, invalid blocks)
- CI/CD integration (run on every PR)
- Performance benchmarks (baseline for regressions)

**Acceptance Criteria:**
- Integration test framework set up
- Core scenarios covered (at least 10 end-to-end tests)
- Tests pass consistently (< 1% flake rate)
- CI/CD integration (tests run automatically)
- Test duration reasonable (< 10 minutes total)
- Documentation of test scenarios
- Test failure debugging guide

---

## Security Measures (ongoing, highest priority)

### Issue #32: Implement Security Measures (start immediately, ongoing)

**Critical Items (address immediately):**

1. **Input Validation (Week 1-2)**
   - Validate all block fields (size limits, range checks)
   - Validate all transaction fields
   - Validate all network message fields
   - Sanitize user inputs (CLI, RPC)

2. **Cryptographic Safety (Week 1-2)**
   - Secure key generation (use proper RNG)
   - Verify all signatures before trusting data
   - Constant-time comparison for sensitive data
   - Audit all crypto library usage

3. **DoS Protection (Week 2-3)**
   - Rate limiting (per-peer message limits)
   - Message size limits (prevent memory exhaustion)
   - Connection limits (max peers)
   - Peer banning for misbehavior
   - Resource limits (CPU, memory, disk)

4. **Consensus Security (Week 3-4)**
   - Anti-replay protections (nonces, timestamps)
   - Challenge uniqueness enforcement
   - Difficulty manipulation prevention
   - Long-range attack prevention

**Ongoing Items:**
- Security code reviews (every PR)
- Dependency audits (monthly)
- Penetration testing (Q1 2026)
- Security audit (Q1 2026)

**Acceptance Criteria:**
- All critical items complete
- Security checklist documented
- Code review checklist includes security
- Automated security tests (fuzzing, property tests)
- Security documentation
- Incident response plan

---

## Success Criteria

**Phase 3 Complete:**
- 2+ nodes successfully sync from genesis
- Blocks propagate across network (< 5 seconds to 95% nodes)
- Network stable under load (10+ nodes, 100+ blocks/hour)
- No critical security vulnerabilities

**Phase 7 Complete:**
- Emission schedule working correctly
- Coinbase validation enforced
- Supply tracking accurate
- Property tests pass (emission ≤ cap, deterministic)

**Phase 4 Complete:**
- Miner successfully mines blocks on test network
- Proof generation working (valid proofs accepted)
- Block building correct (all fields populated)

**Integration Complete:**
- End-to-end test: 3+ nodes + 2+ miners running for 24+ hours
- 1000+ blocks produced
- No crashes or data corruption
- Network performance acceptable (target: 10 second block time)

## Review & Stop Points

- **After Phase 3 Issue #18**: Review peer connectivity and stability (1-2 day pause)
- **After Phase 3 Issue #17**: Review message protocol design (1 day pause)
- **After Phase 3 Complete**: Comprehensive network testing and security review (1 week)
- **After Phase 7 Complete**: Economic model validation and supply audit (3 days)
- **After Phase 4 Complete**: End-to-end mining test on testnet (3 days)
- **After Integration**: Full system test and security audit (2 weeks)

## Changes Tracking

Record all significant decisions and deviations in:
`.copilot-tracking/changes/20251212-spacetime-roadmap-implementation.md`

Include:
- Design decisions made during implementation
- Deviations from original plan (with rationale)
- Performance bottlenecks discovered and addressed
- Security issues found and fixed
- Testing results and benchmarks

## Next Steps

1. **Immediate (Today)**: Start #18 (P2P Network Foundation) and #32 (Security critical items)
2. **Week 1**: Start #67 (Emission Curve), #64 (State Root), #20 (Plot Scanning)
3. **Week 2**: Start #28 (Integration Tests) alongside Phase 3 development
4. **Week 4-6**: Complete #18, start #17 (Network Message Types)
5. **Week 8-12**: Complete Phase 3, continue Phase 7
6. **Week 12-16**: Start Phase 5 (Full Node), complete Phase 4 (Miner)
