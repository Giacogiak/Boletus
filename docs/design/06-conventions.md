# Conventions — project-wide

The rules that hold across `Boletus.Core`, the `.gha` and the docs, each stated once with its
reason. The engine's own conventions — the sign convention (negative inside), the export
format dispatch by extension, primitive parameter forms — are DualC's
[design 09](../../../DualC/docs/design/09-conventions.md) and are not repeated. The
user-facing consequences of these rules (which component needs a `Normalize`, what each
message means) live in the command reference and link back here for the *why*.

## Local only

The heading is the name the rule was born with; the rule it names is the publication rule.
The source is public (`https://github.com/Giacogiak/Boletus`, MIT) with a history that starts
at its public root commit, and CI runs the gate on every push. Three things stay the owner's
explicit say-so: a push to the Yak server (`yak push`), a NuGet publish, and any push that
would carry pre-publication history. The one hard rule underneath is that **no native binary
— `dualc_capi.dll`, `libdualc_capi.so`, `dualc_field_view.exe`, anything built from DualC —
is ever committed**: every copy is built from the submodule, on the developer's machine and
in CI, and `native/<rid>/` is gitignored. Online *restore* of managed packages from nuget.org
is allowed: the `Boletus.Grasshopper` build fetches the `Grasshopper` metapackage and the
reference packs, and that violates nothing. Local builds, the CI `.yak` artifact and a local
package source are all fine ([decisions](../decisions/README.md)).

## Units

DualC is unit-less and fixes **1 world unit = 1 mm at export**: a `.3mf` declares
`unit="millimeter"` in its header and a slicer reads it so
([DualC design 09 § Units](../../../DualC/docs/design/09-conventions.md#units-and-frames)).
Boletus performs **no unit conversion**: the numbers on the canvas — a wavelength, a wall
thickness, a bounds corner — are the millimetres in the file, and the serializer writes
`"units": "mm"` into every graph. A Rhino document in other units is the user's to scale
before the terminal.

## Metric-by-default sources and the Normalize rule

A field is **metric** when its value is true distance (gradient ≈ 1), so a thickness of
`0.1` means 0.1 world units. `Onion`, `Graded Onion`'s base, `Graded Offset`'s base and the
smooth booleans assume a metric input; a raw TPMS is non-metric. The rule is stated once, in
the validator ([02 § Metric and non-metric](02-field-graph-model.md#metric-and-non-metric)),
and every source component keeps it simple for the user by **emitting a metric volume by
default**:

- `Mesh → Volume` is always metric: the parity and pseudonormal signed distances are, and the
  non-metric `winding` field is wrapped in `normalize` inside the component, so a `Normalize`
  after `Mesh → Volume` is never needed.
- The three `Primitive` components and `Strut Lattice` are true SDFs — metric as they are.
- **Raw `TPMS` is the deliberate exception**: it stays non-metric because whether to
  normalize it is the user's call (a normalized TPMS costs more per sample and is not always
  wanted), so the canonical chain is `TPMS → Normalize → Onion`.

Skipping a `Normalize` where one is needed still contours — the component posts an
actionable warning, never an error.

## Onion before clip

The boundary clip is not a component: it is an optional `Boundary` input on `Onion` and
`Graded Onion`, applied **after** the thickness — `intersection(onion(field), boundary)`.
Shelling first and clipping second gives clean cut faces; clipping first would shell the cut
faces too. Keeping the order visible on the canvas is the point: there is no deferred or
implicit effect. Clipping a lattice *without* hollowing it (a network solid) is a plain
`Boolean` intersection.

## Message levels

Grasshopper's three runtime-message levels are used with one meaning each:

- **Remark** — an expected note the user should read once: an infinite-extent source
  (`Plane`, `Strut Lattice`, the infinite cylinder and cone among the primitive types),
  `Mix`'s value-blend caveat, the proxy depth being capped, an open proxy mesh (with its
  edge counts), a DualC library too old for `Write to File`'s cancel and progress.
- **Warning** — actionable and non-fatal; the component solves with what it was given (a
  missing input yields nothing, not an error): a non-metric input to a
  metric-assuming op, `k` given to a hard boolean, a tile depth that gains nothing, a
  conflicting extension replaced, a contour depth above eight, BUSY, an empty contour (no
  surface inside the sampled region — the proxy draws nothing, the writer's file holds the
  engine's placeholder triangle).
- **Error** — the component refuses: an unbounded field with no bounds, an empty path, a
  missing directory, a schema error the validator reports, an out-of-range dropdown index.

The exact strings are on each component's command-reference page; a change to one is a
change to that page.

## Every dropdown documents its full option set

A dropdown input is a `Param_Integer` with `AddNamedValue` per option — Grasshopper shows
the names in the value list — and its **Description spells out every option** (`0 = STL,
1 = 3MF`; `0 = Tiled, 1 = Monolithic`; the seven boolean ops and which take `k`; the six TPMS
families; the four crystals; the three axes; the three displace functions; every primitive
family's type list). The hover tooltip
is the one place a user reads without leaving the canvas, and an integer with no legend is a
guess. The same rule gives the command reference its "every dropdown's full option set"
requirement.

## The GUID rule

Component GUIDs share the prefix `4F1B2A30-0000-4000-8000-…` with a distinct suffix per
component and parameter, checked unique by grep. **A component whose inputs are reordered or
re-typed gets a new GUID**, never the old one: an old canvas then shows a loud
"unrecognized component" placeholder instead of silently remapping its wires onto the
reordered inputs. `Write to File` (`…033`) replaced `Contour / Export` (`…030`) this way,
and `examples/demo.gh` needed a manual rewire.

## Provenance by commit

DualC is pinned by **commit**, and the commit is the one the git submodule `external/DualC`
points at — never a version string: the field-graph parser is compiled inside the library,
so two builds with the same `dualc_version()` can accept different vocabularies
([07 § The version trap](07-invariants-and-limits.md#the-version-trap)). Moving the pin is a
submodule bump committed together with whatever the new vocabulary needs in `Ops.cs`; the
binaries are rebuilt from it by `scripts/build_native.py`, never committed, and the contract
they implement is DualC's header in the submodule, linked and never copied
([`native/README.md`](../../native/README.md)).

## Logic in Core, a thin `.gha`

Anything that can be expressed without Rhino — the graph model, the resolvers, the export
planner, the marshaling — lives in `Boletus.Core` and is unit-tested on the plain runner;
the `.gha` holds only what needs Rhino types, a thread or a process
([04](04-grasshopper-plugin.md)). A component wires inputs to an already-tested `Field`
builder and adds no serialization of its own; the whole risk surface of a new decorator
component is its dropdown map. Grasshopper cannot run headless, so the `.gha` has no
automated coverage and every increment closes on a manual Rhino smoke test.

## Where the other conventions live

- Docs conventions — IDs never renumbered, a README at every level, the footer, the size
  cap, the status legend — are [`docs/README.md` § Conventions](../README.md#conventions).
- The dependency mechanism (project reference for our own code, a git submodule built by one
  script for DualC's) is a settled decision in
  [`docs/decisions/`](../decisions/README.md) with its argument in the roadmap.
- Numeric limits and the invariants the tests pin are [07](07-invariants-and-limits.md);
  the vocabulary is [08](08-glossary.md).

---

← Back to the [design index](README.md) · the [docs index](../README.md)
