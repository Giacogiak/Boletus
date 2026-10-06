# 08 — Strut lattices (field-graph vocabulary & the Boletus components)

The strut-lattice node vocabulary DualC gained at commit `d6b2808` (2026-07-09) and how Boletus
surfaces it on the Grasshopper canvas. Everything here is **field-graph vocabulary** — emitted as
graph-string tokens through the *existing* `dualc_field_create_from_{json,expr}` C-ABI calls; no new
ABI functions, but a **rebuilt `dualc_capi.dll` (≥ `d6b2808`)** is required because the parser lives
inside the DLL (the upstream sync, the version trap, and the open asks are in
[07 §8](07-upstream-coordination/03-export-callback-and-strut-sync.md#8-strut-lattice-vocabulary-sync-d6b2808--done-boletus-side-2026-07-10)).

- **Boletus-side development record** (what was built, files changed, verification): the dated
  increment in [05 § Implemented — strut lattices](05-phase3-grasshopper-components/07-strut-lattice-components.md#implemented--strut-lattices-strut-lattice--graded-offset--mix-2026-07-10).
- **User-facing component guide** (inputs/outputs, where each belongs in the pipeline): the
  `Strut Lattice` / `Graded Offset` / `Mix` sections of the command reference
  ([01 § Strut Lattice](../command_reference/01-sources.md#strut-lattice),
  [02 § Graded Offset](../command_reference/02-decorators.md#graded-offset),
  [02 § Mix](../command_reference/02-decorators.md#mix)).
- **Authoritative DualC syntax & recipes:** `D:\DualC\docs\command_reference\11-dualc_field\02-strut-lattices.md`
  §"Strut lattices" and DualC roadmap `05-tpms-lattices/` #17/#17b; live checks
  `dualc_field --list` / `dualc_field --dump-json "<expr>"`.

> **Provenance.** This file integrates the former `docs/boletus-strut-nodes-reference.md` (the strut
> feature/parameter chart written for the GH component design) — folded into the roadmap structure
> and the loose file deleted on 2026-07-10. Types: `Field` = a child node (another field). All
> numbers are `double`; `center` is `[x,y,z]`; units are world units (mm).

## #20 Functionality map — which capability = which node

The strut feature is the item this file tracks; it keeps the ID **#20** for life (assigned at birth,
kept across files). The "Phase" column below is **DualC's** internal phasing of the feature.

| Capability | Mechanism (graph node) | New node? | DualC phase |
| --- | --- | --- | --- |
| Wireframe crystal lattice (4 types) | `sc` / `bcc` / `fcc` / `octet` **source** | **new** | #17 core |
| Tapered struts (fat joints, thin spans) | `nodeRadius` **param** on the source | **new param** | #17b Ph4 |
| Spatially-graded strut radius | `graded-offset(base, control)` **decorator** | **new** | #17b Ph3 |
| Crystal / radius value-morph | `mix(A, B, control)` **node** | **new** | #17b Ph5 |
| Hollow struts ("straws"/tubes) | `onion(strut, thickness=…)` (existing) | reuse | — |
| Clip to a solid (REQUIRED) | `intersection(box/mesh, strut)` (existing) | reuse | — |
| Hard per-region crystal swap | `union(intersection(halfbox,A), intersection(halfbox,B))` | reuse | — |
| Continuous cross-family solid | `smooth-union(A, B, k=…)` (existing) | reuse | — |

**Not needed:** `normalize` — a strut lattice is a true SDF and feeds an `Onion` / smooth
`Boolean` without one ([design 06 § Metric-by-default sources](../design/06-conventions.md#metric-by-default-sources-and-the-normalize-rule)).

## Source node — `sc` · `bcc` · `fcc` · `octet`

The core component. Same four parameters for every crystal; only the strut topology differs. Boletus
exposes them as **one `Strut Lattice` component with a crystal-type dropdown** (the consolidated
multi-mode convention, like `TPMS`/`Primitive`/`Boolean`).

| GH input | token param | type | default | meaning |
| --- | --- | --- | --- | --- |
| Center | `center` | Point3d → `[x,y,z]` | `[0,0,0]` | world position of a unit-cell centre (shifts the whole tiling) |
| Wavelength | `wavelength` | Number | `1` | unit-cell side = tiling period |
| Radius | `radius` | Number | `0.1` | strut **half-thickness** (beam is `2·radius` across) |
| NodeRadius | `nodeRadius` | Number (optional) | = `radius` | half-thickness at the strut **end-nodes** → tapered when ≠ `radius`; **omit for uniform** |

**Crystal topology** (for the type dropdown / tooltip):

| crystal | struts/cell | shape |
| --- | --- | --- |
| `sc` | 3 | simple cubic — 3 orthogonal axis rods |
| `bcc` | 8 | body-centred — centre node → 8 corners |
| `fcc` | 24 | face-centred — the 6 face "X"s |
| `octet` | 36 | octet truss — `fcc` + 12 octahedral edges |

**Hard rules the component enforces / warns about:** the infinite extent (the remark and the
two fixes), the `radius < wavelength/2` rule of thumb and the resolution rule — a cell must be
smaller than about the radius or thin struts fragment — are the component's page,
[command reference 01 § Strut Lattice](../command_reference/01-sources.md#strut-lattice); the
resolution rule itself is DualC's
([design 10](../../../DualC/docs/design/10-invariants-and-tolerances.md#the-resolution-rule-a-feature-is--23-cells-or-it-does-not-exist)).

## Tapered struts — `nodeRadius` (a param, not a node)

`nodeRadius ≠ radius` tapers each strut: `nodeRadius` thick at **both** end-nodes, pinching to
`radius` at mid-span (two symmetric round-cones per segment, split at the midpoint). Stays an exact
SDF.

| case | effect |
| --- | --- |
| `nodeRadius` omitted or `= radius` | **uniform** strut (byte-identical to pre-taper) |
| `nodeRadius > radius` (2–4×) | fat joints, thin spans — stress-aligned / printable / organic |
| `nodeRadius < radius` | inverse taper — thin nodes, fat mid-span (beaded look) |

- Thinnest part = `2·radius`; widest = `2·nodeRadius` (drives bounds growth).
- Keep `nodeRadius < wavelength/2` so neighbouring nodes don't fully merge.
- Both `radius` and `nodeRadius` are **live uniforms** in `dualc_field_view` — a GH slider drives
  them through the Live Preview side-car with **no shader recompile** (the deferred instant-scrub tier
  in [07 §6](07-upstream-coordination/02-viewer-and-uniform-push.md#6-real-time-parameter-push-uniform-ipc-channel--deferred-performance-optimization) is a further optimisation on top of this).

## Graded strut radius — `graded-offset(base, control)`

Two-child **decorator**: inflates `base` by an offset `t` that ramps across space according to a
`control` field. Local half-thickness becomes `radius + t(control)`. Use this (NOT `onion`) to vary a
**solid** strut's thickness — `onion` would hollow it into a tube. Boletus surfaces it as the
`Graded Offset` component (Decorators); `Field.GradedOffset` reuses `OpCategory.GradedOnion` (only the
base child must be metric).

Signature: `graded-offset(base, control; t1, t2, d0=0, d1)`

| GH input | token param | type | default | meaning |
| --- | --- | --- | --- | --- |
| Base | child `in[0]` | Field | (required) | the strut lattice to thicken |
| Control | child `in[1]` | Field | (required) | scalar field driving the grade (plane = axial, `sphere(radius=0)` = radial `|p|`, a mesh = distance-to-skin) |
| T1 | `t1` | Number | **required** | offset added where control = `d0` |
| T2 | `t2` | Number | **required** | offset added where control = `d1` |
| D0 | `d0` | Number | `0` | control value mapped to `t1` |
| D1 | `d1` | Number | **required** | control value mapped to `t2` |

- Offset ramps `t1 → t2` linearly as control goes `d0 → d1` (clamped outside).
- `t1 == t2` ⇒ a plain uniform offset (handy sanity check).
- Common controls: `plane(normal,offset)` (load-path axial), `sphere(radius=0)` (radial `|p|`), reuse
  the **clip mesh** as control (fatten toward the skin).

## Crystal / radius morph — `mix(A, B, control)`

Three-child **value-lerp** node: `value = lerp(A, B, w)`, where `w = clamp((control − lo)/(hi − lo))`.
Boletus surfaces it as the `Mix` component (Decorators); it is the **first arity-3 op** in the
codebase (`Field.Mix` reuses `OpCategory.HardBoolean` — `IsMetric` = A && B, the control child
ignored).

Signature: `mix(A, B, control; lo=0, hi)`

| GH input | token param | type | default | meaning |
| --- | --- | --- | --- | --- |
| A | child `in[0]` | Field | (required) | field at `w=0` (control ≤ `lo`) |
| B | child `in[1]` | Field | (required) | field at `w=1` (control ≥ `hi`) |
| Control | child `in[2]` | Field | (required) | scalar ramp field |
| Lo | `lo` | Number | `0` | control value where `w=0` |
| Hi | `hi` | Number | **required** | control value where `w=1` |

> ⚠️ **Critical caveat — surfaced in the `Mix` component (a `Remark`).** `mix` blends distance
> **values, not shapes**, so it is watertight only for a same-family radius morph and a cross-family
> morph splits at the mid-plane by design — the engine invariant is DualC's
> [design 10 § `mix` blends values, not shapes](../../../DualC/docs/design/10-invariants-and-tolerances.md#mix-blends-values-not-shapes);
> what to use instead (a smooth `Boolean`, a clip + `Union`, `Graded Offset`) is
> [command reference 02 § Mix](../command_reference/02-decorators.md#mix).

## Compose-order cheatsheet (how the nodes nest)

```
intersection(                       ← REQUIRED clip (box or mesh) → finite solid
  box(min=…,max=…) | mesh(id=…),
  graded-offset(                    ← optional: grade the radius across space
    octet(wavelength=…, radius=…,   ← the strut source (+ optional nodeRadius taper)
          nodeRadius=…),
    sphere(radius=0),               ← control field (radial | plane | mesh)
    t1=…, t2=…, d0=…, d1=… ))
```
Hollow variant: wrap the strut in `onion(strut, thickness=…)` instead of / inside `graded-offset`.
`normalize` is never used with struts.

## Minimal `--expr` exemplars (the Core round-trip test cases)

These are the exprs pinned by `tests/Boletus.Core.Tests/FieldGraphVocabularyTests.cs` — each is
round-tripped byte-for-byte against DualC's `--dump-json`, and the clipped forms are contoured
end-to-end (`Clipped_bcc…`, `Clipped_graded_offset…`, `Clipped_mix…`).

| goal | expr |
| --- | --- |
| BCC in a box | `intersection(box(min=[-1,-1,-1],max=[1,1,1]),bcc(wavelength=0.5,radius=0.05))` |
| Tapered octet | `intersection(box(min=[-1,-1,-1],max=[1,1,1]),octet(wavelength=0.5,radius=0.02,nodeRadius=0.06))` |
| Radial graded BCC | `intersection(box(min=[-1,-1,-1],max=[1,1,1]),graded-offset(bcc(wavelength=0.4,radius=0.02),sphere(radius=0),t1=0.0,t2=0.06,d0=0.0,d1=1.0))` |
| Same-family radius morph | `intersection(box(min=[-1,-1,-1],max=[1,1,1]),mix(bcc(wavelength=0.5,radius=0.03),bcc(wavelength=0.5,radius=0.09),plane(1,0,0,0),lo=-0.6,hi=0.6))` |
| Hollow struts | `intersection(mesh(id="part"),onion(bcc(wavelength=0.5,radius=0.12),thickness=0.03))` |

Verify each with `dualc_field --dump-json "<expr>"` (canonical JSON) and `dualc_field --list` (full
live vocabulary). **Note the `mix` control is `plane(1,0,0,0)` (positional)**, not the grouped
`plane(normal=…,offset=…)` form: DualC's `--dump-json` is representation-preserving, so the emitted
graph must use the same plane form as `Field.Plane` for the round-trip to byte-match.

## Boletus-side implementation

Surfaced 2026-07-10 as three GH components (`Strut Lattice`, `Graded Offset`, `Mix`) over new
`Ops.cs` schemas + `Field.cs` builders — **no serializer/validator change** (categories reused: strut
= `MetricSource`, `graded-offset` = `GradedOnion`, `mix` = `HardBoolean`). The only native step was
re-vendoring `dualc_capi.dll` from `d6b2808`. Full record — files changed, verification (117/117
Core tests), the manual-Rhino-smoke gate — in
[05 § Implemented — strut lattices](05-phase3-grasshopper-components/07-strut-lattice-components.md#implemented--strut-lattices-strut-lattice--graded-offset--mix-2026-07-10);
upstream sync + open asks in [07 §8](07-upstream-coordination/03-export-callback-and-strut-sync.md#8-strut-lattice-vocabulary-sync-d6b2808--done-boletus-side-2026-07-10).

*(2026-09-21, roadmap 09 Phase 4: the Normalize rule, the component's hard rules and the `mix`
caveat that stood here in full are links to their homes — the design layer, DualC's design 10 and
the command reference; the node tables stay as the vocabulary pinned at `d6b2808`.)*

---

← Back to the [Roadmap index](README.md).
