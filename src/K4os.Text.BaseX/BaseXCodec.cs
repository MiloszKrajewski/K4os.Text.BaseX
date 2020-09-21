using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace K4os.Text.BaseX
{
	/// <summary>Base class for all codecs.</summary>
	public abstract class BaseXCodec
	{
		/// <summary>Maximum digits. Effectively all ASCII8 characters.</summary>
		protected const int MAX_DIGIT = 255;

		private readonly byte[] _char2Byte = new byte[MAX_DIGIT + 1];
		private readonly char[] _byte2Char = new char[MAX_DIGIT + 1];
		private readonly bool[] _validChar = new bool[MAX_DIGIT + 1];

		/// <summary>Symbol to value map.</summary>
		protected ReadOnlySpan<byte> CharToByte => _char2Byte.AsSpan();

		/// <summary>Value to symbol map.</summary>
		protected ReadOnlySpan<char> ByteToChar => _byte2Char.AsSpan();

		/// <summary>Create abstract BaseX codec. Populates character maps.</summary>
		/// <param name="base">Codec's base.</param>
		/// <param name="digits">Digits.</param>
		/// <param name="caseSensitive">Indicates if codec's digits are case sensitive.</param>
		/// <exception cref="ArgumentException">Possible errors: not enough digits, too much digits, duplicate digits.</exception>
		protected BaseXCodec(int @base, string digits, bool caseSensitive)
		{
			if (digits is null || digits.Length <= 0)
				throw new ArgumentException("No digits provided");
			if (@base < 2 || @base > MAX_DIGIT)
				throw new ArgumentException($"Expected base to be between 2 and {MAX_DIGIT}");
			if (@base != digits.Length)
				throw new ArgumentException($"Expected {@base} digits");

			BuildDict(digits, caseSensitive);
		}

		/// <summary>Checks if given character is a valid digit.</summary>
		/// <param name="digit">Potential digit.</param>
		/// <returns><c>true</c> if digit is valid, <c>false</c> otherwise.</returns>
		protected bool IsValid(char digit)
		{
			var c = (ushort) digit;
			return c <= MAX_DIGIT && _validChar[c];
		}

		/// <summary>Scans encoded string for errors.</summary>
		/// <param name="source">Encoded buffer.</param>
		/// <returns>Returns index of first invalid character or -1 if no errors found.</returns>
		public int ErrorIndex(ReadOnlySpan<char> source)
		{
			source = StripPadding(source);
			var sourceLength = source.Length;
			var validSet = _validChar.AsSpan();
			for (var i = 0; i < sourceLength; i++)
			{
				var c = (ushort) source[i];
				if (c > MAX_DIGIT || !validSet[c]) return i;
			}

			return -1;
		}

		/// <summary>Validates encoded string for errors. Throws <see cref="ArgumentException"/>
		/// if encoded string is invalid.</summary>
		/// <param name="source">Encoded buffer.</param>
		/// <returns>Same encoded string.</returns>
		public ReadOnlySpan<char> Validate(ReadOnlySpan<char> source)
		{
			var index = ErrorIndex(source);
			return index < 0
				? source
				: throw new ArgumentException($"Invalid digits in encoded at index {index}");
		}

		private void BuildDict(string digits, bool caseSensitive)
		{
			for (var i = 0; i < digits.Length; i++)
			{
				var c = digits[i];
				var d = (byte) i;
				if (c > MAX_DIGIT) throw InvalidChar(c);
				if (_validChar[c]) throw DuplicateChar(c);

				if (caseSensitive)
				{
					_byte2Char[d] = c;
					_char2Byte[c] = d;
					_validChar[c] = true;
				}
				else
				{
					_byte2Char[d] = c;
					var (lc, uc) = (Lower(c), Upper(c));
					_char2Byte[lc] = _char2Byte[uc] = d;
					_validChar[lc] = _validChar[uc] = true;
				}
			}
		}

		/// <summary>Strips padding from encoded buffer.</summary>
		/// <param name="source">Encoded buffer.</param>
		/// <returns>Encoded buffer without padding.</returns>
		public virtual ReadOnlySpan<char> StripPadding(ReadOnlySpan<char> source) =>
			source;

		/// <summary>Calculates how much space is needed to decode.</summary>
		/// <param name="source">Encoded buffer.</param>
		/// <returns>Space actually needed by decoded data.</returns>
		public virtual int DecodedLength(ReadOnlySpan<char> source) =>
			MaximumDecodedLength(StripPadding(source).Length);

		/// <summary>Calculates how much space is needed to decode.</summary>
		/// <param name="sourceLength">Length of encoded buffer.</param>
		/// <returns>Space actually needed by decoded data.</returns>
		public abstract int MaximumDecodedLength(int sourceLength);

		/// <summary>
		/// Calculates how much space is needed to decode.
		/// It also compares it to <paramref name="targetLength"/> and throws
		/// <see cref="ArgumentException"/> exception if there is not enough space.
		/// </summary>
		/// <param name="sourceLength">Encoded buffer.</param>
		/// <param name="targetLength">Space available for decoded data.</param>
		/// <returns>Space actually needed by decoded data.</returns>
		protected int DecodedLength(int sourceLength, int targetLength)
		{
			var expectedLength = MaximumDecodedLength(sourceLength);
			if (targetLength < expectedLength)
				throw new ArgumentException(
					$"Target buffer is too small, expected at least {expectedLength} bytes");

			return expectedLength;
		}

		/// <summary>Calculates how much space is needed to encode.</summary>
		/// <param name="source">Decoded buffer.</param>
		/// <returns>Space actually needed by encoded data.</returns>
		public virtual int EncodedLength(ReadOnlySpan<byte> source) =>
			MaximumEncodedLength(source.Length);

		/// <summary>
		/// Calculates how much space is needed to encode.
		/// </summary>
		/// <param name="sourceLength">Length of decoded buffer.</param>
		/// <returns>Space actually needed by encoded data.</returns>
		public abstract int MaximumEncodedLength(int sourceLength);

		/// <summary>
		/// Calculates how much space is needed to encode.
		/// It also compares it to <paramref name="targetLength"/> and throws
		/// <see cref="ArgumentException"/> exception if there is not enough space.
		/// </summary>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="targetLength">Space available for encoded data.</param>
		/// <returns>Space actually needed by encoded data.</returns>
		protected int EncodedLength(ReadOnlySpan<byte> source, int targetLength)
		{
			var expectedLength = EncodedLength(source);
			if (targetLength < expectedLength)
				throw new ArgumentException(
					$"Target buffer is too small, expected at least {expectedLength} characters");

			return expectedLength;
		}

		/// <summary>Close to metal implementation for decoding procedure.</summary>
		/// <param name="source">Encoded buffer address.</param>
		/// <param name="sourceLength">Encoded buffer length.</param>
		/// <param name="target">Decoded buffer address.</param>
		/// <param name="targetLength">Decoded buffer length.</param>
		/// <returns>Number of bytes decoded.</returns>
		protected abstract unsafe int DecodeImpl(
			char* source, int sourceLength, byte* target, int targetLength);

		/// <summary>Wrapper of actual implementation allowing to use spans instead of pointers.</summary>
		/// <param name="source">Source span.</param>
		/// <param name="target">Target span.</param>
		/// <returns>Number of bytes decoded.</returns>
		protected unsafe int DecodeImpl(ReadOnlySpan<char> source, Span<byte> target)
		{
			fixed (char* sourceP = source)
			fixed (byte* targetP = target)
				return DecodeImpl(
					sourceP, source.Length,
					targetP, target.Length);
		}

		/// <summary>Close to metal implementation for encoding procedure.</summary>
		/// <param name="source">Decoded buffer address.</param>
		/// <param name="sourceLength">Decoded buffer length.</param>
		/// <param name="target">Encoded buffer address.</param>
		/// <param name="targetLength">Encoded buffer length.</param>
		/// <returns>Number of characters decoded.</returns>
		protected abstract unsafe int EncodeImpl(
			byte* source, int sourceLength,
			char* target, int targetLength);

		/// <summary>Wrapper of actual implementation allowing to use spans instead of pointers.</summary>
		/// <param name="source">Source span.</param>
		/// <param name="target">Target span.</param>
		/// <returns>Number of bytes decoded.</returns>
		protected unsafe int EncodeImpl(ReadOnlySpan<byte> source, Span<char> target)
		{
			fixed (byte* sourceP = source)
			fixed (char* targetP = target)
				return EncodeImpl(
					sourceP, source.Length,
					targetP, target.Length);
		}

		/// <summary>Decodes given string into span of bytes.</summary>
		/// <param name="source">Encoded string.</param>
		/// <param name="target">Buffer for decoded data.</param>
		/// <returns>Number of bytes actually written.</returns>
		public int Decode(ReadOnlySpan<char> source, Span<byte> target)
		{
			source = StripPadding(source);
			var targetLength = DecodedLength(source.Length, target.Length);
			return targetLength <= 0 ? 0 : DecodeImpl(source, target);
		}

		/// <summary>Encodes given buffer into span of characters.</summary>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="target">Buffer for decoded characters.</param>
		/// <returns>Number of characters actually written.</returns>
		public int Encode(ReadOnlySpan<byte> source, Span<char> target) =>
			EncodedLength(source, target.Length) == 0 ? 0 : EncodeImpl(source, target);

		/// <summary>Decodes given string into new buffer of bytes.</summary>
		/// <param name="source">Encoded string.</param>
		/// <returns>New buffer.</returns>
		public byte[] Decode(ReadOnlySpan<char> source)
		{
			source = StripPadding(source);
			var targetLength = MaximumDecodedLength(source.Length);
			if (targetLength == 0) return Array.Empty<byte>();

			var target = new byte[targetLength];
			var used = DecodeImpl(source, target.AsSpan());
			if (used != targetLength) Array.Resize(ref target, used);
			return target;
		}

		/// <summary>Encoded byte buffer into new string.</summary>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="arrayPool">Potentially <see cref="ArrayPool{T}"/> to allocate array
		/// for intermediate results.</param>
		/// <returns>New encoded string.</returns>
		public string Encode(ReadOnlySpan<byte> source, ArrayPool<char> arrayPool)
		{
			var targetLength = EncodedLength(source);
			if (targetLength == 0) return string.Empty;

			var pooled = arrayPool.TryRent(sizeof(char), targetLength);
			try
			{
				var target = pooled ?? new char[targetLength];
				var used = EncodeImpl(source, target.AsSpan());
				return new string(target, 0, used);
			}
			finally
			{
				arrayPool.TryReturn(pooled);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char Upper(char c) => c >= 'a' && c <= 'z' ? (char) (c - 'a' + 'A') : c;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static char Lower(char c) => c >= 'A' && c <= 'Z' ? (char) (c - 'A' + 'a') : c;

		/// <summary>Decodes single character.</summary>
		/// <param name="map">Character map.</param>
		/// <param name="c">Character.</param>
		/// <returns>A digit values assigned to character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static unsafe byte Decode1(byte* map, byte c) => *(map + c);

		/// <summary>Decodes single character.</summary>
		/// <param name="map">Character map.</param>
		/// <param name="c">Character.</param>
		/// <returns>A digit values assigned to character.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static unsafe byte Decode1(byte* map, char c) => *(map + (byte) c);

		/// <summary>Encodes single digit.</summary>
		/// <param name="map">Digit map.</param>
		/// <param name="v">Digit value.</param>
		/// <returns>A character assigned to digit value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static unsafe char Encode1(char* map, uint v) => *(map + v);

		/// <summary>Encodes single digit.</summary>
		/// <param name="map">Digit map.</param>
		/// <param name="v">Digit value.</param>
		/// <returns>A character assigned to digit value.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static unsafe char Encode1(char* map, int v) => *(map + (uint) v);

		private static ArgumentException InvalidChar(char c) =>
			new ArgumentException($"Invalid character '{c}'");

		private static ArgumentException DuplicateChar(char c) =>
			new ArgumentException($"Character '{c}' is duplicated");
	}
}
