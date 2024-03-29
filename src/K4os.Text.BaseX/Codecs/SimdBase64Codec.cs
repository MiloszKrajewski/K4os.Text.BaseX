#if NET5_0_OR_GREATER

using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using K4os.Text.BaseX.Internal;

#endif

namespace K4os.Text.BaseX.Codecs;

#if NET5_0_OR_GREATER

/// <summary>Base64 codec implemented with SIMD instructions.</summary>
public class SimdBase64Codec: Base64Codec
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe int UpdateAfterEncode(
		int chunks,
		ref byte* source, ref int sourceLength,
		ref char* target, ref int targetLength)
	{
		var read = chunks * 3;
		var written = chunks * 4;
		source += read;
		sourceLength -= read;
		target += written;
		targetLength -= written;
		return written;
	}

	/// <inheritdoc />
	protected override unsafe int EncodeImpl(
		byte* source, int sourceLength,
		char* target, int targetLength)
	{
		if (sourceLength < 16 || !SimdSettings.IsSimdSupported)
			return base.EncodeImpl(source, sourceLength, target, targetLength);

		var written = 0;

		if (Avx2.IsSupported && sourceLength >= 32 && SimdSettings.AllowAvx2)
		{
			written += UpdateAfterEncode(
				SimdBase64.Encode_AVX2(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (Ssse3.IsSupported && sourceLength >= 16 && SimdSettings.AllowSsse3)
		{
			written += UpdateAfterEncode(
				SimdBase64.Encode_SSSE3(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (sourceLength > 0)
		{
			// no UpdateAfterEncode as this is last step
			written += base.EncodeImpl(source, sourceLength, target, targetLength);
		}

		return written;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe int UpdateAfterDecode(
		int chunks,
		ref char* source, ref int sourceLength,
		ref byte* target, ref int targetLength)
	{
		var read = chunks * 4;
		var written = chunks * 3;
		source += read;
		sourceLength -= read;
		target += written;
		targetLength -= written;
		return written;
	}

	/// <inheritdoc />
	protected override unsafe int DecodeImpl(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		if (sourceLength < 16 || !SimdSettings.IsSimdSupported)
			return base.DecodeImpl(source, sourceLength, target, targetLength);

		var written = 0;

		if (Avx2.IsSupported && sourceLength >= 32 && SimdSettings.AllowAvx2)
		{
			written += UpdateAfterDecode(
				SimdBase64.Decode_AVX2(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (Ssse3.IsSupported && sourceLength >= 16 && SimdSettings.AllowSsse3)
		{
			written += UpdateAfterDecode(
				SimdBase64.Decode_SSSE3(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (sourceLength > 0)
		{
			// no UpdateAfterEncode as this is last step
			written += base.DecodeImpl(source, sourceLength, target, targetLength);
		}

		return written;
	}
}

#endif
