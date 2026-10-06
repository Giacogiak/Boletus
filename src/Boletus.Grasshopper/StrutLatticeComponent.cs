using System;
using System.Drawing;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Source: a periodic strut (wireframe-crystal) lattice — sc/bcc/fcc/octet. Unlike the TPMS
    /// surfaces, a strut lattice is already a true SDF (a union of exact capsules / round-cones), so
    /// it needs <b>no Normalize</b> before a metric op. Like a TPMS it is <b>infinite</b> — clip it
    /// (a Boolean with a solid, or an Onion boundary) or set Min/Max at the terminal. Optional
    /// <c>NodeRadius</c> tapers the struts (fat nodes, pinched spans) when it differs from Radius.
    /// </summary>
    public sealed class StrutLatticeComponent : GH_Component
    {
        public StrutLatticeComponent()
            : base("Strut Lattice", "Strut",
                   "A periodic strut (wireframe-crystal) lattice: 0 sc, 1 bcc, 2 fcc, 3 octet. " +
                   "Already metric (a true SDF) - no Normalize needed. Infinite extent: clip it with " +
                   "a Boolean/Onion boundary or set Min/Max at the terminal. NodeRadius (optional) " +
                   "tapers the struts when it differs from Radius; omit for uniform struts.",
                   "Boletus", "Sources")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var type = new Param_Integer
            {
                Name = "Type",
                NickName = "T",
                Description =
                    "Crystal type: 0 sc (simple cubic), 1 bcc (body-centered), 2 fcc " +
                    "(face-centered), 3 octet (octet truss).",
                Optional = false,
            };
            type.AddNamedValue("sc", 0);
            type.AddNamedValue("bcc", 1);
            type.AddNamedValue("fcc", 2);
            type.AddNamedValue("octet", 3);
            type.SetPersistentData(1); // bcc — the common default
            pManager.AddParameter(type);

            pManager.AddPointParameter("Center", "Ctr",
                "World position of a unit-cell centre (shifts the whole tiling; optional, default origin).",
                GH_ParamAccess.item);
            pManager[1].Optional = true;

            pManager.AddNumberParameter("Wavelength", "W",
                "Unit-cell side = tiling period (optional; engine default 1 if omitted).",
                GH_ParamAccess.item);
            pManager[2].Optional = true;

            pManager.AddNumberParameter("Radius", "R",
                "Strut half-thickness in world units (beam is 2*R across). Keep R < Wavelength/2 for " +
                "an open lattice.", GH_ParamAccess.item, 0.1);

            pManager.AddNumberParameter("NodeRadius", "NR",
                "Optional half-thickness at the strut end-nodes -> tapered struts when it differs " +
                "from Radius (2-4x fatter nodes is the usual look). Omit for uniform struts.",
                GH_ParamAccess.item);
            pManager[4].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V",
                "The strut-lattice field (infinite, metric).", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            int type = 1;
            da.GetData(0, ref type);

            Point3d center = Point3d.Origin;
            bool hasCenter = da.GetData(1, ref center);
            (double X, double Y, double Z)? c = hasCenter ? (center.X, center.Y, center.Z) : ((double, double, double)?)null;

            double wavelength = double.NaN;
            bool hasW = da.GetData(2, ref wavelength);
            double? w = hasW ? wavelength : (double?)null;

            double radius = 0.1;
            da.GetData(3, ref radius);

            double nodeRadius = double.NaN;
            bool hasNr = da.GetData(4, ref nodeRadius);
            double? nr = hasNr ? nodeRadius : (double?)null;

            FieldNode? node = type switch
            {
                0 => Field.Sc(w, radius, nr, c),
                1 => Field.Bcc(w, radius, nr, c),
                2 => Field.Fcc(w, radius, nr, c),
                3 => Field.Octet(w, radius, nr, c),
                _ => null,
            };

            if (node is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Unknown crystal type index {type} (expected 0..3).");
                return;
            }

            // A strut lattice is infinite, like a TPMS — remind the user to bound it.
            AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                "Strut lattice is infinite — clip it with a Boolean/Onion boundary, or set Min/Max at the terminal.");

            da.SetData(0, new VolumeGoo(new Volume(node)));
        }

        public override Guid ComponentGuid =>
            new Guid("4F1B2A30-0000-4000-8000-000000000021");

        protected override Bitmap? Icon => BoletusIcons.Strut;

        public override GH_Exposure Exposure => GH_Exposure.primary;
    }
}
