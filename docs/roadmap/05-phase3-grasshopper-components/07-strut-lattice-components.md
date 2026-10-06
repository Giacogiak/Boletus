# 05 — Phase 3b: The strut-lattice components and the remaining increment

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`; every heading is verbatim from the page this folder replaced (split 2026-09-21, roadmap 09 Phase 4).

## Implemented — strut lattices (`Strut Lattice` / `Graded Offset` / `Mix`) (2026-07-10)

Surfaced DualC's new **strut-lattice vocabulary** (upstream commit `d6b2808`, 2026-07-09) as three
GH components. **The C ABI was unchanged** (`git log e345bf3..d6b2808 -- capi/` is empty) — the only
native step was **re-vendoring `dualc_capi.dll` from `d6b2808`**, because the field-graph *parser* is
compiled inside the DLL and the `e345bf3` DLL rejects `bcc(…)` etc. with `DUALC_ERR_GRAPH`. No
P/Invoke, struct, or marshaling change; the serializer needed no change either (it already sorts
params ASCII-ordinal, omits absent params, and loops any child count). See the sync record in
[07 §8](../07-upstream-coordination/03-export-callback-and-strut-sync.md#8-strut-lattice-vocabulary-sync-d6b2808--done-boletus-side-2026-07-10)
and the provenance in [`native/README.md`](../../../native/README.md).

> **Full node/param reference** — every crystal's params & defaults, the `nodeRadius` taper, the
> `graded-offset` / `mix` signatures with the value-blend caveat, the compose-order cheatsheet, and
> the `--expr` exemplars — lives in **[08 — Strut lattices](../08-strut-lattices.md)** (the folded-in
> strut-nodes reference). The user-facing component guide is in the command reference
> ([01 § Strut Lattice](../../command_reference/01-sources.md#strut-lattice), [02 § Graded Offset](../../command_reference/02-decorators.md#graded-offset),
> [02 § Mix](../../command_reference/02-decorators.md#mix); *2026-09-21, roadmap 09 Phase 4: it was `docs/components.md`, deleted*).
> This section is the **development record** of the Boletus-side increment only.

### What was built

- **`Strut Lattice`** (Sources) — crystal dropdown `sc`/`bcc`/`fcc`/`octet` + `Center` (opt) /
  `Wavelength` (opt) / `Radius` (0.1) / `NodeRadius` (opt → tapered struts). Emits the `sc/bcc/fcc/
  octet` source. **Metric** (`OpCategory.MetricSource` — a true SDF, unlike TPMS: **no Normalize**),
  **infinite** (posts an infinite-extent `Remark`, mirroring `Primitive`'s plane).
- **`Graded Offset`** (Decorators) — base + control Volumes + `t1`/`t2`/`d1`/opt `d0` → `graded-offset`.
  Grades a **solid** strut radius across space (the strut-radius companion to `Onion`, which would
  hollow the struts). `Field.GradedOffset` reuses `OpCategory.GradedOnion` (base-child-must-be-metric).
- **`Mix`** (Decorators) — three Volumes (A, B, control) + `hi`/opt `lo` → `mix` value-lerp morph
  (the **first arity-3 op**). Watertight only for a same-family radius morph; posts a value-blend
  `Remark`. `Field.Mix` reuses `OpCategory.HardBoolean` (`IsMetric` = A && B, control ignored).
- **Core:** `Ops.cs` registrations (strut loop + `graded-offset` + `mix`) and `Field.cs` builders
  (`Sc`/`Bcc`/`Fcc`/`Octet` + shared `Strut` helper, `GradedOffset`, `Mix`). No validator/serializer edits.
- **Icons:** `BoletusIcons.Strut` (bcc node-and-edge cell), `GradedOffset` (graded-thickness struts),
  `Mix` (thin→thick cell morph). Glyph count 17 → **20**.

### Files changed (strut lattices)

| File | Change |
|---|---|
| `native/x64/dualc_capi.dll`, `dualc_field_view.exe` | **re-vendored** from DualC `d6b2808` (viewer also gains adaptive render-scale + metric-SDF fast-path + discrete-GPU auto-select). |
| `native/README.md` | provenance tables → `d6b2808`; documented the **version trap** (`dualc_version()` still `"dualc 0.3.0"` for both DLLs) and the parser-in-DLL reason. |
| `src/Boletus.Core/FieldGraph/Ops.cs`, `Field.cs` | new op schemas + builders (categories reused, no validator/serializer change). |
| `src/Boletus.Grasshopper/StrutLatticeComponent.cs`, `GradedOffsetComponent.cs`, `MixComponent.cs` | **new** components (GUID suffixes `…021`, `…0F0`, `…100`). |
| `src/Boletus.Grasshopper/BoletusIcons.cs` | 3 new procedural glyphs + accessors. |
| `tests/Boletus.Core.Tests/FieldGraphVocabularyTests.cs` | 7 new round-trip cases + `nodeRadius` omit/emit + **three** clipped contour-acceptance tests (bcc / graded-offset / mix — parse ≠ contour, and `mix` is the first arity-3 op) + graded-offset/mix required-param & arity guards. |
| `docs/components.md`, `docs/roadmap/05`, `07`, `roadmap.md`, `README.md`, `CLAUDE.md` | recorded the increment; current-state test count 104 → **117**. |

### Verification (strut lattices)

- **Automated:** `dotnet build Boletus.sln` → **0/0**; `dotnet test tests/Boletus.Core.Tests` →
  **117/117** (was 104). The **regression gate** — the full 104 pre-existing tests pass unchanged
  with the re-vendored DLL, incl. the golden counts (101476/163740, 70032/120612) — proves the DLL
  swap is a clean drop-in. The 7 new round-trips **byte-match** DualC's own `--dump-json` for every
  new node (the emission/marshaling gate); three clipped acceptance tests **contour** `bcc`,
  `graded-offset`, and `mix` end-to-end through the new DLL (parse ≠ contour — and `mix` is the first
  arity-3 op, the child-traversal case the 0/2-child paths never exercised).
- **Pending manual Rhino smoke (no unit test covers the GH SolveInstance path — same precedent as
  every component):** drop `Strut Lattice` (bcc) → `Boolean(Intersection)` with a `Primitive` box →
  `Proxy preview` / `Live Preview`; confirm the lattice renders, the infinite-extent remark fires,
  `Graded Offset` visibly grades the radius (sphere control), and `Mix` morphs (and shows its gap on
  a cross-family blend); export a tiled STL via `Write to File`.

### Next increment (3b.3 — remaining, PLANNED)

Broader `Primitive` set (only 5 of 29 Core primitives are exposed — carries a design fork: the
current 4-input `A/B/N0/N1` model can't hold the heterogeneous params of the other 24, so it needs a
decision on more inputs vs. `IGH_VariableParameterComponent` vs. splitting components); and
(optional) the **adaptive triangle-budget** refinement on the proxy cap. True live lattice preview is
the **raymarch side-car** phase (see the [roadmap](../README.md)).

*2026-10-03 — the broader `Primitive` set landed; the fork was decided by reading the
parameter shapes, as this entry asked: [08](08-broader-primitive-set.md) (D-25 settled,
D-45 opened). The adaptive triangle budget stays with D-19.*

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
