# 04 — Phase 3a: Field-graph model & serializer

> Detail for the [roadmap](README.md). **Status: DONE** — the model, canonical serializer, typed
> builders, schema table and validator landed and are verified, and the op vocabulary has been
> **fanned out to the full ~60-op set** (2026-06-18; see
> [Implemented](#implemented-phase-3a--2026-06-18) below). Every op — all 30 analytic
> primitives, 6 TPMS, `mesh`/`winding`, the 8 booleans/graded, and the 14 decorators/domain ops —
> is gated by a `--dump-json` round-trip against DualC. The managed field-graph model and its
> canonical-JSON serializer are the real work of Boletus, because every GH component emits a node
> into this model and the canvas serializes to the one string the
> [wrapper](03-phase2-core-wrapper.md) sends to DualC.

## Implemented (Phase 3a — 2026-06-18)

All in `src/Boletus.Core/FieldGraph/` (namespace `Boletus.Core.FieldGraph`), Rhino-free,
**no NuGet dependency** (hand-rolled emitter), netstandard2.0.

### Files

| File | Role |
|---|---|
| `FieldValue.cs` | Immutable param value: `Scalar(double)` / `Vector(IReadOnlyList<double>)` / `Text(string)`, with value equality. (Flat `params` is a `Vector` under key `"params"`.) |
| `FieldNode.cs` | Immutable node: `Op` token, `Params` bag, ordered `Children`. Defensive copies → genuinely immutable. `ToJson()` serializes as a document root. |
| `FieldGraphSerializer.cs` | Hand-rolled emitter producing `{ "version":1, "units":"mm", "root":… }`. **Byte-matches** DualC's `--dump-json`: per node `op` first → params **ASCII-ordinal-sorted** (`StringComparer.Ordinal`, matches nlohmann `std::map`) → `in` last; doubles via invariant shortest round-trip with a **forced `.0`** when integral; JSON string escaping (`\`,`"`, controls). |
| `OpSchema.cs` | `OpSchema` (token, child arity, `OpCategory`, params), `ParamSpec` (name, `ParamKind`, required, vector length), and the `OpCategory`/`ParamKind` enums. |
| `Ops.cs` | The pinned op registry (`Ops.TryGet`) — the **full ~60-op vocabulary** (grouped-key + flat-`params` primitives via the `FlatParams(n)` helper, all booleans/graded, all decorators/domain ops). |
| `Field.cs` | Typed fluent builders (e.g. `Field.Gyroid(wavelength)`, `.Normalize()`, `.Onion(child, t)`, `Field.Intersection(a,b)`, `Field.Mesh(path)` / `Field.Mesh(id: …)`). Optional params are nullable → emitted only when supplied (mirrors `--dump-json`, which never fills defaults). |
| `FieldGraphValidator.cs` | `Validate` (all issues) / `ValidateOrThrow` (errors only) + `IsMetric`. `ValidationIssue` carries a DualC-style JSON-pointer `Path`, a `Message`, and a `ValidationSeverity` (Error/Warning). Also enforces **exactly-one-of `path`/`id`** on `mesh`/`winding`. |
| `FieldTree.cs` | *(added with the Phase 3b diskless flip — [05](05-phase3-grasshopper-components/README.md))* Generic bottom-up immutable tree rewrite (`Rewrite(node, map)` + `CopyParams`); shared by `VolumeResolver`, unit-tested directly. |

### Op coverage (full ~60-op vocabulary)

- **Sources:** 6 TPMS (`gyroid`, `schwarz-p`, `diamond`, `fischer-koch`, `lidinoid`, `neovius`),
  `mesh`, `winding`, and all **30 analytic primitives** — the 7 grouped-key ones (`sphere`, `box`,
  `roundbox`, `capsule`, `cappedcylinder`, `torus`, `ellipsoid`) and the other 23 via the universal
  flat `params:[…]` positional array (`plane`, `boxframe`, `cone`, `cappedcone`, `roundcone`,
  `infinitecylinder`, `hexprism`, `triprism`, `octahedron`, `pyramid`, `solidangle`, `cappedtorus`,
  `link`, `cutsphere`, `cuthollowsphere`, `deathstar`, `vesica`, `rhombus`, `verticalcapsule`,
  `roundedcylinder`, `triangle`, `quad`, `infinitecone`).
- **Booleans + graded (all 8):** `union`, `intersection`, `difference`, `xor`, `smooth-union`,
  `smooth-intersection`, `smooth-difference`, `graded-onion`.
- **Decorators + domain ops (all 14, 1 child):** `normalize`, `onion`, `offset`, `round` (alias),
  `scale`, `translate`, `rotate`, `elongate`, `transform`, `twist`, `bend`, `mirror`, `repeat`,
  `repeat-limited`, `displace`.

**Pinning notes (all verified by `--dump-json`):** flat-`params` primitives map positionally onto
the order in `02-dualc_primitive.md`; each builder names those positions for call-site clarity and
packs them into a single `params` vector. **Coverage caveat:** the round-trip oracle certifies the
emitted *positions/values*, not the C# builder *argument names* (a flat builder collapses its named
args into a positional array, so a transposed pair would still round-trip). Those names are pinned
from `02-dualc_primitive.md` and cross-checked by reading, not by the gate. Every primitive param has a DualC default, so all are
modelled **optional** (a bare `roundbox()` is valid). The one per-op type quirk: `twist`/`bend`
take `axis` as a **string** (`"x"`/`"y"`/`"z"`), unlike `rotate`'s vector `axis`. `plane`,
`infinitecylinder`, `infinitecone`, `repeat` are unbounded (bounds are a contour-time flag, not a
node param); `triangle`/`quad` are open surfaces (need an `onion` wrap).

### Pinned op signatures (as built — verified by `--dump-json`)

The per-op tables that stood here — every token's exact emitted shape, its parameter keys and
types, and the `Field` builder that emits it — are the command reference's
[05 — Field-graph ops](../command_reference/05-field-graph-ops.md), where they are kept true
against the source; the parameter meanings and defaults are DualC's
([11/01](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md),
[02](../../../DualC/docs/command_reference/02-dualc_primitive.md)). They were pinned from
`dualc_field --list` + `02-dualc_primitive.md` and gated per op by a `--dump-json` round-trip
(`FieldGraphVocabularyTests`); param key order is irrelevant on input (the serializer
ASCII-ordinal-sorts), types and — for flat primitives — positional order are load-bearing. The
`id` form of `mesh` / `winding` was added with the Phase 3b diskless flip
([05](05-phase3-grasshopper-components/README.md) / [07 § 1](07-upstream-coordination/01-in-memory-mesh.md)).
*(2026-09-21, roadmap 09 Phase 4: the tables moved to the usage layer; this page keeps the
heading as the anchor and the record of how they were pinned.)*

### Key design decisions

- **Generic param bag + schema table**, not strongly-typed per-op records — absorbs DualC
  vocabulary growth without new C# types (resolves the [open question](#open-questions) below).
- **All primitive params are optional** (nullable builder args; flat `params` modelled `required:
  false`). Every DualC primitive param has a default, so a bare `roundbox()` is valid — matching
  DualC's own behaviour and the "errors only on what DualC rejects" rule. (Decorators/domain ops
  keep genuinely **required** params, like the existing `onion`/`rotate`.) The `FlatParams(n)`
  schema helper still pins each flat primitive's element **count** as a validator guard.
- **Named builder per flat primitive** (e.g. `Field.Cone(cx, cy, cz, angleRad, height)`), not a
  generic positional escape hatch — best Grasshopper DX. Each names its positions for the caller,
  then packs them via the private `Flat(op, …)` helper into the single `params` array DualC sees.
- **`triangle`/`quad` categorised `MetricSource`** (so `IsMetric`→true, no `onion` warning). They
  are *unsigned* distance (open surfaces, |∇|=1 but no inside), and `onion` is exactly how they
  gain thickness — so suppressing the warning is intended. A judgment call (noted in `Ops.cs`),
  not confirmed against DualC's own metric-ness view.
- **Tree-only, emit-only.** No JSON *reader* in `Boletus.Core` (DualC parses our output);
  DAG-ref form deferred upstream ([07 § 3](07-upstream-coordination/README.md#3-dag-ref-graph-serialization--optional-optimization)).
- **Metric-ness is a *warning*, not an error.** DualC accepts and contours a non-metric source
  feeding `onion`/`graded-onion`/smooth-boolean — the project's golden-count gate
  (`onion(gyroid(wavelength=0.5),thickness=0.12)`) is exactly such a graph (verified: exits 0,
  contours). So `ValidateOrThrow` throws only on schema **errors** (arity, missing/unknown/
  mistyped params, unknown op — what DualC itself rejects); metric-ness surfaces as an
  actionable warning ("wrap in `normalize` for a metric wall thickness").

### Verification (landed)

`tests/Boletus.Core.Tests/FieldGraphTests.cs` + `FieldGraphValidatorTests.cs` +
`FieldGraphVocabularyTests.cs` — **full suite 70/70 green** (CLI tests skip when the binary is
absent; 0 skipped when present):

- Both shipped fixtures (`gyroid_box.json`, `mesh_lattice.json`) rebuilt from the managed
  builders and **round-tripped through `dualc_field --dump-json`** (the semantic-equality oracle).
- **Every fanned-out op** (all 30 primitives + the 8 domain ops) round-trips through `--dump-json`
  against its equivalent `--expr` — a data-driven `[Theory]` in `FieldGraphVocabularyTests` that
  pins each flat-`params` positional count, each grouped key, and each per-op param type.
- Targeted guards: a flat-`params` primitive emits a positional array of the right length;
  `twist.axis` emits a quoted **string** (not a vector); `transform` rejects a non-16 matrix; a new
  primitive (`torus`) contours to a non-empty mesh at `maxDepth=6`.
- Emitter output **byte-matches** `--dump-json`'s `dump(2)` on the fixture (secondary, format
  check; the round-trip oracle is the general gate, since float formatting is *not* load-bearing).
- Backslash mesh path (the Rhino temp-file case) is escaped to `\\` and parsed by DualC.
- Validator: arity / required / unknown-op / unknown-param **errors** (incl. on new ops: a missing
  required domain-op param, a wrong-length flat `params`); metric-ness **warnings** (incl.
  `graded-onion` checking only the base child, hard-boolean metric propagation);
  `ValidateOrThrow` throws on errors only.

## Why this is the keystone

The C ABI has no per-op factories: composition happens by **building a field-graph string**
(see [01](01-architecture-and-contract.md)). Grasshopper is itself a node DAG, so the
natural mapping is: each GH component creates one managed field-graph node; wiring
components together builds the tree; the terminal component serializes the tree to
canonical JSON and hands it to `DualcField.FromJson`. The serializer's correctness — op
tokens, parameter names, defaults, vector grouping, child arity — *is* the correctness of
every downstream component.

## Scope

Lives in `Boletus.Core` (Rhino-free) so it round-trips in plain unit tests with no Rhino
host.

1. **Node model.** An immutable `FieldNode` with: op token (string), a parameter bag
   (named scalars / vectors), and ordered children. Arity rules: sources (0 children),
   decorators/domain-ops (1), booleans + `graded-onion` (2).
2. **Canonical-JSON serializer.** Emit `{ "version":1, "units":"mm", "root": <node> }`
   where each node is `{ "op": <token>, <params…>, "in": [<children>] }`. Must match
   DualC's `--dump-json` output **exactly** (tokens hyphenated, vectors as grouped keys).
3. **Typed builders** for the full op set (a fluent/factory surface the GH components call),
   so a component never hand-writes JSON.
4. **Validation** with actionable messages (missing required param, wrong arity,
   non-metric source feeding a smooth boolean / `onion` without `normalize`).

## Pinning op signatures (the work the wrapper deferred)

The wrapper passes strings, so this is where op names / parameter order / defaults must be
**pinned from authoritative sources**, in order:

1. `D:\DualC\docs\command_reference\11-dualc_field\01-op-vocabulary.md` (field-graph vocabulary, the
   `--expr` grammar) and `02-dualc_primitive.md` (the 30 primitives' parameter order).
2. `dualc_field --list` (prints the live op vocabulary).
3. `dualc_field <graph> --dump-json` (canonicalises any input — the round-trip oracle).

**Do not** copy parameter tables from memory or prior summaries into code; pin from the
above at implementation time.

## Vocabulary (arity overview)

> The exact per-op signatures **as built** are pinned in
> [Pinned op signatures](#pinned-op-signatures-as-built--verified-by---dump-json) above; this
> section is the original arity-level overview that framed the work.

~60 ops by arity:

- **Sources (38):** 30 analytic primitives + 6 TPMS (`gyroid`, `schwarz-p`, `diamond`,
  `fischer-koch`, `lidinoid`, `neovius`) + `mesh` + `winding`.
- **Decorators / domain ops (14, 1 child):** `offset`(=`round`), `onion`, `scale`,
  `elongate`, `translate`, `rotate`, `transform`, `normalize`; `twist`, `bend`, `mirror`,
  `repeat`, `repeat-limited`, `displace`.
- **Booleans + graded (8, 2 children):** `union`, `intersection`, `difference`, `xor`,
  `smooth-union`, `smooth-intersection`, `smooth-difference`; `graded-onion` (base +
  control).

Semantics the serializer/validator must enforce: **non-metric** sources (raw TPMS,
`winding`) need a `normalize` wrap before `onion`/`graded-onion`/smooth booleans;
**unbounded** graphs (`plane`, infinite primitives, `repeat`) require explicit bounds at
contour time (an invocation flag, not a node); sampling settings (`depth`/`collapse`/
`bounds`/`tile-depth`) are **not** graph nodes.

## Verification (methodology — executed; results in [Verification (landed)](#verification-landed))

This is the strategy the implementation followed; the concrete landed results (suite 70/70,
per-op coverage) are recorded under [Verification (landed)](#verification-landed) above.

- **Per-op round-trip (incremental, not big-bang):** for each op, emit the managed JSON and diff
  against `dualc_field <same> --dump-json`. Must match node-for-node. *(Done — the
  `FieldGraphVocabularyTests` `[Theory]` covers all 30 primitives + 8 domain ops.)*
- **Acceptance baseline:** feed the managed JSON to `DualcField.FromJson` + `Contour` — if the
  same parser accepts it and produces a sane mesh, the JSON is valid (the `--dump-json` diff is
  the stronger check). *(Done — `torus` contours non-empty at `maxDepth=6`.)*
- **Fixtures:** reproduce the shipped sample graphs `gyroid_box.json` and `mesh_lattice.json`
  (`D:\DualC\examples\samples\`) from the managed builders and diff. *(Done — `FieldGraphTests`.)*
- All Rhino-free → runs in `Boletus.Core.Tests`.

## Open questions

- ~~Strongly-typed per-op record vs. generic bag + schemas.~~ **Resolved (2026-06-18):** generic
  bag (`FieldNode.Params`) + a per-op `OpSchema` table (`Ops`), to absorb DualC vocabulary
  growth without new C# types.
- How to represent the eventual **DAG-ref** form (shared sub-fields) — tree-only is valid
  now; see [07 § 3](07-upstream-coordination/README.md#3-dag-ref-graph-serialization--optional-optimization).

---

← Back to the [roadmap](README.md) · prev: [03 — Phase 2 wrapper](03-phase2-core-wrapper.md) · next: [05 — Grasshopper components](05-phase3-grasshopper-components/README.md)
