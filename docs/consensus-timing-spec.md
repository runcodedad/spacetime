# Spacetime Consensus Timing Specification

1. Overview

This document defines how time, epochs, challenges, difficulty, and block production interact within the Spacetime Proof-of-Space-Time (PoST) consensus system. It clarifies the relationship between challenge windows, epochs, and target block time, and establishes the rules for handling empty epochs and difficulty adjustment.

⸻

2. Key Time Concepts

2.1 Target Block Time
	•	The desired long-term average time between accepted blocks.
	•	Example: 60 seconds per block.
	•	Controlled by difficulty adjustment.

2.2 Challenge Window
	•	A short time interval where miners must submit proofs for a specific challenge.
	•	Typical duration: 1–5 seconds.
	•	After this window closes, proofs for that challenge are rejected.

2.3 Epoch
	•	One full challenge cycle:
	1.	Network issues a challenge
	2.	Miners compute and submit proofs
	3.	Network evaluates proofs
	4.	If a valid proof meets difficulty → block is produced
	5.	Otherwise → epoch ends with no block
	•	One epoch = one challenge window.
	•	Many epochs may occur before a block is produced.

⸻

3. Block Production Rules

3.1 When a Block Is Produced

A block is produced only if:
	•	At least one miner submits a valid proof
	•	The proof’s score is below the difficulty threshold

The lowest valid score wins the right to produce the block.

3.2 When No Block Is Produced

If no proof meets difficulty:
	•	The epoch ends with no block
	•	A new epoch begins with a new challenge
	•	This is normal and expected

The network does not stall; it simply moves forward.

⸻

4. Difficulty Mechanism

4.1 Purpose

Difficulty ensures that:

On average, one epoch every target block time produces a winning proof.

Example with a 60-second block target and 3-second challenge windows:
	•	~20 epochs occur per block
	•	Only one epoch needs to produce a winner
	•	Difficulty ensures this happens consistently over time

4.2 Adjustment

Difficulty adjusts based on:
	•	Time since last block
	•	Recent block intervals
	•	Rolling averages

If blocks arrive too quickly, difficulty increases.
If blocks arrive too slowly, difficulty decreases.

⸻

5. Challenge Generation Rules

5.1 Frequency

A new challenge is issued every epoch (i.e., every challenge window).

5.2 No-Winner Handling

If no miner produces a valid proof:
	•	The challenge expires
	•	No block is produced
	•	A new epoch begins with a new challenge
	•	Difficulty eventually adjusts to restore target block time

⸻

6. Consensus Timing Summary
	•	Target Block Time: long-term target (e.g., 60s)
	•	Challenge Window: short operational period (1–5s)
	•	Epoch: one challenge window; may or may not produce a block
	•	Difficulty: governs average block frequency, not per-challenge timing
	•	Empty Epochs: common, normal, and expected in PoST

This structure keeps proof generation quick and responsive, while maintaining stable long-term block timing across the network.