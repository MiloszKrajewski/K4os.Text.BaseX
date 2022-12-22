using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmarks;

public class Sse2Base16Encoder
{
	private const byte PERM_0213 = 0b11011000;
	private const byte PERM_0123 = 0b11100100;
	private const byte PERM_2301 = 0b01001110;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void StoreVector(Vector128<byte> source, char* target)
	{
		Sse2.Store((byte*)(target + 0x00), Sse2.UnpackLow(source, Vector128<byte>.Zero));
		Sse2.Store((byte*)(target + 0x08), Sse2.UnpackHigh(source, Vector128<byte>.Zero));
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void StoreVector(Vector256<byte> source, char* target)
	{
		source = Avx2.Permute4x64(source.AsInt64(), PERM_0213).AsByte();
		Avx.Store((byte*)(target + 0x00), Avx2.UnpackLow(source, Vector256<byte>.Zero));
		Avx.Store((byte*)(target + 0x10), Avx2.UnpackHigh(source, Vector256<byte>.Zero));
	}

	public static void BuildDigitMap(char chr0, char chrA, Span<byte> ascii)
	{
		if (ascii.Length < 16) ThrowOutputBufferTooShort(nameof(ascii));
		for (var i = 0; i < 10; i++) ascii[i] = (byte)(i + chr0);
		for (var i = 10; i < 16; i++) ascii[i] = (byte)(i + chrA - 10);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> ToAscii(Vector128<byte> digits, byte chr0, byte chrA) =>
		Sse2.Add(
			Sse2.Add(digits, Vector128.Create(chr0)),
			Sse2.And(
				Sse2.CompareGreaterThan(digits.AsSByte(), Vector128.Create((sbyte)9)).AsByte(),
				Vector128.Create((byte)(chrA - chr0 - 10))));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector256<byte> ToAscii(Vector256<byte> digits, byte chr0, byte chrA) =>
		Avx2.Add(
			Avx2.Add(digits, Vector256.Create(chr0)),
			Avx2.And(
				Avx2.CompareGreaterThan(digits.AsSByte(), Vector256.Create((sbyte)9)).AsByte(),
				Vector256.Create((byte)(chrA - chr0 - 10))));

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static Vector128<byte> ToAscii(Vector128<byte> digits, Vector128<byte> ascii) =>
		Ssse3.Shuffle(ascii, digits);

	[DoesNotReturn]
	private static void ThrowOutputBufferTooShort(string argumentName) =>
		throw new ArgumentException($"Buffer {argumentName} is too small", argumentName);

	public static unsafe int Encode_SSE2(
		ReadOnlySpan<byte> source, Span<char> target, ReadOnlySpan<byte> ascii)
	{
		if (!Sse2.IsSupported) return 0;

		var length = source.Length & ~0x0F;
		if (length <= 0) return 0;

		if (target.Length < length * 2) ThrowOutputBufferTooShort(nameof(target));

		var ascii0 = ascii[0];
		var asciiA = ascii[10];

		fixed (byte* s0 = source)
		fixed (char* t0 = target)
		{
			var sP = s0;
			var tP = t0;
			var lP = s0 + length;

			while (sP < lP)
			{
				var vector16 = Sse2.LoadVector128(sP);

				var loDigits = Sse2.And(vector16, Vector128.Create((byte)0x0F));
				var hiDigits = Sse2.ShiftRightLogical(
					Sse2.And(vector16, Vector128.Create((byte)0xF0)).AsUInt16(), 4
				).AsByte();

				StoreVector(ToAscii(Sse2.UnpackLow(hiDigits, loDigits), ascii0, asciiA), tP);
				StoreVector(ToAscii(Sse2.UnpackHigh(hiDigits, loDigits), ascii0, asciiA), tP + 16);

				sP += 16;
				tP += 32;
			}
		}

		return length;
	}

	public static unsafe int Encode_SSSE3(
		ReadOnlySpan<byte> source, Span<char> target, ReadOnlySpan<byte> ascii)
	{
		if (!Ssse3.IsSupported || !Sse2.IsSupported) return 0;

		var length = source.Length & ~0x0F;
		if (length <= 0) return 0;

		if (target.Length < length * 2) ThrowOutputBufferTooShort(nameof(target));

		fixed (byte* s0 = source)
		fixed (char* t0 = target)
		fixed (byte* mP = ascii)
		{
			var map = Sse2.LoadVector128(mP);

			var sP = s0;
			var tP = t0;
			var lP = s0 + length;

			while (sP < lP)
			{
				var vector16 = Sse2.LoadVector128(sP);

				var loDigits = Sse2.And(vector16, Vector128.Create((byte)0x0F));
				var hiDigits = Sse2.ShiftRightLogical(
					Sse2.And(vector16, Vector128.Create((byte)0xF0)).AsUInt16(), 4
				).AsByte();

				StoreVector(ToAscii(Sse2.UnpackLow(hiDigits, loDigits), map), tP);
				StoreVector(ToAscii(Sse2.UnpackHigh(hiDigits, loDigits), map), tP + 16);

				sP += 16;
				tP += 32;
			}
		}

		return length;
	}

	public static unsafe int Encode_AVX2(
		ReadOnlySpan<byte> source, Span<char> target, ReadOnlySpan<byte> ascii)
	{
		if (!Avx2.IsSupported) return 0;

		var length = source.Length & ~0x1F;
		if (length <= 0) return 0;

		if (target.Length < length * 2) ThrowOutputBufferTooShort(nameof(target));
		
		var ascii0 = ascii[0];
		var asciiA = ascii[10];

		fixed (byte* s0 = source)
		fixed (char* t0 = target)
		{
			var sP = s0;
			var tP = t0;
			var lP = s0 + length;

			while (sP < lP)
			{
				var vector32 = Avx2
					.Permute4x64(Avx.LoadVector256(sP).AsInt64(), PERM_0213)
					.AsByte();

				var loDigits = Avx2.And(vector32, Vector256.Create((byte)0x0F));
				var hiDigits = Avx2.ShiftRightLogical(
					Avx2.And(vector32, Vector256.Create((byte)0xF0)).AsUInt16(), 4
				).AsByte();
				
				StoreVector(ToAscii(Avx2.UnpackLow(hiDigits, loDigits), ascii0, asciiA), tP);
				StoreVector(ToAscii(Avx2.UnpackHigh(hiDigits, loDigits), ascii0, asciiA), tP + 32);

				sP += 32;
				tP += 64;
			}
		}

		return length;
	}
}
