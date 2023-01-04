using System;
using K4os.Text.BaseX.Internal;
using Xunit;

namespace K4os.Text.BaseX.Test;

[Flags]
public enum SimdLevel
{
	None = 0,
	Sse2 = 1, Ssse3 = 2, Avx2 = 3,
	All = Avx2,
}

public class SimdBase16Tests
{
	private const int Unaligned64K = 0x10000 + 32 + 16 + 4 + 3;

	private static void SpansAreEqual(ReadOnlySpan<char> expected, ReadOnlySpan<char> actual)
	{
		Assert.Equal(expected.Length, actual.Length);
		for (var i = 0; i < expected.Length; i++)
			if (expected[i] != actual[i])
				throw new Exception($"Expected '{expected[i]}' at {i}, got '{actual[i]}'");
	}

	private static void SpansAreEqual(ReadOnlySpan<byte> expected, ReadOnlySpan<byte> actual)
	{
		Assert.Equal(expected.Length, actual.Length);
		for (var i = 0; i < expected.Length; i++)
			if (expected[i] != actual[i])
				throw new Exception($"Expected '{expected[i]}' at {i}, got '{actual[i]}'");
	}

	~SimdBase16Tests() { UpdateSimdLevel(SimdLevel.All); }

	private static void UpdateSimdLevel(SimdLevel level)
	{
		SimdSettings.AllowAvx2 = level >= SimdLevel.Avx2;
		SimdSettings.AllowSsse3 = level >= SimdLevel.Ssse3;
		SimdSettings.AllowSse2 = level >= SimdLevel.Sse2;
	}

	[Theory]
	[InlineData(SimdLevel.Sse2, 3)]
	[InlineData(SimdLevel.Avx2, 3)]
	[InlineData(SimdLevel.Sse2, 16)]
	[InlineData(SimdLevel.Ssse3, 16)]
	[InlineData(SimdLevel.Avx2, 32)]
	[InlineData(SimdLevel.Sse2, 1024)]
	[InlineData(SimdLevel.Ssse3, 1024)]
	[InlineData(SimdLevel.Avx2, 1024)]
	[InlineData(SimdLevel.None, Unaligned64K)]
	[InlineData(SimdLevel.Sse2, Unaligned64K)]
	[InlineData(SimdLevel.Ssse3, Unaligned64K)]
	[InlineData(SimdLevel.Avx2, Unaligned64K)]
	public void EncoderCorrectness(SimdLevel level, int length)
	{
		UpdateSimdLevel(level);

		var codec = new SimdBase16Codec(false);

		var source = new byte[length];
		var expected = new char[length * 2];
		var actual = new char[length * 2];

		new Random(0).NextBytes(source);

		Convert.ToHexString(source).AsSpan().CopyTo(expected);
		codec.Encode(source, actual);

		SpansAreEqual(expected, actual);
	}
	
	[Theory]
	[InlineData(SimdLevel.None, 1)]
	[InlineData(SimdLevel.Sse2, 1)]
	[InlineData(SimdLevel.Ssse3, 1)]
	[InlineData(SimdLevel.Avx2, 1)]
	[InlineData(SimdLevel.Ssse3, 3)]
	[InlineData(SimdLevel.Avx2, 3)]
	[InlineData(SimdLevel.Sse2, 1337)]
	public void UnalignedEncoderCorrectness(SimdLevel level, int offset)
	{
		var length = Unaligned64K;
		UpdateSimdLevel(level);

		var codec = new SimdBase16Codec();

		var source = new byte[length + offset];
		var expected = new char[length * 2 + offset];
		var actual = new char[length * 2 + offset];

		new Random(0).NextBytes(source);

		var sourceP = source.AsSpan(offset);
		var expectedP = expected.AsSpan(offset);
		var actualP = actual.AsSpan(offset);
		
		Convert.ToHexString(sourceP).AsSpan().CopyTo(expectedP);
		codec.Encode(sourceP, actualP);

		SpansAreEqual(expectedP, actualP);
	}

	[Theory]
	[InlineData(SimdLevel.None, Unaligned64K)]
	[InlineData(SimdLevel.Sse2, Unaligned64K)]
	[InlineData(SimdLevel.Ssse3, Unaligned64K)]
	[InlineData(SimdLevel.Avx2, Unaligned64K)]
	public void EncoderLowerCase(SimdLevel level, int length)
	{
		UpdateSimdLevel(level);

		var codec = new SimdBase16Codec(true);

		var source = new byte[length];
		var expected = new char[length * 2];
		var actual = new char[length * 2];

		new Random(0).NextBytes(source);

		Convert.ToHexString(source).ToLowerInvariant().AsSpan().CopyTo(expected);
		codec.Encode(source, actual);

		SpansAreEqual(expected, actual);
	}

	[Theory]
	[InlineData(SimdLevel.Sse2, 3)]
	[InlineData(SimdLevel.Avx2, 3)]
	[InlineData(SimdLevel.Sse2, 16)]
	[InlineData(SimdLevel.Avx2, 32)]
	[InlineData(SimdLevel.Sse2, 1024)]
	[InlineData(SimdLevel.Avx2, 1024)]
	[InlineData(SimdLevel.None, Unaligned64K)]
	[InlineData(SimdLevel.Sse2, Unaligned64K)]
	[InlineData(SimdLevel.Avx2, Unaligned64K)]
	public void DecoderCorrectness(SimdLevel level, int length)
	{
		UpdateSimdLevel(level);

		var codec = new SimdBase16Codec(false);

		var source = new byte[length];
		var target = new char[length * 2];

		new Random(0).NextBytes(source);

		Convert.ToHexString(source).AsSpan().CopyTo(target);

		var actual = new byte[length];

		codec.Decode(target, actual);

		SpansAreEqual(source, actual);
	}

	[Theory]
	[InlineData(SimdLevel.None, Unaligned64K)]
	[InlineData(SimdLevel.Sse2, Unaligned64K)]
	[InlineData(SimdLevel.Avx2, Unaligned64K)]
	public void DecoderLowerCase(SimdLevel level, int length)
	{
		UpdateSimdLevel(level);

		// note: codes is setup with upper case but it should be tolerant enough
		var codec = new SimdBase16Codec(false);

		var source = new byte[length];
		var target = new char[length * 2];

		new Random(0).NextBytes(source);

		Convert.ToHexString(source).ToLowerInvariant().AsSpan().CopyTo(target);

		var actual = new byte[length];

		codec.Decode(target, actual);

		SpansAreEqual(source, actual);
	}
}
