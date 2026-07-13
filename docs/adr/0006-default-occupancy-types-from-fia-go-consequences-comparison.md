# 0006. Default occupancy types come from the HEC-FIA / go-consequences shared set

Date: 2026-07-08
Status: Accepted

## Context

ADR-0002 established that default occupancy types live in Core as code-defined defaults, but not what those defaults are. Two candidate sources existed in the Corps' consequence engines:

1. **HEC-FIA/LifeSim** — `Occupancy Type Defaults.sqlite`: 40 occupancy types with structure/content/other/vehicle depth-damage curves (XML `MonotonicUncertainCurve` blobs), life-loss/evacuation parameters, and a value/foundation-height variation table.
2. **go-consequences** — [`structures/occtypes.json`](https://github.com/USACE/go-consequences/blob/main/structures/occtypes.json) @ `dff647cabbc1abae3f084e80b7f6a166651d05d8` (2021-12-27): 51 types with structure/contents damage functions keyed by damage driver (depth, erosion, salinity, wave), per-ordinate uncertainty distributions, and source attribution.

A one-off comparison tool (`Tools/OccTypeComparison`, since removed — this ADR and `docs/occupancy-type-defaults/` are its record) mapped both sources onto a canonical intermediate schema and compared them. Findings, presented to technical experts on 2026-07-08:

- All 40 HEC-FIA type names exist in go-consequences; the 11 go-consequences extras are mostly NACCS coastal types with no depth-damage curve at all.
- Of the 80 comparable structure/contents depth-damage curves, **79 are numerically identical** (go-consequences attributes its depth curves to "HEC-FIA damage functions (Galveston)").
- The single divergence is **AGR1 structure**, where the go-consequences curve is the HEC-FIA curve shifted one foot deeper — consistent with a data-entry off-by-one.
- Each source carries content the other lacks: FIA has vehicle/other curves, life-loss and evacuation parameters, and value uncertainty; go-consequences has erosion/coastal drivers, per-ordinate uncertainty distributions, and source attribution.

## Decision

- Core's default occupancy types are the **shared set of 40** that both engines implement, transcribed from the HEC-FIA source.
- For AGR1, the **HEC-FIA version** of the structure curve is accepted, after consultation with the current HEC-FIA user manual and software.
- The defaults are implemented as `OccupancyTypeDefaults.GetDefaults()` (`Consequences/Occupancy/OccupancyTypeDefaults.cs`), generated from the FIA SQLite with damage-curve y-values converted from percent to fractions (matching `Building.Compute`, which multiplies value directly by `GetYFromX(depth)`). `All_Warned`/`All_Mobilized` map to `CollectivelyWarned`/`CollectivelyMobilize`; foundation-height offsets and value percentages stay at defaults because all FIA variation entries for them are None.
- Sixteen RES1 curves carry Normal per-ordinate standard deviations in the source; central values are transcribed, and the uncertainty data is preserved in the canonical JSON for when `UncertainOccupancyType` matures.
- The comparison tool itself is **not retained** — the transcription is complete and the record lives in this ADR plus the outputs below.

## Record

`docs/occupancy-type-defaults/` holds the comparison outputs of the 2026-07-08 run:

- `comparison-report.html` — self-contained expert-facing report: coverage matrix, attribute inventory, methodology, and overlaid depth-damage curves per type, sorted most-divergent first.
- `occtypes-fia.canonical.json` / `occtypes-go.canonical.json` — both sources in the neutral intermediate schema (curves with central values and uncertainty bands, plus all non-curve parameters), including everything not transcribed into Core: FIA vehicle/other curves, life-loss/evacuation/submergence parameters, and go-consequences erosion/coastal functions.

## Consequences

Easier: Core ships defaults both existing engines already agree on, with full provenance; the not-yet-modeled data (uncertainty, life loss, vehicle/other, coastal drivers) is preserved in a machine-readable form to draw on as Core's model grows. Harder: updating the defaults means editing generated-style code by hand or rebuilding tooling, since the comparison/generation tool was deliberately removed.
