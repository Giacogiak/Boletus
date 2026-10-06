using Boletus.Core;
using Grasshopper.Kernel.Types;

namespace Boletus.Grasshopper
{
    /// <summary>
    /// Grasshopper data wrapper around a <see cref="Volume"/> — the payload that travels along
    /// Volume wires between Boletus components.
    /// </summary>
    public sealed class VolumeGoo : GH_Goo<Volume>
    {
        public VolumeGoo() { }

        public VolumeGoo(Volume volume) { Value = volume; }

        public override bool IsValid => Value?.Core != null;

        public override string TypeName => "Volume";

        public override string TypeDescription => "A DualC implicit volume (field graph + in-memory mesh sources).";

        // Volume/FieldNode are immutable, so sharing the reference is a safe duplicate.
        public override IGH_Goo Duplicate() => new VolumeGoo(Value);

        public override string ToString() =>
            Value?.Core is null ? "Null Volume" : $"Volume [{Value.Core.Op}]";
    }
}
