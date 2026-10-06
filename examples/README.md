# Examples

Grasshopper canvases that exercise the Boletus palette. These are **demonstration
definitions**, not test fixtures — the automated gates live in
[`tests/Boletus.Core.Tests`](../tests/Boletus.Core.Tests).

## Opening a `.gh` here

A Boletus `.gh` only opens with the plugin loaded:

1. Install the plugin: the `.yak` CI builds (Rhino's Package Manager, or drag it onto
   Rhino), or build it yourself — `python scripts/build_native.py`, then
   `dotnet build src/Boletus.Grasshopper -c Debug` — and copy `Boletus.Grasshopper.gha` +
   `Boletus.Core.dll` + `dualc_capi.dll` + `dualc_field_view.exe` into
   `%APPDATA%\Grasshopper\Libraries\` (unblock the `.gha` first: right-click → Properties →
   Unblock).
2. Open **Rhino 8** → **Grasshopper**, then open the `.gh` file.

Without the plugin the components load as red placeholders (Grasshopper can't find the
`Boletus.*` GUIDs).

## Files

| File | What it shows |
|---|---|
| `demo.gh` | A showcase canvas exercising most of the palette — `TPMS` / `Normalize`, `Primitive`, `Mesh → Volume`, `Onion` / `Graded Onion`, the domain warps (`Transform` / `Offset` / `Twist` / `Bend` / `Displace`), `Boolean`, and `Proxy preview`. Use it to see the components wired together on a real canvas. |

## The canonical four-step workflow

The **`isolate → thicken → skin → union`** recipe — turning a TPMS lattice and its enclosing
volume into one printable part — is walked through step by step, with the exact per-component
wiring, in [command reference 06 § W2 — a printable part](../docs/command_reference/06-worked-examples.md#w2--a-printable-part-isolate--thicken--skin--union).
That composition is pinned by an automated Core test
([`FieldGraphExampleTests.cs`](../tests/Boletus.Core.Tests/FieldGraphExampleTests.cs)), which
round-trips it byte-for-byte through DualC's `--dump-json` and contours it end-to-end (Grasshopper
itself can't run headless, so the Core composition is the machine-checkable half).
