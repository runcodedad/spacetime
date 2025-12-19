---
applyTo: ".copilot-tracking/changes/20251219-spacetime-roadmap-changes.md"
---

# Roadmap: spacetime (Generated 2025-12-19)

## Summary

Enable miner integration of the newly implemented plot scanning strategies and progress to a functioning miner event loop and CLI so miners can boot, scan plots, respond to challenges, and submit proofs.

## Phases

### Phase 1 — Miner Integration (Target: 2025-12-26)
- Integrate plot scanning strategies into the miner runtime; implement the miner event loop.
- Issues (in order):
  - #21 — Implement Miner Event Loop (Priority: High)
  - #19 — Implement Miner Configuration and CLI (Priority: Medium)

### Phase 2 — Miner Hardening & Tooling (Target: 2026-01-09)
- Add tests, benchmarks, and CLI polish; add packaging and deployment scripts for miner.
- Issues (in order):
  - #29 — Implement Simulation and Benchmarking Tools (Priority: Medium)
  - #36 — Create Deployment and Bootstrap Scripts (Priority: Low)

## Scoring Legend

- Impact: {{weight_impact}}%
- Urgency: {{weight_urgency}}%
- Effort: {{weight_effort}}%
- Dependency Centrality: {{weight_dependency}}%
- Stakeholder Value: {{weight_value}}%
- Risk: {{weight_risk}}%

## Top Risks & Mitigations

- Risk: Miner event loop integration uncovers API mismatches — Mitigation: Add small integration tests and feature-flagged rollout.
- Risk: Performance regressions in scanning strategies — Mitigation: Run quick benchmarks and allow fallback to previous strategy.

## Needs Clarification

- #21 — Clarify expected interaction model between `PlotManager` and miner event loop (Why: interface/ownership assumptions not explicit).

## How to use this roadmap

- Follow Phase 1 tasks in order; open PRs that reference the issue numbers. If implementation uncovers additional dependencies, update this roadmap and re-run prioritization.
