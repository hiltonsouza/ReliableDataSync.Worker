# Reliable Data Synchronization Worker (.NET)

This project demonstrates a worker-based automation system designed to safely synchronize structured data between human-managed systems and a constrained external platform.

## Problem
In many enterprise environments, data is manually entered and stored in relational databases, but must be propagated to legacy or external systems with strict execution rules.  
These systems often do not provide reliable APIs or easy feedback mechanisms.

## Solution
This worker implements:
- Validation and pre-processing of input data
- Idempotent execution to prevent duplicate operations
- Controlled retries and error classification
- State reconciliation and auditability through status tracking

## Key Concepts
- Worker-based architecture
- Idempotent processing
- State machine for execution control
- Operational safety and reprocessing
- Clear execution feedback for human operators

## Tech Stack
- .NET 8
- C#
- Entity Framework Core
- xUnit
- Docker (local execution)

## Notes
The external system is simulated to reflect constrained, contract-driven integrations often found in legacy enterprise environments.
