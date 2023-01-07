using System;

namespace K4os.Text.BaseX
{
	/// <summary>
	/// Extensions for BaseX codecs.
	/// </summary>
	public static class BaseXExtensions
	{
		/// <summary>Encodes given buffer into new encoded string.</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Decoded buffer.</param>
		/// <returns>New encoded string.</returns>
		public static string Encode(this BaseXCodec codec, byte[] source) =>
			codec.Encode(source.AsSpan());

		/// <summary>Encodes given buffer into new encoded string.</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Decoded buffer.</param>
		/// <param name="offset">Offset in source buffer.</param>
		/// <param name="length">Length of source buffer.</param>
		/// <returns>New encoded string.</returns>
		public static string Encode(
			this BaseXCodec codec, byte[] source, int offset, int length) =>
			codec.Encode(source.AsSpan(offset, length));

		/// <summary>Decodes encoded string into new byte buffer</summary>
		/// <param name="codec">Codec.</param>
		/// <param name="source">Encoded string.</param>
		/// <returns>New decoded buffer.</returns>
		public static byte[] Decode(
			this BaseXCodec codec, string source) =>
			codec.Decode(source.AsSpan());
	}
}
