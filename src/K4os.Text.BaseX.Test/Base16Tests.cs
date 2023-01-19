using System;
using K4os.Text.BaseX.Codecs;
using Xunit;

namespace K4os.Text.BaseX.Test
{
	public class Base16Tests
	{
		[Fact]
		public void Decode4Bytes()
		{
			var codec = new Base16Codec();
			var value = BitConverter.ToUInt32(codec.Decode("01020304"));
			Assert.Equal(0x04030201u, value);
		}

		[Fact]
		public void Decode8Bytes()
		{
			var codec = new Base16Codec();
			var value = BitConverter.ToUInt64(codec.Decode("01020304FF88AA7C"));
			Assert.Equal(0x7caa88ff04030201uL, value);
		}

		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4)]
		[InlineData(5), InlineData(7), InlineData(8), InlineData(9)]
		[InlineData(1024), InlineData(1337)]
		[InlineData(16 * 1024 * 1024 + 7)]
		public void CompareToBitConverter(int length, int seed = 0)
		{
			var expected = new byte[length];
			new Random(seed).NextBytes(expected);
			var text = BitConverter.ToString(expected).Replace("-", string.Empty);
			var actual = new Base16Codec().Decode(text);

			Assert.Equal(expected, actual);
		}

		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4)]
		[InlineData(5), InlineData(7), InlineData(8), InlineData(9)]
		[InlineData(1024), InlineData(1337)]
		[InlineData(16 * 1024 * 1024 + 7)]
		public void Roundtrip(int length, int seed = 0)
		{
			var codec = new Base16Codec();
			var original = new byte[length];
			new Random(seed).NextBytes(original);

			var encoded = codec.Encode(original);
			var decoded = codec.Decode(encoded);

			Assert.Equal(original, decoded);
		}
	}
}
