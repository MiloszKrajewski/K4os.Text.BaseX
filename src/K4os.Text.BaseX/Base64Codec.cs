using System;
using System.Runtime.CompilerServices;

namespace K4os.Text.BaseX
{
	/// <summary>Base64 codec.</summary>
	public class Base64Codec: BaseXCodec
	{
		private readonly bool _usePadding;
		private readonly char _paddingChar;

		/// <summary>
		/// Creates default Base64 codec.
		/// <see cref="Base64"/> class for some default codecs.
		/// Please note, this operation is relatively slow (to use in hot spots) so
		/// prefer using codecs as static/singletons.
		/// </summary>
		public Base64Codec(): this(true) { }

		/// <summary>
		/// Creates default Base64 codec with or without padding.
		/// <see cref="Base64"/> class for some default codecs.
		/// Please note, this operation is relatively slow (to use in hot spots) so
		/// prefer using codecs as static/singletons.
		/// </summary>
		/// <param name="usePadding">Indicates if padding should be used.</param>
		public Base64Codec(bool usePadding):
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
		public Base64Codec(string digits, bool usePadding, char paddingChar):
			base(64, digits, true)
		{
			if (IsValid(paddingChar))
				// padding cannot be a digit
				throw new ArgumentException(
					$"Padding character '{paddingChar}' is also a digit");

			_usePadding = usePadding;
			_paddingChar = paddingChar;
		}

		/// <inheritdoc />
		public override int MaximumEncodedLength(int sourceLength)
		{
			if (sourceLength <= 0) return 0;

			var blocks = sourceLength / 3;
			var tail = sourceLength % 3;
			return blocks * 4 + (tail == 0 ? 0 : _usePadding ? 4 : tail + 1);
		}

		/// <inheritdoc />
		public override int MaximumDecodedLength(int sourceLength)
		{
			if (sourceLength <= 0) return 0;

			var blocks = sourceLength / 4;
			var tail = sourceLength % 4;
			if (tail == 1)
				throw new ArgumentException(
					"Encoded buffer is corrupted. Is it properly padded?");

			return blocks * 3 + (tail == 0 ? 0 : tail - 1);
		}

		/// <inheritdoc />
		public override ReadOnlySpan<char> StripPadding(ReadOnlySpan<char> source) =>
			source.Slice(0, LengthWithoutPadding(source, _paddingChar));

		private static unsafe int LengthWithoutPadding(
			ReadOnlySpan<char> source, char paddingChar)
		{
			fixed (char* sourceP = source)
				return LengthWithoutPadding(sourceP, source.Length, paddingChar);
		}

		private static unsafe int LengthWithoutPadding(
			char* source, int length, char paddingChar)
		{
			if (length <= 0 || source[length - 1] != paddingChar) return length;
			if (source[0] == paddingChar) return 0;

			for (var i = length - 1; i >= 0; i--)
				if (source[i] != paddingChar)
					return i + 1;

			return 0;
		}

		/// <inheritdoc />
		protected override unsafe int EncodeImpl(
			byte* source, int sourceLength, char* target, int targetLength)
		{
			fixed (char* map = ByteToChar)
			{
				var targetStart = target;

				var blocks = EncodeBlocks(map, source, sourceLength, target);
				source += blocks * 3;
				target += blocks * 4;
				target += EncodeTail(map, source, sourceLength, target);

				return (int) (target - targetStart);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe uint EncodeBlocks(
			char* map, byte* source, int sourceLength, char* target)
		{
			var blocks = (uint) sourceLength / 3;
			var limit = source + blocks * 3;

			while (source < limit)
			{
				Encode4(map, source, target);
				source += 3;
				target += 4;
			}

			return blocks;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private unsafe uint EncodeTail(char* map, byte* source, int sourceLength, char* target)
		{
			var tail = (uint) sourceLength % 3;
			if (tail == 0) return 0;

			// set0 = set1 = read0 = true
			// set2 = read1 = overflow > 1
			// pad3 = usePadding
			// pad2 = !set2 && pad3

			var set2 = tail > 1;
			var pad3 = _usePadding;

			var b0 = *(source + 0);
			var b1 = set2 ? *(source + 1) : (byte) 0;

			// 0-1
			*(target + 0) = Encode1(map, (b0 & 0xFCu) >> 2);
			*(target + 1) = Encode1(map, (b1 & 0xF0u) >> 4 | (b0 & 0x03u) << 4);

			// 2
			if (set2) *(target + 2) = Encode1(map, (b1 & 0x0Fu) << 2);
			else if (pad3) *(target + 2) = _paddingChar;

			// 3
			if (pad3) *(target + 3) = _paddingChar;

			return pad3 ? 4 : tail + 1;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void Encode4(char* map, byte* source, char* target)
		{
			var b0 = *(source + 0);
			var b1 = *(source + 1);
			var b2 = *(source + 2);

			*(target + 0) = Encode1(map, (b0 & 0xFCu) >> 2);
			*(target + 1) = Encode1(map, (b1 & 0xF0u) >> 4 | (b0 & 0x03u) << 4);
			*(target + 2) = Encode1(map, (b2 & 0xC0u) >> 6 | (b1 & 0x0Fu) << 2);
			*(target + 3) = Encode1(map, (b2 & 0x3Fu));
		}

		/// <inheritdoc />
		protected override unsafe int DecodeImpl(
			char* source, int sourceLength, byte* target, int targetLength)
		{
			fixed (byte* map = CharToByte)
			{
				var targetStart = target;

				var blocks = DecodeBlocks(map, source, sourceLength, target);
				source += blocks * 4;
				target += blocks * 3;
				target += DecodeTail(map, source, sourceLength, target);

				return (int) (target - targetStart);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe uint DecodeBlocks(
			byte* map, char* source, int sourceLength, byte* target)
		{
			var blocks = (uint) sourceLength / 4;
			var limit = source + blocks * 4;

			while (source < limit)
			{
				Decode4(map, source, target);
				source += 4;
				target += 3;
			}

			return blocks;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe int DecodeTail(
			byte* map, char* source, int sourceLength, byte* target)
		{
			var overflow = sourceLength % 4;
			// overflow can be 0, 2, 3, overflow == 1 is actually "corrupted", 
			// for performance reasons we don't care about this here 
			if (overflow <= 1) return 0;

			// overflow = 2234
			// set0 = get0 = get1 = always
			// set1 = get2 = overflow > 2

			var set1 = overflow > 2;

			var b0 = Decode1(map, (byte) *(source + 0));
			var b1 = Decode1(map, (byte) *(source + 1));

			*(target + 0) = (byte) (((uint) b0 << 2) | ((b1 & 0x30u) >> 4));

			if (!set1) return 1;

			var b2 = Decode1(map, (byte) *(source + 2));
			*(target + 1) = (byte) (((b2 & 0x3Cu) >> 2) | ((b1 & 0x0Fu) << 4));

			return 2;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe void Decode4(byte* map, char* source, byte* target)
		{
			var b0 = Decode1(map, (byte) *(source + 0));
			var b1 = Decode1(map, (byte) *(source + 1));
			var b2 = Decode1(map, (byte) *(source + 2));
			var b3 = Decode1(map, (byte) *(source + 3));

			*(target + 0) = (byte) (((uint) b0 << 2) | ((b1 & 0x30u) >> 4));
			*(target + 1) = (byte) (((b2 & 0x3Cu) >> 2) | ((b1 & 0x0Fu) << 4));
			*(target + 2) = (byte) (((b2 & 0x03u) << 6) | b3);
		}
	}
}
