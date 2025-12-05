---
agent: "agent"
---

<!-- markdownlint-disable-file -->

# Implementation Prompt: Spacetime Roadmap (Generated 2024-12-04)

## Overview

Implement the Spacetime Proof-of-Space-Time blockchain in 9 phases, progressing from core consensus logic through networking, mining, testing, production hardening, and developer tooling. Each phase builds on previous work with clear acceptance criteria and test requirements.

## Phase-by-phase Execution

### Phase 1 — Core Consensus & Blockchain Foundation

**Prerequisites:**
- ✅ Block structure complete (#9, #10, #25 closed)
- ✅ Consensus primitives complete (#7, #8 closed)
- ✅ Plot system complete (#2-#5 closed)
- ✅ Genesis configuration complete (#35 closed)

**Tasks (ordered by dependency):**

1. **#6 — Implement Difficulty Adjustment Algorithm**
   - Implementation notes: Start with difficulty-to-target conversion function. Implement adjustment algorithm with configurable window. Add comprehensive simulation tests with various mining scenarios. Document the mathematical relationship between difficulty and target.
   - Acceptance: Unit tests pass, simulation shows stable block times, edge cases tested (min/max difficulty)

2. **#14 — Implement Chain Storage Layer** (Can parallelize with #6)
   - Implementation notes: Use RocksDB via rocksdb-sharp library (as noted in issue comments). Design schema for headers, bodies, transactions, metadata. Implement efficient lookups by hash and height. Add corruption detection.
   - Acceptance: Storage operations tested, benchmark performance, atomic operations verified

3. **#11 — Implement Block Validation Logic** (Requires #6 complete)
   - Implementation notes: Integrate difficulty validation from #6. Implement header, proof, and transaction validation. Return specific error types for debugging. Optimize for fast validation.
   - Acceptance: All validation rules tested, valid blocks accepted, invalid blocks rejected with clear errors

4. **#13 — Implement Chain State Management**
   - Implementation notes: **DECISION REQUIRED**: Choose UTXO vs Account model before starting. Create ADR (Architecture Decision Record). Implement state storage, transitions, and rollback capability.
   - Acceptance: State transitions tested, rollback works correctly, state lookups efficient

5. **#12 — Implement Chain Reorganization Logic** (Requires #13, #14 complete)
   - Implementation notes: Detect reorg conditions, rollback to fork point, reapply blocks from new chain. Return transactions to mempool. Emit reorg events for monitoring.
   - Acceptance: Reorg scenarios tested (including multiple reorgs), no state corruption, orphaned blocks handled

**Phase 1 Acceptance Criteria:**
- All 5 issues (#6, #11, #13, #12, #14) closed and merged
- Unit test coverage ≥ 90% for new code
- Integration tests demonstrate: genesis → difficulty adjustment → multi-block chain → reorg
- No blocking issues remain for Phase 2

### Phase 2 — Transactions & State Management

**Prerequisites:**
- Phase 1 complete (especially #13 Chain State Management)
- Transaction structure complete (#25 closed)

**Tasks (ordered):**

1. **#24 — Implement Transaction Validation** (Requires #13 complete)
   - Implementation notes: Implement signature, balance, nonce, fee validation. Add double-spend detection. Return specific validation errors. Add comprehensive edge case tests.
   - Acceptance: All validation rules tested, valid transactions accepted, invalid rejected with clear errors, performance benchmarks complete

2. **#23 — Implement Transaction Pool (Mempool)** (Requires #24 complete)
   - Implementation notes: In-memory storage with priority ordering by fee. Implement eviction policies and size limits. Handle transaction conflicts. Integrate with #24 validation.
   - Acceptance: Mempool operations tested, priority ordering verified, size limits enforced, integration tests with block building

**Phase 2 Acceptance Criteria:**
- Both issues (#24, #23) closed and merged
- Transaction lifecycle tested end-to-end: creation → validation → mempool → inclusion in block
- Unit test coverage ≥ 90%
- Performance benchmarks show acceptable throughput

### Phase 3 — P2P Networking Layer

**Prerequisites:**
- Phase 1 complete (need blocks to synchronize)
- Phase 2 complete (need transactions to relay)

**Tasks (ordered by dependency):**

1. **#18 — Implement P2P Network Foundation**
   - Implementation notes: **DECISION REQUIRED**: Choose TLS vs Noise protocol for encryption. Implement TCP connection management, peer discovery, connection pooling, handshake protocol. Add peer reputation/scoring.
   - Acceptance: Peer connections stable, discovery works, encryption enabled, unit and integration tests pass

2. **#17 — Implement Network Message Types** (Can parallelize with #18)
   - Implementation notes: Use Protocol Buffers as recommended. Define all message types (discovery, sync, consensus, transaction). Implement serialization/deserialization and validation.
   - Acceptance: All message types defined, serialization tested, validation working, documentation complete

3. **#15 — Implement Message Relay and Broadcasting** (Requires #18, #17 complete)
   - Implementation notes: Implement efficient gossip protocol. Add message deduplication tracking. Implement flood control and rate limiting. Add priority queuing (blocks > proofs > txs).
   - Acceptance: Messages propagate correctly, no duplicates, bandwidth optimized, network simulation tests pass

4. **#16 — Implement Block Synchronization** (Requires #18, #17, #11, #14 complete)
   - Implementation notes: Implement header-first sync with parallel block downloads. Add resume capability after interruption. Integrate with #11 for validation and #14 for storage. Add progress reporting. Handle malicious peers.
   - Acceptance: Node syncs from genesis successfully, parallel downloads work, interruption recovery tested, performance benchmarks complete

**Phase 3 Acceptance Criteria:**
- All 4 issues (#18, #17, #15, #16) closed and merged
- Multi-node integration tests: 3+ nodes sync and relay blocks/transactions
- Network simulation tests with 10+ nodes
- Unit test coverage ≥ 90%

### Phase 4 — Miner Implementation

**Prerequisites:**
- Phase 1 complete (need consensus system)
- Plot system complete (#2-#5 closed) ✅
- Network connection capability from Phase 3

**Tasks (ordered):**

1. **#20 — Implement Plot Scanning Strategies**
   - Implementation notes: Implement full scan and sampling strategies. Add parallel scanning across multiple plots. Make configurable. Add early termination for winning proofs. Optimize for cache-friendly access.
   - Acceptance: Multiple strategies tested, parallel scanning works, performance benchmarks for each strategy, configuration working

2. **#21 — Implement Miner Event Loop** (Requires #20 complete)
   - Implementation notes: Implement boot sequence (load config, plots, connect to node). Implement epoch loop: listen for challenges, trigger proof search, track best proof, submit proofs, build and broadcast blocks. Add performance monitoring.
   - Acceptance: Miner responds to challenges, generates valid proofs, builds blocks when winning, integration tests with full node pass

3. **#19 — Implement Miner Configuration and CLI** (Can parallelize with #20/#21)
   - Implementation notes: Define config file format (TOML/YAML). Implement CLI commands (create-plot, list-plots, delete-plot, start, stop, status). Add configuration validation and environment variable support.
   - Acceptance: Config parsing works, all CLI commands functional, validation working, documentation complete

4. **#49 — Support directory scanning for plot discovery** (Requires #19 complete)
   - Implementation notes: Add ScanDirectoryAsync method to PlotManager. Use existing AddPlotAsync for discovered plots. Support optional recursive scanning. Handle cache file paths by convention.
   - Acceptance: Directory scanning works, recursive option functional, cache tracking correct, tests pass

**Phase 4 Acceptance Criteria:**
- All 4 issues (#20, #21, #19, #49) closed and merged
- End-to-end miner test: miner connects to node, receives challenge, generates proof, submits winning proof, block accepted by network
- Performance benchmarks for proof generation
- Unit test coverage ≥ 90%

### Phase 5 — Full Node Implementation

**Prerequisites:**
- Phase 1-3 complete (all core systems ready)
- Phase 4 complete (for full node-miner interaction)

**Tasks (ordered):**

1. **#26 — Implement Full Node Lifecycle**
   - Implementation notes: Implement boot sequence (load config, chain, state, start network, connect peers). Implement sync phase (headers, blocks, validation). Implement steady-state consensus loop (challenges, proofs, blocks, state updates). Add graceful shutdown.
   - Acceptance: Node boots successfully, syncs with network, participates in consensus, handles all messages, graceful shutdown works, integration tests pass

2. **#27 — Implement Full Node Configuration and CLI** (Can parallelize with #26)
   - Implementation notes: Define config file format. Implement CLI commands (init, start, stop, status, peers, chain-info, validate-block). Add configuration validation.
   - Acceptance: Config parsing works, all CLI commands functional, validation working, documentation complete

**Phase 5 Acceptance Criteria:**
- Both issues (#26, #27) closed and merged
- Multi-node testnet: 3+ full nodes + 2+ miners running stable network
- Block production and propagation working smoothly
- Unit test coverage ≥ 90%

### Phase 6 — Quality Assurance & Testing

**Prerequisites:**
- Phases 1-5 complete (need full system for comprehensive testing)

**Tasks (ordered):**

1. **#28 — Implement Integration Test Suite**
   - Implementation notes: Create test harness for network simulation. Implement end-to-end scenarios: genesis→multi-block, plot→proof→block, node-miner interaction, block propagation, reorgs, transaction lifecycle. Add multi-node network simulations. Include failure scenario tests.
   - Acceptance: Integration test framework set up, core scenarios covered, tests pass consistently, CI/CD integrated, documentation complete

2. **#29 — Implement Simulation and Benchmarking Tools** (Can parallelize with #28)
   - Implementation notes: Create network simulator (N nodes, configurable latency/loss, attack scenarios). Implement performance benchmarks (plot creation, proof generation, block validation, network throughput, sync performance). Add visualization tools.
   - Acceptance: Network simulator working, benchmark suite implemented, visualization available, CI integration for performance tracking

**Phase 6 Acceptance Criteria:**
- Both issues (#28, #29) closed and merged
- Integration test suite runs in CI/CD
- Performance benchmarks show acceptable metrics
- Simulation tests validate network behavior under stress

### Phase 7 — Production Hardening

**Prerequisites:**
- Phases 1-6 complete (need full system for security and production features)

**Tasks (ordered by priority):**

1. **#32 — Implement Security Measures** ⚠️ **CRITICAL**
   - Implementation notes: **INTEGRATE THROUGHOUT DEVELOPMENT**, not just at end. Implement network security (DoS protection, rate limiting, peer banning, message validation). Ensure cryptographic security (secure key generation, key storage, signature verification). Add consensus security (anti-replay, challenge uniqueness, difficulty manipulation prevention). Add input validation across all interfaces.
   - Acceptance: DoS protections working, cryptographic operations secure, consensus attack vectors mitigated, security audit checklist completed, penetration testing performed, documentation complete

2. **#31 — Implement Error Handling and Recovery** (Can parallelize with #32)
   - Implementation notes: Add graceful error handling throughout codebase. Implement automatic recovery from transient failures. Add corruption detection (database, plot files, state) with recovery procedures. Implement crash recovery (resume from last known good state). Add error reporting and debugging tools.
   - Acceptance: Error handling consistent, automatic recovery working, corruption detection tested, crash recovery verified, unit tests for error scenarios, documentation complete

3. **#30 — Implement Logging and Monitoring** (Can parallelize with #32)
   - Implementation notes: Integrate structured logging framework (JSON format). Add log levels (DEBUG, INFO, WARN, ERROR) with component-specific loggers. Implement metrics collection (block validation time, proof generation, network latency, disk I/O, memory). Export metrics in Prometheus format. Add alerting on critical issues. Implement log rotation.
   - Acceptance: Logging framework integrated, all components log appropriately, metrics collection working, metrics exportable, example Grafana dashboards available, documentation complete

**Phase 7 Acceptance Criteria:**
- All 3 issues (#32, #31, #30) closed and merged
- Security audit performed by external party
- Penetration testing completed with issues resolved
- Error recovery tested under various failure scenarios
- Monitoring dashboards deployed and functional

### Phase 8 — Developer Experience & Tooling

**Prerequisites:**
- Phases 1-7 complete (need stable system to document and deploy)

**Tasks (ordered by priority):**

1. **#33 — Create Comprehensive Documentation**
   - Implementation notes: Create README with quickstart, architecture documentation, API documentation (from code comments), protocol specification, configuration guide, deployment guide, troubleshooting guide, contributing guide, FAQ. Add examples, tutorials, diagrams, visualizations.
   - Acceptance: All documentation written, published (GitHub Pages or similar), reviewed for clarity, examples tested

2. **#34 — Create Developer Tools** (Can parallelize with #33)
   - Implementation notes: Implement block explorer (web UI), wallet CLI for transactions, plot inspector tool, proof validator tool, network diagnostic tools, configuration validator, genesis block generator, testnet bootstrap scripts.
   - Acceptance: Essential tools implemented and documented, tools tested, installation instructions available

3. **#36 — Create Deployment and Bootstrap Scripts** (Can start with #33/#34)
   - Implementation notes: Create Docker containers for node and miner. Create Docker Compose for local testnet. Create Kubernetes manifests (optional). Create bootstrap scripts for Ubuntu/Debian, CentOS/RHEL, macOS, Windows. Create systemd service files. Create update/upgrade scripts. Create monitoring setup scripts.
   - Acceptance: Docker images build successfully, local testnet deployable via Docker Compose, bootstrap scripts work on target platforms, documentation complete, example configs available

**Phase 8 Acceptance Criteria:**
- All 3 issues (#33, #34, #36) closed and merged
- Documentation published and accessible
- Developer tools functional and documented
- Deployment scripts tested on multiple platforms
- Local testnet can be deployed in < 10 minutes with Docker Compose

### Phase 9 — Research & Future Enhancements

**Prerequisites:**
- Phases 1-8 complete (mainnet-ready system)
- Research tasks can start anytime but implementation blocked until post-1.0

**Tasks (no specific order - research items):**

1. **#37 — Research: Plot Compression Techniques**
   - Implementation notes: Research compression techniques that maintain security. Evaluate trade-offs (storage vs computation). Create design document with recommendations.
   - Acceptance: Research document complete, trade-offs analyzed, recommendation made

2. **#38 — Research: Smart Contract System Design**
   - Implementation notes: Research smart contract models. Design VM architecture. Evaluate gas metering strategies. Create comprehensive design document.
   - Acceptance: Design document complete, VM architecture specified, implementation plan outlined

3. **#39 — Research: Zero-Knowledge Proof Integration**
   - Implementation notes: Research ZK-proof systems for blockchain. Evaluate privacy and scalability benefits. Design integration points. Create design document.
   - Acceptance: Research document complete, integration design specified, implementation feasibility assessed

**Phase 9 Acceptance Criteria:**
- Research documents complete and reviewed
- Implementation plans created for approved research items
- Recommendations inform post-1.0 roadmap

## Review & Stop Points

- **After Phase 1**: Technical review of core consensus implementation. Verify difficulty adjustment behaves correctly in simulations.
- **After Phase 2**: Product review of transaction system. Verify transaction validation and mempool work correctly.
- **After Phase 3**: Network testing with 10+ node simulation. Verify sync and relay work reliably.
- **After Phase 5**: Internal testnet launch. Run multi-node testnet for 1-2 weeks to verify stability.
- **After Phase 6**: QA review of test coverage and performance benchmarks.
- **After Phase 7**: Security audit required before public testnet. External penetration testing.
- **After Phase 8**: Public testnet launch. Community testing period before mainnet.

## Success Criteria

- All 29 implementation issues (excluding research) closed and merged
- No unresolved hard blockers remain
- Security audit passed with all critical/high issues resolved
- Public testnet running stably for 4+ weeks
- Documentation complete and published
- Deployment scripts tested on all major platforms
- Performance benchmarks meet targets:
  - Block validation: < 100ms
  - Proof generation: < 30 seconds for 100GB plot
  - Network propagation: < 5 seconds to 90% of nodes
  - Sync speed: > 1000 blocks/minute
- Test coverage ≥ 90% for all core modules
- Zero known critical security vulnerabilities
- Regression rate < 2% (new bugs vs total issues)

## Changes Tracking

Document all significant decisions, architectural changes, and deviations from this roadmap in:
- `.copilot-tracking/changes/YYYYMMDD-decision-name.md`

Include:
- Date and context
- Decision made
- Rationale
- Impacted issues/components
- Alternatives considered

## Notes for Implementation

1. **Parallel Work**: Within each phase, issues marked "can parallelize" can be worked on simultaneously by different team members.

2. **Testing Throughout**: Don't wait until Phase 6 for testing. Each issue must include unit tests (90%+ coverage) and relevant integration tests.

3. **Security First**: Issue #32 (Security Measures) should be integrated throughout development, not just implemented in Phase 7. Consider security implications for every feature.

4. **Documentation Incrementally**: While Phase 8 focuses on comprehensive documentation, maintain inline documentation (XML comments) and README updates throughout development.

5. **Early Performance Monitoring**: Implement basic monitoring (#30) earlier if needed for debugging during development.

6. **Decision Points**: Issues #13 (state model) and #18 (encryption protocol) require architectural decisions before implementation. Create ADRs (Architecture Decision Records) for these.

7. **Issue #22 Cleanup**: Close issue #22 if truly duplicate, or remove duplicate label if not. This needs clarification before proceeding.

8. **Research Items Flexibility**: Phase 9 research items (#37, #38, #39) can be explored in parallel with other phases as time permits, but implementation should wait until post-1.0.
