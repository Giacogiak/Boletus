# 05 — Phase 3b: The broader `Primitive` set

Part of [05 — Phase 3b: Grasshopper components](README.md), the record of the `Boletus.Grasshopper` `.gha`. The increment [07 § Next increment](07-strut-lattice-components.md#next-increment-3b3--remaining-planned) left open, closed on 2026-10-03 — the last of 3b.3.

## Implemented — the broader `Primitive` set

2026-10-03. Every DualC solid primitive is on the canvas. Before this session `Primitive` exposed five of
the twenty-nine builders `Field.cs` has carried since Phase 3a; the other twenty-four sat
behind the fork [07 § Next increment](07-strut-lattice-components.md#next-increment-3b3--remaining-planned)
named — the fixed `A / B / N0 / N1` model cannot hold their parameters. The fork was
decided by reading the parameter shapes, as that entry asked, and the twenty-two solids
landed in one session on Linux; the two open surfaces stay out (below). The usage contract
is [cmdref 01 § Primitive](../../command_reference/01-sources.md#primitive), [§ Segment
Primitive](../../command_reference/01-sources.md#segment-primitive) and [§ Axial
Primitive](../../command_reference/01-sources.md#axial-primitive); the mechanism is
[design 04 § The component families](../../design/04-grasshopper-plugin.md#the-component-families).
This page is the development record only.

### What was built

- **`Primitive`** gained **BoxFrame (5)** and **Ellipsoid (6)** — the two remaining shapes
  that fit two points and two numbers (BoxFrame: `A` / `B` the box, `N1` the edge; Ellipsoid:
  `A` the centre, `B` the radii). Indices 0–4 unchanged, so a saved canvas is unaffected.
- **`PrimitiveCatalog`** (Core, Rhino-free) — the two new families as data: per shape, the
  name and tooltip each fixed slot takes, the engine default an empty number falls back to,
  whether a slot is an angle (entered in degrees, converted to radians at build), whether the
  shape is unbounded, and the `Field` builder it calls. A family's dropdown index is an ID:
  the constructor throws if a shape sits anywhere but at its index, so a shape is only ever
  appended, never reordered.
- **`PrimitiveFamilyComponent`** — the shared shell: a type dropdown, the family's point and
  number slots. The slot count never changes; after each solve the slots are **relabelled**
  (name, nickname, tooltip) after the one type seen, the message bar shows its name, an
  unused slot says `Ignored by …`. A list of types keeps the generic labels and shows
  `(mixed)`. The unbounded shapes post the same remark shape `Plane` does.
- **`Segment Primitive`** (`…041`, five shapes over `A` / `B` / `R0` / `R1`) and **`Axial
  Primitive`** (`…042`, sixteen shapes over `Center` / `P0`–`P3`), two procedural glyphs
  (a diagonal capsule with its end points; a cone on a dashed axis). Glyph count 20 → 22.
- The dropdown tooltips list every option with its index, as every multi-choice input does.

### The fork (D-25) and what stays out (D-45)

Three options were on the table; the parameter shapes decided it.

| Approach | Why not |
| --- | --- |
| **Split by parameter shape** — chosen | Twenty-two shapes fall into exactly three rows: two points + ≤ 2 numbers (the existing model, plus BoxFrame and Ellipsoid), two points + ≤ 2 lengths (five), one point + ≤ 4 numbers (sixteen). Fixed inputs, so a saved canvas never loses a wire; the relabelling gives the canvas the real parameter names; consistent with D-22, which refused variable parameters for `Deform`. |
| One wider `Primitive` (`A`–`D`, `N0`–`N3`, twenty-nine types) | The simplest code and the hardest canvas to read: nine slots, every one reinterpreted per type, most of them dead for most types. |
| `IGH_VariableParameterComponent`, the inputs rebuilt per type | Reads best, but changing the type breaks wires — and it is the machinery D-22 dropped. |

**D-45** — `triangle` and `quad` stay out. Both are open surfaces: an unsigned distance, no
inside, so on their own they contour to nothing and need an `Offset` or `Onion` to become a
solid plate. A plate the palette already expresses is a `Primitive` Box / RoundBox or an
`Axial Primitive` Rhombus under a `Transform`. DEFERRED on a canvas that needs a thin polygon
plate those cannot express ([decisions](../../decisions/README.md)).

Two things the record keeps for the next source component: `Axial Primitive`'s shapes stand
on the engine's own axis (Y for most, Z for the prisms) — the component says so and points to
`Transform` rather than growing an orientation input; and [cmdref 01](../../command_reference/01-sources.md)
closed this session a few words under the size cap — the next source component splits it
into a `01-sources/` folder (principle 5).

### Files changed (broader Primitive set)

| File | Change |
|---|---|
| `src/Boletus.Core/FieldGraph/PrimitiveCatalog.cs` | **new** — `PrimitiveSlotUse`, `PrimitiveShape`, `PrimitiveFamily`, the `Segment` and `Axial` catalogs. |
| `src/Boletus.Grasshopper/PrimitiveFamilyComponent.cs`, `SegmentPrimitiveComponent.cs`, `AxialPrimitiveComponent.cs` | **new** — the shell and the two components (GUID suffixes `…041`, `…042`). |
| `src/Boletus.Grasshopper/PrimitiveComponent.cs` | BoxFrame (5), Ellipsoid (6); the dropdown tooltip lists every option. |
| `src/Boletus.Grasshopper/BoletusIcons.cs` | `SegmentPrimitive`, `AxialPrimitive` glyphs. |
| `tests/Boletus.Core.Tests/PrimitiveCatalogTests.cs` | **new** — 45 tests (below). |
| `scripts/check_data.json` | the test floor 117 → 162. |
| `docs/command_reference/00`, `01`, `05`, `README`; `docs/design/04`, `06`; `docs/decisions/`; `docs/roadmap/05/`, `README`; `STRUCTURE.md` | the contract, the mechanism, D-25 settled / D-45 opened, this record, the snapshot. |

### Verification (broader Primitive set)

- **Automated, on Linux** (D-44): `dotnet build Boletus.sln` → **0 warnings**;
  `dotnet test` → **162/162** (was 117): 21 *builder* theories — each catalog shape built
  from explicit inputs is byte-identical to the `Field` builder it must call, pinning the
  slot → positional order and the degree → radian conversion; 21 *contour* theories — every
  shape at the component's defaults, through `libdualc_capi.so`, yields a non-empty mesh
  (the two infinite ones under explicit bounds), so no default the engine rejects reaches
  the canvas; the index-is-position, unique-name, empty-slot-takes-default and
  only-two-infinite guards. The full gate `python3 scripts/check.py` green.
- **Pending manual Rhino smoke** (no unit test covers the GH path; a Windows-machine step):
  `Axial Primitive` through its sixteen types — the slots relabel, the message bar follows,
  the number slots show *Ignored by …* past the type's count, an empty slot gives the engine
  default; a list of types on `T` shows `(mixed)` with the generic labels and no exception
  from the relabelling after the solve; `Segment Primitive` Capsule between two Rhino points
  → `Boolean` Union → `Proxy preview`; `Primitive` Ellipsoid and BoxFrame; InfiniteCylinder
  posts its remark and contours under terminal bounds; a canvas saved with the type set to
  Rhombus reopens with the Rhombus labels.

---

← Back to the [Phase 3b index](README.md) · the [Roadmap index](../README.md).
