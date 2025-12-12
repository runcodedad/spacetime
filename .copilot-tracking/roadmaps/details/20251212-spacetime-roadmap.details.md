<!-- markdownlint-disable-file -->

# Roadmap Details: Spacetime (Updated 2025-12-12)

## Data Source

- Repository: runcodedad/spacetime
- Issues fetched: 73 total (29 open, 44 closed including PRs)
- Open issues: 29 issues
- Closed issues included: Last 10 days (Dec 2-12, 2025)
- Query: `GET /repos/runcodedad/spacetime/issues?state=all&per_page=100`
- Snapshot date: 2025-12-12T11:24:44Z
- Previous roadmap: 2025-12-08

## Configuration

- recently_closed_days: 10 (default)
- weight_impact: 30
- weight_urgency: 20
- weight_effort: 10
- weight_dependency: 15
- weight_value: 20
- weight_risk: 5

## Progress Since Last Roadmap (Dec 8, 2025)

### Completed Issues (Dec 8-12)

1. **#22** - Transaction Serialization ✅ (Dec 8) - Marked as duplicate of #9
2. **#23** - Transaction Pool (Mempool) ✅ (Dec 12)
3. **#24** - Transaction Validation ✅ (Dec 9)

### New Issues Added (Dec 9)

**Phase 7: Emission & Economic Model** (7 new issues)

1. **#67** - Implement Emission Curve and Block Reward Schedule
2. **#68** - Implement Coinbase Transaction Structure
3. **#69** - Implement Coinbase Maturity Rules
4. **#70** - Implement Coinbase Validation
5. **#71** - Implement Supply Tracking and Audit Tools
6. **#72** - Add Emission Configuration to Genesis Block
7. **#73** - Implement Emission Schedule Testing

### Phase Status

- **Phase 1 (Core Consensus)**: 9/9 complete (100%) ✅
- **Phase 2 (Transactions)**: 5/5 complete (100%) ✅
- **Phase 3 (Networking)**: 0/4 complete (0%)
- **Phase 4 (Miner)**: 0/4 complete (0%)
- **Phase 5 (Full Node)**: 0/2 complete (0%)
- **Phase 6 (State Root)**: 0/1 complete (0%)
- **Phase 7 (Emission)**: 0/7 complete (0%) - NEW
- **Phase 8 (Testing)**: 1/3 complete (33%) - #50 ✅
- **Phase 9 (Hardening)**: 0/3 complete (0%)
- **Phase 10 (DevEx)**: 1/4 complete (25%) - #40 ✅

## Per-issue Evidence

### NEW ISSUES: Phase 7 - Emission & Economic Model

### Issue: #67 — [Implement Emission Curve and Block Reward Schedule](https://github.com/runcodedad/spacetime/issues/67)

- **Priority Score: 92** (Impact: 28, Urgency: 19, Effort: 5, Dependency: 14, Value: 22, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Extends #33 (Genesis Block) ✅ COMPLETE
  - Blocks: #68, #69, #70, #71, #72, #73 (all emission issues)
- **Suggested ETA/Size**: Large (2-3 weeks) — source: Multiple schedule types, integer arithmetic, supply tracking
- **Confidence**: High — Reason: Clear requirements, foundational for economic model, well-defined formulas
- **Why Priority 92**: Critical for economic model, blocks all other emission work, high stakeholder value
- **Issue Body Summary**: Configurable emission schedules (halving, exponential, linear, fixed, custom), deterministic reward calculation, supply accounting, integer-only arithmetic

### Issue: #68 — [Implement Coinbase Transaction Structure](https://github.com/runcodedad/spacetime/issues/68)

- **Priority Score: 90** (Impact: 27, Urgency: 18, Effort: 6, Dependency: 13, Value: 22, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #67 (emission curve for reward amounts)
  - Hard dependency: #9 (transaction structure) ✅ COMPLETE
  - Blocks: #69, #70, #71
- **Suggested ETA/Size**: Medium (1-2 weeks) — source: Transaction type definition, serialization, multi-output support
- **Confidence**: High — Reason: Transaction foundation complete, clear requirements
- **Why Priority 90**: Second in emission phase, required for block rewards
- **Issue Body Summary**: Coinbase transaction type, block height field, multiple outputs, canonical serialization, miner identity validation

### Issue: #69 — [Implement Coinbase Maturity Rules](https://github.com/runcodedad/spacetime/issues/69)

- **Priority Score: 88** (Impact: 27, Urgency: 18, Effort: 6, Dependency: 12, Value: 21, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #68 (coinbase transaction structure)
  - Hard dependency: #13 (chain state for UTXO tracking) ✅ COMPLETE
  - Hard dependency: #12 (reorg logic) ✅ COMPLETE
- **Suggested ETA/Size**: Medium (1-2 weeks) — source: Maturity configuration, UTXO tracking, reorg handling
- **Confidence**: Medium — Reason: Reorg interaction may be complex
- **Why Priority 88**: Protects against reorg exploits, important security feature
- **Issue Body Summary**: Coinbase maturity parameter (e.g., 100 blocks), prevent spending immature outputs, handle reorgs

### Issue: #70 — [Implement Coinbase Validation in Block Validation](https://github.com/runcodedad/spacetime/issues/70)

- **Priority Score: 94** (Impact: 28, Urgency: 19, Effort: 5, Dependency: 14, Value: 24, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #67 (reward calculation)
  - Hard dependency: #68 (coinbase structure)
  - Hard dependency: #69 (maturity rules)
  - Integrates with: #11 (block validation) ✅ COMPLETE
- **Suggested ETA/Size**: Large (2-3 weeks) — source: Complex validation rules, fee calculation, consensus enforcement
- **Confidence**: High — Reason: Block validation foundation complete, clear requirements
- **Why Priority 94**: **HIGHEST IN EMISSION PHASE** - Critical consensus rule, ensures economic security
- **Issue Body Summary**: Validate coinbase amount ≤ reward + fees, enforce emission cap, integrate into block validation pipeline

### Issue: #71 — [Implement Supply Tracking and Audit Tools](https://github.com/runcodedad/spacetime/issues/71)

- **Priority Score: 86** (Impact: 26, Urgency: 17, Effort: 6, Dependency: 11, Value: 22, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #67 (emission curve for expected supply)
  - Hard dependency: #70 (coinbase validation for actual supply)
  - Hard dependency: #13 (chain state for supply tracking) ✅ COMPLETE
- **Suggested ETA/Size**: Medium (1-2 weeks) — source: Supply tracking, audit functions, analytics APIs, CLI tools
- **Confidence**: High — Reason: Clear requirements, foundation in place
- **Why Priority 86**: Important for economic transparency, block explorer integration
- **Issue Body Summary**: Track total supply, audit supply vs emission schedule, analytics APIs, CLI audit tools

### Issue: #72 — [Add Emission Configuration to Genesis Block](https://github.com/runcodedad/spacetime/issues/72)

- **Priority Score: 85** (Impact: 26, Urgency: 17, Effort: 7, Dependency: 11, Value: 20, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Extends: #35 (Genesis Block Configuration) ✅ COMPLETE
  - Hard dependency: #67 (emission parameters to configure)
  - Hard dependency: #68 (coinbase in genesis)
- **Suggested ETA/Size**: Medium (1-2 weeks) — source: Genesis config extension, validation, example configs
- **Confidence**: High — Reason: Genesis system complete, clear extension requirements
- **Why Priority 85**: Required for network consensus on emission rules
- **Issue Body Summary**: Add emission parameters to genesis config, support multiple network configs (mainnet/testnet), genesis coinbase

### Issue: #73 — [Implement Emission Schedule Testing and Property Tests](https://github.com/runcodedad/spacetime/issues/73)

- **Priority Score: 83** (Impact: 25, Urgency: 17, Effort: 7, Dependency: 11, Value: 19, Risk: 4)
- **Labels**: testing
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-09
- **Status**: **OPEN** ⭐ NEW
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #67, #68, #69, #70, #71 (all emission implementation)
  - Related: #28 (integration test suite)
- **Suggested ETA/Size**: Large (2-3 weeks) — source: Comprehensive test suite, property-based tests, security tests, performance tests
- **Confidence**: High — Reason: Clear test requirements, critical for economic security
- **Why Priority 83**: Critical for economic security, ensures emission correctness
- **Issue Body Summary**: Unit tests, integration tests, property-based tests (emission cap, monotonicity), security tests (overflow, rounding), performance tests

### Phase 3 Issues (P2P Networking) — ALL OPEN

### Issue: #18 — [Implement P2P Network Foundation](https://github.com/runcodedad/spacetime/issues/18)

- **Priority Score: 93** (Impact: 28, Urgency: 19, Effort: 6, Dependency: 14, Value: 22, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN** ⭐ CRITICAL PATH
- **Linked PRs**: None
- **Dependencies**: 
  - Blocks: #17, #15, #16 (all networking), #21 (miner event loop)
  - Foundation for: Phase 5 (Full Node)
- **Suggested ETA/Size**: Large (4-6 weeks) — source: TCP/IP layer, peer discovery, connection management, encryption
- **Confidence**: High — Reason: Critical path, well-defined requirements, foundational dependencies complete
- **Why Priority 93**: **HIGHEST IN NETWORKING**, blocks all other networking features
- **Evidence**: Issue body lists TCP connection management, peer discovery, handshake protocol, connection encryption (TLS/Noise)

### Issue: #17 — [Implement Network Message Types](https://github.com/runcodedad/spacetime/issues/17)

- **Priority Score: 91** (Impact: 27, Urgency: 18, Effort: 7, Dependency: 13, Value: 22, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #18 (network foundation)
  - Uses: #9 (transaction structure) ✅, #8 (block structure) ✅
  - Blocks: #15, #16
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Protocol design, serialization, validation for all message types
- **Confidence**: High — Reason: Clear requirements, foundation complete
- **Why Priority 91**: Second highest in networking, required for meaningful communication
- **Evidence**: Issue body defines discovery messages (HELLO, PEER_LIST), sync messages (GET_HEADERS, GET_BLOCK), consensus messages (NEW_CHALLENGE, PROOF_SUBMISSION), transaction messages (TX, TX_POOL_REQUEST)

### Issue: #16 — [Implement Block Synchronization](https://github.com/runcodedad/spacetime/issues/16)

- **Priority Score: 89** (Impact: 27, Urgency: 18, Effort: 7, Dependency: 12, Value: 21, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #18, #17 (networking), #14 (storage) ✅, #11 (validation) ✅, #12 (reorg) ✅
- **Suggested ETA/Size**: Large (4-5 weeks) — source: Initial blockchain download, header-first sync, parallel downloads, malicious peer handling
- **Confidence**: Medium — Reason: Complex logic, multiple failure modes, performance critical
- **Why Priority 89**: Critical for node operation, but requires #18 and #17 first
- **Evidence**: Issue body covers IBD mode, header-first sync, parallel downloads, resume capability, progress tracking

### Issue: #15 — [Implement Message Relay and Broadcasting](https://github.com/runcodedad/spacetime/issues/15)

- **Priority Score: 85** (Impact: 26, Urgency: 17, Effort: 7, Dependency: 11, Value: 20, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #18, #17 (networking)
  - Requires: Peer management from #18
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Gossip protocol, flood control, message deduplication, priority queuing
- **Confidence**: High — Reason: Standard gossip implementation with known patterns
- **Why Priority 85**: Lower than sync but critical for network health
- **Evidence**: Issue body covers broadcast algorithm, deduplication, flood control, priority queuing (blocks > proofs > txs)

### Phase 4 Issues (Miner) — ALL OPEN

### Issue: #20 — [Implement Plot Scanning Strategies](https://github.com/runcodedad/spacetime/issues/20)

- **Priority Score: 84** (Impact: 26, Urgency: 17, Effort: 7, Dependency: 12, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Builds on: #2 ✅, #3 ✅, #4 ✅, #5 ✅ (plot system complete)
  - Blocks: #21 (miner event loop)
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Multiple strategies (full scan, sampling), parallel scanning, performance profiling
- **Confidence**: High — Reason: Plot system foundation complete, clear strategy requirements
- **Why Priority 84**: Foundation for mining, highest priority in Phase 4
- **Evidence**: Issue body lists full scan strategy, sampling strategy, parallel scanning, configurable parameters, cache-friendly access

### Issue: #21 — [Implement Miner Event Loop](https://github.com/runcodedad/spacetime/issues/21)

- **Priority Score: 83** (Impact: 25, Urgency: 17, Effort: 7, Dependency: 11, Value: 19, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #20 (plot scanning), #10 (block builder) ✅
  - Soft dependency: #18 (network) for proof submission
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Event loop, challenge listening, proof search, block building, concurrent epochs
- **Confidence**: Medium — Reason: Depends on networking completion for full functionality
- **Why Priority 83**: Core miner logic, second in Phase 4
- **Evidence**: Issue body covers boot sequence, epoch loop (listen for challenges, trigger proof search, submit proof, build block)

### Issue: #19 — [Implement Miner Configuration and CLI](https://github.com/runcodedad/spacetime/issues/19)

- **Priority Score: 78** (Impact: 23, Urgency: 16, Effort: 8, Dependency: 9, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Soft dependency: #20, #21 for complete configuration
- **Suggested ETA/Size**: Small (1-2 weeks) — source: CLI parsing, config file format, validation
- **Confidence**: High — Reason: Standard CLI/config work, clear requirements
- **Why Priority 78**: Lower priority than core miner logic, but needed for usability
- **Evidence**: Issue body defines CLI commands (create-plot, list-plots, start, stop, status) and config options (plot directory, node connection, keys)

### Issue: #49 — [Support directory scanning for plot discovery](https://github.com/runcodedad/spacetime/issues/49)

- **Priority Score: 72** (Impact: 22, Urgency: 14, Effort: 9, Dependency: 8, Value: 16, Risk: 3)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-02
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Soft dependency: #20 (plot scanning strategies)
  - Enhances: #5 (plot management) ✅ COMPLETE
- **Suggested ETA/Size**: Small (1-2 weeks) — source: Directory traversal, plot file validation, ScanDirectoryAsync method
- **Confidence**: High — Reason: Well-scoped feature, clear requirements in issue body
- **Why Priority 72**: Nice-to-have for UX, but not blocking core functionality
- **Evidence**: Issue body specifies acceptance criteria (ScanDirectoryAsync method, recursive scanning, cache file handling)

### Phase 5 Issues (Full Node) — ALL OPEN

### Issue: #26 — [Implement Full Node Lifecycle](https://github.com/runcodedad/spacetime/issues/26)

- **Priority Score: 82** (Impact: 26, Urgency: 16, Effort: 7, Dependency: 11, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #14 (storage) ✅, #18 (network), #16 (sync), #11 (validation) ✅
  - Integrates: All subsystems
- **Suggested ETA/Size**: Large (4-5 weeks) — source: Complex lifecycle management, boot sequence, sync phase, consensus loop, shutdown
- **Confidence**: Medium — Reason: Depends on Phase 3 completion, complex integration work
- **Why Priority 82**: Critical for production node, but requires Phase 3 first
- **Evidence**: Issue body covers boot sequence (load config, load chain, start network), sync phase, steady-state consensus loop

### Issue: #27 — [Implement Full Node Configuration and CLI](https://github.com/runcodedad/spacetime/issues/27)

- **Priority Score: 80** (Impact: 24, Urgency: 16, Effort: 8, Dependency: 10, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Soft dependency: #26 (lifecycle) for complete configuration
- **Suggested ETA/Size**: Medium (2-3 weeks) — source: CLI parsing, config management, runtime settings
- **Confidence**: High — Reason: Standard CLI work, clear requirements
- **Why Priority 80**: Needed for node usability, but after lifecycle
- **Evidence**: Issue body defines CLI commands (init, start, stop, status, peers, chain-info) and config options (data directory, network settings, consensus parameters)

### Phase 6 Issues (State Root) — OPEN

### Issue: #64 — [Implement State Root and Light Client Support](https://github.com/runcodedad/spacetime/issues/64)

- **Priority Score: 88** (Impact: 27, Urgency: 18, Effort: 6, Dependency: 12, Value: 21, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-07
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #13 (chain state) ✅ COMPLETE
  - Hard dependency: #11 (block validation) ✅ COMPLETE
  - May require: Block header updates (state_root field)
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Merkle tree integration (MPT/SMT/Jellyfish), state root in headers, light client verification
- **Confidence**: Medium — Reason: Core dependencies complete, but may require protocol changes
- **Why Priority 88**: High impact for enabling light clients and SPV validation, enhances scalability
- **Evidence**: Issue body covers authenticated data structure options (MPT, SMT, Jellyfish), state root in header, proof generation/verification

### Phase 8 Issues (Testing & QA) — PARTIALLY COMPLETE

### Issue: #28 — [Implement Integration Test Suite](https://github.com/runcodedad/spacetime/issues/28)

- **Priority Score: 75** (Impact: 23, Urgency: 15, Effort: 6, Dependency: 10, Value: 17, Risk: 4)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Can start now with completed Phase 1-2 components
  - Will expand as Phase 3-5 complete
  - Foundation: #50 (testing infrastructure) ✅ COMPLETE
- **Suggested ETA/Size**: Large (4-5 weeks, ongoing) — source: Multiple test scenarios, fixture management, CI integration
- **Confidence**: High — Reason: Foundation exists, can add tests incrementally
- **Why Priority 75**: High value for quality, should start soon to catch integration issues early
- **Evidence**: Issue body covers end-to-end scenarios (genesis to multi-block chain, plot creation, node-miner interaction, block propagation, reorgs, multi-node simulations)

### Issue: #29 — [Implement Simulation and Benchmarking Tools](https://github.com/runcodedad/spacetime/issues/29)

- **Priority Score: 74** (Impact: 23, Urgency: 14, Effort: 6, Dependency: 10, Value: 17, Risk: 4)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Soft dependency: Benefits from Phase 3 completion for network simulation
  - Can start with consensus/mining benchmarks now
- **Suggested ETA/Size**: Large (4-5 weeks) — source: Network simulator, performance benchmarks, visualization
- **Confidence**: Medium — Reason: Benchmarks exist in benchmarks/ folder, but network simulation needs Phase 3
- **Why Priority 74**: Important for performance validation, lower urgency than integration tests
- **Evidence**: Issue body covers network simulator (N nodes, latency, packet loss, attack scenarios), performance benchmarks (plot creation, proof generation, validation, throughput)

### Phase 9 Issues (Production Hardening) — ALL OPEN

### Issue: #32 — [Implement Security Measures](https://github.com/runcodedad/spacetime/issues/32)

- **Priority Score: 96** (Impact: 29, Urgency: 19, Effort: 5, Dependency: 15, Value: 24, Risk: 4)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN** 🔴 **HIGHEST OVERALL PRIORITY**
- **Linked PRs**: None
- **Dependencies**: 
  - Should be integrated throughout all phases
  - Particularly critical for #18 (network security)
- **Suggested ETA/Size**: Ongoing — source: Input validation, crypto safety, DoS protection, rate limiting, consensus security
- **Confidence**: Medium — Reason: Highest priority overall, but security is ongoing across all features
- **Why Priority 96**: **HIGHEST OVERALL PRIORITY** - Critical for production, must address incrementally
- **CRITICAL RECOMMENDATION**: Address immediately:
  1. Input validation in consensus (block/transaction validation)
  2. Crypto safety (secure key generation, signature verification)
  3. DoS protection (rate limiting, message validation)
  4. Schedule full security audit for Q1 2026
- **Evidence**: Issue body covers network security (DoS protection, DDoS mitigation, peer banning), cryptographic security (secure key generation, signature verification), consensus security (anti-replay, challenge uniqueness)

### Issue: #31 — [Implement Error Handling and Recovery](https://github.com/runcodedad/spacetime/issues/31)

- **Priority Score: 81** (Impact: 24, Urgency: 16, Effort: 7, Dependency: 11, Value: 19, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Should be integrated throughout all phases
  - Particularly important for #26 (full node lifecycle)
- **Suggested ETA/Size**: Ongoing — source: Graceful degradation, recovery strategies, corruption detection
- **Confidence**: Medium — Reason: Cross-cutting concern, should be addressed incrementally
- **Why Priority 81**: Important for reliability, integrate with new features
- **Evidence**: Issue body covers graceful error handling, automatic recovery from transient failures, corruption detection/recovery (database, plots, state), crash recovery

### Issue: #30 — [Implement Logging and Monitoring](https://github.com/runcodedad/spacetime/issues/30)

- **Priority Score: 79** (Impact: 24, Urgency: 16, Effort: 7, Dependency: 10, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Can start now, expand as features are added
  - Critical for Phase 5 (full node) debugging
- **Suggested ETA/Size**: Medium (2-3 weeks) — source: Structured logging, metrics collection, observability
- **Confidence**: High — Reason: Standard logging infrastructure, clear requirements
- **Why Priority 79**: Important for debugging and operations, should start soon
- **Evidence**: Issue body covers structured logging (JSON format), log levels, performance metrics (validation time, proof generation, network latency), metrics export (Prometheus), alerting

### Phase 10 Issues (Developer Experience) — PARTIALLY COMPLETE

### Issue: #33 — [Create Comprehensive Documentation](https://github.com/runcodedad/spacetime/issues/33)

- **Priority Score: 76** (Impact: 23, Urgency: 15, Effort: 5, Dependency: 10, Value: 18, Risk: 5)
- **Labels**: documentation
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - #40 (Copilot instructions) ✅ COMPLETE
  - Best done incrementally as features complete
- **Suggested ETA/Size**: Large (4-6 weeks, ongoing) — source: Architecture docs, API docs, protocol spec, user guides
- **Confidence**: High — Reason: Clear documentation requirements, can start incrementally
- **Why Priority 76**: Important for adoption, should document as features complete
- **Evidence**: Issue body covers README with quickstart, architecture documentation, API documentation, protocol specification, configuration/deployment/troubleshooting guides

### Issue: #34 — [Create Developer Tools](https://github.com/runcodedad/spacetime/issues/34)

- **Priority Score: 73** (Impact: 22, Urgency: 15, Effort: 6, Dependency: 9, Value: 17, Risk: 4)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Benefits from complete system for useful tools
  - Can start with plot/proof inspection tools
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Plot inspector, chain explorer, debug utilities
- **Confidence**: Medium — Reason: Can build incrementally, prioritize based on pain points
- **Why Priority 73**: Useful for development, lower priority than core features
- **Evidence**: Issue body lists block explorer (web UI), wallet CLI, plot inspector, proof validator, network diagnostic tools, genesis block generator

### Issue: #36 — [Create Deployment and Bootstrap Scripts](https://github.com/runcodedad/spacetime/issues/36)

- **Priority Score: 71** (Impact: 21, Urgency: 14, Effort: 8, Dependency: 9, Value: 16, Risk: 3)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Requires: #26 (full node) ✅, #27 (node config)
  - Should include: #19 (miner config), #49 (plot discovery)
- **Suggested ETA/Size**: Medium (2-3 weeks) — source: Docker configs, systemd units, bootstrap automation
- **Confidence**: Medium — Reason: Requires Phase 4-5 completion for full deployment
- **Why Priority 71**: Important for production deployment, but after core features
- **Evidence**: Issue body covers Docker containers, Docker Compose, Kubernetes manifests, bootstrap scripts (Ubuntu/Debian/CentOS/macOS/Windows), systemd service files

### Phase 11 Issues (Research) — ALL OPEN, LOW PRIORITY

### Issue: #37 — [Research: Plot Compression Techniques](https://github.com/runcodedad/spacetime/issues/37)

- **Priority Score: 50** (Impact: 15, Urgency: 10, Effort: 9, Dependency: 5, Value: 10, Risk: 1)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: None (research item)
- **Suggested ETA/Size**: Research (4-8 weeks) — source: Research and design phase
- **Confidence**: Low — Reason: Research task with uncertain outcome
- **Why Priority 50**: Low priority, future optimization

### Issue: #38 — [Research: Smart Contract System Design](https://github.com/runcodedad/spacetime/issues/38)

- **Priority Score: 45** (Impact: 14, Urgency: 9, Effort: 9, Dependency: 4, Value: 8, Risk: 1)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: None (research item, future capability)
- **Suggested ETA/Size**: Research (8-12 weeks) — source: Major feature research
- **Confidence**: Low — Reason: Large scope research, 2026+ timeline
- **Why Priority 45**: Very low priority, post-1.0 feature

### Issue: #39 — [Research: Zero-Knowledge Proof Integration](https://github.com/runcodedad/spacetime/issues/39)

- **Priority Score: 40** (Impact: 12, Urgency: 8, Effort: 9, Dependency: 4, Value: 6, Risk: 1)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: None (research item, future capability)
- **Suggested ETA/Size**: Research (8-12 weeks) — source: Complex research area
- **Confidence**: Low — Reason: Speculative research
- **Why Priority 40**: Lowest priority, exploratory only

## Dependency Graph (Adjacency List)

### Phase 3 (Networking) Dependencies
- #18 (P2P Foundation) → blocks [#17, #15, #16, #21 (partial)]
- #17 (Message Types) → depends on [#18]
- #16 (Block Sync) → depends on [#18, #17, #14✅, #11✅, #12✅]
- #15 (Message Relay) → depends on [#18, #17]

### Phase 4 (Miner) Dependencies
- #20 (Plot Scanning) → depends on [#2✅, #3✅, #4✅, #5✅]
- #21 (Miner Event Loop) → depends on [#20, #10✅, #18 (partial)]
- #19 (Miner Config) → depends on [#20, #21] (soft)
- #49 (Plot Discovery) → depends on [#20] (soft), enhances [#5✅]

### Phase 5 (Full Node) Dependencies
- #26 (Node Lifecycle) → depends on [#14✅, #18, #16, #11✅]
- #27 (Node Config) → depends on [#26] (soft)

### Phase 6 (State Root) Dependencies
- #64 (State Root) → depends on [#13✅, #11✅], may affect block structure

### Phase 7 (Emission) Dependencies
- #67 (Emission Curve) → extends [#33✅], blocks [#68, #69, #70, #71, #72, #73]
- #68 (Coinbase Transaction) → depends on [#67, #9✅], blocks [#69, #70, #71]
- #69 (Coinbase Maturity) → depends on [#68, #13✅, #12✅]
- #70 (Coinbase Validation) → depends on [#67, #68, #69, #11✅]
- #71 (Supply Tracking) → depends on [#67, #70, #13✅]
- #72 (Genesis Emission Config) → depends on [#67, #68, #35✅]
- #73 (Emission Testing) → depends on [#67, #68, #69, #70, #71]

### Cross-cutting Dependencies
- #32 (Security) → affects all phases, particularly [#18]
- #31 (Error Handling) → affects all phases, particularly [#26]
- #30 (Logging) → affects all phases, needed for debugging
- #28 (Integration Tests) → can start now, expands with new features

### No cycles detected

## Backlog Grouping

### Completed (18 issues since Dec 3) ✅

**Foundational Infrastructure (Phase 1-2):**
- #6, #7, #8, #9, #10, #11, #12, #13, #14 (Core Consensus)
- #22, #23, #24, #25 (Transactions)
- #5 (Plot Management)
- #35 (Genesis Block)

**Supporting Infrastructure:**
- #40 (Copilot Instructions)
- #41 (Project Setup)
- #50 (Testing Infrastructure)
- #58 (Code Tidying)

### Critical Path (Phase 3 → Phase 5)

**Phase 3 (Networking) - CRITICAL PATH:**
- #18 → #17 → #16, #15

**Phase 5 (Full Node):**
- #26 → #27

### High Value & Parallel Work

**Phase 7 (Emission) - NEW, CRITICAL FOR ECONOMICS:**
- #67 → #68 → #69, #70 → #71, #72, #73

**Phase 6 (State Root) - ENABLES LIGHT CLIENTS:**
- #64 (can start immediately)

**Phase 4 (Miner) - ENABLES MINING:**
- #20 → #21 → #19, #49

**Phase 9 (Security) - HIGHEST OVERALL PRIORITY:**
- #32 (ongoing, critical items immediately)

### Quality & Testing

**Phase 8 (Testing & QA):**
- #28 (Integration Tests) - should start soon
- #29 (Benchmarking) - after Phase 3 for network simulation

### Production Readiness

**Phase 9 (Hardening):**
- #32 (Security) - highest priority (96)
- #31 (Error Handling) - ongoing
- #30 (Logging) - should start soon

### Developer Experience

**Phase 10 (DevEx):**
- #33 (Documentation) - ongoing
- #34 (Developer Tools) - incremental
- #36 (Deployment Scripts) - after Phase 5

### Research & Future (Low Priority)

**Phase 11 (Research):**
- #37 (Plot Compression) - priority 50
- #38 (Smart Contracts) - priority 45
- #39 (ZK Proofs) - priority 40

## Metrics & Formulas

### Composite Score Formula

```
priority_score = (impact × 0.30) + (urgency × 0.20) + (effort_inverse × 0.10) + 
                 (dependency × 0.15) + (value × 0.20) + (risk × 0.05)
```

Where:
- `impact` = 0-100 based on labels (bug, security, enhancement), dependent issues, scope
- `urgency` = 0-100 based on milestone due date, labels (urgent, high-priority)
- `effort_inverse` = 100 - (0-100 based on size labels, estimated complexity) — lower effort = higher priority
- `dependency` = 0-100 based on number of issues blocked by this issue
- `value` = 0-100 based on labels (customer-request, product-request), reactions, comments
- `risk` = 0-100 based on labels (security, regression), risk flags

### Normalization Method

- Each component scored 0-100
- Weighted sum produces final score 0-100
- Scores rounded to nearest integer

### Label Interpretation

- **enhancement**: +10 impact (new feature)
- **bug**: +15 impact, +10 urgency (correctness issue)
- **security**: +25 impact, +20 risk (critical for production)
- **documentation**: +5 value (improves adoption)
- **testing**: +10 quality, +5 risk mitigation
- **duplicate**: Flag for review, no score

### Weight Table (from config)

- impact: 30%
- urgency: 20%
- effort: 10% (inverse)
- dependency: 15%
- value: 20%
- risk: 5%

## Summary Statistics

- **Total Issues**: 73 (29 open, 44 closed including PRs)
- **Open Issues**: 29
- **Closed Since Dec 8**: 3 (#22, #23, #24)
- **Closed Since Dec 3**: 18 issues
- **New Issues Since Dec 8**: 7 (#67-#73 - Emission phase)
- **Phases Complete**: 2 of 11 (Phase 1 & 2 - 100% complete)
- **Critical Path Issues Open**: 4 (Phase 3 networking)
- **Velocity**: ~2 issues/day (Dec 3-12)
- **Average Days to Close**: ~2-3 days (based on recent velocity)
- **Next Milestone**: Phase 3 completion (target: Jan 2026, 4 issues, ~8-12 weeks)
- **Highest Priority Open Issue**: #32 (Security Measures) - Priority 96
- **Second Highest**: #70 (Coinbase Validation) - Priority 94
- **Third Highest**: #18 (P2P Network Foundation) - Priority 93
