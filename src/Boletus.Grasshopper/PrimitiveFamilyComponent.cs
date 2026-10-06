using System.Collections.Generic;
using System.Linq;
using Boletus.Core;
using Boletus.Core.FieldGraph;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Shared shell of the catalog-driven primitive components (D-25): a type dropdown, then the
    /// family's fixed point slots and number slots from <see cref="PrimitiveCatalog"/>. The slot
    /// count never changes, so a saved canvas never loses a wire; what changes with the type is each
    /// slot's name, nickname and tooltip, so the canvas reads "Height" rather than "P1". The number
    /// slots carry no persistent data: an empty one takes the selected shape's default.
    /// </summary>
    public abstract class PrimitiveFamilyComponent : GH_Component
    {
        private readonly PrimitiveFamily _family;
        private readonly Point3d[] _pointDefaults;
        private readonly HashSet<int> _typesSeen = new HashSet<int>();

        protected PrimitiveFamilyComponent(string name, string nickname, string description,
            PrimitiveFamily family, Point3d[] pointDefaults)
            : base(name, nickname, description, "Boletus", "Sources")
        {
            _family = family;
            _pointDefaults = pointDefaults;
        }

        private int NumberStart => 1 + _family.PointSlots.Count;

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            var type = new Param_Integer
            {
                Name = "Type",
                NickName = "T",
                Description = "Shape: " + string.Join(", ", _family.Shapes.Select(s => $"{s.Index} {s.Name}")) + ".",
            };
            foreach (var s in _family.Shapes) type.AddNamedValue(s.Name, s.Index);
            type.SetPersistentData(0);
            pManager.AddParameter(type);

            var first = _family.Shapes[0];
            for (int i = 0; i < _family.PointSlots.Count; i++)
            {
                var use = first.Points[i];
                pManager.AddPointParameter(use?.Name ?? _family.PointSlots[i], use?.NickName ?? _family.PointSlots[i],
                    use?.Description ?? "", GH_ParamAccess.item, _pointDefaults[i]);
            }
            for (int i = 0; i < _family.NumberSlots.Count; i++)
            {
                var use = first.Numbers[i];
                int at = pManager.AddNumberParameter(use?.Name ?? _family.NumberSlots[i], use?.NickName ?? _family.NumberSlots[i],
                    use?.Description ?? "", GH_ParamAccess.item);
                pManager[at].Optional = true;
            }
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
            pManager.AddParameter(new VolumeParameter(), "Volume", "V", "The primitive volume.", GH_ParamAccess.item);
        }

        protected override void BeforeSolveInstance()
        {
            _typesSeen.Clear();
            base.BeforeSolveInstance();
        }

        protected override void SolveInstance(IGH_DataAccess da)
        {
            int type = 0;
            da.GetData(0, ref type);
            _typesSeen.Add(type);

            var shape = _family.Find(type);
            if (shape is null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error,
                    $"Unknown {_family.Name} type {type} (expected 0..{_family.Shapes.Count - 1}).");
                return;
            }

            var points = new (double X, double Y, double Z)[_family.PointSlots.Count];
            for (int i = 0; i < points.Length; i++)
            {
                Point3d p = _pointDefaults[i];
                da.GetData(1 + i, ref p);
                points[i] = (p.X, p.Y, p.Z);
            }

            var numbers = new double?[_family.NumberSlots.Count];
            for (int i = 0; i < numbers.Length; i++)
            {
                double v = double.NaN;
                if (da.GetData(NumberStart + i, ref v)) numbers[i] = v;
            }

            if (shape.IsInfinite)
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark,
                    $"{shape.Name} is infinite — clip it or set bounds at Contour.");

            da.SetData(0, new VolumeGoo(new Volume(shape.Build(points, numbers))));
        }

        protected override void AfterSolveInstance()
        {
            base.AfterSolveInstance();
            // One type across the solution: name the slots after it. Several (a list on Type):
            // the slots keep the generic names, since no one shape describes them.
            var shape = _typesSeen.Count == 1 ? _family.Find(_typesSeen.First()) : null;
            Message = shape?.Name ?? (_typesSeen.Count > 1 ? "(mixed)" : null);
            if (Relabel(shape)) Attributes?.ExpireLayout();
        }

        private bool Relabel(PrimitiveShape? shape)
        {
            bool changed = false;
            for (int i = 0; i < _family.PointSlots.Count; i++)
                changed |= Label(Params.Input[1 + i], shape?.Points[i], _family.PointSlots[i], shape);
            for (int i = 0; i < _family.NumberSlots.Count; i++)
                changed |= Label(Params.Input[NumberStart + i], shape?.Numbers[i], _family.NumberSlots[i], shape);
            return changed;
        }

        private static bool Label(IGH_Param param, PrimitiveSlotUse? use, string generic, PrimitiveShape? shape)
        {
            string name = use?.Name ?? generic;
            string nick = use?.NickName ?? generic;
            string desc = use?.Description
                ?? (shape is null ? "Meaning depends on the type." : $"Ignored by {shape.Name}.");
            if (param.Name == name && param.NickName == nick && param.Description == desc) return false;
            param.Name = name;
            param.NickName = nick;
            param.Description = desc;
            return true;
        }
    }
}
