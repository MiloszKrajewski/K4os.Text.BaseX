#if NET5_0_OR_GREATER
using System.Runtime.Intrinsics.X86;
#endif

namespace K4os.Text.BaseX.Internal;

/// <summary>
/// BaseX SIMD settings. Available only on .NET 5+.
/// </summary>
public class SimdSettings
{
	/// <summary>Allows using AVX2.</summary>
	public static bool AllowAvx2 { get; set; } = true;
	
	/// <summary>Allows using SSSE3.</summary>
	public static bool AllowSsse3 { get; set; } = true;
	
	/// <summary>Allows using SSE2.</summary>
	public static bool AllowSse2 { get; set; } = true;

	#if !NET5_0_OR_GREATER
	
	/// <summary>Indicates if any of SIMD instruction sets are supported.</summary>
	public const bool IsSimdSupported = false;

	#else

	/// <summary>Indicates if any of SIMD instruction sets are supported.</summary>
	public static bool IsSimdSupported => Sse2.IsSupported; // SSE2 or above
	
	#endif
}
