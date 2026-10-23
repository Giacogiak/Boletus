using System;
using System.Collections.Generic;

namespace Boletus.Core.LivePreview
{
    /// <summary>
    /// The command line each side-car takes, built here so it is tested without a process.
    /// <see cref="ViewerKind.FieldView"/>: <c>&lt;graph&gt; [--bounds x0,y0,z0,x1,y1,z1]</c>
    /// (DualC's own flag; bounds reach it at launch only). <see cref="ViewerKind.Editor"/>:
    /// <c>&lt;graph&gt; [--explore] [--title &lt;text&gt;]</c> — no bounds flag, because they ride in the
    /// graph document (<see cref="LivePreviewDocument"/>) and so stay live on reload. The
    /// editor's two flags are passed only once the pinned editor has them (the <c>explore</c>
    /// switch; the version-trap rule: gated by the pin, never probed).
    /// </summary>
    public static class ViewerArguments
    {
        public const string BoundsFlag = "--bounds";
        public const string ExploreFlag = "--explore";
        public const string TitleFlag = "--title";

        public static IReadOnlyList<string> Build(ViewerKind kind, string graphPath, ViewBounds? bounds,
                                                  string? title = null, bool explore = false)
        {
            if (string.IsNullOrEmpty(graphPath)) throw new ArgumentException("graphPath is required.", nameof(graphPath));
            var args = new List<string> { graphPath };
            switch (kind)
            {
                case ViewerKind.FieldView:
                    if (bounds is { } b)
                    {
                        if (!b.IsValid) throw new ArgumentException("Bounds need six finite numbers with min < max per axis.", nameof(bounds));
                        args.Add(BoundsFlag);
                        args.Add(b.ToFlagValue());
                    }
                    break;
                case ViewerKind.Editor:
                    if (explore) args.Add(ExploreFlag);
                    if (!string.IsNullOrWhiteSpace(title))
                    {
                        args.Add(TitleFlag);
                        args.Add(title!);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown viewer kind.");
            }
            return args;
        }
    }
}
