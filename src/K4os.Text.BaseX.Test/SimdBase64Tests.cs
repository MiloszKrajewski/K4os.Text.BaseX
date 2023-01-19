using System;
using K4os.Text.BaseX.Codecs;
using K4os.Text.BaseX.Internal;
using Xunit;

namespace K4os.Text.BaseX.Test;

public class SimdBase64Tests
{
	[Theory]
	[InlineData(12, 0)]
	[InlineData(15, 0)]
	[InlineData(16, 12)]
	[InlineData(17, 12)]
	[InlineData(12 * 1337 + 15, 12 * 1337)]
	[InlineData(12 * 1337 + 16, 12 * 1337 + 12)]
	public void AdjustBeforeEncodeUnlimitedTarget(int provided, int expected)
	{
		Assert.Equal(
			expected, 
			SimdBase64.AdjustBeforeEncode(true, provided, 0x100000, 16));
	}
	
	[Theory]
	[InlineData(0, 1000, 0)]
	[InlineData(12, 1000, 0)]
	[InlineData(16, 1000, 12)]
	[InlineData(12 * 1337 + 15, 100000, 12 * 1337)]
	[InlineData(12 * 1337 + 16, 100000, 12 * 1338)]
	[InlineData(12 * 1337 + 16, 16 * 1338, 12 * 1338)]
	[InlineData(12 * 1337 + 15, 16 * 1337, 12 * 1337)]
	[InlineData(12 * 1337 + 16, 16, 12)]
	public void AdjustBeforeEncode(int sourceLength, int targetLength, int expected)
	{
		Assert.Equal(
			expected, 
			SimdBase64.AdjustBeforeEncode(true, sourceLength, targetLength, 16));
	}
	
	[Theory]
	[InlineData(0, 1000, 0)]
	[InlineData(15, 1000, 0)]
	[InlineData(16, 1000, 16)]
	[InlineData(16, 16, 16)]
	[InlineData(16, 15, 0)]
	[InlineData(1337 * 16, 1337 * 12, 1336 * 16)]
	[InlineData(1337 * 16, 1336 * 12 + 15, 1336 * 16)]
	[InlineData(1337 * 16, 1336 * 12 + 16, 1337 * 16)]
	[InlineData(1337 * 16 + 15, 1336 * 12 + 16, 1337 * 16)]
	[InlineData(10000, 12, 0)]
	[InlineData(10000, 16, 16)]
	public void AdjustBeforeDecode(int sourceLength, int targetLength, int expected)
	{
		Assert.Equal(
			expected, 
			SimdBase64.AdjustBeforeDecode(true, sourceLength, targetLength, 16));
	}

	[Fact]
	public unsafe void Transforming16Bytes()
	{
		var source = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
		var expected = Convert.ToBase64String(source).Substring(0, 16);

		var target = new char[1024];
		fixed (byte* s = source)
		fixed (char* t = target)
		{
			SimdBase64.EncodeSse(s, t);
		}

		var actual = new string(target, 0, 16);

		Tools.SpansAreEqual(expected, actual);
	}

	[Fact]
	public unsafe void TransformingRandom16BytesStress()
	{
		var source = new byte[16];
		var target = new char[128];
		var random = new Random(42);

		for (var i = 0; i < 1_000_000; i++)
		{
			random.NextBytes(source);
			var expected = Convert.ToBase64String(source).Substring(0, 16);

			fixed (byte* s = source)
			fixed (char* t = target)
			{
				SimdBase64.EncodeSse(s, t);
			}

			var actual = new string(target, 0, 16);

			Tools.SpansAreEqual(expected, actual);
		}
	}

	[Theory]
	[InlineData(12)]
	[InlineData(16)]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void EncodingOnly(int length)
	{
		var simd = new SimdBase64Codec();
		
		var source = new byte[length];
		var random = new Random(42);
		random.NextBytes(source);

		var expected = Convert.ToBase64String(source);
		var actual = simd.Encode(source);
		
		Tools.SpansAreEqual(expected, actual);
	}
	
	[Fact]
	public void EncodingOnlyAllPadding()
	{
		var random = new Random(42);
		
		for (var i = 0; i < 128; i++)
		{
			var length = 0x10000 + i;
			var simd = new SimdBase64Codec();
		
			var source = new byte[length];
			random.NextBytes(source);

			var expected = Convert.ToBase64String(source);
			var actual = simd.Encode(source);
		
			Tools.SpansAreEqual(expected, actual);
		}
	}
	
	[Theory]
	[InlineData(12)]
	[InlineData(16)]
	[InlineData(1337)]
	[InlineData(0x10000)]
	public void DecodingOnly(int length)
	{
		var simd = new SimdBase64Codec();
		
		var source = new byte[length];
		var random = new Random(42);
		random.NextBytes(source);
		var encoded = Convert.ToBase64String(source);

		var decoded = simd.Decode(encoded);
		
		Tools.SpansAreEqual(source, decoded);
	}
	
	[Fact]
	public void DecodingOnlyAllPadding()
	{
		var random = new Random(42);
		
		for (var i = 0; i < 128; i++)
		{
			var length = 0x10000 + i;
			var simd = new SimdBase64Codec();

			var source = new byte[length];
			random.NextBytes(source);
			var encoded = Convert.ToBase64String(source);

			var decoded = simd.Decode(encoded);

			Tools.SpansAreEqual(source, decoded);
		}
	}
}
