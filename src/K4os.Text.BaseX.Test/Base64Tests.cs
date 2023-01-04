using System;
using Xunit;

namespace K4os.Text.BaseX.Test
{
	// using Base64Codec = BetterBase64Codec;

	public class Base64Tests
	{
		public static Base64Codec Codec = new();

		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5)]
		[InlineData(1024), InlineData(1025), InlineData(1026), InlineData(1027), InlineData(1028)]
		[InlineData(1337), InlineData(1338), InlineData(1339), InlineData(1340), InlineData(1311)]
		[InlineData(1024*1024+2)]
		public void EncoderTests(int length)
		{
			var original = new byte[length];
			new Random(1337).NextBytes(original);

			var expected = Convert.ToBase64String(original);
			var actual = Codec.Encode(original);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5)]
		[InlineData(1024), InlineData(1025), InlineData(1026), InlineData(1027), InlineData(1028)]
		[InlineData(1337), InlineData(1338), InlineData(1339), InlineData(1340), InlineData(1311)]
		[InlineData(1024*1024+2)]
		public void Roundtrip(int length)
		{
			var original = new byte[length];
			new Random(1337).NextBytes(original);

			var encoded = Codec.Encode(original);
			var decoded = Codec.Decode(encoded);

			Assert.Equal(original, decoded);
		}
		
		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5)]
		[InlineData(1024), InlineData(1025), InlineData(1026), InlineData(1027), InlineData(1028)]
		[InlineData(1337), InlineData(1338), InlineData(1339), InlineData(1340), InlineData(1311)]
		[InlineData(1024*1024+2)]
		public void RoundtripNoPadding(int length)
		{
			var noPaddingCodec = new Base64Codec(false);

			var original = new byte[length];
			new Random(1337).NextBytes(original);

			var encoded = noPaddingCodec.Encode(original);
			
			Assert.False(encoded.EndsWith('='));
			
			var decoded = noPaddingCodec.Decode(encoded);

			Assert.Equal(original, decoded);
		}
	}
}
