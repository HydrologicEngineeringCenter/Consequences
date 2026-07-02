# 0003. Each layer is a separate project within a single solution

Date: 2026-07-02
Status: Accepted

## Context

The system is growing multiple layers (Life Loss, Agriculture, NSI/Network Access, Hazard Provider, Traffic Simulation, etc.) that need clear boundaries but also need to be built and versioned together.

## Decision

Each layer is implemented as its own project, but all layers live within the same solution rather than being split into separate repos/solutions.

## Consequences

Easier: enforced separation between layers via project boundaries, while still allowing shared builds, cross-project refactoring, and a single version history. Harder: Automatic versioning differing between projects less straightforward if desired.  
