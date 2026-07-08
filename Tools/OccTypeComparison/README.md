# OccTypeComparison

One-off analysis tool comparing two candidate sources of default occupancy types for
ConsequencesCore (see ADR-0002):

1. **HEC-FIA/LifeSim SQLite defaults** — `Occupancy Type Defaults.sqlite` (not in repo;
   pass `--sqlite <path>`, default `E:\Occupancy Type Defaults.sqlite`).
2. **go-consequences** — `data/occtypes.json`, a vendored snapshot of
   [USACE/go-consequences `structures/occtypes.json`](https://github.com/USACE/go-consequences/blob/main/structures/occtypes.json)
   at commit `dff647cabbc1abae3f084e80b7f6a166651d05d8` (2021-12-27).

## Run

```
dotnet run --project Tools/OccTypeComparison
```

Outputs to `Tools/OccTypeComparison/output/` (gitignored):

- `occtypes-fia.canonical.json` / `occtypes-go.canonical.json` — both sources mapped to a
  neutral intermediate schema (`CanonicalOccType`), usable by other tooling.
- `comparison-report.html` — self-contained expert-facing report: coverage matrix,
  attribute inventory, and overlaid structure/contents depth-damage curves per shared
  occupancy type, sorted most-divergent first.

## Results

`results/` holds the committed outputs of the 2026-07-08 run and `results/DECISION.md`,
which records the outcome: accept the HEC-FIA AGR1 structure curve, and immediately
implement the shared set of 40 defaults in Consequences Core.
