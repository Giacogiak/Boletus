using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: DualC's <c>porous</c> node — seeds placed in a volume, one Delaunay
    /// tetrahedralization of them, and the unsigned distance to the cell complex (the Voronoi
    /// walls / Delaunay faces, or their edges). Like TPMS it is a source whose output still
    /// needs a thickness: the walls are zero-thick until Onion / Graded Onion / Offset /
    /// Graded Offset give them one, and a Boolean clips them to the volume (DualC D-69's
    /// composition, Boletus D-49). Every key at DualC's default is left out of the graph, so
    /// the canvas's JSON reads like DualC's own samples.
    /// </summary>
    public sealed class PorousShellComponent : GH_Component
    {
        // The dropdowns' tokens, index = the named value; [0] is DualC's default.
        private static readonly string[] CellsTokens = { "voronoi", "delaunay" };
        private static readonly string[] ModeTokens = { "walls", "struts" };
        private static readonly string[] DistTokens = { "poisson", "jitter", "random" };
        private static readonly string[] RampTokens = { "linear", "smooth", "gaussian" };

        public PorousShellComponent()
            : base("Porous Shell", "Porous",
                   "A porous field (sponge, foam, bone): seeds placed in the Volume, and the distance to " +
                   "their cells' walls or struts. Zero-thick on its own - give it a thickness (Onion, " +
                   "Graded Onion, Offset, Graded Offset), then clip it with the volume (Boolean).",
                   "Boletus", "Sources")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "The volume to fill with cells. Read only while the cells are built (a metric volume " +
                "places the boundary pores right; Normalize a raw TPMS or Winding first).",
                GH_ParamAccess.item);

            pManager.AddParameter(Dropdown("Cells", "Cl",
                "Cell complex: 0 Voronoi (the cells of the seeds: foam, bone), 1 Delaunay (the " +
                "tetrahedra's faces and edges).",
                "Voronoi", "Delaunay"));
            pManager.AddParameter(Dropdown("Mode", "M",
                "What gets the thickness: 0 Walls (closed cells, each interior cell a sealed cavity), " +
                "1 Struts (open cells, the edges only).",
                "Walls", "Struts"));
            pManager.AddParameter(Dropdown("Distribution", "D",
                "Seed placement: 0 Poisson (blue noise, minimum distance Spacing), 1 Jitter (a grid of " +
                "pitch Spacing, each point moved by up to Jitter), 2 Random (white noise).",
                "Poisson", "Jitter", "Random"));

            pManager.AddNumberParameter("Spacing", "S",
                "Mean distance between seeds (where the control is <= d0), > 0. Smaller = more, " +
                "smaller cells; the cost grows with the seed count.", GH_ParamAccess.item);

            pManager.AddNumberParameter("Spacing2", "S2",
                "Optional seed distance where the control is >= d1 (default = Spacing). A value " +
                "different from Spacing grades the cells and needs Control and d1.", GH_ParamAccess.item);
            pManager[5].Optional = true;
            pManager.AddParameter(new VolumeParameter(), "Control", "C",
                "Optional control field grading the spacing from Spacing to Spacing2 (e.g. a Sphere of " +
                "radius 0: the distance from a point). Ignored unless Spacing2 differs from Spacing.",
                GH_ParamAccess.item);
            pManager[6].Optional = true;
            pManager.AddNumberParameter("d0", "d0",
                "Control value where the spacing is Spacing (optional; default 0).", GH_ParamAccess.item);
            pManager[7].Optional = true;
            pManager.AddNumberParameter("d1", "d1",
                "Control value where the spacing is Spacing2 (required when graded).", GH_ParamAccess.item);
            pManager[8].Optional = true;
            pManager.AddParameter(Dropdown("Ramp", "R",
                "Grading curve between d0 and d1: 0 Linear, 1 Smooth (smoothstep), 2 Gaussian.",
                "Linear", "Smooth", "Gaussian"));

            pManager.AddIntegerParameter("Relax", "Rx",
                "Optional Lloyd iterations, 0-50 (default 0): more regular cells, slower to build.",
                GH_ParamAccess.item);
            pManager[10].Optional = true;
            pManager.AddIntegerParameter("Seed", "Sd",
                "Optional random stream, >= 0 (default 0); the same graph gives the same cells on any OS.",
                GH_ParamAccess.item);
            pManager[11].Optional = true;
            pManager.AddNumberParameter("Smooth", "Sm",
                "Optional radius rounding the cells' corners, >= 0 (default 0). Delaunay walls want " +
                "about twice the wall thickness.", GH_ParamAccess.item);
            pManager[12].Optional = true;
            pManager.AddNumberParameter("Jitter", "J",
                "Optional seed displacement in grid pitches (default 1; 0 = a regular grid). Read only " +
                "when Distribution = Jitter.", GH_ParamAccess.item);
            pManager[13].Optional = true;
        }

        private static Param_Integer Dropdown(string name, string nick, string description, params string[] names)
        {
            var p = new Param_Integer { Name = name, NickName = nick, Description = description, Optional = false };
            for (int i = 0; i < names.Length; i++) p.AddNamedValue(names[i], i);
            p.SetPersistentData(0);
            return p;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "The porous field: zero on the cell walls or struts. Give it a thickness (Onion, " +
                "Graded Onion, Offset, Graded Offset), then clip it with the volume.", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            VolumeGoo? volumeGoo = null;
            if (!da.GetData(0, ref volumeGoo) || volumeGoo?.Value?.Core is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "No volume connected.");
                return;
            }

            int cells = 0, mode = 0, dist = 0, ramp = 0;
            da.GetData(1, ref cells);
            da.GetData(2, ref mode);
            da.GetData(3, ref dist);
            da.GetData(9, ref ramp);
            if (!InRange("Cells", cells, CellsTokens) || !InRange("Mode", mode, ModeTokens) ||
                !InRange("Distribution", dist, DistTokens) || !InRange("Ramp", ramp, RampTokens))
                return;

            // Spacing is a required input: Grasshopper itself stops the solve when it is empty.
            double spacing = double.NaN;
            da.GetData(4, ref spacing);
            if (!(spacing > 0) || double.IsInfinity(spacing))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Spacing must be > 0.");
                return;
            }

            double spacing2 = double.NaN, d0 = double.NaN, d1 = double.NaN, smooth = double.NaN, jitter = double.NaN;
            int relax = 0, seed = 0;
            bool hasS2 = da.GetData(5, ref spacing2);
            VolumeGoo? controlGoo = null;
            bool hasControl = da.GetData(6, ref controlGoo) && controlGoo?.Value?.Core != null;
            bool hasD0 = da.GetData(7, ref d0);
            bool hasD1 = da.GetData(8, ref d1);
            bool hasRelax = da.GetData(10, ref relax);
            bool hasSeed = da.GetData(11, ref seed);
            bool hasSmooth = da.GetData(12, ref smooth);
            bool hasJitter = da.GetData(13, ref jitter);

            bool graded = hasS2 && spacing2 != spacing;
            if (graded && !hasControl)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Spacing2 differs from Spacing: connect a Control field.");
                return;
            }
            if (graded && !hasD1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    "Spacing2 differs from Spacing: set d1 (the control value where the spacing is Spacing2).");
                return;
            }
            if (hasControl && !graded)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning,
                    "The Control is ignored unless Spacing2 differs from Spacing.");
            if (hasJitter && dist != 1)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    "Jitter is read only when Distribution = Jitter.");

            // A key at DualC's default is left out (null), so DualC fills it and the JSON stays
            // minimal; the control is a child only when the spacing is graded.
            FieldNode node = Field.Porous(
                volumeGoo.Value.Core, spacing,
                control: graded ? controlGoo!.Value.Core : null,
                cells: cells != 0 ? CellsTokens[cells] : null,
                mode: mode != 0 ? ModeTokens[mode] : null,
                dist: dist != 0 ? DistTokens[dist] : null,
                spacing2: graded ? spacing2 : (double?)null,
                d0: graded && hasD0 && d0 != 0 ? d0 : (double?)null,
                d1: graded ? d1 : (double?)null,
                ramp: graded && ramp != 0 ? RampTokens[ramp] : null,
                relax: hasRelax && relax != 0 ? relax : (int?)null,
                seed: hasSeed && seed != 0 ? seed : (long?)null,
                smooth: hasSmooth && smooth != 0 ? smooth : (double?)null,
                jitter: dist == 1 && hasJitter && jitter != 1 ? jitter : (double?)null);

            da.SetData(0, new VolumeGoo(graded
                ? Volume.Combine(node, volumeGoo.Value, controlGoo!.Value)
                : Volume.Combine(node, volumeGoo.Value)));
        }

        private bool InRange(string input, int index, string[] tokens)
        {
            if (index >= 0 && index < tokens.Length) return true;
            AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                $"Unknown {input} index {index} (expected 0..{tokens.Length - 1}).");
            return false;
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000110");

        protected override Bitmap? Icon => BoletusIcons.PorousShell;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
