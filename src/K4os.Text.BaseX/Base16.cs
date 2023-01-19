using System;
using K4os.Text.BaseX.Codecs;

#if NET5_0_OR_GREATER
using K4os.Text.BaseX.Internal;
#endif

namespace K4os.Text.BaseX
{
	/// <summary>Static class with helper and factory methods for Base16 codec.</summary>
	public static class Base16
	{
		internal const string LowerDigits = "0123456789abcdef";
		internal const string UpperDigits = "0123456789ABCDEF";

		/// <summary>Codec using lower case characters by default.</summary>
		public static Base16Codec Lower { get; } = CreateCodec(true);

		/// <summary>Codec using upper case characters by default.</summary>
		public static Base16Codec Upper { get; } = CreateCodec(false);

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
		
		private static Base16Codec CreateCodec(bool lowerCase)
		{
			#if NET5_0_OR_GREATER
			if (SimdSettings.IsSimdSupported) return new SimdBase16Codec(lowerCase);
			#endif
			return new Base16Codec(lowerCase);
		}
	}
}
