# Occupancy type defaults comparison — results and decision

Run date: 2026-07-08. Inputs: HEC-FIA `Occupancy Type Defaults.sqlite` and
go-consequences `occtypes.json` @ `dff647c` (2021-12-27). Files in this folder are the
outputs of that run (`comparison-report.html` plus both sources in the canonical
intermediate schema).

## Findings

- 40 occupancy types are defined by both sources; 11 exist only in go-consequences
  (mostly NACCS coastal types with no depth-damage curve).
- Of the 80 comparable structure/contents depth-damage curves, 79 are numerically
  identical and 1 diverges: **AGR1 structure**, where the go-consequences curve is the
  HEC-FIA curve shifted one foot deeper.

## Decision

- **AGR1:** accept the **HEC-FIA version** of the structure curve, after consultation
  with the current HEC-FIA user manual and software.
- **Outcome:** there is a shared set of **40 default occupancy types that both engines
  implement**, and this shared set will be **immediately implemented in Consequences
  Core** (as code-defined defaults per ADR-0002).
