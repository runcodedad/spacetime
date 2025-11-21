### Define and Implement Block Structure

**Description:**
Define the block structure and implement serialization/deserialization.

**Requirements:**
- Block header fields:
  - Previous block hash
  - Timestamp
  - Epoch number
  - Difficulty target
  - Miner public key
  - Plot ID
  - Proof score
  - Proof Merkle root
  - Transaction Merkle root
  - Block signature
- Proof object structure
- Transaction list
- Optional metadata fields
- Efficient serialization format (e.g., Protocol Buffers, CBOR, or custom binary)
- Header hash calculation

**Acceptance Criteria:**
- Block structure fully defined
- Serialization/deserialization implemented
- Header hash calculation
- Unit tests for serialization roundtrip
- Backward compatibility considerations
- Documentation of block format