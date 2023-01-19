using System;
using Xunit;

namespace K4os.Text.BaseX.Test;

public class LookupBase64Tests
{
	[Fact]
	public unsafe void All3ByteChunks()
	{
		Span<byte> source = stackalloc byte[3];
		Span<char> expected = stackalloc char[4];
		Span<char> actual = stackalloc char[4];
		
		var baseline = new Base64Codec();
		var tested = new LookupBase64Codec();

		for (var x = 0; x <= 255; x++)
		for (var y = 0; y <= 255; y++)
		for (var z = 0; z <= 255; z++)
		{
			source[0] = (byte)x;
			source[1] = (byte)y;
			source[2] = (byte)z;
			
			baseline.Encode(source, expected);
			tested.Encode(source, actual);
			
			Tools.SpansAreEqual(expected, actual);
		}
	}
	
	[Fact]
	public unsafe void All4CharChunks()
	{
		Span<char> source = stackalloc char[4];
		Span<byte> expected = stackalloc byte[3];
		Span<byte> actual = stackalloc byte[3];
		
		var baseline = new Base64Codec();
		var tested = new LookupBase64Codec();

		const string digits = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
		var alphabetSize = digits.Length;

		for (var a = 0; a < alphabetSize; a++)
		for (var b = 0; b < alphabetSize; b++)
		for (var c = 0; c < alphabetSize; c++)
		for (var d = 0; d < alphabetSize; d++)
		{
			source[0] = digits[a];
			source[1] = digits[b];
			source[2] = digits[c];
			source[3] = digits[d];
			
			baseline.Decode(source, expected);
			tested.Decode(source, actual);
			
			Tools.SpansAreEqual(expected, actual);
		}
	}

	#if !DEBUG

	[Theory]
	[InlineData(0x10000, 1000)]
	[InlineData(16 * 1024 * 1024 + 0xFF, 100, 1337)]
	public void StressTestWithRandomData(int length, int retry, int seed = 0)
	{
		var original = new byte[length];
		var random = new Random(seed);
		var baseline = new Base64Codec();
		var tested = new LookupBase64Codec();

		var outputLength = baseline.MaximumEncodedLength(length);
		var expected = new char[outputLength];
		var actual = new char[outputLength];
		var decoded = new byte[length];

		while (retry-- > 0)
		{
			random.NextBytes(original);
			baseline.Encode(original, expected);
			tested.Encode(original, actual);
			Tools.SpansAreEqual(expected, actual);

			tested.Decode(actual, decoded);
			Tools.SpansAreEqual(original, decoded);
		}
	}

	#endif
}
