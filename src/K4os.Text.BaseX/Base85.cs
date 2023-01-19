using System;
using K4os.Text.BaseX.Codecs;

namespace K4os.Text.BaseX
{
	/// <summary>Static class with helper and factory methods for Base85 codec.</summary>
	public static class Base85
	{
		internal const string Digits85 =
			"!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
			"[\\]^_`abcdefghijklmnopqrstu";

		internal const char DigitZ = 'z';

		/// <summary>Default Base85 codec.</summary>
		public static Base85Codec Default { get; } = new Base85Codec();
		
		/// <summary>Converts byte array to Base85 string.</summary>
		/// <param name="decoded">Decoded buffer.</param>
		/// <returns>Base64 encoded string.</returns>
		public static string ToBase85(this byte[] decoded) => Default.Encode(decoded);
		
		/// <summary>Converts byte span to Base85 string.</summary>
		/// <param name="decoded">Decoded buffer.</param>
		/// <returns>Base64 encoded string.</returns>
		public static string ToBase85(this ReadOnlySpan<byte> decoded) => Default.Encode(decoded);

		/// <summary>Converts Base85 encoded string to byte array.</summary>
		/// <param name="encoded">Encoded string.</param>
		/// <returns>Decoded byte array.</returns>
		public static byte[] FromBase85(this string encoded) => Default.Decode(encoded);
	}
}
