# 🤖 Copilot Context — Spacetime Blockchain

## Project Overview
**Spacetime** is a learning-oriented blockchain project that implements a **Proof of Space-Time (PoST)** consensus mechanism.  
It is designed to be:
- **Energy-efficient** (disk-based mining, minimal CPU/GPU use)
- **Fair and mineable** (no privileged validators)
- **Educational and modular** (readable, incremental design)
- **Built in C# / .NET 10+**

This repository demonstrates how to build a decentralized blockchain network from scratch — including block structures, mining logic, P2P communication, and the novel PoST consensus mechanism.

---

## Core Design Principles
Copilot should align with these when suggesting code:

- **Determinism** — avoid randomness that isn’t reproducible across nodes.  
- **Simplicity first** — prefer clarity over micro-optimizations early on.  
- **Extensibility** — organize code to allow later addition of features like validator finality or proof upgrades.  
- **Energy Efficiency** — focus on lightweight disk I/O, avoid unnecessary hashing or loops.  
- **Fairness & Decentralization** — no central authority; all nodes derive challenges locally.

---

## High-Level Architecture

### Core Components
| Module | Purpose |
|---------|----------|
| `Blockchain` | Core block, transaction, and chainstate logic |
| `Consensus` | Proof of Space-Time, challenge generation, difficulty adjustment |
| `Plots` | Disk-based data creation and proof verification |
| `Mining` | Challenge polling and proof submission |
| `Network` | P2P synchronization, message exchange |
| `Wallet` | Key generation, transaction signing |
| `CLI` | User interface for running nodes, creating plots, sending txs |

---

## Key Concepts Copilot Should Understand

### Proof of Space (PoS)
- Uses **disk space** as the scarce resource.
- Miners pre-generate “plots” — deterministic files filled with pseudorandom entries.
- Each entry corresponds to a Merkle leaf for later proof verification.

### Proof of Space-Time (PoST)
- Extends PoS by requiring **continuous participation over epochs**.
- Every epoch, a **challenge** is derived deterministically:
