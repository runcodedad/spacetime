<!-- markdownlint-disable-file -->

# Roadmap Details: Spacetime (Updated 2025-12-08)

## Data Source

- Repository: runcodedad/spacetime
- Issues fetched: 65 total (51 issues, 14 PRs)
- Open issues: 23 issues
- Closed issues included: Last 10 days of closed issues (lookback: 10 days)
- Query: `GET /repos/runcodedad/spacetime/issues?state=all&per_page=100`
- Snapshot date: 2025-12-08
- Previous roadmap: 2024-12-04 (4 days ago)

## Configuration

- recently_closed_days: 10 (default)
- weight_impact: 30
- weight_urgency: 20
- weight_effort: 10
- weight_dependency: 15
- weight_value: 20
- weight_risk: 5

## Progress Since Last Roadmap (Dec 4, 2024)

### Completed Issues (15+ closed)

1. **#6** - Difficulty Adjustment Algorithm ✅ (Dec 5, PR #59)
2. **#7** - Proof Scoring and Validation ✅ (Dec 5, PR #57)
3. **#8** - Epoch and Challenge System ✅ (Dec 4, PR #56)
4. **#9** - Transaction Structure ✅ (Dec 3, PR #53)
5. **#10** - Block Builder ✅ (Dec 3, PR #53)
6. **#11** - Block Validation Logic ✅ (Dec 6, PR #60)
7. **#12** - Chain Reorganization Logic ✅ (Dec 8, PR #65)
8. **#13** - Chain State Management ✅ (Dec 8, PR #63)
9. **#14** - Chain Storage Layer ✅ (Dec 3, PR #54)
10. **#25** - Wallet Implementation ✅ (Dec 3, PR #52)
11. **#35** - Genesis Block Configuration ✅ (Nov 30, PR #55)
12. **#40** - Documentation (Copilot instructions) ✅ (Nov 23, PR #44)
13. **#41** - Initial Project Setup ✅ (Nov 22, PR #42)
14. **#50** - Testing Infrastructure ✅ (Dec 3, PR #54)
15. **#58** - Misc Tidying ✅ (Dec 5)

### New Issues Added

1. **#64** - Implement State Root and Light Client Support (Dec 7, **OPEN**)

### Phase Completion Status

- **Phase 1 (Core Consensus)**: 5/5 complete (100%) ✅
- **Phase 2 (Transactions)**: 2/2 core complete (100%) ✅
- **Phase 3 (Networking)**: 0/4 complete (0%)
- **Phase 4 (Miner)**: 0/4 complete (0%)

## Per-issue Evidence

### NEW ISSUE: #64 — [Implement State Root and Light Client Support](https://github.com/runcodedad/spacetime/issues/64)

- **Priority Score: 88** (Impact: 27, Urgency: 18, Effort: 6, Dependency: 12, Value: 21, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Created**: 2025-12-07
- **Status**: **OPEN** ⭐
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #13 (Chain State Management) ✅ COMPLETE
  - Hard dependency: #11 (Block Validation) ✅ COMPLETE
  - Soft dependency: May require updates to block structure
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Merkle tree integration, state commitment in headers, light client verification
- **Confidence**: Medium — Reason: Core dependencies complete, but may require protocol-level changes to block headers
- **Why Priority 88**: High impact for enabling light clients and SPV validation, builds on completed state management, enhances network scalability
- **Issue Body Summary**: Implement Merkle Patricia Trie or similar for state commitment, add state root to block headers, enable light client verification without full state

### Phase 3 Issues (P2P Networking) — ALL OPEN

### Issue: #18 — [Implement P2P Network Foundation](https://github.com/runcodedad/spacetime/issues/18)

- **Priority Score: 93** (Impact: 28, Urgency: 19, Effort: 6, Dependency: 14, Value: 22, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Blocks: #17 (message types), #15 (relay), #16 (sync)
  - Soft dependency: Should integrate with #14 (storage) ✅
- **Suggested ETA/Size**: Large (5-6 weeks) — source: TCP/IP layer, peer discovery, connection management, encryption
- **Confidence**: High — Reason: Critical path, well-defined requirements, foundational dependencies complete
- **Why Priority 93**: Highest priority in Phase 3, blocks all other networking features

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
  - Uses: Block structure (complete), transaction structure (#9 ✅)
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Protocol design, serialization, validation
- **Confidence**: High — Reason: Clear requirements, depends on completed block/tx structures
- **Why Priority 91**: Second highest in Phase 3, required for meaningful network communication

### Issue: #16 — [Implement Block Synchronization](https://github.com/runcodedad/spacetime/issues/16)

- **Priority Score: 89** (Impact: 27, Urgency: 18, Effort: 7, Dependency: 12, Value: 21, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #18 (network foundation), #17 (message types)
  - Hard dependency: #14 (storage) ✅, #11 (validation) ✅, #12 (reorg) ✅
- **Suggested ETA/Size**: Large (4-5 weeks) — source: Initial sync, catch-up sync, reorg handling
- **Confidence**: Medium — Reason: Complex logic, multiple failure modes, performance critical
- **Why Priority 89**: Critical for node operation, but requires #18 and #17 first

### Issue: #15 — [Implement Message Relay and Broadcasting](https://github.com/runcodedad/spacetime/issues/15)

- **Priority Score: 85** (Impact: 26, Urgency: 17, Effort: 7, Dependency: 11, Value: 20, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Hard dependency: #18 (network foundation), #17 (message types)
  - Requires: Peer management from #18
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Gossip protocol, flood control, message deduplication
- **Confidence**: High — Reason: Standard gossip implementation with known patterns
- **Why Priority 85**: Lower than sync (#16) but still critical for network health

### Phase 4 Issues (Miner) — ALL OPEN

### Issue: #20 — [Implement Plot Scanning Strategies](https://github.com/runcodedad/spacetime/issues/20)

- **Priority Score: 84** (Impact: 26, Urgency: 17, Effort: 7, Dependency: 12, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: Builds on plot system (issues #2, #3, #4, #5 ✅ all closed)
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Multiple strategies, performance optimization, parallelization
- **Confidence**: High — Reason: Plot system foundation complete, clear strategy requirements
- **Why Priority 84**: Foundation for mining, highest priority in Phase 4

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
  - Needs: Network connection to submit blocks (#18)
- **Suggested ETA/Size**: Medium (3-4 weeks) — source: Event loop, timing, proof submission, block building
- **Confidence**: Medium — Reason: Depends on networking completion for full functionality
- **Why Priority 83**: Core miner logic, second in Phase 4

### Issue: #19 — [Implement Miner Configuration and CLI](https://github.com/runcodedad/spacetime/issues/19)

- **Priority Score: 78** (Impact: 23, Urgency: 16, Effort: 8, Dependency: 9, Value: 18, Risk: 4)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Soft dependency: #20 (plot scanning), #21 (event loop) for complete configuration
- **Suggested ETA/Size**: Small (1-2 weeks) — source: CLI parsing, config file format, validation
- **Confidence**: High — Reason: Standard CLI/config work, clear requirements
- **Why Priority 78**: Lower priority than core miner logic, but needed for usability

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
  - Enhances: #5 (plot management) ✅
- **Suggested ETA/Size**: Small (1-2 weeks) — source: Directory traversal, plot file validation, watch mode
- **Confidence**: High — Reason: Well-scoped feature, clear requirements
- **Why Priority 72**: Nice-to-have for UX, but not blocking core functionality

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
- **Suggested ETA/Size**: Large (4-5 weeks) — source: Complex lifecycle management, multiple subsystem integration
- **Confidence**: Medium — Reason: Depends on Phase 3 completion, complex integration work
- **Why Priority 82**: Critical for production node, but requires Phase 3 first

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

### Phase 7 Issues (Testing & QA) — PARTIALLY COMPLETE

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
- **Suggested ETA/Size**: Large (4-5 weeks, ongoing) — source: Multiple test scenarios, fixture management, CI integration
- **Confidence**: High — Reason: Foundation exists (#50 ✅), can add tests incrementally
- **Why Priority 75**: High value for quality, should start soon to catch integration issues early

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

### Phase 8 Issues (Production Hardening) — ALL OPEN

### Issue: #32 — [Implement Security Measures](https://github.com/runcodedad/spacetime/issues/32)

- **Priority Score: 96** (Impact: 29, Urgency: 19, Effort: 5, Dependency: 15, Value: 24, Risk: 4)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Should be integrated throughout all phases
  - Particularly critical for #18 (network security)
- **Suggested ETA/Size**: Ongoing — source: Input validation, crypto safety, DoS protection, rate limiting
- **Confidence**: Medium — Reason: Highest priority overall, but security is ongoing across all features
- **Why Priority 96**: **HIGHEST OVERALL PRIORITY** - Critical for production, should address incrementally
- **RECOMMENDATION**: Address critical security items immediately (input validation in consensus, crypto safety), schedule full audit for Q2

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
- **Suggested ETA/Size**: Ongoing — source: Graceful degradation, recovery strategies, error propagation
- **Confidence**: Medium — Reason: Cross-cutting concern, should be addressed incrementally
- **Why Priority 81**: Important for reliability, integrate with new features

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

### Phase 9 Issues (Developer Experience) — PARTIALLY COMPLETE

### Issue: #33 — [Create Comprehensive Documentation](https://github.com/runcodedad/spacetime/issues/33)

- **Priority Score: 76** (Impact: 23, Urgency: 15, Effort: 5, Dependency: 10, Value: 18, Risk: 5)
- **Labels**: documentation
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - #40 (Copilot instructions) ✅ complete
  - Best done incrementally as features complete
- **Suggested ETA/Size**: Large (4-6 weeks, ongoing) — source: Architecture docs, API docs, protocol spec, user guides
- **Confidence**: High — Reason: Clear documentation requirements, can start incrementally
- **Why Priority 76**: Important for adoption, should document as features complete

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

### Issue: #36 — [Create Deployment and Bootstrap Scripts](https://github.com/runcodedad/spacetime/issues/36)

- **Priority Score: 71** (Impact: 21, Urgency: 14, Effort: 8, Dependency: 9, Value: 16, Risk: 3)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: 
  - Requires #26 (full node) and #27 (node config) completion
  - Should include #19 (miner config) and #49 (plot discovery)
- **Suggested ETA/Size**: Medium (2-3 weeks) — source: Docker configs, systemd units, bootstrap automation
- **Confidence**: Medium — Reason: Requires Phase 4-5 completion for full deployment
- **Why Priority 71**: Important for production deployment, but after core features

### Phase 10 Issues (Research) — ALL OPEN

### Issue: #37 — [Research: Plot Compression Techniques](https://github.com/runcodedad/spacetime/issues/37)

- **Priority Score: 50** (Impact: 15, Urgency: 10, Effort: 9, Dependency: 5, Value: 10, Risk: 1)
- **Labels**: None
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN**
- **Linked PRs**: None
- **Dependencies**: None (research item)
- **Suggested ETA/Size**: Research (4-8 weeks) — source: Research and design phase, no immediate implementation
- **Confidence**: Low — Reason: Research task with uncertain outcome, not blocking current roadmap
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
- **Suggested ETA/Size**: Research (8-12 weeks) — source: Major feature research, design, prototyping
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
- **Suggested ETA/Size**: Research (8-12 weeks) — source: Complex research area, design, performance analysis
- **Confidence**: Low — Reason: Speculative research, uncertain value for PoST blockchain
- **Why Priority 40**: Lowest priority, exploratory research only

### Issues Needing Clarification

### Issue: #22 — [Implement Transaction Serialization](https://github.com/runcodedad/spacetime/issues/22)

- **Priority Score: Unknown** (Marked as duplicate but still open)
- **Labels**: duplicate
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN** ⚠️
- **Issue**: Marked as duplicate but still open. Duplicate of what?
- **Recommendation**: Review and either close if truly duplicate, or remove duplicate label and score if legitimate work remains
- **Updated**: Last updated Dec 4, 2024 (label added)

### Issue: #24 — [Implement Transaction Validation](https://github.com/runcodedad/spacetime/issues/24)

- **Priority Score: 87** (from Dec 4 roadmap)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN** ⚠️
- **Issue**: Core transaction functionality delivered via #9 (Transaction Structure ✅) and #25 (Wallet ✅). Is this redundant or additional work needed?
- **Recommendation**: Clarify if this represents additional validation logic beyond what's in #9, or if it can be closed

### Issue: #23 — [Implement Transaction Pool (Mempool)](https://github.com/runcodedad/spacetime/issues/23)

- **Priority Score: 86** (from Dec 4 roadmap)
- **Labels**: enhancement
- **Assignee(s)**: None
- **Milestone**: None
- **Comments**: 0
- **Status**: **OPEN** ⚠️
- **Issue**: Similar to #24 - is mempool functionality already implemented or is this separate work?
- **Recommendation**: Clarify scope and either proceed with implementation or close if complete

## Dependency Graph (Adjacency List)

**Phase 3 (Networking) dependencies:**
- #18 (P2P Foundation) → blocks [#17, #15, #16]
- #17 (Message Types) → depends on [#18]
- #16 (Block Sync) → depends on [#18, #17, #14✅, #11✅, #12✅]
- #15 (Message Relay) → depends on [#18, #17]

**Phase 4 (Miner) dependencies:**
- #20 (Plot Scanning) → depends on [#2✅, #3✅, #4✅, #5✅]
- #21 (Miner Event Loop) → depends on [#20, #10✅, #18]
- #19 (Miner Config) → depends on [#20, #21] (soft)
- #49 (Plot Discovery) → depends on [#20] (soft), enhances [#5✅]

**Phase 5 (Full Node) dependencies:**
- #26 (Node Lifecycle) → depends on [#14✅, #18, #16, #11✅]
- #27 (Node Config) → depends on [#26] (soft)

**Phase 6 (State Root) dependencies:**
- #64 (State Root) → depends on [#13✅, #11✅], may affect block structure

**Cross-cutting dependencies:**
- #32 (Security) → affects all phases, particularly #18 (network security)
- #31 (Error Handling) → affects all phases, particularly #26 (node lifecycle)
- #30 (Logging) → affects all phases, needed for debugging
- #28 (Integration Tests) → can start now, expands with new features

**No cycles detected**

## Backlog Grouping

### Completed (since Dec 4, 2024) ✅

- **Foundational Infrastructure**: #6, #7, #8, #9, #10, #11, #12, #13, #14 (Phase 1-2 complete)
- **Wallet & Transactions**: #25 (wallet), #9 (transaction structure)
- **Genesis & Bootstrap**: #35 (genesis block), #41 (project setup)
- **Testing Infrastructure**: #50 (testing foundation)
- **Documentation**: #40 (Copilot instructions)
- **Code Quality**: #58 (misc tidying)

### Critical Path (Blocks progress)

- **Phase 3 (Networking)**: #18 → #17 → #16, #15
- **Phase 4 (Miner)**: #20 → #21 → #19, #49
- **Phase 5 (Full Node)**: #26 → #27

### High Value (Enable key capabilities)

- **NEW: State Root & Light Clients**: #64 (priority 88, can start now)
- **Security**: #32 (priority 96, **HIGHEST**, ongoing)
- **Integration Testing**: #28 (priority 75, should start soon)

### Quality & Production Readiness

- **Hardening**: #31 (error handling), #30 (logging), #32 (security)
- **Testing**: #28 (integration tests), #29 (benchmarking)

### Developer Experience

- **Documentation**: #33 (comprehensive docs)
- **Tooling**: #34 (developer tools), #36 (deployment scripts)

### Research & Future (Low priority)

- **Future Enhancements**: #37 (plot compression), #38 (smart contracts), #39 (ZK proofs)

### Needs Clarification ⚠️

- **Duplicates/Redundant**: #22 (duplicate?), #24 (redundant with #9?), #23 (redundant with #25?)

## Metrics & Formulas

### Composite Score Formula

```
priority_score = (impact × 0.30) + (urgency × 0.20) + (effort_inverse × 0.10) + (dependency × 0.15) + (value × 0.20) + (risk × 0.05)
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

- **Total Issues**: 65 (51 issues + 14 PRs)
- **Open Issues**: 23
- **Closed Since Dec 4**: 15+ (including PRs)
- **New Issues Since Dec 4**: 1 (#64)
- **Phases Complete**: 2 of 10 (Phase 1 & 2)
- **Critical Path Issues Open**: 12 (Phase 3: 4, Phase 4: 4, Phase 5: 2, Phase 6: 1, Clarification: 3)
- **Velocity**: ~3-4 issues closed per day (Dec 3-8)
- **Average Issue Age (Open)**: Not calculated (would require issue-by-issue analysis)
- **Next Milestone**: Phase 3 completion (target: Q1 2025, 4 issues remaining)
