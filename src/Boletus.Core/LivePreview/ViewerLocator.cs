using System;
using System.IO;

namespace Boletus.Core.LivePreview
{
    /// <summary>Where the two side-car executables are, relative to the plugin's own folder.</summary>
    public sealed class ViewerPaths
    {
        public ViewerPaths(string fieldViewExe, string editorExe)
        {
            FieldViewExe = fieldViewExe ?? throw new ArgumentNullException(nameof(fieldViewExe));
            EditorExe = editorExe ?? throw new ArgumentNullException(nameof(editorExe));
        }

        /// <summary><c>dualc_field_view.exe</c> beside the <c>.gha</c>.</summary>
        public string FieldViewExe { get; }

        /// <summary><c>editor/DualCEditor.exe</c> beside the <c>.gha</c> (with its Qt DLLs), or the override.</summary>
        public string EditorExe { get; }

        public bool FieldViewExists => File.Exists(FieldViewExe);
        public bool EditorExists => File.Exists(EditorExe);

        /// <summary>The executable for <paramref name="kind"/>, whether or not it exists.</summary>
        public string ExeFor(ViewerKind kind) => kind == ViewerKind.Editor ? EditorExe : FieldViewExe;
    }

    /// <summary>The viewer that will actually be launched, after the fallback rule.</summary>
    public readonly struct ViewerChoice
    {
        public ViewerChoice(ViewerKind kind, string exe, bool fellBack)
        {
            Kind = kind; Exe = exe; FellBack = fellBack;
        }

        public ViewerKind Kind { get; }
        public string Exe { get; }

        /// <summary>True when the editor was asked for, is absent, and the field view stands in.</summary>
        public bool FellBack { get; }
    }

    /// <summary>
    /// Finds the side-car executables and applies the one fallback rule: the editor, when
    /// asked for but not shipped, gives way to <c>dualc_field_view.exe</c> with a warning.
    /// Rhino-free so the rule is unit-tested on the plain runner.
    /// </summary>
    public static class ViewerLocator
    {
        public const string FieldViewExeName = "dualc_field_view.exe";
        public const string EditorFolderName = "editor";
        public const string EditorExeName = "DualCEditor.exe";

        /// <summary>The environment variable that points <c>Live Preview</c> at a DualCEditor build
        /// elsewhere (co-development; the shipped folder otherwise).</summary>
        public const string EditorOverrideVariable = "BOLETUS_EDITOR_EXE";

        /// <summary>
        /// The paths under <paramref name="pluginDir"/> (the <c>.gha</c>'s folder):
        /// <c>dualc_field_view.exe</c> and <c>editor/DualCEditor.exe</c>, the latter replaced by
        /// <paramref name="editorOverride"/> when that is non-empty.
        /// </summary>
        public static ViewerPaths Locate(string pluginDir, string? editorOverride = null)
        {
            string dir = pluginDir ?? string.Empty;
            string editor = string.IsNullOrWhiteSpace(editorOverride)
                ? Path.Combine(dir, EditorFolderName, EditorExeName)
                : editorOverride!;
            return new ViewerPaths(Path.Combine(dir, FieldViewExeName), editor);
        }

        /// <summary>
        /// Resolve <paramref name="requested"/> against what exists: the field view when it is
        /// asked for and present; the editor when it is asked for and present; the field view
        /// with <see cref="ViewerChoice.FellBack"/> when the editor is asked for but absent and
        /// the field view is there; null when nothing launchable exists.
        /// </summary>
        public static ViewerChoice? Resolve(ViewerKind requested, ViewerPaths paths)
        {
            if (paths is null) throw new ArgumentNullException(nameof(paths));
            if (requested == ViewerKind.Editor)
            {
                if (paths.EditorExists) return new ViewerChoice(ViewerKind.Editor, paths.EditorExe, fellBack: false);
                if (paths.FieldViewExists) return new ViewerChoice(ViewerKind.FieldView, paths.FieldViewExe, fellBack: true);
                return null;
            }
            return paths.FieldViewExists ? new ViewerChoice(ViewerKind.FieldView, paths.FieldViewExe, fellBack: false) : (ViewerChoice?)null;
        }
    }
}
