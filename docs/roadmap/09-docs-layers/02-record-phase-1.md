# Record — Phase 1 (the gate)

**DONE (2026-09-21).** Delivered: `scripts/check.py` ported from DualC's gate (same name,
same flags — `--fast`/`--docs`, `--build`, `--strict`, `--hook`, `--selftest`, `--list`,
`--only`/`--skip` — so the owner's `/repo-docs-lifecycle` and `/docs-semantic-lint`
workflows run here unmodified); `scripts/check_data.json` with Boletus's exceptions;
`scripts/check_fixtures/` (the eleven DualC pairs minus `flag-table`, plus a `dualc-links`
pair); `scripts/hooks/pre-commit` (`--fast`), `.gitattributes` pinning it to LF,
`core.hooksPath` set; `.claude/settings.json` with the `Stop` hook running
`check.py --docs --hook`; `__pycache__/` ignored.

**What changed in the port.** Removed: the cmake `configure`/`build`/`warnings`/`ctest`
tier, the `parity` GPU tier, `vendoring` (no `THIRD_PARTY.md` here — the vendored DLL's
provenance is `native/README.md`), `flag-table` (it parses `examples/*.cpp`; the component
analogue is deferred, see the plan's *Deliberately not done*), `print_metrics`. Added:
`dotnet-build` (`dotnet build Boletus.sln -c Debug`, any warning fails) and `dotnet-test`
(count asserted against a floor, `dotnet.tests_expected_min`, that only goes up); and
`dualc-links` (fast, report-only → `--strict` fails): every DualC path a maintained file
cites — absolute `D:\DualC\…`, relative `../DualC/…`, bare `docs/<layer>/…`/`capi/…`,
or a page name alone such as the pre-split `11-dualc_field` page — is resolved against the checkout named by
`DUALC_ROOT` or `dualc_links.dualc_root`; `NN` alone resolves to the `NN-slug` page or
folder; a page name that is now a folder is reported as such; `file:///D:/DualC` URIs are
reported; `docs/raw/` is not scanned (immutable); the check SKIPs when the checkout is absent.
`ordered_item_files` is empty; `status_vocab_only.tables` stays empty until Phase 3 writes
the decisions index.

**Evidence at close.** `--selftest`: 26 fixture runs, 0 wrong. `--fast`: 0.56 s, 25 checks —
13 passed, 7 failed, 2 skipped (`decisions-index`, `design-no-history` await Phase 3).
`--build`: `dotnet-build` 0 warnings (15.5 s), `dotnet-test` **117 passed** (floor 117).
Hook mode: exit 2 with the report on stderr, exit 1 when `stop_hook_active`. `dualc-links`
against `D:\DualC` at `c43bdec`: 72 files scanned, 51 paths cited, **6 missing** — the six
the plan predicted (`README.md:4` `file://` URI; `OpSchema.cs:59`, `Ops.cs:10,49`,
`FieldGraphExampleTests.cs:13,67` naming the pre-split `11-dualc_field` page, now a folder); the
`02-dualc_primitive.md` citations still resolve. `dualc-links` with `DUALC_ROOT` pointing
nowhere: SKIP.

**The red gate, by design.** The seven failing checks are the Phase 2 worklist and were
expected: `indexes` (no `docs/roadmap/README.md`), `footer` (`roadmap.md` ends on its
detail list), `status-vocab`/`status-sync` (same missing README), `claude-md` (9 restated
counts/versions in `CLAUDE.md`), `root-entry` (no `AGENTS.md`; 14 dates/versions in
`CLAUDE.md`), `structure` (12 files missing from `STRUCTURE.md`: the eight components and
helpers added since the map was last rewritten, `MeshMaterializingResolver` and its test,
`dualc_field_view.exe`). The Phase 1 commit is therefore made with `--no-verify`; Phase 2
is the only other session that may close red, and only on those checks.

**Baselines recorded.** `size_baseline`: `components.md` (717 / 6,943 / 41,199),
`04` (300 / 2,618 / 20,034), `05` (830 / 8,515 / 63,133), `07` (337 / 3,182 / 23,702).
`heading_status_baseline`: `04` 1, `05` 13, `07` 2, and 1 for the plan import under
`raw/` (immutable; its one dated heading is grandfathered).

**Deviations from the plan.** `dualc-links` also resolves page-name-only citations
(the `11-dualc_field` page name with no folder) — the plan named only full paths, but the eight `.cs`
comments it exists to catch use the bare form. `status_vocab_only` was emptied for the
interim rather than left pointing at files that do not exist yet.

---

← Back to the [Docs layers index](README.md) · the [Roadmap index](../README.md).
