#if NET5_0_OR_GREATER

using System;
using System.Runtime.CompilerServices;
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
	// ReSharper disable InconsistentNaming

	private const sbyte ascii0 = (sbyte)'0';
	private const sbyte ascii9 = (sbyte)'9';
	private const sbyte lowerA = (sbyte)'a';
	private const sbyte upperA = (sbyte)'A';

	// ReSharper disable once IdentifierTypo
	// adjusted lower case ascii a after adding 0
	private const sbyte alcaaaa0 = lowerA - ascii0 - 10;

	private const sbyte sbyte9 = 9;
	private const byte byte0x0F = 0x0F;
	private const sbyte sbyte0x20 = 0x20;
	private const ushort ushort0x00FF = 0x00FF;

	// ReSharper restore InconsistentNaming

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector128<sbyte> ToAscii_SSE2(
		Vector128<byte> digits, sbyte aaa) =>
		Sse2.Add(
			Sse2.Add(digits.AsSByte(), Vector128.Create(ascii0)),
			Sse2.And(
				Vector128.Create(aaa),
				Sse2.CompareGreaterThan(digits.AsSByte(), Vector128.Create(sbyte9)))
		).AsSByte();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector128<sbyte> ToAscii_SSSE3(
		Vector128<byte> digits, Vector128<sbyte> ascii) =>
		Ssse3.Shuffle(ascii, digits.AsSByte());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector256<sbyte> ToAscii_AVX2(
		Vector256<byte> digits, Vector256<sbyte> ascii) =>
		Avx2.Shuffle(ascii, digits.AsSByte());

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static int AdjustBeforeEncode(
		bool support, int sourceLength, int targetLength, int vectorSize)
	{
		if (!support || sourceLength <= 0) return 0;

		sourceLength &= ~(vectorSize - 1);

		if (targetLength < sourceLength * 2)
			throw new ArgumentException("Output buffer is too small");

		return sourceLength;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static unsafe int Encode_SSE2(
		byte* source, int sourceLength,
		char* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeEncode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		// this is "adjusted ascii A after adding 0" (aaaaa0?)
		var aaa = (sbyte)(nibbleToAscii[0x0A] - ascii0 - 10);
		var mask = Vector128.Create(byte0x0F);

		var t = target;
		var limit = source + sourceLength;

		// this loop would benefit from loop alignment,
		// bit is seems compiler does not do it,
		// so a performance depends on luck
		for (var s = source; s < limit; s += 16)
		{
			var chunk = LoadBytes128(s);

			var digitsH = Sse2.And(Sse2.ShiftRightLogical(chunk.AsUInt16(), 4).AsByte(), mask);
			var digitsL = Sse2.And(chunk, mask);

			SaveAscii128(ToAscii_SSE2(Sse2.UnpackLow(digitsH, digitsL), aaa), t);
			t += 16;
			SaveAscii128(ToAscii_SSE2(Sse2.UnpackHigh(digitsH, digitsL), aaa), t);
			t += 16;
		}

		return sourceLength;
	}

	[MethodImpl(MethodImplOptions.AggressiveOptimization)]
	public static unsafe int Encode_SSSE3(
		byte* source, int sourceLength,
		char* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeEncode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var ascii = LoadBytes128((byte*)nibbleToAscii).AsSByte();
		var mask = Vector128.Create(byte0x0F);

		var t = target;
		var limit = source + sourceLength;

		// this loop would benefit from loop alignment,
		// bit is seems compiler does not do it,
		// so a performance depends on luck
		for (var s = source; s < limit; s += 16)
		{
			var chunk = LoadBytes128(s);

			var digitsH = Sse2.And(Sse2.ShiftRightLogical(chunk.AsUInt16(), 4).AsByte(), mask);
			var digitsL = Sse2.And(chunk, mask);

			SaveAscii128(ToAscii_SSSE3(Sse2.UnpackLow(digitsH, digitsL), ascii), t);
			t += 16;
			SaveAscii128(ToAscii_SSSE3(Sse2.UnpackHigh(digitsH, digitsL), ascii), t);
			t += 16;
		}

		return sourceLength;
	}

	public static unsafe int Encode_AVX2(
		byte* source, int sourceLength,
		char* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeEncode(
			Avx2.IsSupported, sourceLength, targetLength, Vector256<byte>.Count);
		if (sourceLength <= 0) return 0;

		var map = Avx2.Permute4x64(
			Sse2.LoadVector128(nibbleToAscii).ToVector256Unsafe().AsInt64(),
			PERM_0101
		).AsSByte();
		var mask = Vector256.Create(byte0x0F);

		var t = target;
		var limit = source + sourceLength;

		for (var s = source; s < limit; s += 32)
		{
			var chunk = LoadBytes256(s);

			var digitsL = Avx2.And(
				chunk, mask);
			var digitsH = Avx2.And(
				Avx2.ShiftRightLogical(chunk.AsUInt16(), 4).AsByte(), mask);

			SaveAscii256(ToAscii_AVX2(Avx2.UnpackLow(digitsH, digitsL), map), t);
			t += 32;
			SaveAscii256(ToAscii_AVX2(Avx2.UnpackHigh(digitsH, digitsL), map), t);
			t += 32;
		}

		return sourceLength;
	}

	private static int AdjustBeforeDecode(
		bool support, int sourceLength, int targetLength, int vectorSize)
	{
		if (!support || sourceLength <= 0) return 0;

		sourceLength &= ~(vectorSize * 2 - 1);

		if (targetLength * 2 < sourceLength)
			throw new ArgumentException("Output buffer is too small");

		return sourceLength;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> FromAscii_SSE2(Vector128<sbyte> digits)
	{
		digits = Sse2.Or(digits, Vector128.Create(sbyte0x20));
		var diff = Sse2.And(
			Sse2.CompareGreaterThan(digits, Vector128.Create(ascii9)),
			Vector128.Create(alcaaaa0));
		return Sse2.And(
			Sse2.Subtract(Sse2.Subtract(digits, diff), Vector128.Create(ascii0)).AsByte(),
			Vector128.Create(byte0x0F));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> FromAscii_SSSE3(
		Vector128<sbyte> digits, Vector128<sbyte> aat)
	{
		// 30 -> digit, 41 -> upper, 61 -> lower
		// map = [0, 0, 0, -48, -65 + 10, 0, -97 + 10, 0, 0, 0, 0, 0, 0, 0, 0, 0]
		// adj = shuffle(map, shr(ascii, 4))
		// return and(add(ascii, adj), 0x0F)

		var type = Sse2.And(
			Sse2.ShiftRightLogical(digits.AsInt16(), 4).AsByte(),
			Vector128.Create(byte0x0F));
		var diff = Ssse3.Shuffle(aat.AsByte(), type).AsSByte();
		return Sse2.Subtract(digits, diff).AsByte();
	}

	public static unsafe int Decode_SSE2(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		sourceLength = AdjustBeforeDecode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var asciiMask = Vector128.Create(ushort0x00FF);

		var t = target;
		var limit = source + sourceLength;

		for (var s = source; s < limit; s += 32)
		{
			var chunk0 = LoadAscii128(s + 0x00);
			var chunk1 = LoadAscii128(s + 0x10);

			var digitsH = Sse2.PackUnsignedSaturate(
				Sse2.And(chunk0.AsUInt16(), asciiMask).AsInt16(),
				Sse2.And(chunk1.AsUInt16(), asciiMask).AsInt16()
			).AsSByte();

			var digitsL = Sse2.PackUnsignedSaturate(
				Sse2.ShiftRightLogical(chunk0.AsUInt16(), 8).AsInt16(),
				Sse2.ShiftRightLogical(chunk1.AsUInt16(), 8).AsInt16()
			).AsSByte();

			var nibblesL = FromAscii_SSE2(digitsL);
			var nibblesH = FromAscii_SSE2(digitsH);

			SaveBytes128(
				Sse2.Or(nibblesL, Sse2.ShiftLeftLogical(nibblesH.AsUInt16(), 4).AsByte()),
				t);

			t += 16;
		}

		return sourceLength;
	}

	public static unsafe int Decode_SSSE3(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		sourceLength = AdjustBeforeDecode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var asciiMask = Vector128.Create(ushort0x00FF);
		var aat = Vector128.Create(
			0, 0, 0, ascii0, upperA - 10, 0, lowerA - 10, 0, 0, 0, 0, 0, 0, 0, 0, 0);

		var t = target;
		var limit = source + sourceLength;

		for (var s = source; s < limit; /* s += 32 */)
		{
			var chunk0 = LoadAscii128(s);
			s += 16;
			var chunk1 = LoadAscii128(s);
			s += 16;

			var digitsH = Sse2.PackUnsignedSaturate(
				Sse2.And(chunk0.AsUInt16(), asciiMask).AsInt16(),
				Sse2.And(chunk1.AsUInt16(), asciiMask).AsInt16()
			).AsSByte();

			var digitsL = Sse2.PackUnsignedSaturate(
				Sse2.ShiftRightLogical(chunk0.AsUInt16(), 8).AsInt16(),
				Sse2.ShiftRightLogical(chunk1.AsUInt16(), 8).AsInt16()
			).AsSByte();

			var nibblesL = FromAscii_SSSE3(digitsL, aat);
			var nibblesH = FromAscii_SSSE3(digitsH, aat);

			SaveBytes128(
				Sse2.Or(nibblesL, Sse2.ShiftLeftLogical(nibblesH.AsUInt16(), 4).AsByte()),
				t);

			t += 16;
		}

		return sourceLength;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<byte> FromAscii_AVX2(
		Vector256<sbyte> digits, Vector256<sbyte> aat)
	{
		// 30 -> digit, 41 -> upper, 61 -> lower
		// map = [0, 0, 0, -48, -65 + 10, 0, -97 + 10, 0, 0, 0, 0, 0, 0, 0, 0, 0]
		// adj = shuffle(map, shr(ascii, 4))
		// return and(add(ascii, adj), 0x0F)

		var type = Avx2.And(
			Avx2.ShiftRightLogical(digits.AsInt16(), 4).AsByte(),
			Vector256.Create(byte0x0F));
		var diff = Avx2.Shuffle(aat.AsByte(), type).AsSByte();
		return Avx2.Subtract(digits, diff).AsByte();
	}

	public static unsafe int Decode_AVX2(
		char* source, int sourceLength,
		byte* target, int targetLength,
		sbyte* nibbleToAscii)
	{
		sourceLength = AdjustBeforeDecode(
			Avx2.IsSupported, sourceLength, targetLength, Vector256<byte>.Count);
		if (sourceLength <= 0) return 0;

		var t = target;
		var limit = source + sourceLength;

		var asciiMask = Vector256.Create(ushort0x00FF);
		var aat = Vector256.Create(
			0, 0, 0, ascii0, upperA - 10, 0, lowerA - 10, 0, 0, 0, 0, 0, 0, 0, 0, 0,
			0, 0, 0, ascii0, upperA - 10, 0, lowerA - 10, 0, 0, 0, 0, 0, 0, 0, 0, 0);

		for (var s = source; s < limit; /* s += 64 */)
		{
			var chunk0 = LoadAscii256(s);
			s += 32;
			var chunk1 = LoadAscii256(s);
			s += 32;

			var digitsH = Avx2.PackUnsignedSaturate(
				Avx2.And(chunk0.AsUInt16(), asciiMask).AsInt16(),
				Avx2.And(chunk1.AsUInt16(), asciiMask).AsInt16()
			).AsSByte();

			var digitsL = Avx2.PackUnsignedSaturate(
				Avx2.ShiftRightLogical(chunk0.AsUInt16(), 8).AsInt16(),
				Avx2.ShiftRightLogical(chunk1.AsUInt16(), 8).AsInt16()
			).AsSByte();

			var nibblesH = FromAscii_AVX2(digitsH, aat);
			var nibblesL = FromAscii_AVX2(digitsL, aat);

			SaveBytes256(
				Avx2.Or(nibblesL, Avx2.ShiftLeftLogical(nibblesH.AsUInt16(), 4).AsByte()),
				t);

			t += 32;
		}

		return sourceLength;
	}
}

#endif
