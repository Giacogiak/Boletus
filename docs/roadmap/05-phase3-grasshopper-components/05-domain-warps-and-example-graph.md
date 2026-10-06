# 05 — Phase 3b: The domain-warp decorators and the example graph

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).

## Implemented 3b.3 (partial) — domain-warp decorators (2026-06-25)

The fourth 3b.3 item is done: four new single-child **decorator** components — `Offset`, `Twist`,
`Bend`, `Displace` — joining `Transform` on the **Decorators** sub-panel. The user asked for these
explicitly; before, `Transform` was the only domain decorator on the canvas.

- **Pure GH plumbing — zero Core change.** All four ops were *already* in `Boletus.Core` (`Field.Offset`/
  `Twist`/`Bend`/`Displace`, registered in `Ops.cs`, each gated against DualC's `--dump-json` in the
  vocabulary suite). So the new code touches **no serialization** — it only wires GH inputs to the
  existing, already-verified builders. The whole risk surface is the dropdown int→string mapping.
- **Standalone, not consolidated.** Four separate components (matching the `Transform`/`Onion`/
  `Normalize` standalone precedent), per the user's choice — a single multi-mode "Deform" would need
  `IGH_VariableParameterComponent` because the params are heterogeneous (twist/bend: scalar+axis;
  displace: fn+amp+freq; offset: scalar). Scope was held to the four requested; `mirror`/`elongate`/
  `repeat` (also already in Core) were deferred.
- **Dropdowns follow the `TpmsComponent` pattern** (`Param_Integer` + `AddNamedValue` +
  `SetPersistentData`), and **enumerate their full option set in the input Description** (project
  convention). The int→token maps — axis `0/1/2 → "x"/"y"/"z"` (shared `AxisName` helper),
  fn `0/1/2 → "sine"/"gyroid"/"bumps"` — use the exact strings the vocabulary tests round-trip through
  DualC; an out-of-range index posts a `GH_RuntimeMessageLevel.Error`.
- **`Displace` amplitude/frequency are optional inputs** (`pManager[i].Optional = true`, `double?`):
  omitting either defers to DualC's engine default (0.1 / 6) rather than hard-coding a GH-side default
  that could drift. `Offset`'s `Distance` and `Displace`'s `Amplitude` are world units, so their
  tooltips note "Normalize a raw TPMS first" (the metric convention).
- **Icons** — four new procedurally-drawn `BoletusIcons` glyphs (Offset = grown concentric outline;
  Twist = foreshortened rotating rungs; Bend = baseline warped into an arc; Displace = wavy surface
  over a baseline). The set is now **16** glyphs.

### Files changed (3b.3 domain-warp decorators)

| File | Change |
|---|---|
| `src/Boletus.Grasshopper/OffsetComponent.cs`, `TwistComponent.cs`, `BendComponent.cs`, `DisplaceComponent.cs` | **new** — the four decorator components (GUID suffixes `B0`/`C0`/`D0`/`E0`). |
| `src/Boletus.Grasshopper/AxisName.cs` | **new** — shared axis int→`"x"/"y"/"z"` map (Twist + Bend). |
| `src/Boletus.Grasshopper/BoletusIcons.cs` | +4 glyph getters + `Draw*` routines (16 total). |
| `docs/components.md`, `docs/roadmap/05`, `roadmap.md`, `CLAUDE.md` | recorded the increment + user reference. |

### Verification (3b.3 warps) — what is and isn't proven

- **Automated:** `dotnet build src/Boletus.Grasshopper` → **0/0**; suite still **83/83** (no Core
  touched — the four ops were already covered by the vocabulary tests). GUIDs grep-checked unique.
- **Sound by construction (not separately tested):** the dropdown label↔token maps are authored on
  both sides (`AddNamedValue("X",0)` ↔ `0→"x"`), and the tokens are the ones the vocabulary suite
  already round-trips through DualC — so the full chain (label → map → `Field.*` builder → tested JSON)
  has no hidden-index gap. The GH layer has no automated coverage (Rhino dependency).
- **Pending manual Rhino smoke:** confirm the four appear under **Boletus → Decorators** with icons,
  then `Primitive (box) → Twist → Contour` visibly twists (a bounded shape, not an infinite TPMS), and
  spot-check Bend / Offset / Displace (vary the axis/function dropdowns).

## Implemented 3b.3 (partial) — example graph (2026-07-03)

The fifth 3b.3 item is done: the canonical **`isolate → thicken → skin → union`** four-step
workflow (DualC `docs/command_reference/11-dualc_field/04-workflow-open-surface.md`) is composed and validated.

- **No new components — pure composition of the existing palette.** The four workflow verbs map
  onto components that already exist: **isolate** = `TPMS → Normalize → Onion(Boundary = Primitive
  Box)` (onion-before-clip → `intersection(onion(normalize(gyroid)), box)`); **thicken** = the
  `Onion` thickness (`Graded Onion` for the graded variant); **skin** = a *second* `Onion` on a
  `Primitive` Box with **no Normalize / no Boundary** (`onion(box, t)`; the box SDF is already
  metric); **union** = `Boolean` op Union bonding the two. The seat-strictly-inside refinement uses
  `Offset(box, −t)` as the lattice's boundary before the union.
- **"Validated" = an automatable Core round-trip** (the GH idiom — GH can't run headless). New
  `tests/Boletus.Core.Tests/FieldGraphExampleTests.cs` builds the full composition with the
  `Field.*` builders and asserts our JSON is **byte-identical to DualC's `--dump-json`** of the
  equivalent `--expr` (reusing the vocabulary suite's CLI-oracle pattern — CLI-gated, skips when the
  binary is absent). Two CLI-independent tests **always run**: a pure guard (validate has no errors +
  every expected op appears in the JSON) and an **end-to-end contour** (`FromJson` → `Contour` at
  depth 6 → non-empty mesh) that exercises Boletus's own serializer → native path regardless of the
  CLI (safe at depth 6 — the `onion(box)` skin is a solid shell that alone yields geometry). Suite
  **83 → 86** (three new tests).
- **Documented.** `docs/components.md` gained a second worked example ("a printable part") giving
  the per-component wiring table, the full field expression, and the `Offset` seat-inside note.
- **`demo.gh` untouched.** The user's untracked canvas from 2026-07-03 is left as-is; GH can't be
  driven headless, so no `.gh` is authored/committed here. The validation is the Core test + docs.

### Files changed (3b.3 example graph)

| File | Change |
|---|---|
| `tests/Boletus.Core.Tests/FieldGraphExampleTests.cs` | **new** — pure guard + CLI-gated `--dump-json` byte-match + end-to-end contour of the four-step composition (3 tests). |
| `docs/components.md` | **new** second worked example (isolate → thicken → skin → union) + `Offset` seat-inside refinement. |
| `docs/roadmap/05`, `roadmap.md`, `README.md`, `STRUCTURE.md`, `CLAUDE.md` | recorded the increment; test count 83 → 86. |

### Verification (3b.3 example graph)

- **Automated:** `dotnet test tests/Boletus.Core.Tests` → **86/86** (was 83). With `dualc_field.exe`
  present the round-trip asserts the byte-match; the pure guard **and the end-to-end contour** run
  regardless. No `Boletus.Core` source changed — all four ops were already `--dump-json`-gated per-op;
  this pins the *composition* (and that it contours to a real part).
- **Pending manual Rhino smoke (GH is headless):** reproduce the four-step canvas and confirm
  `Contour/Export` writes one watertight part (strut ends meet the skin's inner wall, no box faces).

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
