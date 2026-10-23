# Settled decisions, continued — from D-52 on

Part of the [decisions index](README.md): the settled table of
[01-settled.md](01-settled.md) continues here, that page having reached the size contract.
Same columns, the same one ID sequence, the same rule — a row here never changes status; a
settled decision that is reversed gets a new row on the README with the new date. The
present-tense *effect* of these decisions lives in [`docs/design/`](../design/README.md).

| ID | Decision | Status | Date | Where recorded |
| --- | --- | --- | --- | --- |
| D-52 | DualCEditor in exploration mode is the `Live Preview` side-car: the submodule `external/DualCEditor`, built against Boletus's DualC pin, shipped with its Qt runtime (LGPL, dynamic) as the `.yak`'s `editor/` folder; Grasshopper the only input, bounds in the graph's `view` key; both pins bump in one commit | DONE | 2026-10-22 | [12](../roadmap/12-explorer-sidecar.md) · [raw plan](../raw/2026-10-22-dualceditor-sidecar-plan.md) |
| D-54 | `dualc_field_view.exe` stays shipped as `Field view` and the fallback when `editor/` is absent; retired only after the explorer's smoke test on two machines | DONE | 2026-10-22 | [12](../roadmap/12-explorer-sidecar.md) |

---

← Back to the [decisions index](README.md) · the [docs index](../README.md)
