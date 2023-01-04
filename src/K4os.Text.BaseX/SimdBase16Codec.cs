#if NET5_0_OR_GREATER

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;
using K4os.Text.BaseX.Internal;

namespace K4os.Text.BaseX;

/// <summary>
/// SSE2/AVX2 implementation of Base16 encoder.
/// </summary>
public class SimdBase16Codec: Base16Codec
{
	/// <summary>
	/// Creates new instance of <see cref="SimdBase16Codec"/>. Defaults to upper case.
	/// </summary>
	public SimdBase16Codec(): base(false) { }

	/// <summary>Creates new instance of <see cref="SimdBase16Codec"/>.</summary>
	/// <param name="lowerCase">Indicates that codec should use lower case when encoding.</param>
	public SimdBase16Codec(bool lowerCase): base(lowerCase) { }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe int UpdateAfterEncode(
		int written,
		ref byte* source, ref int sourceLength,
		ref char* target, ref int targetLength)
	{
		Debug.Assert((written & 0x01) == 0);

		source += written >> 1;
		sourceLength -= written >> 1;
		target += written;
		targetLength -= written;
		return written;
	}

	/// <inheritdoc />
	protected override unsafe int EncodeImpl(
		byte* source, int sourceLength, char* target, int targetLength)
	{
		if (sourceLength < 16 || !SimdSettings.IsSimdSupported)
			return base.EncodeImpl(source, sourceLength, target, targetLength);

		var written = 0;

		if (Avx2.IsSupported && sourceLength >= 32 && SimdSettings.AllowAvx2)
		{
			written += UpdateAfterEncode(
				EncodeAvx2(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (Ssse3.IsSupported && sourceLength >= 16 && SimdSettings.AllowSsse3)
		{
			written += UpdateAfterEncode(
				EncodeSsse3(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (Sse2.IsSupported && sourceLength >= 16 && SimdSettings.AllowSse2)
		{
			written += UpdateAfterEncode(
				EncodeSse2(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (sourceLength > 0)
		{
			// no UpdateAfterEncode as this is last step
			written += base.EncodeImpl(source, sourceLength, target, targetLength);
		}

		return written;
	}

	private unsafe int EncodeSse2(byte* source, int sourceLength, char* target, int targetLength)
	{
		fixed (byte* nibbleToAscii = ByteToUtf8)
		{
			var read = SimdBase16.EncodeSse2(
				source, sourceLength,
				target, targetLength,
				(sbyte*)nibbleToAscii);
			return read << 1;
		}
	}

	private unsafe int EncodeSsse3(byte* source, int sourceLength, char* target, int targetLength)
	{
		fixed (byte* nibbleToAscii = ByteToUtf8)
		{
			var read = SimdBase16.EncodeSsse3(
				source, sourceLength,
				target, targetLength,
				(sbyte*)nibbleToAscii);
			return read << 1;
		}
	}

	private unsafe int EncodeAvx2(byte* source, int sourceLength, char* target, int targetLength)
	{
		fixed (byte* nibbleToAscii = ByteToUtf8)
		{
			var read = SimdBase16.EncodeAvx2(
				source, sourceLength,
				target, targetLength,
				(sbyte*)nibbleToAscii);
			return read << 1;
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe int UpdateAfterDecode(
		int written,
		ref char* source, ref int sourceLength,
		ref byte* target, ref int targetLength)
	{
		source += written << 1;
		sourceLength -= written << 1;
		target += written;
		targetLength -= written;
		return written;
	}

	/// <inheritdoc />
	protected override unsafe int DecodeImpl(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		if (sourceLength < 32 || !SimdSettings.IsSimdSupported)
			return base.DecodeImpl(source, sourceLength, target, targetLength);

		var written = 0;

		if (Avx2.IsSupported && sourceLength >= 64 && SimdSettings.AllowAvx2)
		{
			written += UpdateAfterDecode(
				DecodeAvx2(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (Sse2.IsSupported && sourceLength >= 32 && SimdSettings.AllowSse2)
		{
			written += UpdateAfterDecode(
				DecodeSse2(source, sourceLength, target, targetLength),
				ref source, ref sourceLength, ref target, ref targetLength);
		}

		if (sourceLength > 0)
		{
			// no UpdateAfterDecode as this is last step
			written += base.DecodeImpl(source, sourceLength, target, targetLength);
		}

		return written;
	}

	private unsafe int DecodeSse2(char* source, int sourceLength, byte* target, int targetLength)
	{
		fixed (byte* nibbleToAscii = ByteToUtf8)
		{
			var read = SimdBase16.DecodeSse2(
				source, sourceLength,
				target, targetLength,
				(sbyte*)nibbleToAscii);
			return read >> 1;
		}
	}

	private unsafe int DecodeAvx2(char* source, int sourceLength, byte* target, int targetLength)
	{
		fixed (byte* nibbleToAscii = ByteToUtf8)
		{
			var read = SimdBase16.DecodeAvx2(
				source, sourceLength,
				target, targetLength,
				(sbyte*)nibbleToAscii);
			return read >> 1;
		}
	}
}

#endif