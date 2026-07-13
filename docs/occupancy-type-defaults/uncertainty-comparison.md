# Uncertainty comparison — distributions about the damage-curve ordinates

Date: 2026-07-13. Follow-up to the central-tendency comparison recorded in
[ADR-0006](../adr/0006-default-occupancy-types-from-fia-go-consequences-comparison.md).
Sources: HEC-FIA `Occupancy Type Defaults.sqlite` (per-ordinate `Normal_Standard_Deviation`
attributes in the curve XML) and go-consequences `occtypes.json` @ `dff647c` (per-ordinate
`ydistributions`). AGR1 structure is excluded, as its central curve was already identified
as divergent and resolved in favor of HEC-FIA.

## Finding: uncertainty about the shared occupancy types is consistent

Across the 79 comparable structure/contents depth-damage curve pairs, the two sources
agree exactly on uncertainty — same families, same parameters, same depth grids:

- **63 pairs are deterministic in both sources.** Neither carries any spread about the
  central values.
- **16 pairs carry Normal uncertainty in both sources** — the eight RES1 single-family
  types (1S/2S/3S/SL × with/without basement), structure and contents. Per-ordinate
  standard deviations are **identical to the digit at every ordinate**
  (max |σ_FIA − σ_go| = 0.000), on identical grids (19 points over −2…16 ft for
  no-basement types; 25 points over −8…16 ft for basement types). No tails exist where
  one source specifies uncertainty over a wider depth range.
- **No curve has uncertainty in one source but not the other.**

Like the central values (79/80 identical), this confirms shared lineage: the
go-consequences RES1 Normal parameters are a transcription of the HEC-FIA EGM curves, not
an independent estimate.

Character of the shared uncertainty: standard deviation grows with damage. RES1-1SWB
structure, for example, runs σ = 0.01 damage-points at 0.7% damage up to σ = 2.3 points at
the 81.1% terminal damage — a ±2σ band under ±5 points at worst (roughly a 3% coefficient
of variation).

## Uncertainty each source has that the other lacks (additive, not conflicting)

- **go-consequences only:** Triangular distributions on every erosion curve (540 ordinates
  on shared types, 145 on the go-only NACCS types) and Empirical (histogram) distributions
  on the coastal composite-driver curves (depth+salinity, depth+salinity+wave — 320
  ordinates). HEC-FIA has no erosion or coastal drivers to compare against.
- **HEC-FIA only:** occupancy-level Triangular variations on the three submergence
  parameters (roof/structure/floor; 120 rows — life-loss domain). FIA's structure-value,
  content-value, and foundation-height variations are all "None" in the defaults, and its
  vehicle/"other" curves are fully deterministic, so the 16 RES1 Normal curves are the only
  per-ordinate uncertainty FIA ships.

## Implication for Core

When `UncertainOccupancyType` matures, the 16 RES1 Normal curves can be adopted without a
reconciliation decision — the sources already agree. The only open expert choices are
whether to pull in go-consequences' erosion/coastal uncertainty or HEC-FIA's submergence
variations; these are additive rather than conflicting. The parameters are preserved in
`occtypes-fia.canonical.json` and `occtypes-go.canonical.json` in this folder.
