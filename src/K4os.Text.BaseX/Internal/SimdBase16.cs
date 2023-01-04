#if NET5_0_OR_GREATER

using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace K4os.Text.BaseX.Internal;

/*
 * NOTE: .NET 5.0 its own implementation of Base16 and it is most likely very good.
 * See: https://source.dot.net/#System.Net.Primitives/src/libraries/Common/src/System/HexConverter.cs
 * It seems a little bit safer (explicit unaligned access) and potentially faster (shuffle on both encode/decode).
 * This is a little bit above my pay grade, so I will stick to my implementation which I actually understand.
 */

internal class SimdBase16: SimdTools
{
	private static int AdjustBeforeEncode(
		bool support, int sourceLength, int targetLength, int vectorSize)
	{
		sourceLength = !support || sourceLength <= 0 ? 0 : sourceLength & ~(vectorSize - 1);
		if (sourceLength > 0 && targetLength < sourceLength * 2)
			throw new ArgumentException("Output buffer is too small");

		return sourceLength;
	}

	public static unsafe int EncodeSse2(
		byte* source, int sourceLength,
		char* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeEncode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var ascii0 = nibbleToAscii[0x00];
		var asciiA = nibbleToAscii[0x0A];

		var s = source;
		var t = target;
		var l = source + sourceLength;

		var maskF = Vector128.Create((byte)0x0F);

		while (s < l)
		{
			var chunk = Sse2.LoadVector128(s);
			s += 16;

			var loDigits = Sse2.And(
				chunk,
				maskF);
			var hiDigits = Sse2.And(
				Sse2.ShiftRightLogical(chunk.AsUInt16(), 4).AsByte(),
				maskF);

			SaveAscii128(ToAscii(Sse2.UnpackLow(hiDigits, loDigits), ascii0, asciiA), t);
			t += 16;

			SaveAscii128(ToAscii(Sse2.UnpackHigh(hiDigits, loDigits), ascii0, asciiA), t);
			t += 16;
		}

		return sourceLength;
	}

	public static unsafe int EncodeSsse3(
		byte* source, int sourceLength,
		char* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeEncode(
			Ssse3.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var map = Sse2.LoadVector128(nibbleToAscii).AsByte();

		var s = source;
		var t = target;
		var l = source + sourceLength;

		var maskF = Vector128.Create((byte)0x0F);

		while (s < l)
		{
			var chunk = Sse2.LoadVector128(s);
			s += 16;

			var loDigits = Sse2.And(
				chunk,
				maskF);
			var hiDigits = Sse2.And(
				Sse2.ShiftRightLogical(chunk.AsUInt16(), 4).AsByte(),
				maskF);

			SaveAscii128(ToAscii(Sse2.UnpackLow(hiDigits, loDigits), map), t);
			t += 16;

			SaveAscii128(ToAscii(Sse2.UnpackHigh(hiDigits, loDigits), map), t);
			t += 16;
		}

		return sourceLength;
	}

	public static unsafe int EncodeAvx2(
		byte* source, int sourceLength,
		char* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeEncode(
			Avx2.IsSupported, sourceLength, targetLength, Vector256<byte>.Count);
		if (sourceLength <= 0) return 0;

		var s = source;
		var t = target;
		var l = source + sourceLength;

		var map = Avx2
			.Permute4x64(Sse2.LoadVector128(nibbleToAscii).ToVector256Unsafe().AsInt64(), PERM_0101)
			.AsByte();

		var maskF = Vector256.Create((byte)0x0F);

		while (s < l)
		{
			var chunk = Avx2
				.Permute4x64(Avx.LoadVector256(s).AsInt64(), PERM_0213)
				.AsByte();
			s += 32;

			var loDigits = Avx2.And(
				chunk,
				maskF);
			var hiDigits = Avx2.And(
				Avx2.ShiftRightLogical(chunk.AsUInt16(), 4).AsByte(),
				maskF);

			SaveAscii256(ToAscii(Avx2.UnpackLow(hiDigits, loDigits), map), t);
			t += 32;

			SaveAscii256(ToAscii(Avx2.UnpackHigh(hiDigits, loDigits), map), t);
			t += 32;
		}

		return sourceLength;
	}

	private static int AdjustBeforeDecode(
		bool support, int sourceLength, int targetLength, int vectorSize)
	{
		sourceLength = !support || sourceLength <= 0 ? 0 : sourceLength & ~(vectorSize * 2 - 1);
		if (sourceLength > 0 && targetLength * 2 < sourceLength)
			throw new ArgumentException("Output buffer is too small");

		return sourceLength;
	}

	public static unsafe int DecodeSse2(
		char* source, int sourceLength,
		byte* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeDecode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var ascii0 = nibbleToAscii[0x00];
		var asciiA = nibbleToAscii[0x0A];

		var s = source;
		var t = target;
		var l = source + sourceLength;

		var asciiMask = Vector128.Create((ushort)0xFF);

		while (s < l)
		{
			var chunk0 = Sse2.PackUnsignedSaturate(
				Sse2.And(Sse2.LoadVector128((byte*)(s + 0)).AsUInt16(), asciiMask).AsInt16(),
				Sse2.And(Sse2.LoadVector128((byte*)(s + 8)).AsUInt16(), asciiMask).AsInt16());
			s += 16;

			var chunk1 = Sse2.PackUnsignedSaturate(
				Sse2.And(Sse2.LoadVector128((byte*)(s + 0)).AsUInt16(), asciiMask).AsInt16(),
				Sse2.And(Sse2.LoadVector128((byte*)(s + 8)).AsUInt16(), asciiMask).AsInt16());
			s += 16;

			var hiDigits = Sse2.PackUnsignedSaturate(
				Sse2.And(chunk0.AsUInt16(), asciiMask).AsInt16(),
				Sse2.And(chunk1.AsUInt16(), asciiMask).AsInt16());

			var loDigits = Sse2.PackUnsignedSaturate(
				Sse2.ShiftRightLogical(chunk0.AsUInt16(), 8).AsInt16(),
				Sse2.ShiftRightLogical(chunk1.AsUInt16(), 8).AsInt16());

			hiDigits = Sse2
				.ShiftLeftLogical(FromAscii(hiDigits, ascii0, asciiA).AsUInt16(), 4)
				.AsByte();

			loDigits = FromAscii(loDigits, ascii0, asciiA);

			Sse2.Store(t, Sse2.Or(loDigits, hiDigits));
			t += 16;
		}

		return sourceLength;
	}

	public static unsafe int DecodeAvx2(
		char* source, int sourceLength,
		byte* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeDecode(
			Avx2.IsSupported, sourceLength, targetLength, Vector256<byte>.Count);
		if (sourceLength <= 0) return 0;

		var ascii0 = nibbleToAscii[0x00];
		var asciiA = nibbleToAscii[0x0A];

		var s = source;
		var t = target;
		var l = source + sourceLength;

		var asciiMask = Vector256.Create((ushort)0xFF);

		while (s < l)
		{
			var chunk0 = LoadAscii256(s);
			s += 32;

			var chunk1 = LoadAscii256(s);
			s += 32;

			var hiDigits = Avx2.PackUnsignedSaturate(
				Avx2.And(chunk0.AsUInt16(), asciiMask).AsInt16(),
				Avx2.And(chunk1.AsUInt16(), asciiMask).AsInt16());

			var loDigits = Avx2.PackUnsignedSaturate(
				Avx2.ShiftRightLogical(chunk0.AsUInt16(), 8).AsInt16(),
				Avx2.ShiftRightLogical(chunk1.AsUInt16(), 8).AsInt16());

			hiDigits = Avx2
				.ShiftLeftLogical(FromAscii(hiDigits, ascii0, asciiA).AsUInt16(), 4)
				.AsByte();

			loDigits = FromAscii(loDigits, ascii0, asciiA);

			SaveBytes256(Avx2.Or(loDigits, hiDigits), t);

			t += 32;
		}

		return sourceLength;
	}
}

#endif