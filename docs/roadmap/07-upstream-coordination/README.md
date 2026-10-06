# 07 — Upstream coordination with DualC

**PARTIAL**: per item — § 1 (the in-memory mesh-source resolver), § 7 (the export
progress/cancel callback, upstream 2026-10-02, Boletus side 2026-10-05) and § 8 (the
strut-lattice vocabulary sync) are DONE; § 5 (the viewer binary) DONE with its 5a/5b phases;
§ 9 is the dated inventory of what the pinned DualC offers beyond what Boletus binds;
§ 2, § 3 and § 6 are DEFERRED, each with its trigger and its row in the [decisions index](../../decisions/README.md);
§ 4 (version pinning) is the standing hygiene ask the version trap keeps open.

Items Boletus **needs from** or **triggers in** the DualC repo (the submodule `external/DualC`;
`D:\DualC`, the sibling checkout, in the dated text), one numbered
section each — the section numbers are the stable addresses. None blocks the palette; each
unlocks or simplifies a later step. What Boletus does today with what landed — the diskless
path, the side-car process model, single-flight writes — is `docs/design/`
([03](../../design/03-volume-and-resolvers.md), [04](../../design/04-grasshopper-plugin.md),
[07](../../design/07-invariants-and-limits.md)); the native build and the pin are
[`native/README.md`](../../../native/README.md). Split on 2026-09-21 (roadmap 09 Phase 4)
from one page into this folder: the three short standing asks (§ 2–§ 4) stay on this page,
the long sections are the children, every heading verbatim.

## The pages of this topic

| Page | Sections |
| --- | --- |
| [01 The in-memory mesh-source resolver](01-in-memory-mesh.md) | [§ 1 — DONE, DualC v0.3.0 + the Boletus-side flip](01-in-memory-mesh.md#1-in-memory-mesh-source-resolver--done-dualc-v030-upstream--the-boletus-side-flip-both-2026-06-19) |
| this page | [§ 2 static-linked MSVC runtime](#2-static-linked-msvc-runtime-mt--zero-prerequisite-install) · [§ 3 DAG-ref serialization](#3-dag-ref-graph-serialization--optional-optimization) · [§ 4 version pinning / provenance](#4-version-pinning--provenance--maintainability-hygiene) |
| [02 The viewer binary and the uniform push](02-viewer-and-uniform-push.md) | [§ 5 `dualc_field_view` viewer binary](02-viewer-and-uniform-push.md#5-dualc_field_view-viewer-binary--gates-the-raymarch-preview-phase) · [§ 6 real-time parameter push](02-viewer-and-uniform-push.md#6-real-time-parameter-push-uniform-ipc-channel--deferred-performance-optimization) |
| [03 The export callback and the strut-lattice sync](03-export-callback-and-strut-sync.md) | [§ 7 export progress + cancel callback](03-export-callback-and-strut-sync.md#7-export-progress--cancel-callback--unlocks-true-abort--a-real-progress-bar-phase-c) · [§ 9 DualC at the pin — the 2fcd19f inventory](03-export-callback-and-strut-sync.md#9-dualc-at-the-pin--what-2fcd19f-offers-that-boletus-does-not-use) · [§ 8 strut-lattice vocabulary sync](03-export-callback-and-strut-sync.md#8-strut-lattice-vocabulary-sync-d6b2808--done-boletus-side-2026-07-10) |

## 2. Static-linked MSVC runtime (`/MT`) — *zero-prerequisite install*

**DEFERRED** — trigger below. *(2026-09-21, roadmap 09 Phase 3: status line added as the first body line so the `decisions-index` gate finds this entry; the row is D-05 in the [decisions index](../../decisions/README.md).)*

**Today:** `dualc_capi.dll` dynamically links the VC++ runtime, so a clean machine needs
the VC++ Redistributable installed.

**Want:** build `dualc_capi.dll` with the **static** MSVC runtime so the runtime is folded
in and the only remaining dependency is the Windows UCRT (present on Win10+) — a truly
zero-prereq Yak install ([06](../06-phase4-distribution-and-packaging.md)).

**Boletus side:** none — this is a DualC CMake/build change. Until it lands, the wrapper
runs on machines that have the VC++ runtime; app-local bundling of the runtime DLLs is the
fallback.

**Trigger:** before the first Yak intended for a machine other than a dev box.

## 3. DAG-ref graph serialization — *optional optimization*

**DEFERRED** — trigger below; upstream it is DualC D-12. *(2026-09-21, roadmap 09 Phase 3: status line added as the first body line so the `decisions-index` gate finds this entry; the row is D-06 in the [decisions index](../../decisions/README.md).)*

**Today:** the field-graph wire format is **tree-only**. A field feeding two consumers
serializes as a **duplicated subtree** (and a duplicated `mesh(path)` still **bakes once**
via the path cache), so this is correct, just not maximally compact.

**Want:** the deferred DAG-ref superset (`id`/`ref`, `let … in …`) DualC notes will "land
with Grasshopper," letting a shared sub-field serialize once — a natural fit since a GH
canvas is a DAG.

**Boletus side:** the [serializer](../04-phase3-field-graph-serializer.md) emits tree-only for
now; adopt DAG-refs when upstream ships them. **Not a blocker.**

**Trigger:** large GH canvases with heavy shared sub-fields where duplicated-subtree JSON
size or build time becomes a problem.

## 4. Version pinning / provenance — *maintainability hygiene*

Track which DualC commit/version produced the vendored `dualc_capi.dll`, keep a copy of
`dualc_c.h` as the pinned contract, and assert `DualcField.Version()` at startup. Detail in
[02 — Dependency strategy](../02-dependency-strategy.md). The ABI is stable, but the **graph
vocabulary can grow**, so a version check catches a graph string that uses an op the
shipped DLL doesn't know. *(2026-10-06, roadmap 10: the commit is the gitlink of the
submodule `external/DualC`, the header is read there (D-07), the assert stays D-08.)*

*(2026-09-21, roadmap 09 Phase 5, the first semantic lint: of the three asks, the header copy was dropped (D-07)
and the startup assert deferred (D-08), because a version check cannot catch a vocabulary gap —
[design 07 § The version trap](../../design/07-invariants-and-limits.md#the-version-trap); what
stands is the pin by commit in [`native/README.md`](../../../native/README.md) and the upstream
ask of § 8 for a version bump or a capability query.)*

## 10. A bounded rename retry in `AtomicOutput::commit` on Windows — *ask*

**PLANNED** — asked 2026-10-06, open upstream. Every DualC export writes `<path>.part` and
`std::filesystem::rename`s it over `<path>` once the stream is closed (`AtomicOutput::commit`
in `examples/example_common.cpp`); on failure the driver returns rc 2, the C ABI reports
`DUALC_ERR_IO` "tiled STL export failed (writer or validation error)", and the
`error_code` message goes to `std::cerr` only.

**Evidence** (Boletus CI, [10/03 § The flaky Windows gate](../10-public-delivery/03-ci.md#the-flaky-windows-gate--open)):
on the `windows-2022` runner, four of nine gate runs lost one file-writing test — twice the
tiled STL export, once the CLI parity, once one of six parallel exports with exactly that
`Io` error — on an unchanged library built from the pinned commit, never on Linux, never
the same test twice in a row, and a second pass on the same build always 180/180. No facet
count ever differed; the mesh is right and the file is not. The shape is a sharing
violation on the rename while an on-access scanner or indexer still holds the file it just
saw closed.

**Want:** in `AtomicOutput::commit()`, retry the rename a bounded number of times with a short
back-off when the error is `ERROR_SHARING_VIOLATION` or `ERROR_ACCESS_DENIED` (Windows
only; the practice of git and of most installers), and carry the `error_code` message into
the C ABI's `err` buffer so a host can show it. **Not a blocker** for the plugin: a user who
hits it re-clicks `Write ▶`; it blocks a green Windows gate on every push, since the
concurrency test exercises the plugin's own two-exports-at-once scenario.

**Boletus side:** nothing to change once it lands beyond the pin bump; until then the
workflow's retry step tells a reviewer that a red Windows gate is this item.

---

← Back to the [Roadmap index](../README.md) · prev: [06 — Distribution & packaging](../06-phase4-distribution-and-packaging.md) · next: [08 — Strut lattices](../08-strut-lattices.md)
