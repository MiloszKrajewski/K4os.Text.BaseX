using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace K4os.Text.BaseX.Codecs;

/// <summary>
/// Specialized version of Base64 codec built on top of lookup tables,
/// trades additional memory for better performance. Please note,
/// this is still not faster than SIMD version, but may fill the gap
/// in environments where SIMD is not available.
/// </summary>
public class LookupBase64Codec: Base64Codec
{
	// ReSharper disable InconsistentNaming

	// NOTE: this is extra 1M of memory, yay!

	private readonly ushort[] _b01c01 = new ushort[0x10000]; // 128k
	private readonly ushort[] _b12c23 = new ushort[0x10000]; // 128k

	private readonly byte[] _c01b0 = new byte[0x10000]; // 64k
	private readonly byte[] _c12b1 = new byte[0x10000]; // 64k
	private readonly byte[] _c23b2 = new byte[0x10000]; // 64k

	// ReSharper restore InconsistentNaming

	/// <summary>
	/// Creates default Base64 codec.
	/// See <see cref="Base64"/> class for some default codecs.
	/// Please note, this operation is relatively slow (to use in hot spots) so
	/// prefer using codecs as static/singletons.
	/// </summary>
	public LookupBase64Codec(): this(true) { }

	/// <summary>
	/// Creates default Base64 codec.
	/// See <see cref="Base64"/> class for some default codecs.
	/// Please note, this operation is relatively slow (to use in hot spots) so
	/// prefer using codecs as static/singletons.
	/// </summary>
	/// <param name="usePadding">Indicates if padding should be used.</param>
	public LookupBase64Codec(bool usePadding):
		this($"{Base64.Digits62}+/", usePadding, '=') { }

	/// <summary>
	/// Creates Base64 codec.
	/// <see cref="Base64"/> class for some default codecs.
	/// Please note, this operation is relatively slow (to use in hot spots) so
	/// prefer using codecs as static/singletons.
	/// </summary>
	/// <param name="digits">Digits.</param>
	/// <param name="usePadding">Indicates if padding should be used.</param>
	/// <param name="paddingChar">Padding char (irrelevant, if <paramref name="usePadding"/> is <c>false</c></param>
	public LookupBase64Codec(string digits, bool usePadding, char paddingChar):
		base(digits, usePadding, paddingChar)
	{
		BuildLookup();
	}

	private unsafe void BuildLookup()
	{
		var source3 = stackalloc byte[3];
		var target4 = stackalloc char[4];

		fixed (char* charMap = ByteToChar)
		{
			for (var a = 0; a <= 255; a++)
			for (var b = 0; b <= 255; b++)
			{
				source3[0] = source3[2] = (byte)a;
				source3[1] = (byte)b;

				Encode4(charMap, source3, target4);

				var b01 = (uint)*(ushort*)(source3 + 0);
				var b12 = (uint)*(ushort*)(source3 + 1);

				var c01 = Pack2(target4 + 0);
				var c12 = Pack2(target4 + 1);
				var c23 = Pack2(target4 + 2);

				// a b a -> 0 1 2 3 -> a b a

				_b01c01[b01] = (ushort)c01;
				_b12c23[b12] = (ushort)c23;

				_c01b0[c01] = (byte)a;
				_c12b1[c12] = (byte)b;
				_c23b2[c23] = (byte)a;
			}
		}
	}

	/// <inheritdoc />
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected override unsafe int EncodeImpl(
		byte* source, int sourceLength, char* target, int targetLength)
	{
		fixed (ushort* b01c01 = _b01c01)
		fixed (ushort* b12c23 = _b12c23)
		fixed (char* map = ByteToChar)
		{
			var targetStart = target;

			var blocks = LookupEncodeBlocks(b01c01, b12c23, source, sourceLength, target);
			source += blocks * 3;
			target += blocks * 4;
			target += EncodeTail(map, source, (uint)sourceLength % 3, target);

			return (int)(target - targetStart);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private static unsafe uint LookupEncodeBlocks(
		ushort* b01c01, ushort* b12c23,
		byte* source, int sourceLength, char* target)
	{
		var blocks = (uint)sourceLength / 3;
		var limit = source + blocks * 3;

		while (source < limit)
		{
			LookupEncode4(b01c01, b12c23, source, target);
			source += 3;
			target += 4;
		}

		return blocks;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private static unsafe void LookupEncode4(
		ushort* b01c01, ushort* b12c23,
		byte* source, char* target)
	{
		var char4 = // 00 00 44 33 00 00 22 11 
			*(b01c01 + (uint)*(ushort*)(source + 0)) |
			((ulong)*(b12c23 + (uint)*(ushort*)(source + 1)) << 32);
		*(ulong*)target = // 00 44 00 33 00 22 00 11
			(char4 & 0x000000FF000000FF) | ((char4 & 0x0000FF000000FF00) << 8);
	}

	/// <inheritdoc />
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	protected override unsafe int DecodeImpl(
		char* source, int sourceLength, byte* target, int targetLength)
	{
		
		fixed (byte* c01b0 = _c01b0)
		fixed (byte* c12b1 = _c12b1)
		fixed (byte* c23b2 = _c23b2)
		fixed (byte* map = Utf8ToByte)
		{
			var targetStart = target;

			/*
			// for very small input the naive version is actually a little bit faster
			// 28ns vs 30ns, but the check for length (sourceLength < 32) is not free
			// either, so the "adaptive" version is not worth it (31ns)
			// So yes, this decoder will be a little bit slower for small inputs
			
			// Note, this 2ns difference is not a big deal, assuming 10000 requests a second 
			// (like a web server under high load) it gives 2s total within 24 hours.
			// https://www.wolframalpha.com/input?i=2ns+*+%2810000%2Fs%29+*+24h
			 
			var blocks = sourceLength < 32
				? DecodeBlocks(map, source, sourceLength, target)
				: LookupDecodeBlocks(c01b0, c12b1, c23b2, source, sourceLength, target);
			*/

			var blocks = LookupDecodeBlocks(c01b0, c12b1, c23b2, source, sourceLength, target);
			source += blocks * 4;
			target += blocks * 3;
			target += DecodeTail(map, source, (uint)sourceLength % 4, target);

			return (int)(target - targetStart);
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private protected static unsafe uint LookupDecodeBlocks(
		byte* c01b0, byte* c12b1, byte* c23b2,
		char* source, int sourceLength, byte* target)
	{
		var blocks = (uint)sourceLength / 4;
		var limit = source + blocks * 4;

		while (source < limit)
		{
			LookupDecode4(c01b0, c12b1, c23b2, source, target);
			source += 4;
			target += 3;
		}

		return blocks;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	private protected static unsafe void LookupDecode4(
		byte* c01b0, byte* c12b1, byte* c23b2,
		char* source, byte* target)
	{
		var c0123 = Pack4(source);
		*(target + 0) = *(c01b0 + ((c0123 >> 0) & 0xFFFF));
		*(target + 1) = *(c12b1 + ((c0123 >> 8) & 0xFFFF));
		*(target + 2) = *(c23b2 + (c0123 >> 16)); // no need for mask
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe uint Pack2(char* source)
	{
		// 11 xx 22 xx -> 11 22 xx xx
		var sparse = *(uint*)source;
		return (sparse & 0x000000FF) | ((sparse >> 8) & 0x00FF00);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe uint Pack4(char* source)
	{
		// 11 xx 22 xx 33 xx 44 xx -> 11 22 33 44
		var sparse = *(ulong*)source;
		var packed = (sparse & 0x000000FF000000FF) | ((sparse >> 8) & 0x0000FF000000FF00);
		return (uint)(packed | (packed >> 16));
	}
}
