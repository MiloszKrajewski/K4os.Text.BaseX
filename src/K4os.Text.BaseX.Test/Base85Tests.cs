using System;
using System.Text;
using System.Threading.Tasks;
using K4os.Text.BaseX.Codecs;
using ReferenceCodec.Logos;
using Xunit;

namespace K4os.Text.BaseX.Test
{
	public class Base85Tests
	{
		private const string WikipediaText =
			"Man is distinguished, not only by his reason, but by this singular " +
			"passion from other animals, which is a lust of the mind, that by a " +
			"perseverance of delight in the continued and indefatigable generation " +
			"of knowledge, exceeds the short vehemence of any carnal pleasure.";

		private const string WikipediaAscii85 =
			@"9jqo^BlbD-BleB1DJ+*+F(f,q/0JhKF<GL>Cj@.4Gp$d7F!,L7@<6@)/0JDEF<G%<+EV:2F!,O<DJ" +
			@"+*.@<*K0@<6L(Df-\0Ec5e;DffZ(EZee.Bl.9pF""AGXBPCsi+DGm>@3BB/F*&OCAfu2/AKYi(DIb" +
			@":@FD,*)+C]U=@3BN#EcYf8ATD3s@q?d$AftVqCh[NqF<G:8+EV:.+Cf>-FD5W8ARlolDIal(DId<j" +
			@"@<?3r@:F%a+D58'ATD4$Bl@l3De:,-DJs`8ARoFb/0JMK@qB4^F!,R<AKZ&-DfTqBG%G>uD.RTpAK" +
			@"Yo'+CT/5+Cei#DII?(E,9)oF*2M7/c";

		[Fact]
		public void EmptyBufferEncodesAsEmptyString()
		{
			var codec = new Base85Codec();
			var encoded = codec.Encode(Array.Empty<byte>());
			Assert.Equal(string.Empty, encoded);
		}

		[Fact]
		public void SingleByteEncodesToTwoCharacters()
		{
			var codec = new Base85Codec();
			var encoded = codec.Encode(new byte[] { 0 });
			Assert.Equal("!!", encoded);
		}

		[Theory]
		[InlineData("", "")]
		[InlineData("00", "!!")]
		[InlineData("0000", "!!!")]
		[InlineData("000000", "!!!!")]
		[InlineData("00000000", "z")]
		[InlineData("0000000000", "z!!")]
		[InlineData("0000000000000000", "zz")]
		public void SomeWellKnownPaddingCases(string sourceHex, string expectedAscii85)
		{
			var codec = new Base85Codec();
			var source = Base16.Default.Decode(sourceHex);
			var target = codec.Encode(source);
			Assert.Equal(expectedAscii85, target);
		}

		[Fact]
		public void EncodingExampleFromWikipedia()
		{
			var text = WikipediaText;
			var original = Encoding.ASCII.GetBytes(text);
			var encoded = Base85.Default.Encode(original);
			Assert.Equal(WikipediaAscii85, encoded);
		}

		[Fact]
		public void DecodingExampleFromWikipedia()
		{
			var text = WikipediaAscii85;
			var decodedBytes = Base85.Default.Decode(text);
			var decodedText = Encoding.ASCII.GetString(decodedBytes);
			Assert.Equal(WikipediaText, decodedText);
		}

		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5), InlineData(6)]
		[InlineData(1337), InlineData(1024), InlineData(4000), InlineData(5000)]
		[InlineData(16 * 1024 * 1024)]
		public void EncodeRoundtripVsReference(int length)
		{
			var random = new Random(1337); // same seed
			var original = new byte[length];
			random.NextBytes(original);

			var encoded = Base85.Default.Encode(original);
			var decoded = Ascii85.Decode(encoded);

			Assert.Equal(decoded, original);
		}

		[Theory]
		[InlineData(1), InlineData(2), InlineData(3), InlineData(4), InlineData(5), InlineData(6)]
		[InlineData(1337), InlineData(1024), InlineData(4000), InlineData(5000)]
		[InlineData(16 * 1024 * 1024)]
		public void DecoderRoundtripVsReference(int length)
		{
			var random = new Random(1337); // same seed
			var original = new byte[length];
			random.NextBytes(original);

			var encoded = Ascii85.Encode(original);
			var decoded = Base85.Default.Decode(encoded);

			Assert.Equal(decoded, original);
		}

		[Theory]
		[InlineData("0000xx", "zxxx")]
		[InlineData("x0000xx", "xxxxxxxxx")]
		[InlineData("xx0000xx", "xxxxxxxxxx")]
		[InlineData("xxx0000xx", "xxxxxxxxxxxx")]
		[InlineData("xxxx0000xx", "xxxxxzxxx")]
		[InlineData("xxxx0000", "xxxxxz")]
		[InlineData("xxxx000000", "xxxxxzxxx")]
		public void RleCompression(string sourceTemplate, string targetTemplate)
		{
			var random = new Random(1337); // same seed
			byte NonZero() => (byte) (random.Next(255) + 1);
			var original = new byte[sourceTemplate.Length];
			for (var i = 0; i < original.Length; i++)
				original[i] = sourceTemplate[i] == '0' ? (byte) 0 : NonZero();

			var encoded = Base85.Default.Encode(original);
			
			Assert.Equal(targetTemplate.Length, encoded.Length);
			for (int i = 0; i < encoded.Length; i++)
			{
				var isZ = encoded[i] == 'z';
				var shouldBeZ = targetTemplate[i] == 'z';
				Assert.Equal(shouldBeZ, isZ);
			}

			var decoded = Base85.Default.Decode(encoded);
			
			Assert.Equal(original, decoded);
		}
		
		public static uint Mod85(uint value) => 
			value - (uint)((value * 3233857729uL) >> 38) * 85;

		[Fact]
		public void Mod85TrickFullCoverage()
		{
			Parallel.For(0, (long)uint.MaxValue + 1, i => {
				var u32 = (uint)i;
				if (Mod85(u32) != u32 % 85)
					throw new ArithmeticException($"Mod85 fails for {u32}");
			});
		}
	}
}
