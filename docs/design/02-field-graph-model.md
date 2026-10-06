# The field-graph model and its serializer

The managed mirror of DualC's field graph — the keystone of the plugin. Every Grasshopper
component emits one node into this model; the canvas serializes to the one string the
wrapper sends to DualC; so the serializer's correctness — op tokens, parameter names, vector
grouping, child arity — *is* the correctness of every component. It lives in
`src/Boletus.Core/FieldGraph/` (namespace `Boletus.Core.FieldGraph`), Rhino-free, with no
NuGet dependency (the JSON emitter is hand-rolled), on netstandard2.0. The vocabulary itself
is DualC's: the op tokens, their parameters and the `--expr` grammar are
[`docs/command_reference/11-dualc_field/01-op-vocabulary.md`](../../../DualC/docs/command_reference/11-dualc_field/01-op-vocabulary.md),
the primitives' positional parameter order is
[`02-dualc_primitive.md`](../../../DualC/docs/command_reference/02-dualc_primitive.md), and
`dualc_field --list` prints the live set.

## The files

| File | Role |
| --- | --- |
| `FieldValue.cs` | Immutable parameter value — `Scalar(double)`, `Vector(IReadOnlyList<double>)`, `Text(string)` — with value equality. A flat `params` array is a `Vector` under the key `"params"`. |
| `FieldNode.cs` | Immutable node: `Op` token, `Params` bag, ordered `Children`. Defensive copies make it genuinely immutable; `ToJson()` serializes it as a document root. |
| `FieldGraphSerializer.cs` | The canonical-JSON emitter, held to a byte match with DualC's `--dump-json` ([§ The byte-match rules](#the-byte-match-rules)). |
| `OpSchema.cs` | `OpSchema` (token, child arity, `OpCategory`, `ParamSpec`s), `ParamSpec` (name, `ParamKind`, required, vector length) and the `OpCategory` / `ParamKind` enums. |
| `Ops.cs` | The pinned op registry, `Ops.TryGet(token)`: one schema per op of the vocabulary (`Ops.Tokens` lists every registered token), the flat primitives through the `FlatParams(n)` helper that pins each one's element count. |
| `Field.cs` | Typed static builders — `Field.Gyroid(wavelength)`, `Field.Normalize(child)`, `Field.Onion(child, t)`, `Field.Intersection(a, b)`, `Field.Mesh(path)` / `Field.Mesh(id: …)`, `Field.Bcc(…)`, `Field.Mix(…)` — so a component never hand-writes JSON. Optional parameters are nullable and emitted only when supplied. |
| `PrimitiveCatalog.cs` | The Rhino-free catalog behind `Segment Primitive` and `Axial Primitive`: one entry per shape with its fixed-slot labels, engine defaults and the `Field` builder it calls; an entry's index is its ID, so a shape is only ever appended (the constructor throws otherwise). |
| `FieldGraphValidator.cs` | `Validate` (every issue), `ValidateOrThrow` (errors only) and `IsMetric`; a `ValidationIssue` carries a DualC-style JSON-pointer `Path`, a `Message` and a `ValidationSeverity`. |
| `FieldTree.cs` | `Rewrite(node, map)`, the generic bottom-up immutable tree rewrite, and `CopyParams`; shared by both resolvers ([03](03-volume-and-resolvers.md)). |

## The model

A field graph is an immutable **tree**: sources have no children, decorators and domain ops
one, booleans and the graded ops two, `mix` three. Parameters are a generic bag — `Params`
plus a per-op `OpSchema` — rather than a strongly-typed record per op, so DualC's vocabulary
grows without new C# types: a new op is a schema row and a builder. The model is
**emit-only**: there is no JSON reader in `Boletus.Core`, because DualC parses what Boletus
writes and nothing ever flows back as a graph. The wire format is **tree-only**: a field
feeding two consumers serializes as a duplicated subtree, which is correct and merely not
compact — the DAG-ref superset is DualC's to ship and Boletus's to adopt
([decisions](../decisions/README.md)).

## The byte-match rules

`FieldGraphSerializer` emits `{ "version": 1, "units": "mm", "root": <node> }`, two-space
indented, and each node as `{ "op": <token>, <params…>, "in": [<children>] }` — `in`
present only on a node with children; a source omits it. It
**byte-matches** DualC's `--dump-json` output, which is what the vocabulary tests pin:

- per node, `op` first, then the parameters sorted **ASCII-ordinally**
  (`StringComparer.Ordinal`, the order of nlohmann's `std::map`), then `in` last;
- doubles in the invariant shortest round-trip form with a **forced `.0`** when integral;
- JSON string escaping for backslash, quote and control characters — a Windows path with
  backslashes survives the trip;
- absent optional parameters are omitted, never filled with defaults, exactly as
  `--dump-json` never fills them.

Parameter key order is irrelevant on input; types, and for flat primitives the positional
order, are load-bearing. Float formatting is not load-bearing for DualC — the round-trip
through `--dump-json` is the semantic oracle and the byte match a secondary format check.

## Coverage

`Ops.cs` carries the whole vocabulary `dualc_field --list` prints, in three families:

- **Sources.** The TPMS families (`gyroid`, `schwarz-p`, `diamond`, `fischer-koch`,
  `lidinoid`, `neovius`; optional `center` and `wavelength`), `mesh` and `winding`
  (exactly one of `path` / `id`; `mesh` also takes `sign` and `normals`), the strut crystals
  (`sc`, `bcc`, `fcc`, `octet`; `center`, `wavelength`, `radius`, `nodeRadius`, all optional), and
  every analytic primitive: the seven with **grouped keys** (`sphere`, `box`, `roundbox`,
  `capsule`, `cappedcylinder`, `torus`, `ellipsoid`) and the rest through DualC's universal
  flat `params:[…]` positional array.
- **Booleans and graded ops.** `union`, `intersection`, `difference`, `xor`, the three
  `smooth-*` variants with `k`, `graded-onion` and `graded-offset` (base + control;
  `t1`, `t2`, `d1` required, `d0` optional), and the three-child `mix` (`hi` required, `lo`
  optional) — the first arity-3 op, exercising the child traversal the 0- and 2-child paths
  never did.
- **Decorators and domain ops** (one child): `normalize`, `onion`, `offset` and its alias
  `round`, `scale`, `translate`, `rotate`, `elongate`, `transform` (a 16-element row-major
  matrix), `twist`, `bend`, `mirror`, `repeat`, `repeat-limited`, `displace`.

Pinning notes that hold: every primitive parameter has a DualC default, so all are modelled
optional (a bare `roundbox()` is valid); a **named builder per flat primitive** — e.g.
`Field.Cone(cx, cy, cz, angleRad, height)` — names the positions for the caller and packs
them into the single `params` array DualC sees, so the round-trip certifies the emitted
positions; for the catalog shapes the slot → position order is pinned by
`PrimitiveCatalogTests` against the builders, while the builders' own *argument names* are
pinned by reading the primitive page, not by a
test; `twist` / `bend` take `axis` as a **string** (`"x"` / `"y"` / `"z"`), unlike `rotate`'s
vector; `plane`, the infinite primitives and `repeat` are unbounded (bounds are a contour-time
flag, not a node parameter); `triangle` / `quad` are open surfaces that need an `onion` to
gain thickness. The per-op table — token, parameters, builder — is the command reference's,
not this page's.

## Metric and non-metric

An `OpCategory` on each schema drives `FieldGraphValidator.IsMetric`: a raw `Tpms` source and
`winding` are non-metric; a `MetricSource` (every analytic primitive, `mesh`, the strut
crystals — true SDFs), `Normalize`, `Onion`, `GradedOnion` and a `SmoothBoolean` yield a
metric field; a one-child `Decorator` propagates its child; a `HardBoolean` is metric only
when both operands are (`mix` reuses this category, so its control child is ignored).
`triangle` / `quad` are categorised `MetricSource` on purpose — unsigned distance with unit
gradient — so that wrapping one in `onion`, the way it gains thickness, raises no warning;
that is a judgement call noted in `Ops.cs`, not a DualC classification.

**Metric-ness is a warning, not an error.** DualC accepts and contours a non-metric source
feeding `onion`, `graded-onion` or a smooth boolean — the golden-count graph is exactly such a
composition — so `ValidateOrThrow` throws only on schema **errors** (arity, a missing or
unknown or mistyped parameter, an unknown op, both or neither of `path` / `id` on a mesh
leaf: what DualC itself rejects), and metric-ness surfaces as an actionable warning that
names the fix ("wrap it in `normalize`"). The user-facing rule is
[06 § The Normalize rule](06-conventions.md#metric-by-default-sources-and-the-normalize-rule).

## What is not a node

Sampling settings — depth, collapse, bounds, tile depth — are invocation parameters
(`DualcContourParams`, `ExportPlan`), never graph nodes; a graph is a shape, not a run.

---

← Back to the [design index](README.md) · the [docs index](../README.md)
