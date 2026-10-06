using System;
using System.Collections.Generic;
using Boletus.Core.FieldGraph;

namespace Boletus.Core
{
    /// <summary>
    /// The runtime "volume" — Boletus's universal in-memory currency. It is <b>meshless and
    /// lightweight</b>: a symbolic field-graph (<see cref="Core"/>, a DualC <see cref="FieldNode"/>
    /// tree) plus, for any <c>mesh</c>/<c>winding</c> leaves, the source geometry kept <b>in
    /// memory</b> (<see cref="Meshes"/>). Nothing is baked or written to disk while a volume is
    /// composed; the geometry is handed to DualC only when a Contour/Export terminal consumes it
    /// (in RAM via <see cref="VolumeResolver"/> + <c>DualcField.FromJson(json, meshes)</c>). The
    /// boundary clip is NOT part of a volume — it is an explicit <c>intersection</c> emitted by the
    /// Onion/Graded-onion/Boolean components.
    /// </summary>
    /// <remarks>
    /// This type is deliberately Rhino-free (it lives in <c>Boletus.Core</c>): only its Grasshopper
    /// wrappers (<c>VolumeGoo</c>/<c>VolumeParameter</c>) belong to the <c>.gha</c>.
    /// </remarks>
    public sealed class Volume
    {
        /// <summary>The symbolic field. Lightweight — passing it between components is ~free.</summary>
        public FieldNode Core { get; }

        /// <summary>In-memory geometry for the graph's mesh leaves, keyed by content-hash id.</summary>
        public IReadOnlyDictionary<string, MeshBuffer> Meshes { get; }

        /// <summary>Placeholder scheme for a mesh-leaf path that resolves to an in-memory buffer
        /// (rewritten to <c>mesh(id=…)</c> for the in-memory ABI, or a temp-file path for the legacy
        /// fallback, at the terminal).</summary>
        public const string MemoryScheme = "mem://";

        private static readonly IReadOnlyDictionary<string, MeshBuffer> Empty =
            new Dictionary<string, MeshBuffer>(0, StringComparer.Ordinal);

        public Volume(FieldNode core, IReadOnlyDictionary<string, MeshBuffer>? meshes = null)
        {
            Core = core ?? throw new ArgumentNullException(nameof(core));
            Meshes = meshes ?? Empty;
        }

        /// <summary>A new volume with a different <see cref="Core"/> but the same mesh buffers.</summary>
        public Volume WithCore(FieldNode core) => new Volume(core, Meshes);

        /// <summary>A new volume with <paramref name="core"/> and the mesh buffers of every input
        /// merged (keys are content hashes, so identical meshes dedupe).</summary>
        public static Volume Combine(FieldNode core, params Volume[] inputs)
        {
            if (inputs is null || inputs.Length == 0) return new Volume(core);
            var merged = new Dictionary<string, MeshBuffer>(StringComparer.Ordinal);
            foreach (var v in inputs)
            {
                if (v is null) continue;
                foreach (var kv in v.Meshes) merged[kv.Key] = kv.Value;
            }
            return new Volume(core, merged);
        }

        /// <summary>
        /// True if <paramref name="node"/> is a <c>mesh</c>/<c>winding</c> leaf whose <c>path</c> is a
        /// <see cref="MemoryScheme"/> placeholder, yielding the in-memory buffer <paramref name="id"/>.
        /// The single source of truth for detecting an in-memory mesh leaf (shared by the resolver
        /// and the legacy materializer).
        /// </summary>
        public static bool TryGetMemoryMeshId(FieldNode node, out string id)
        {
            id = string.Empty;
            if (node is null) return false;
            if (node.Op != "mesh" && node.Op != "winding") return false;
            if (!node.Params.TryGetValue("path", out var pathVal) || pathVal.Kind != FieldValueKind.Text)
                return false;
            if (!pathVal.AsText.StartsWith(MemoryScheme, StringComparison.Ordinal)) return false;
            id = pathVal.AsText.Substring(MemoryScheme.Length);
            return true;
        }
    }
}
