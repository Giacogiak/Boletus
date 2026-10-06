# 10/01 — The fresh root and the archive

Part of [10 — Public delivery](README.md). The heading below is the item's, verbatim.

## #32 Published with a fresh root, the history archived offline

**DONE — 2026-10-06.** For legal reasons the public repository carries no pre-publication
history: one root commit, "Initial public release of Boletus", made from the final prepared
tree, and the remote `https://github.com/Giacogiak/Boletus` created empty by the owner and
pushed once that commit existed. The procedure is DualC's, step for step:

1. Every preparation of this block (#33, #34, the license, the docs) landed as ordinary
   gate-green commits on the old `main`, so the archive holds the finished work too.
2. `git bundle create … main` of that `main` — one ref, like DualC's bundle — verified with
   `git bundle verify`, zipped as `~/Documents/Boletus-history-2026-10-06.zip` (one file in
   the zip, no folder, the loose bundle deleted), test-cloned once with `-b main`.
3. `git checkout --orphan`, one commit of the same tree, `git branch -M … main`, reflog
   expired, `git gc --prune=now`; `git rev-list --all --max-parents=0` prints one hash.
4. The remote added, `main` pushed, `git ls-remote` showing `refs/heads/main` alone.

The restore procedure — a separate read-only clone of the bundle, or a frozen
`refs/archive/` ref inside the working repo with an untracked `pre-push` guard that refuses
any push whose root is not the public one — is the owner's guide
`~/Documents/git-history-restore-guide.md`, one file for DualC and Boletus, outside every
repo. Two Boletus-specific notes it carries: the archive holds the submodule as a gitlink only
(a restored clone needs `git submodule update --init`, which fetches DualC from GitHub), and
the old history still has the binaries committed, so a restored tree is never pushed.

**What it means for the docs.** Every commit hash cited in `docs/` before this date names a
commit of the archive, not of the public history — the same note DualC's record carries.
The `D:\DualC` paths in dated roadmap text are the historical sibling checkout and stay as
written; the rewritable pages point at the submodule.

**Added with the root.** `LICENSE` (MIT, the same text as DualC's) and `CITATION.cff`
(version `0.1.0`, the plugin's own, equal to `BoletusInfo.Version` and the Yak manifest —
the gate's `yak-version` check keeps the last two equal).

**Update — 2026-10-06: published.** The remote is <https://github.com/Giacogiak/Boletus>; its
history is one root commit, `407738d` ("Initial public release of Boletus"), with no parent,
and `git ls-remote` showed `refs/heads/main` alone after the push. The archive
`~/Documents/Boletus-history-2026-10-06.zip` holds the 70 pre-publication commits, tip
`fd8ede7` (the preparation commit of #33 and #34), verified and test-cloned before the
reset; the untracked `pre-push` guard of the guide's Method B is installed in the working
repo with this root. The pre-commit hook was bypassed for the root commit alone (the fast
tier's `git log` has nothing to read on an unborn branch); the same tree had passed the full
gate as `fd8ede7`, and the fast tier passed again on the root.

---

← Back to the [block index](README.md) · the [Roadmap index](../README.md).
