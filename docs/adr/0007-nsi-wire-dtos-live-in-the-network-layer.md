# 0007. NSI wire DTOs live in the Network layer, not Core

Date: 2026-08-05
Status: Accepted

## Context

`NsiStructure` — a field-for-field mirror of the NSI structures response — lived in Core at `Consequences/Buildings/NsiStructure.cs`. It is not a Core type: it is a third party's schema, versioned by NSI, carrying attributes (`ftprntsrc`, `crerank`, `depindex`) the damage and life-loss models never read.

ADR 0005 gives Core ownership of JSON/XML serialization via attributes, and notes the overlap with what the NSI importer was then handling in Core. Read narrowly, that decision is about Core serializing *Core's own types*; it does not require Core to host every schema the system reads.

Core's domain types also cannot be deserialization targets for NSI. `Building` requires an `OccupancyType` carrying damage curves; the response supplies only an occupancy type *name*. Something has to resolve the name against the occupancy type set, so the wire model and the domain model are necessarily different types with a projection between them.

## Decision

NSI wire DTOs live in the Network layer, under `Consequences.Network/DTOs`. Core continues to own serialization of Core's own types per ADR 0005; that record stands unchanged.

Projection from the wire model onto a domain type happens in the Network layer, behind `INsiStructureMapper<TReceptor>`. Parsing (`NsiJsonParser`) knows only the DTOs, mapping (`BuildingMapper`) knows both sides, and the importer composes them.

## Consequences

Easier: Core stops carrying a third party's schema, so an NSI field rename or format change is contained in the Network layer. One DTO can feed several receptor types — `Building` today, life-loss and agriculture receptors later — by swapping the mapper rather than reshaping the parse. Parsing is testable against captured responses with no network.

Harder: two types now describe a structure, and surfacing a new NSI attribute in the domain means touching both the DTO and a mapper. Consumers wanting raw NSI attributes must depend on the Network layer rather than Core.
