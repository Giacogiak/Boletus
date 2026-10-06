# Boletus — Project Structure

Complete file-by-file map. Excludes build output (`bin/`, `obj/`, `*.user`, `build-native/`,
the built binaries under `native/<rid>/`) and the contents of the DualC git submodule
`external/DualC`, whose files are DualC's own (its map is its `docs/README.md`). How the
layers work is `docs/design/`; why they are the way they are is `docs/roadmap/`; the map is
this file.

```
Boletus/
├── Boletus.sln                 Solution: Boletus.Core + Boletus.Core.Tests + Boletus.Grasshopper, every config row mapped to x64
├── README.md                   Front page: pitch, architecture, build & test (clone, native build, gate, CI), Core usage, the components, the DualC dependency, license
├── AGENTS.md                   Agent entry file: build/test/gate commands, the remote and CI, the five docs principles, the reading order
├── CLAUDE.md                   `@AGENTS.md` — Claude Code's import of the entry file, nothing else
├── STRUCTURE.md                This file
├── LICENSE                     MIT (the same text as DualC's)
├── CITATION.cff                How to cite Boletus: title, author, version, license, the repository URL
├── .gitignore                  Ignores bin/, obj/, *.gha, *.rhp, *.yak, __pycache__/, build-native/ and the built binaries under native/x64/ and native/linux-x64/
├── .gitattributes              Pins scripts/hooks/* to LF (core.autocrlf would break the shebang)
├── .gitmodules                 The one submodule: external/DualC → https://github.com/Giacogiak/DualC.git
│
├── external/
│   └── DualC/                  The DualC engine as a git submodule, pinned by the gitlink (the pin); built by scripts/build_native.py, never compiled by MSBuild
│
├── .github/
│   └── workflows/
│       └── ci.yml              CI: an Ubuntu job and a Windows job each build the native side from the submodule and run the gate; Windows also stages the plugin and uploads the .yak and the bare folder
│
├── yak/
│   └── manifest.yml            The Yak package manifest (name, version = BoletusInfo.Version, authors, description, url); staged beside the .gha by CI
│
├── docs/                       Documentation, one folder per class of fact — the map is docs/README.md
│   ├── README.md               Docs entry: what lives where, the fact-class → owner table, conventions, how to search
│   ├── design/                 Compiled current state: how the plugin is, rewritten freely, dated nowhere — indexed by its README
│   ├── decisions/              The decisions index (README: open and rejected; 01-settled.md: the choices that hold) — indexed by its README
│   ├── command_reference/      Usage contract: one page per component family, the op-token table, the worked examples — indexed by its README
│   ├── roadmap/                Append-only development record: one numbered block per topic — indexed by its README
│   └── raw/                    Immutable inputs (plans, imported text, snapshots; exempt from the size contract) — indexed by its README
│
├── src/
│   ├── Boletus.Core/           The Rhino-free managed layer: P/Invoke wrapper + field-graph model + the Volume datatype (netstandard2.0, no NuGet deps)
│   │   ├── Boletus.Core.csproj netstandard2.0; Platforms=x64; an Exists-conditioned <None> item per platform copies the built native/<rid>/ library to the output (flows to consumers)
│   │   ├── NativeMethods.cs    Raw P/Invoke surface (internal): every dualc_* entry point of the pinned library (Cdecl; the 0.4.0 *_with_diagnostics twins, the 0.5.0 cancel token and *_with_progress twins included) plus a pointer-typed probe alias of the contour diagnostics twin, the DualcProgressFnNative delegate + the blittable ContourParamsNative / MeshNative / DualcMeshSourceNative / DualcDiagnosticsNative structs
│   │   ├── DualcFieldHandle.cs SafeHandle over the native field: ReleaseHandle → dualc_field_destroy, exactly once
│   │   ├── DualcCancelTokenHandle.cs  SafeHandle over DualC's host-owned cancel token (ABI 0.5.0): ReleaseHandle → dualc_cancel_token_destroy
│   │   ├── DualcProgress.cs    DualcStage enum (Sample / Contour / Write / Tile) and the DualcProgress report (Done, Total, Fraction)
│   │   ├── DualcDiagnostics.cs The ABI 0.4.0 DualcDiagnostics as a readonly struct: EmptyContour (the one-triangle placeholder), the output counts and OutputWatertight, BoundsFallback / GridBoundsExceeded, the never-inspected Input* fields, AnyIssue; built from the native struct only after an OK status
│   │   ├── Native.cs           DualcStatus enum, DualcException, UTF-8 byte[] marshaling helpers, status → exception
│   │   ├── DualcContourParams.cs  Idiomatic contour params (MaxDepth, MinDepth, Collapse, bounds, Manifold, NumThreads); Default() from the DLL, ToNative()
│   │   ├── DualcMeshData.cs    Managed deep copy of a native mesh: positions / normals / indices + counts
│   │   ├── DualcField.cs       Public API + IDisposable: Version, SupportsProgress / SupportsDiagnostics (the two ABI-level probes, by entry point), FromExpr, FromJson, FromJson(json, meshes), Contour, Export, ExportTiledStl — each with an IProgress + CancellationToken overload over one WithToken helper; Contour and Export also with a trailing out DualcDiagnostics, plain and with progress
│   │   ├── MeshBuffer.cs       Rhino-free in-memory mesh (the in-memory mesh-source currency): vertices/triangles/normals, ContentHash, WriteObj
│   │   ├── Volume.cs           The exchange datatype: a FieldNode core + MeshBuffers by content-hash id; the mem:// leaf scheme; WithCore / Combine
│   │   ├── VolumeResolver.cs   Terminal rewrite for the in-process DLL: mem://<id> leaves → mesh(id=…) + the referenced buffers (no disk)
│   │   ├── MeshMaterializingResolver.cs  Terminal rewrite for a separate process: mem://<id> leaves → temp OBJ + mesh(path=…)
│   │   ├── Export/
│   │   │   └── ExportPlan.cs   Pure export planning: path/extension from Format, Tiled vs Monolithic strategy, effective tile depth, warnings
│   │   └── FieldGraph/         Managed node tree → canonical JSON (byte-identical to DualC's --dump-json)
│   │       ├── FieldValue.cs   Immutable param value: Scalar / Vector / Text, value equality
│   │       ├── FieldNode.cs    Immutable graph node: op token, ordinal params, children; ToJson()
│   │       ├── FieldGraphSerializer.cs  The canonical emitter: op → ordinal-sorted params → in; integral doubles forced to `.0`
│   │       ├── OpSchema.cs     Schema descriptors: OpSchema, ParamSpec, OpCategory, ParamKind
│   │       ├── Ops.cs          The pinned op registry: every token DualC's field-graph parser accepts, with arity and param specs
│   │       ├── Field.cs        Typed fluent builders: TPMS, mesh/winding sources, strut lattices, every primitive, booleans, decorators, domain ops
│   │       ├── PrimitiveCatalog.cs  The Segment / Axial primitive families: per-shape slot names, tooltips, engine defaults, degree→radian, the builder each calls
│   │       ├── FieldTree.cs    Generic bottom-up immutable rewrite (shared by both resolvers)
│   │       └── FieldGraphValidator.cs  Schema errors (arity, required, unknown) + metric-ness warnings; ValidateOrThrow; IsMetric
│   │
│   └── Boletus.Grasshopper/    The GH .gha (net7.0-windows, x64) over Boletus.Core by project reference
│       ├── Boletus.Grasshopper.csproj  TargetExt=.gha; Grasshopper metapackage compile-only (NU1701 suppressed); copies the built DLL + the viewer exe from native/x64/ beside the .gha (Exists-conditioned)
│       ├── BoletusInfo.cs      GH_AssemblyInfo: name, description, id, author, version
│       ├── BoletusPriority.cs  GH_AssemblyPriority: registers the native-DLL resolver against the Core assembly (where the DllImports live)
│       ├── BoletusIcons.cs     Procedurally drawn GDI+ glyphs, one per component and parameter; a fresh Bitmap per Icon getter; no image assets
│       ├── VolumeGoo.cs        GH_Goo<Volume> — the canvas wrapper
│       ├── VolumeParameter.cs  GH_Param<VolumeGoo> — the Volume wire type (hidden)
│       ├── RhinoMeshConvert.cs DualcMeshData → Rhino.Geometry.Mesh (shared by the proxy terminal)
│       ├── AxisName.cs         Dropdown int → "x"/"y"/"z" for the twist/bend warps
│       ├── TpmsComponent.cs    Source: TPMS family + wavelength → Volume
│       ├── PrimitiveComponent.cs  Source: analytic primitive dropdown (box … plane, boxframe, ellipsoid) → Volume
│       ├── PrimitiveFamilyComponent.cs  Source base: a type dropdown + the family's fixed slots, relabelled per type after each solve, over PrimitiveCatalog
│       ├── SegmentPrimitiveComponent.cs Source: two points + two numbers — capsule, cappedcylinder, roundcone, vesica, infinitecylinder
│       ├── AxialPrimitiveComponent.cs   Source: one point + four numbers — the sixteen centred shapes, cone to infinitecone
│       ├── MeshToVolumeComponent.cs  Source: Rhino mesh → in-memory MeshBuffer + mesh/winding leaf
│       ├── StrutLatticeComponent.cs  Source: sc/bcc/fcc/octet strut lattice (+ optional node-radius taper) → Volume
│       ├── OnionComponent.cs   Decorator: shell of a thickness, optional boundary clip applied after the thickness
│       ├── GradedOnionComponent.cs  Decorator: thickness graded by a control field, optional boundary clip
│       ├── NormalizeComponent.cs    Decorator: normalize — the metric-correctness gate
│       ├── TransformComponent.cs    Decorator: Rhino Transform → transform op
│       ├── OffsetComponent.cs  Decorator: offset by a distance
│       ├── TwistComponent.cs   Decorator: twist about an axis
│       ├── BendComponent.cs    Decorator: bend about an axis
│       ├── DisplaceComponent.cs     Decorator: displace by a named function (+ optional amplitude/frequency)
│       ├── GradedOffsetComponent.cs Decorator: solid radius graded by a control field (graded-offset)
│       ├── BooleanComponent.cs Boolean: union/intersection/difference/xor + the smooth variants with k
│       ├── MixComponent.cs     Boolean: three-child value-lerp morph (mix)
│       ├── WriteToFileComponent.cs  Terminal: export-only STL/3MF writer — on-canvas Write ▶ / Cancel ■ button, background thread, the engine's progress in the label, cooperative cancel (ABI 0.5.0) or queued restart on an older DLL, BUSY detection
│       ├── ProxyPreviewComponent.cs Terminal: viewport-only proxy mesh at a depth clamped to a hard ceiling; draws via DrawViewportMeshes
│       └── LivePreviewComponent.cs  Terminal: launches dualc_field_view.exe (built beside the .gha) on a temp .json of the Volume (a separate process)
│
├── tests/
│   └── Boletus.Core.Tests/     xUnit, net9.0 / win-x64, Rhino-free — the marshaling gate, the field-graph round-trips, the resolvers, the export planner
│       ├── Boletus.Core.Tests.csproj  References Boletus.Core; copies cube.obj and the built dualc_field CLI (Exists-conditioned, per platform) to the output; test packages pinned to the local cache
│       ├── TestPaths.cs        Where the CLI-backed suites find dualc_field (beside the assembly, DUALC_FIELD_EXE, the historical D:\ path) and DualC's examples/samples (DUALC_SAMPLES_DIR, the submodule)
│       ├── WrapperTests.cs     Version, defaults, the golden-count contour (the marshaling gate), export, the error statuses, the mesh-path source
│       ├── CliParityTests.cs   Wrapper STL byte-identical to dualc_field (skips when the CLI is absent)
│       ├── FieldGraphTests.cs  Serializer/builders against the shipped DualC fixtures and --dump-json; pure-formatter checks
│       ├── FieldGraphVocabularyTests.cs  Every op round-tripped through --dump-json against its --expr twin; flat-params and axis guards
│       ├── FieldGraphExampleTests.cs     The isolate → thicken → skin → union example graph, byte-matched against --dump-json
│       ├── FieldGraphValidatorTests.cs   Validator: metric warnings vs schema errors, ValidateOrThrow
│       ├── PrimitiveCatalogTests.cs  Each catalog shape against the Field builder it must call (slot order, degrees→radians); every shape contoured at its defaults
│       ├── MeshBufferTests.cs  The in-memory mesh path hits the same golden counts as the disk path; onion-before-clip; ContentHash; OBJ writer
│       ├── VolumeResolverTests.cs        mem:// → mesh(id=…) rewrite, buffer dedup, disk paths untouched, missing-buffer error
│       ├── MeshMaterializingResolverTests.cs  mem:// → temp OBJ + mesh(path=…) rewrite; mesh-free graphs pass through
│       ├── ExportPlanTests.cs  ExportPlan.Resolve: extension rules, strategy table, tile-depth default and warnings, error cases
│       ├── ProgressAndCancelTests.cs  The 0.5.0 overloads: golden counts through the hooked calls, stage shapes, a pre-cancelled token, a cancel from the callback and from another thread (no file, no .part), a throwing consumer
│       ├── DiagnosticsTests.cs The 0.4.0 out DualcDiagnostics overloads: the probe, DualC's far-away-sphere empty contour (placeholder counts, flagged), the golden counts through the diagnostics twin (the struct's layout), progress twins returning the same struct, the exported STL's facet count against OutputTriangles for the healthy and the empty field
│       └── cube.obj            Minimal unit-cube fixture for the mesh-source tests
│
├── native/                     Where scripts/build_native.py drops the binaries it builds from the submodule — never committed
│   ├── README.md               The pin (the submodule's commit), how to build, what lands where, the version trap, the ABI contract pointer
│   ├── x64/                    (gitignored) dualc_capi.dll, dualc_field.exe, dualc_field_view.exe, BUILD-INFO.txt — the Windows build
│   └── linux-x64/              (gitignored) libdualc_capi.so, dualc_field, BUILD-INFO.txt — the Linux build, so the Core tests run there
│
├── examples/
│   ├── README.md               How to open the demo canvas (the .gha must be installed)
│   └── demo.gh                 Demonstration Grasshopper canvas
│
├── scripts/
│   ├── check.py                THE GATE (ported from DualC): every docs check + native-built + dotnet build/test in one command — what CI runs
│   ├── check_data.json         Its declared exceptions: size baseline, index roots, forbidden patterns, the DualC tree paths (submodule first), the test floor, the yak manifest
│   ├── build_native.py         Builds DualC's C ABI, dualc_field and (Windows) the viewer from external/DualC (or --dualc / DUALC_ROOT) into native/<rid>/, runs dualc_c_demo, writes BUILD-INFO.txt
│   ├── docs_loss_audit.py      Did a docs restructuring lose a fact? Set arithmetic over two git trees (ported from DualC)
│   ├── docs_loss_audit.json    Its triage: one verdict per residual unit, each citation verified on the head tree
│   ├── check_fixtures/         `check.py --selftest` trees: one pass/ and one fail/ miniature repo per docs check
│   └── hooks/
│       └── pre-commit          sh wrapper running `check.py --fast`; enable with `git config core.hooksPath scripts/hooks`
│
└── .claude/
    └── settings.json           Project hooks: Stop → `scripts/check.py --docs --hook` (the docs gate after every turn)
```

## Build targets at a glance

- `Boletus.Core` — netstandard2.0 class library; the only project that P/Invokes.
- `Boletus.Core.Tests` — xUnit runner (net9.0, win-x64); `dotnet test` is the second tier
  of the gate.
- `Boletus.Grasshopper` — net7.0-windows library with output extension `.gha`; the `.gha`,
  `Boletus.Core.dll`, `dualc_capi.dll` and `dualc_field_view.exe` are what a Rhino install
  needs, and nothing else is emitted beside them. CI's Windows job packages exactly those
  four with `yak/manifest.yml` into one `.yak`.
- The native side — `dualc_capi`, `dualc_field`, `dualc_field_view` — is CMake-built from
  the submodule by `scripts/build_native.py`, outside MSBuild, before the solution.

## What is intentionally absent

- **No C++ source of Boletus's own, and no `add_subdirectory` of DualC from MSBuild.** The
  engine is built from the submodule by one script, and the managed projects only copy the
  outputs; the procedure is `native/README.md`.
- **No committed binaries.** `native/<rid>/` is gitignored; a clone builds it.
- **No image assets.** Every component icon is drawn in code (`BoletusIcons.cs`).
- **No release job and no Yak-server push.** CI uploads the `.yak` as a run artifact only.
- **No `docs/` file-by-file listing here.** Each docs folder's README is its index.
