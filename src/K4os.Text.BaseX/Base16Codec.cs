using System;
using System.Runtime.CompilerServices;

namespace K4os.Text.BaseX
{
	/// <summary>Base16 codec.</summary>
	public class Base16Codec: BaseXCodec
	{
		/// <summary>
		/// Creates Base16 codec with default digits.
		/// <see cref="Base16"/> class for some default codecs.
		/// Please note, this operation is relatively slow (to use in hot spots) so
		/// prefer using codecs as static/singletons.
		/// </summary>
		public Base16Codec(): this(Base16.UpperDigits) { }
		
		/// <summary>
		/// Creates Base16 codec using specific set of digits.
		/// <see cref="Base16"/> class for some default codecs.
		/// Please note, this operation is relatively slow (to use in hot spots) so
		/// prefer using codecs as static/singletons.
		/// </summary>
		/// <param name="digits"></param>
		public Base16Codec(string digits): base(16, digits, false) { }

		/// <inheritdoc />
		protected override unsafe int DecodeImpl(
			char* source, int sourceLength,
			byte* target, int targetLength)
		{
			var targetStart = target;
			var sourceEnd = source + sourceLength;

			fixed (byte* map = CharToByte)
			{
				while (source < sourceEnd)
				{
					*target = Decode2(map, *(uint*) source);
					source += 2;
					target++;
				}
			}

			return (int) (target - targetStart);
		}

		/// <inheritdoc />
		protected override unsafe int EncodeImpl(
			byte* source, int sourceLength, char* target, int targetLength)
		{
			var targetStart = target;
			var sourceEnd = source + sourceLength;

			fixed (char* map = ByteToChar)
			{
				while (source < sourceEnd)
				{
					*(uint*) target = Encode2(map, *source);
					source++;
					target += 2;
				}
			}

			return (int) (target - targetStart);
		}

		/// <inheritdoc />
		public override int MaximumDecodedLength(int sourceLength)
		{
			if ((sourceLength & 0x01) != 0)
				throw new ArgumentException("Even number of digits expected");

			return sourceLength <= 0 ? 0 : sourceLength / 2;
		}

		/// <inheritdoc />
		public override int MaximumEncodedLength(int sourceLength) =>
			sourceLength <= 0 ? 0 : sourceLength * 2;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe byte Decode2(byte* map, uint char2) =>
			(byte) (Decode1(map, (byte) char2) << 4 | Decode1(map, (byte) (char2 >> 16)));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe uint Encode2(char* map, byte value) =>
			(uint) Encode1(map, (uint) value & 0x0F) << 16 | Encode1(map, (uint) value >> 4);
	}
}
