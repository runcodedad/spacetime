# Roadmap Details: spacetime (Generated 2025-12-19)

## Data Source

- Repository: runcodedad/spacetime
- Issues fetched: 33 (open + recent closed)
- Closed issues included (lookback days): 10
- Query: `repo:runcodedad/spacetime is:issue is:open` and `repo:runcodedad/spacetime is:issue is:closed closed:>=2025-12-09`

## Per-issue Evidence

- Issue: #20 — Implement Plot Scanning Strategies
  - Priority Score: 88 (Impact: high, Urgency: medium, Effort: medium)
  - Labels: plotting, miner, effort: medium
  - Assignee(s): Copilot, runcodedad
  - Comments: 0
  - Linked PRs: active branch `copilot/implement-plot-scanning-strategies` (local workspace)
  - Dependencies: none explicit
  - Suggested ETA/Size: 1-3 days (label: effort: medium)
  - Confidence: High — Completed (closed)

- Issue: #21 — Implement Miner Event Loop
  - Priority Score: 94 (Impact: high, Urgency: high, Effort: medium)
  - Labels: enhancement, miner, effort: medium
  - Assignee(s): none
  - Comments: 0
  - Linked PRs: none
  - Dependencies:
    - Soft dependency: #20 — plot scanning strategies (evidence: miner needs scanning to search plots)
  - Suggested ETA/Size: 1-3 days (label: effort: medium)
  - Confidence: Medium — needs interface clarifications with `PlotManager`

- Issue: #19 — Implement Miner Configuration and CLI
  - Priority Score: 70 (Impact: medium, Urgency: medium, Effort: medium)
  - Labels: enhancement, miner, effort: medium
  - Suggested ETA/Size: 1-3 days
  - Confidence: Medium

## Dependency Graph (Adjacency list)

- #21 → [#20]  (soft dependency: event loop uses plot scanning strategies)
- #19 → []

## Backlog Grouping

- Quick wins:
  - #49 — Support directory scanning for plot discovery (effort: small)
  - #69 — Implement Coinbase Maturity Rules (effort: small)

- Feature work:
  - #21 — Implement Miner Event Loop
  - #19 — Implement Miner Configuration and CLI

- Foundational infra:
  - #67 — Implement Emission Curve and Block Reward Schedule
  - #64 — Implement State Root and Light Client Support

- Technical debt / Research:
  - #37 — Research: Plot Compression Techniques
  - #39 — Research: Zero-Knowledge Proof Integration

## Metrics & Formulas

- Composite score formula: composite = normalize( impact*{{weight_impact}} + urgency*{{weight_urgency}} + (1-effort)*{{weight_effort}} + dependency_centrality*{{weight_dependency}} + value*{{weight_value}} - risk*{{weight_risk}} )
- Normalization method: linear normalization to 0-100 by observed min/max in dataset
- Weight table placeholders:
  - impact: {{weight_impact}}
  - urgency: {{weight_urgency}}
  - effort: {{weight_effort}}
  - dependency: {{weight_dependency}}
  - value: {{weight_value}}
  - risk: {{weight_risk}}

## Notes & Assumptions
- Interface assumptions between miner and plot manager are not fully specified; implement small adapter layer and add tests to validate integration.
- The workspace branch `copilot/implement-plot-scanning-strategies` contains the plot scanning implementation and should be used as the starting point for miner integration.
