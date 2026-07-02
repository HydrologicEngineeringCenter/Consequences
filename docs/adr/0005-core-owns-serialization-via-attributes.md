# 0005. Consequences Core owns JSON/XML serialization via attributes

Date: 2026-07-02
Status: Accepted

## Context

Multiple layers need to serialize Core types to/from JSON and XML (this overlaps with what the NSI importer previously handled in Core).

## Decision

Consequences Core owns to/from JSON and XML serialization for its types, implemented through attribution (attributes on the types) rather than a separate serialization layer.

## Consequences

Easier: one shared, low-cost serialization mechanism with wide applicability across consumers; XML support is just an extra attribute alongside JSON. Harder: Core's public types are now coupled to serialization attributes, which constrains how freely they can be reshaped later.
