using System;
using System.Buffers;

namespace K4os.Text.BaseX
{
	/// <summary>
	/// Extensions for BaseX codecs.
	/// </summary>
	public static class BaseXExtensions
	{
		private static readonly ArrayPool<char> ArrayPool = ArrayPool<char>.Shared;

		/// <summary>Encodes given buffer into new encoded string.</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="usePool">Indicates if array pool should be used.</param>
		/// <returns>New encoded string.</returns>
		public static string Encode(
			this BaseXCodec codec, byte[] source, bool usePool = true) =>
			codec.Encode(source.AsSpan(), usePool ? ArrayPool : null);

		/// <summary>Encodes given buffer into new encoded string.</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="offset">Offset in source buffer.</param>
		/// <param name="length">Length of source buffer.</param>
		/// <param name="usePool">Indicates if array pool should be used.</param>
		/// <returns>New encoded string.</returns>
		public static string Encode(
			this BaseXCodec codec, byte[] source, int offset, int length, bool usePool = true) =>
			codec.Encode(source.AsSpan(offset, length), usePool ? ArrayPool : null);

		/// <summary>Encodes given buffer into new encoded string.</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="usePool">Indicates if array pool should be used.</param>
		/// <returns>New encoded string.</returns>
		public static string Encode(
			this BaseXCodec codec, ReadOnlySpan<byte> source, bool usePool = true) =>
			codec.Encode(source, usePool ? ArrayPool : null);

		/// <summary>Decodes encoded string into new byte buffer</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Encoded string.</param>
		/// <returns>New decoded buffer.</returns>
		public static byte[] Decode(
			this BaseXCodec codec, string source) =>
			codec.Decode(source.AsSpan());
	}
}
