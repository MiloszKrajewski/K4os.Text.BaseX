using System;

namespace K4os.Text.BaseX;

/// <summary>
/// Common interface for all BaseX codecs.
/// </summary>
public interface IBaseXCodec
{
	/// <summary>Scans encoded string for errors.</summary>
	/// <param name="source">Encoded buffer.</param>
	/// <returns>Returns index of first invalid character or -1 if no errors found.</returns>
	int ErrorIndex(ReadOnlySpan<char> source);

	/// <summary>Validates encoded string for errors. Throws <see cref="ArgumentException"/>
	/// if encoded string is invalid.</summary>
	/// <param name="source">Encoded buffer.</param>
	/// <returns>Same encoded string.</returns>
	ReadOnlySpan<char> Validate(ReadOnlySpan<char> source);

	/// <summary>Strips padding from encoded buffer.</summary>
	/// <param name="source">Encoded buffer.</param>
	/// <returns>Encoded buffer without padding.</returns>
	ReadOnlySpan<char> StripPadding(ReadOnlySpan<char> source);

	/// <summary>Calculates how much space is needed to decode.</summary>
	/// <param name="source">Encoded buffer.</param>
	/// <returns>Space actually needed by decoded data.</returns>
	int DecodedLength(ReadOnlySpan<char> source);

	/// <summary>Calculates how much space is needed to decode.</summary>
	/// <param name="sourceLength">Length of encoded buffer.</param>
	/// <returns>Space actually needed by decoded data.</returns>
	int MaximumDecodedLength(int sourceLength);

	/// <summary>
	/// Indicates if <see cref="EncodedLength(ReadOnlySpan{byte})"/> method returns exact value,
	/// or just estimated one. If unknown, it is safer to return <c>false</c>. 
	/// </summary>
	bool IsEncodedLengthKnown { get; }

	/// <summary>Calculates how much space is needed to encode.</summary>
	/// <param name="source">Decoded buffer.</param>
	/// <returns>Space actually needed by encoded data.</returns>
	int EncodedLength(ReadOnlySpan<byte> source);

	/// <summary>
	/// Calculates how much space is needed to encode (pessimistically).
	/// </summary>
	/// <param name="sourceLength">Length of decoded buffer.</param>
	/// <returns>Space actually needed by encoded data.</returns>
	int MaximumEncodedLength(int sourceLength);

	/// <summary>Decodes given string into span of bytes.</summary>
	/// <param name="source">Encoded string.</param>
	/// <param name="target">Buffer for decoded data.</param>
	/// <returns>Number of bytes actually written.</returns>
	int Decode(ReadOnlySpan<char> source, Span<byte> target);

	/// <summary>Encodes given buffer into span of characters.</summary>
	/// <param name="source">Decoded buffer.</param>
	/// <param name="target">Buffer for decoded characters.</param>
	/// <returns>Number of characters actually written.</returns>
	int Encode(ReadOnlySpan<byte> source, Span<char> target);

	/// <summary>Decodes given string into new buffer of bytes.</summary>
	/// <param name="source">Encoded string.</param>
	/// <returns>New buffer.</returns>
	byte[] Decode(ReadOnlySpan<char> source);

	/// <summary>Encoded byte buffer into new string.</summary>
	/// <param name="source">Decoded buffer.</param>
	/// <returns>New encoded string.</returns>
	string Encode(ReadOnlySpan<byte> source);
}

