using Microsoft.Win32.SafeHandles;

namespace Boletus.Core
{
    /// <summary>
    /// Owns a native <c>DualcCancelToken*</c> (DualC ABI 0.5.0): one sticky atomic flag the host
    /// requests from any thread to make a <c>*_with_progress</c> call return
    /// <c>DUALC_CANCELLED</c> at its next checkpoint. One token per job — it cannot be reset.
    /// </summary>
    /// <remarks>
    /// The token must outlive every native call it was passed to: the handle is created on the
    /// thread that runs the call and disposed only after the call returned, and the
    /// <see cref="System.Threading.CancellationTokenRegistration"/> that forwards a managed cancel
    /// into <c>dualc_cancel_token_request</c> is disposed <b>before</b> the handle, so a late
    /// cancel never touches a destroyed token (the one use-after-free in this API).
    /// <c>dualc_cancel_token_request</c> is the only function on a token that may run concurrently
    /// with a call using it.
    /// </remarks>
    internal sealed class DualcCancelTokenHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        // Parameterless ctor required so the interop marshaller can construct the instance for the
        // SafeHandle return of dualc_cancel_token_create.
        public DualcCancelTokenHandle() : base(ownsHandle: true) { }

        protected override bool ReleaseHandle()
        {
            NativeMethods.dualc_cancel_token_destroy(handle);
            return true;
        }
    }
}
