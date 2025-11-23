# 🌌 Spacetime Blockchain

> **Spacetime** — a fair, low-energy blockchain where **disk and time** prove value.

---

## 🚀 Overview

**Spacetime** is an experimental blockchain designed to explore a **Proof-of-Space-Time (PoST)** consensus model — a novel, energy-efficient alternative to Proof-of-Work (PoW) and Proof-of-Stake (PoS).

Instead of burning energy or locking tokens, miners **prove they dedicate disk space over time**.  
This makes Spacetime:
- 🟢 **Low in energy consumption** — no GPUs or ASICs required.  
- ⚖️ **Fair and accessible** — anyone with free disk space can mine.  
- 🧠 **Educational** — a readable, modular blockchain built for learning.

> 💡 *Built in C# / .NET 10 as a clean, composable learning project.*

---

## 🧩 Key Concepts

| Concept | Description |
|----------|--------------|
| **Proof of Space (PoS)** | Miners generate local *plots* — files filled with deterministic pseudorandom data. |
| **Proof of Space-Time (PoST)** | Extends PoS by requiring miners to remain active over time (epochs). |
| **Challenge** | Deterministic seed derived from the last block’s hash and randomness beacon. |
| **Plot File** | On-disk structure miners scan to produce proofs. |
| **Epoch** | A fixed period in which challenges and proofs occur. |
| **Proof** | Cryptographic evidence that a miner has valid data matching the challenge. |

---

## 🛠️ Tech Stack

- **Language:** C#  
- **Runtime:** .NET 10+  
- **Storage:** File-based (append-only ledger + disk plots)  
- **Networking:** Custom P2P message layer  
- **Cryptography:** SHA-256 + ECDSA  
- **OS Support:** Windows, macOS, Linux  

---

## 🧱 Project Roadmap

Spacetime is developed in **incremental phases**:

1. **Core Blockchain Layer** — blocks, transactions, and chainstate  
2. **Plotting System** — deterministic proof-of-space files  
3. **Consensus Engine** — PoST challenge and difficulty logic  
4. **Networking Layer** — peer sync and gossip protocol  
5. **Wallet & CLI** — local keys, mining, and monitoring  
6. **Epoch Logic** — time-based participation and decay  
7. **Testing & Simulation** — local testnet and performance tuning  

A detailed engineering checklist is available in [`docs/implementation-checklist.md`](docs/implementation-checklist.md).

---

## ⚙️ Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download)
- Git
- (Optional) Visual Studio Code or JetBrains Rider

### Clone & Build
```bash
git clone https://github.com/yourusername/spacetime.git
cd spacetime
dotnet build
```

---

## 🤖 Contributing with GitHub Copilot

This project includes comprehensive GitHub Copilot instructions to help generate contextually appropriate code. 

📄 **[`.github/copilot-instructions.md`](.github/copilot-instructions.md)** - Contains:
- Project architecture and design principles
- Coding standards and conventions  
- Testing requirements and patterns
- Blockchain-specific guidance
- Security considerations
- Common patterns and anti-patterns

The instructions help Copilot understand:
- Spacetime's Proof-of-Space-Time consensus mechanism
- C# 14.0 / .NET 10 best practices for this project
- Async/await patterns and performance considerations
- Cryptographic operations and binary data handling
- Thread safety for concurrent plot operations

*These instructions are automatically used by GitHub Copilot when working in this repository.*

---

## 📚 Documentation

- **[Implementation Checklist](docs/implementation-checklist.md)** - Detailed development roadmap
- **[Requirements](docs/requirements.md)** - Technical specifications and architecture
- **[Copilot Instructions](.github/copilot-instructions.md)** - AI-assisted development guidelines
