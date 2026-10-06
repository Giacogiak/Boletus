using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Boletus.Core
{
    /// <summary>DualC C-ABI status codes (the <c>int</c> return of every fallible call).</summary>
    public enum DualcStatus
    {
        Ok       = 0, // DUALC_OK
        Bounds   = 1, // DUALC_ERR_BOUNDS  -- unbounded field: set HasBounds + bounds
        Io       = 2, // DUALC_ERR_IO      -- unknown extension or writer/file failure
        Graph    = 3, // DUALC_ERR_GRAPH   -- field-graph parse/build error (+ locator)
        Usage    = 4, // DUALC_ERR_USAGE   -- bad argument (e.g. null handle)
        Unknown  = 5, // DUALC_ERR_UNKNOWN -- any other failure
        Cancelled = 6, // DUALC_CANCELLED  -- the cancel token was requested; nothing written (ABI 0.5.0)
    }

    /// <summary>
    /// Thrown when a native call returns a non-OK status. <see cref="Code"/> is the
    /// raw <c>DUALC_ERR_*</c> value; for <see cref="DualcStatus.Graph"/> the message
    /// carries a locator (a JSON pointer for JSON input, a character offset for expr).
    /// </summary>
    public sealed class DualcException : Exception
    {
        public DualcStatus Code { get; }

        public DualcException(int code, string message)
            : base($"DualC error {code} ({(DualcStatus)code}): {message}")
        {
            Code = (DualcStatus)code;
        }
    }

    internal static class Native
    {
        private const int ErrBufLen = 512;

        /// <summary>NUL-terminated UTF-8 bytes for a <c>const char*</c> in-param.</summary>
        public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes((s ?? string.Empty) + '\0');

        public static byte[] NewErrBuffer() => new byte[ErrBufLen];

        /// <summary>Read a NUL-terminated UTF-8 string out of a byte buffer.</summary>
        public static string ReadUtf8(byte[] buf)
        {
            int n = Array.IndexOf(buf, (byte)0);
            if (n < 0) n = buf.Length;
            return Encoding.UTF8.GetString(buf, 0, n);
        }

        /// <summary>Read a NUL-terminated UTF-8 string out of a native pointer
        /// (netstandard2.0 has no Marshal.PtrToStringUTF8).</summary>
        public static string ReadUtf8(IntPtr ptr)
        {
            if (ptr == IntPtr.Zero) return string.Empty;
            var bytes = new List<byte>(32);
            for (int i = 0; ; i++)
            {
                byte b = Marshal.ReadByte(ptr, i);
                if (b == 0) break;
                bytes.Add(b);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        /// <summary>Throw a <see cref="DualcException"/> if <paramref name="rc"/> is non-OK.</summary>
        public static void Check(int rc, byte[] err)
        {
            if (rc == (int)DualcStatus.Ok) return;
            throw new DualcException(rc, ReadUtf8(err));
        }
    }
}
