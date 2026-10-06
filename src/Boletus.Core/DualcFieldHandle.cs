using System;
using Microsoft.Win32.SafeHandles;

namespace Boletus.Core
{
    /// <summary>
    /// Owns a native <c>DualcField*</c>. Derives from
    /// <see cref="SafeHandleZeroOrMinusOneIsInvalid"/> so a NULL handle (the value
    /// the ABI writes on a failed create) is treated as invalid and never released,
    /// and a live handle is released exactly once — even on finalization or an
    /// async teardown — by calling <c>dualc_field_destroy</c>.
    /// </summary>
    internal sealed class DualcFieldHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Parameterless ctor required so the interop marshaller can construct the
        // instance for an `out DualcFieldHandle` parameter.
        public DualcFieldHandle() : base(ownsHandle: true) { }

        protected override bool ReleaseHandle()
        {
            NativeMethods.dualc_field_destroy(handle);
            return true;
        }
    }
}
