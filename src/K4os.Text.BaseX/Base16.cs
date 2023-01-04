using System;
using K4os.Text.BaseX.Internal;

#if !NET5_0_OR_GREATER
// this makes SimdBase16Codec and Base16Codec painting to the same class  
using SimdBase16Codec = K4os.Text.BaseX.Base16Codec;
#endif

namespace K4os.Text.BaseX
{
	/// <summary>Static class with helper and factory methods for Base16 codec.</summary>
	public static class Base16
	{
		internal const string LowerDigits = "0123456789abcdef";
		internal const string UpperDigits = "0123456789ABCDEF";

		/// <summary>Codec using lower case characters by default.</summary>
		public static Base16Codec Lower { get; } =
			SimdSettings.IsSimdSupported ? new SimdBase16Codec(true) : new Base16Codec(true);

		/// <summary>Codec using upper case characters by default.</summary>
		public static Base16Codec Upper { get; } =
			SimdSettings.IsSimdSupported ? new SimdBase16Codec(false) : new Base16Codec(false);

		/// <summary>Default codec (same as <see cref="Upper"/>.</summary>
		public static Base16Codec Default => Upper;

		/// <summary>Converts byte array to Base16 string.</summary>
		/// <param name="decoded">Decoded buffer.</param>
		/// <returns>Base16 encoded string.</returns>
		public static string ToHex(this byte[] decoded) => Default.Encode(decoded);

		/// <summary>Converts byte span to Base16 string.</summary>
		/// <param name="decoded">Decoded buffer.</param>
		/// <returns>Base16 encoded string.</returns>
		public static string ToHex(this ReadOnlySpan<byte> decoded) => Default.Encode(decoded);

		/// <summary>Converts Base16 encoded string to byte array.</summary>
		/// <param name="encoded">Encoded string.</param>
		/// <returns>Decoded byte array.</returns>
		public static byte[] FromHex(this string encoded) => Default.Decode(encoded);
	}
}
