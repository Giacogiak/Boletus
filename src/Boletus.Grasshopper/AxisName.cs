namespace Boletus.Grasshopper
{
    /// <summary>
    /// Maps the axis-dropdown integer (0/1/2) onto DualC's accepted axis tokens ("x"/"y"/"z"). The
    /// strings are the exact values the twist/bend ops round-trip through DualC's <c>--dump-json</c>
    /// (gated by the Core vocabulary tests) — do not paraphrase them. Returns <c>null</c> for an
    /// out-of-range index so the caller can raise a runtime error.
    /// </summary>
    internal static class AxisName
    {
        public static string? From(int axis) => axis switch
        {
            0 => "x",
            1 => "y",
            2 => "z",
            _ => null,
        };
    }
}
