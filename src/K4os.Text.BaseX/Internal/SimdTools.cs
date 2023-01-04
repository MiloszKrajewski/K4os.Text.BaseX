#if NET5_0_OR_GREATER

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace K4os.Text.BaseX.Internal;

internal class SimdTools: SimdSettings
{
	protected const byte PERM_0101 = 0b01000100;
	protected const byte PERM_0213 = 0b11011000;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveAscii128(Vector128<byte> source, char* target)
	{
		Sse2.Store((byte*)(target + 0x00), Sse2.UnpackLow(source, Vector128<byte>.Zero));
		Sse2.Store((byte*)(target + 0x08), Sse2.UnpackHigh(source, Vector128<byte>.Zero));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveAscii256(Vector256<byte> source, char* target)
	{
		source = Avx2.Permute4x64(source.AsInt64(), PERM_0213).AsByte();
		Avx.Store((byte*)(target + 0x00), Avx2.UnpackLow(source, Vector256<byte>.Zero));
		Avx.Store((byte*)(target + 0x10), Avx2.UnpackHigh(source, Vector256<byte>.Zero));
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
	protected static Vector128<byte> ToAscii(Vector128<byte> digits, sbyte ascii0, sbyte asciiA) =>
		Sse2.Add(
			Sse2.Add(digits, Vector128.Create((byte)ascii0)),
			Sse2.And(
				Sse2.CompareGreaterThan(digits.AsSByte(), Vector128.Create((sbyte)9)).AsByte(),
				Vector128.Create((byte)(asciiA - ascii0 - 10))));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector128<byte> ToAscii(Vector128<byte> digits, Vector128<byte> ascii) =>
		Ssse3.Shuffle(ascii, digits);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector256<byte> ToAscii(Vector256<byte> digits, Vector256<byte> ascii) =>
		Avx2.Shuffle(ascii, digits);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<sbyte> FromAsciiImpl(
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
	private static Vector256<sbyte> FromAsciiImpl(
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
		FromAsciiImpl(digits.AsSByte(), ascii0, (sbyte)(asciiA | 0x20)).AsByte();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static Vector256<byte> FromAscii(Vector256<byte> digits, sbyte ascii0, sbyte asciiA) =>
		FromAsciiImpl(digits.AsSByte(), ascii0, (sbyte)(asciiA | 0x20)).AsByte();

}

#endif
