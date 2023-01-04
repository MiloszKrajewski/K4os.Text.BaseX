using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmarks;

internal class DecoderPlayground
{
	protected const byte PERM_0213 = 0b11011000;
	
	private static int AdjustBeforeDecode(
		bool support, int sourceLength, int targetLength, int vectorSize)
	{
		sourceLength = !support || sourceLength <= 0 ? 0 : sourceLength & ~(vectorSize * 2 - 1);
		if (sourceLength > 0 && targetLength * 2 < sourceLength)
			throw new ArgumentException("Output buffer is too small");

		return sourceLength;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe Vector256<byte> LoadAscii256(char* source)
	{
		var asciiMask = Vector256.Create((ushort)0x7F);
		var chunk0 = Avx2.And(Avx.LoadVector256((byte*)(source + 0x00)).AsUInt16(), asciiMask);
		var chunk1 = Avx2.And(Avx.LoadVector256((byte*)(source + 0x10)).AsUInt16(), asciiMask);
		var merged = Avx2.PackUnsignedSaturate(chunk0.AsInt16(), chunk1.AsInt16());
		return Avx2.Permute4x64(merged.AsInt64(), PERM_0213).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveBytes256(Vector256<byte> bytes, byte* target)
	{
		Avx.Store(target, Avx2.Permute4x64(bytes.AsInt64(), PERM_0213).AsByte());
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<sbyte> FromAscii0X20(
		Vector128<sbyte> digits, sbyte ascii0, sbyte asciiA)
	{
		Debug.Assert((ascii0 & asciiA & 0x20) != 0);

		digits = Sse2.Or(digits, Vector128.Create((sbyte)0x20));
		var diff = Sse2.And(
			Sse2.CompareGreaterThan(digits, Vector128.Create((sbyte)(asciiA - 1))),
			Vector128.Create((sbyte)(asciiA - ascii0 - 10)));
		return Sse2.And(
			Sse2.Subtract(Sse2.Subtract(digits, diff), Vector128.Create(ascii0)),
			Vector128.Create((sbyte)0x0F));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector256<sbyte> FromAscii0X20(
		Vector256<sbyte> digits, sbyte ascii0, sbyte asciiA)
	{
		Debug.Assert((ascii0 & asciiA & 0x20) != 0);

		digits = Avx2.Or(digits, Vector256.Create((sbyte)0x20));
		var diff = Avx2.And(
			Avx2.CompareGreaterThan(digits, Vector256.Create((sbyte)(asciiA - 1))),
			Vector256.Create((sbyte)(asciiA - ascii0 - 10)));
		return Avx2.And(
			Avx2.Subtract(Avx2.Subtract(digits, diff), Vector256.Create(ascii0)),
			Vector256.Create((sbyte)0x0F));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector128<byte> FromAscii(
		Vector128<byte> digits, sbyte ascii0, sbyte asciiA) =>
		FromAscii0X20(digits.AsSByte(), ascii0, (sbyte)(asciiA | 0x20)).AsByte();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector256<byte> FromAscii(Vector256<byte> digits, sbyte ascii0, sbyte asciiA) =>
		FromAscii0X20(digits.AsSByte(), ascii0, (sbyte)(asciiA | 0x20)).AsByte();

	public static unsafe int DecodeSse2(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		sourceLength = AdjustBeforeDecode(
			Sse2.IsSupported, sourceLength, targetLength, Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		const sbyte ascii0 = (sbyte)'0';
		const sbyte asciiA = (sbyte)'A';

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
		byte* target, int targetLength)
	{
		sourceLength = AdjustBeforeDecode(
			Avx2.IsSupported, sourceLength, targetLength, Vector256<byte>.Count);
		if (sourceLength <= 0) return 0;

		const sbyte ascii0 = (sbyte)'0';
		const sbyte asciiA = (sbyte)'A';

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
