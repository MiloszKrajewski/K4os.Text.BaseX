// ReSharper disable IdentifierTypo
// ReSharper disable InconsistentNaming
// ReSharper disable CommentTypo

#if NET5_0_OR_GREATER

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

/*
 * NOTE: most (if not all) of the SIMD code is based on work by Wojciech Mula:
 * https://github.com/WojciechMula/base64simd
 * http://0x80.pl/notesen/2016-01-17-sse-base64-decoding.html
 * http://0x80.pl/notesen/2016-01-12-sse-base64-encoding.html
 */

namespace K4os.Text.BaseX.Internal;

internal class SimdBase64: SimdTools
{
	// this is internal to allow testing, but it really shouldn't
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int AdjustBeforeEncode(
		bool support, int sourceLength, int targetLength, uint vectorSize) =>
		AdjustBeforeTransform(
			support, 
			sourceLength, vectorSize / 4 * 3, 
			targetLength, vectorSize, 
			vectorSize);
	
	public static unsafe int Encode_SSSE3(
		byte* source, int sourceLength,
		char* target, int targetLength)
	{
		sourceLength = AdjustBeforeEncode(
			Ssse3.IsSupported, sourceLength, targetLength, (uint)Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var sourceStart = source;
		var sourceLimit = source + sourceLength;

		var t = target;
		for (var s = source; s < sourceLimit; s += 12, t += 16)
		{
			Encode_SSSE3(s, t);
		}

		return (int)((source - sourceStart) / 3);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void Encode_SSSE3(byte* source, char* target)
	{
		SaveAscii128(ToAscii_SSSE3(Unpack_SSSE3(LoadBytes128(source))), target, Vector128<sbyte>.Zero);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> Unpack_SSSE3(Vector128<byte> vector)
	{
		var shuffled = Ssse3.Shuffle(
			vector, 
			Vector128.Create((byte)1, 0, 2, 1, 4, 3, 5, 4, 7, 6, 8, 7, 10, 9, 11, 10));

		// t0    = [0000cccc|cc000000|aaaaaa00|00000000]
		// const __m128i t0 = _mm_and_si128(in, _mm_set1_epi32(0x0fc0fc00));
		var t0 = Sse2.And(shuffled, Vector128.Create(0x0fc0fc00).AsByte());

		// t1    = [00000000|00cccccc|00000000|00aaaaaa]
		// (c * (1 << 10), a * (1 << 6)) >> 16 (note: an unsigned multiplication)
		// const __m128i t1 = _mm_mulhi_epu16(t0, _mm_set1_epi32(0x04000040));
		var t1 = Sse2.MultiplyHigh(t0.AsUInt16(), Vector128.Create(0x04000040).AsUInt16());

		// t2    = [00000000|00dddddd|000000bb|bbbb0000]
		// const __m128i t2 = _mm_and_si128(in, _mm_set1_epi32(0x003f03f0));
		var t2 = Sse2.And(shuffled, Vector128.Create(0x003f03f0).AsByte());

		// t3    = [00dddddd|00000000|00bbbbbb|00000000](
		// (d * (1 << 8), b * (1 << 4))
		// const __m128i t3 = _mm_mullo_epi16(t2, _mm_set1_epi32(0x01000010));
		var t3 = Sse2.MultiplyLow(t2.AsUInt16(), Vector128.Create(0x01000010).AsUInt16());

		// res   = [00dddddd|00cccccc|00bbbbbb|00aaaaaa] = t1 | t3
		// const __m128i indices = _mm_or_si128(t1, t3);
		return Sse2.Or(t1, t3).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<sbyte> ToAscii_SSSE3(Vector128<byte> input)
	{
		// __m128i lookup_pshufb_improved(const __m128i input)
		// // reduce  0..51 -> 0
		// //        52..61 -> 1 .. 10
		// //            62 -> 11
		// //            63 -> 12
		// __m128i result = _mm_subs_epu8(input, packed_byte(51));
		var result = Sse2.SubtractSaturate(input, Vector128.Create((byte)51));

		// // distinguish between ranges 0..25 and 26..51:
		// //         0 .. 25 -> remains 0
		// //        26 .. 51 -> becomes 13
		// const __m128i less = _mm_cmpgt_epi8(packed_byte(26), input);
		var less = Sse2.CompareGreaterThan(Vector128.Create((sbyte)26), input.AsSByte()).AsByte();
		
		// result = _mm_or_si128(result, _mm_and_si128(less, packed_byte(13)));
		result = Sse2.Or(result, Sse2.And(less, Vector128.Create((byte)13)));

		// const __m128i shift_LUT = _mm_setr_epi8(
		//     'a' - 26, '0' - 52, '0' - 52, '0' - 52, '0' - 52, '0' - 52,
		//     '0' - 52, '0' - 52, '0' - 52, '0' - 52, '0' - 52, '+' - 62,
		//     '/' - 63, 'A', 0, 0
		// );
		var shiftLut = Vector128.Create(
			'a' - 26, '0' - 52, '0' - 52, '0' - 52, 
			'0' - 52, '0' - 52, '0' - 52, '0' - 52, 
			'0' - 52, '0' - 52, '0' - 52, '+' - 62, 
			'/' - 63, 'A' - 00, 0, 0);
		
		// // read shift
		// result = _mm_shuffle_epi8(shift_LUT, result);
		result = Ssse3.Shuffle(shiftLut, result.AsSByte()).AsByte();

		// return _mm_add_epi8(result, input);
		return Sse2.Add(result, input).AsSByte();
	}
	
	// this is internal to allow testing, but it really shouldn't
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal static int AdjustBeforeDecode(
		bool support, int sourceLength, int targetLength, uint vectorSize) =>
		AdjustBeforeTransform(
			support, 
			sourceLength, vectorSize, 
			targetLength, vectorSize / 4 * 3, 
			vectorSize);
	
	/// <summary>
	/// Encodes to Base64 in 12 bytes chunks (16 characters output).
	/// Returns number of 3 bytes (or 4 characters) chunks encoded.
	/// </summary>
	/// <param name="source">Source buffer address.</param>
	/// <param name="sourceLength">Source buffer length.</param>
	/// <param name="target">Target buffer address.</param>
	/// <param name="targetLength">Target buffer address.</param>
	/// <returns>Number of 3 bytes (or 4 characters) chunks encoded.</returns>
	public static unsafe int Decode_SSSE3(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		sourceLength = AdjustBeforeDecode(
			Ssse3.IsSupported, sourceLength, targetLength, (uint)Vector128<byte>.Count);
		if (sourceLength <= 0) return 0;

		var sourceStart = source;
		var sourceLimit = source + sourceLength;

		while (source < sourceLimit)
		{
			Decode_SSSE3(source, target);
			source += 16;
			target += 12;
		}

		return (int)((source - sourceStart) / 4);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe void Decode_SSSE3(char* source, byte* target)
	{
		SaveBytes128(Decode_SSSE3(FromAscii_SSSE3(LoadAscii128(source))), target);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> FromAscii_SSSE3(Vector128<byte> vector)
	{
		// __m128i lookup_pshufb(const __m128i input)

		// const __m128i higher_nibble = _mm_srli_epi32(input, 4) & packed_byte(0x0f);
		var higherNibble = Sse2.And(
			Sse2.ShiftRightLogical(vector.AsInt32(), 4).AsByte(),
			Vector128.Create((byte)0x0f));
		
		// const __m128i eq_2f = _mm_cmpeq_epi8(input, packed_byte(0x2f));
		var eq2f = Sse2.CompareEqual(vector.AsSByte(), Vector128.Create((sbyte)0x2f));
		
		// const __m128i shift_LUT = _mm_setr_epi8(
		//     /* 0 */ 0x00,        /* 1 */ 0x00,        /* 2 */ 0x3e - 0x2b, /* 3 */ 0x34 - 0x30,
		//     /* 4 */ 0x00 - 0x41, /* 5 */ 0x0f - 0x50, /* 6 */ 0x1a - 0x61, /* 7 */ 0x29 - 0x70,
		//     /* 8 */ 0x00,        /* 9 */ 0x00,        /* a */ 0x00,        /* b */ 0x00,
		//     /* c */ 0x00,        /* d */ 0x00,        /* e */ 0x00,        /* f */ 0x00
		// );
		var shiftLut = Vector128.Create(
			/* 0 */ 0x00, /* 1 */ 0x00, /* 2 */ 0x3e - 0x2b, /* 3 */ 0x34 - 0x30,
			/* 4 */ 0x00 - 0x41, /* 5 */ 0x0f - 0x50, /* 6 */ 0x1a - 0x61, /* 7 */ 0x29 - 0x70,
			/* 8 */ 0x00, /* 9 */ 0x00, /* a */ 0x00, /* b */ 0x00,
			/* c */ 0x00, /* d */ 0x00, /* e */ 0x00, /* f */ 0x00);

		ValidateFromAscii(vector, higherNibble, eq2f);

		// const __m128i shift  = _mm_shuffle_epi8(shift_LUT, higher_nibble);
		var shift = Ssse3.Shuffle(shiftLut.AsByte(), higherNibble.AsByte());

		// const __m128i t0     = _mm_add_epi8(input, shift);
		var t0 = Sse2.Add(vector.AsSByte(), shift.AsSByte());

		// const __m128i result = _mm_add_epi8(t0, _mm_and_si128(eq_2f, packed_byte(-3)));
		var result = Sse2.Add(t0, Sse2.And(eq2f, Vector128.Create((sbyte)-3)));

		return result.AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Conditional("DEBUG")]
	private static void ValidateFromAscii(
		Vector128<byte> vector, 
		Vector128<byte> higherNibble, 
		Vector128<sbyte> eq2f)
	{
		// This happens only in DEBUG mode
		// for both performance (lazy excuse) and consistency as none of the other
		// codecs does validation, so if this was doing it, then it would violate LSP 
		const sbyte linv = 1;
		const sbyte hinv = 0;

		// const __m128i lower_bound_LUT = _mm_setr_epi8(
		//     /* 0 */ linv, /* 1 */ linv, /* 2 */ 0x2b, /* 3 */ 0x30,
		//     /* 4 */ 0x41, /* 5 */ 0x50, /* 6 */ 0x61, /* 7 */ 0x70,
		//     /* 8 */ linv, /* 9 */ linv, /* a */ linv, /* b */ linv,
		//     /* c */ linv, /* d */ linv, /* e */ linv, /* f */ linv
		// );
		var lowerBoundLut = Vector128.Create(
			/* 0 */ linv, /* 1 */ linv, /* 2 */ 0x2b, /* 3 */ 0x30,
			/* 4 */ 0x41, /* 5 */ 0x50, /* 6 */ 0x61, /* 7 */ 0x70,
			/* 8 */ linv, /* 9 */ linv, /* a */ linv, /* b */ linv,
			/* c */ linv, /* d */ linv, /* e */ linv, /* f */ linv);

		// const __m128i upper_bound_LUT = _mm_setr_epi8(
		//     /* 0 */ hinv, /* 1 */ hinv, /* 2 */ 0x2b, /* 3 */ 0x39,
		//     /* 4 */ 0x4f, /* 5 */ 0x5a, /* 6 */ 0x6f, /* 7 */ 0x7a,
		//     /* 8 */ hinv, /* 9 */ hinv, /* a */ hinv, /* b */ hinv,
		//     /* c */ hinv, /* d */ hinv, /* e */ hinv, /* f */ hinv
		// );
		var upperBoundLut = Vector128.Create(
			/* 0 */ hinv, /* 1 */ hinv, /* 2 */ 0x2b, /* 3 */ 0x39,
			/* 4 */ 0x4f, /* 5 */ 0x5a, /* 6 */ 0x6f, /* 7 */ 0x7a,
			/* 8 */ hinv, /* 9 */ hinv, /* a */ hinv, /* b */ hinv,
			/* c */ hinv, /* d */ hinv, /* e */ hinv, /* f */ hinv);

		// const __m128i upper_bound = _mm_shuffle_epi8(upper_bound_LUT, higher_nibble);
		var upperBound = Ssse3.Shuffle(upperBoundLut.AsByte(), higherNibble.AsByte());

		// const __m128i lower_bound = _mm_shuffle_epi8(lower_bound_LUT, higher_nibble);
		var lowerBound = Ssse3.Shuffle(lowerBoundLut.AsByte(), higherNibble.AsByte());

		// const __m128i below = _mm_cmplt_epi8(input, lower_bound);
		var below = Sse2.CompareLessThan(vector.AsSByte(), lowerBound.AsSByte());

		// const __m128i above = _mm_cmpgt_epi8(input, upper_bound);
		var above = Sse2.CompareGreaterThan(vector.AsSByte(), upperBound.AsSByte());

		// // in_range = not (below or above) or eq_2f
		// // outside  = not in_range = below or above and not eq_2f (from de Morgan law)
		// const __m128i outside = _mm_andnot_si128(eq_2f, above | below);
		var outside = Sse2.AndNot(eq2f, Sse2.Or(above, below));

		// const auto mask = _mm_movemask_epi8(outside);
		var mask = Sse2.MoveMask(outside.AsSByte());
		if (mask != 0) throw new Exception("Invalid character in input");
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> Pack_SSSE3(Vector128<byte> vector)
	{
		// // input:  [00dddddd|00cccccc|00bbbbbb|00aaaaaa]
		// // merge:  [0000cccc|ccdddddd|0000aaaa|aabbbbbb]
		// const __m128i merge_ab_and_bc = _mm_maddubs_epi16(values, packed_dword(0x01400140));
		var merged = Ssse3.MultiplyAddAdjacent(
			vector, Vector128.Create(0x01400140).AsSByte());

		// // result: [00000000|aaaaaabb|bbbbcccc|ccdddddd]
		// return _mm_madd_epi16(merge_ab_and_bc, packed_dword(0x00011000));
		return Sse2.MultiplyAddAdjacent(
			merged.AsInt16(),
			Vector128.Create(0x00011000).AsInt16()).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static Vector128<byte> Decode_SSSE3(Vector128<byte> vector)
	{
		// // input:  packed_dword([00dddddd|00cccccc|00bbbbbb|00aaaaaa] x 4)
		// // merged: packed_dword([00000000|ddddddcc|ccccbbbb|bbaaaaaa] x 4)
		// const __m128i merged = pack(values);
		var merged = Pack_SSSE3(vector);

		// // merged = packed_byte([0XXX|0YYY|0ZZZ|0WWW])
		// const __m128i shuf = _mm_setr_epi8(
		//         2,  1,  0,
		//         6,  5,  4,
		//        10,  9,  8,
		//        14, 13, 12,
		//       char(0xff), char(0xff), char(0xff), char(0xff)
		// );
		const sbyte ff = -1;
		var shuffle = Vector128.Create(2, 1, 0, 6, 5, 4, 10, 9, 8, 14, 13, 12, ff, ff, ff, ff);

		// // lower 12 bytes contains the result
		// const __m128i shuffled = _mm_shuffle_epi8(merged, shuf);
		return Ssse3.Shuffle(merged.AsSByte(), shuffle).AsByte();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe int Encode_AVX2(
		byte* source, int sourceLength,
		char* target, int targetLength)
	{
		return 0;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static unsafe int Decode_AVX2(
		char* source, int sourceLength,
		byte* target, int targetLength)
	{
		return 0;
	}
}

#endif
