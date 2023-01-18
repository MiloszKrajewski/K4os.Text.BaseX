using System;
using System.Runtime.CompilerServices;

namespace K4os.Text.BaseX;

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
	/// Creates Base16 codec.
	/// <see cref="Base16"/> class for some default codecs.
	/// Please note, this operation is relatively slow (to use in hot spots) so
	/// prefer using codecs as static/singletons.
	/// </summary>
	/// <param name="lowerCase">Use lower case alphabet.</param>
	public Base16Codec(bool lowerCase): 
		this(lowerCase ? Base16.LowerDigits : Base16.UpperDigits) { }
		
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
		var sourceLimit = source + sourceLength;

		fixed (byte* map = Utf8ToByte)
		{
			source += 8;
				
			while (source <= sourceLimit)
			{
				*(target + 0) = Decode2(map, *(uint*) (source - 8));
				*(target + 1) = Decode2(map, *(uint*) (source - 6));
				*(target + 2) = Decode2(map, *(uint*) (source - 4));
				*(target + 3) = Decode2(map, *(uint*) (source - 2));
				source += 8;
				target += 4;
			}

			source -= 8;
				
			while (source < sourceLimit)
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
		var sourceLimit = source + sourceLength;

		fixed (char* map = ByteToChar)
		{
			source += 4;
				
			while (source <= sourceLimit)
			{
				*(uint*) (target + 0) = Encode2(map, *(source - 4));
				*(uint*) (target + 2) = Encode2(map, *(source - 3));
				*(uint*) (target + 4) = Encode2(map, *(source - 2));
				*(uint*) (target + 6) = Encode2(map, *(source - 1));
				source += 4;
				target += 8;
			}

			source -= 4;

			while (source < sourceLimit)
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
	public override bool IsEncodedLengthKnown => true;

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