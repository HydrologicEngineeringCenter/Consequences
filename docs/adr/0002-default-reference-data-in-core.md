# 0002. Default reference data lives in Consequences Core, as overridable code-defined defaults

Date: 2026-07-02
Status: Accepted

## Context

Occupancy types and damage functions need default values somewhere. There's a pull toward a damage-function database, and consumers of Core (like NSI) may want to supply their own defaults.

## Decision

Consequences Core owns the default occupancy types and damage functions. Damage function defaults are sourced from NSI, and a layer above Core is allowed to override Core's defaults. Defaults are implemented as a static method returning an array of objects, defined directly in code — not as external/compiled text files.

## Consequences

Easier: defaults are versioned with the code, type-checked at compile time, and simple to override at a higher layer. Harder: changing defaults requires a code change and rebuild rather than editing a data file.
