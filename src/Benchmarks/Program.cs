using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Text;
using BenchmarkDotNet.Attributes;
using Benchmarks;
using K4os.Text.BaseX;

#if !DEBUG
using BenchmarkDotNet.Running;
using System.Runtime.CompilerServices;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
#else

//var x = Vector256.Create(
//	0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 
//	16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31);
//var y = Avx2.UnpackLow(
//	Avx2.Permute4x64(x.AsInt64(), 0b11011000).AsByte(), 
//	Vector256<byte>.Zero);

ReadOnlySpan<byte> source = new byte[] {
	0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07,
	0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
	0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28, 0x29,
	0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x40,
};
var targetB = new byte[1024];
var targetC = new char[1024];
Span<byte> charMap = stackalloc byte[16];
Sse2Base16Encoder.BuildDigitMap('0', 'A', charMap);
var done = Sse2Base16Encoder.Encode_AVX2(source, targetC, charMap);
Console.WriteLine(new string(targetC.AsSpan().Slice(0, done*2)));
#endif

unsafe string Debug(Vector128<byte> v)
{
	var bytes = stackalloc byte[16];
	Sse2.Store(bytes, v);
	return Base16.ToHex(new Span<byte>(bytes, 16));
}

string DebugUtf8(Span<byte> v) => Encoding.ASCII.GetString(v);

unsafe void Base64Playground()
{
	var raw12 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
	var chr16 = new char[16];
	Convert.ToBase64CharArray(raw12, 0, 12, chr16, 0);

	fixed (byte* p = raw12)
	{
		var str = Ssse3.Shuffle(
				Sse2.LoadVector128(p),
				Vector128.Create(2, 2, 1, 0, 5, 5, 4, 3, 8, 8, 7, 6, 11, 11, 10, 9).AsByte())
			.AsUInt32();

		var mask = Vector128.Create(0x3F000000).AsUInt32();

		// ReSharper disable once JoinDeclarationAndInitializer
		Vector128<uint> res;

		// Shift bits by 2, mask in only the first byte:
		res = Sse2.And(Sse2.ShiftRightLogical(str, 2), mask);
		mask = Sse2.ShiftRightLogical(mask, 8);

		// Shift bits by 4, mask in only the second byte:
		res = Sse2.Or(Sse2.And(Sse2.ShiftRightLogical(str, 4), mask), res);
		mask = Sse2.ShiftRightLogical(mask, 8);

		// Shift bits by 6, mask in only the third byte:
		res = Sse2.Or(Sse2.And(Sse2.ShiftRightLogical(str, 6), mask), res);
		mask = Sse2.ShiftRightLogical(mask, 8);

		// No shift necessary for the fourth byte because we duplicated
		// the third byte to this position; just mask:
		res = Sse2.Or(Sse2.And(str, mask), res);

		// Reorder to 32-bit little-endian:
		var res8 = Ssse3.Shuffle(
			res.AsByte(),
			Vector128.Create(3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12).AsByte());

//		// The bits have now been shifted to the right locations;
//		// translate their values 0..63 to the Base64 alphabet:
//
//		// set 1: 0..25, "ABCDEFGHIJKLMNOPQRSTUVWXYZ"
//		s1mask = _mm_cmplt_epi8(res, _mm_set1_epi8(26));
//		blockmask = s1mask;
//
//		// set 2: 26..51, "abcdefghijklmnopqrstuvwxyz"
//		s2mask = _mm_andnot_si128(blockmask, _mm_cmplt_epi8(res, _mm_set1_epi8(52)));
//		blockmask |= s2mask;
//
//		// set 3: 52..61, "0123456789"
//		s3mask = _mm_andnot_si128(blockmask, _mm_cmplt_epi8(res, _mm_set1_epi8(62)));
//		blockmask |= s3mask;
//
//		// set 4: 62, "+"
//		s4mask = _mm_andnot_si128(blockmask, _mm_cmplt_epi8(res, _mm_set1_epi8(63)));
//		blockmask |= s4mask;
//
//		// set 5: 63, "/"
//		// Everything that is not blockmasked
//
//		// Create the masked character sets:
//		s1 = s1mask & _mm_add_epi8(res, _mm_set1_epi8('A'));
//		s2 = s2mask & _mm_add_epi8(res, _mm_set1_epi8('a' - 26));
//		s3 = s3mask & _mm_add_epi8(res, _mm_set1_epi8('0' - 52));
//		s4 = s4mask & _mm_set1_epi8('+');
//		s5 = _mm_andnot_si128(blockmask, _mm_set1_epi8('/'));
//
//		// Blend all the sets together and store:
//		_mm_storeu_si128((__m128i *)opos, s1 | s2 | s3 | s4 | s5);
//
//		ipos += 12;	// 3 * 4 bytes of input
//		opos += 16;	// 4 * 4 bytes of output
//		srclen -= 12;
	}

//	Vector128<byte> Load16(char* chunk) =>
//		Sse2.PackUnsignedSaturate(
//			Sse2.LoadVector128((byte*)chunk).AsInt16(), 
//			Sse2.LoadVector128((byte*)chunk + 16).AsInt16());
}
