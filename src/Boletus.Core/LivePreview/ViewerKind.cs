namespace Boletus.Core.LivePreview
{
    /// <summary>
    /// Which side-car executable <c>Live Preview</c> launches on its temp graph file. The
    /// integer values are the component's <c>Viewer</c> dropdown (a Grasshopper
    /// <c>Param_Integer</c> with named values), so they are part of the usage contract
    /// (<c>docs/command_reference/04-terminals/03-live-preview.md</c>) and never renumbered.
    /// </summary>
    public enum ViewerKind
    {
        /// <summary>DualC's reference GPU viewer <c>dualc_field_view.exe</c>: keyboard and
        /// console controls, no on-screen widgets; beside the <c>.gha</c>.</summary>
        FieldView = 0,

        /// <summary>DualCEditor in exploration mode: the Qt window with the section toolbar,
        /// the Objects, Parameters and Log docks; under <c>editor/</c> beside the <c>.gha</c>.</summary>
        Editor = 1,
    }
}
