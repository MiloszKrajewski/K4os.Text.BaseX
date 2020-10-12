using System;

namespace K4os.Text.BaseX
{
	/// <summary>Static class with helper and factory methods for Base64 codec.</summary>
	public static class Base64
	{
		internal const string Digits62 =
			"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

		/// <summary>Default Base64 codec.</summary>
		public static Base64Codec Default { get; } = new Base64Codec();

		/// <summary>URL friendly Base64 codec.</summary>
		public static Base64Codec Url { get; } = new Base64Codec(
			$"{Digits62}-_", false, '=');
		
		/// <summary>Converts byte array to Base64 string.</summary>
		/// <param name="decoded">Decoded buffer.</param>
		/// <returns>Base64 encoded string.</returns>
		public static string ToBase64(this byte[] decoded) => Default.Encode(decoded);
		
		/// <summary>Converts byte span to Base64 string.</summary>
		/// <param name="decoded">Decoded buffer.</param>
		/// <returns>Base64 encoded string.</returns>
		public static string ToBase64(this ReadOnlySpan<byte> decoded) => Default.Encode(decoded);

		/// <summary>Converts Base64 encoded string to byte array.</summary>
		/// <param name="encoded">Encoded string.</param>
		/// <returns>Decoded byte array.</returns>
		public static byte[] FromBase64(this string encoded) => Default.Decode(encoded);
	}
}
