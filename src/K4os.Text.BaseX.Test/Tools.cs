using System;
using Xunit;

namespace K4os.Text.BaseX.Test;

public static class Tools
{
	public static void SpansAreEqual(
		ReadOnlySpan<char> expected, 
		ReadOnlySpan<char> actual, 
		int length = -1)
	{
		if (length < 0)
		{
			Assert.Equal(expected.Length, actual.Length);
			length = expected.Length;
		}

		Assert.True(expected.Length <= length);
		Assert.True(actual.Length <= length);

		for (var i = 0; i < length; i++)
			if (expected[i] != actual[i])
				throw new Exception($"Expected '{expected[i]}' at {i}, got '{actual[i]}'");
	}

	public static void SpansAreEqual(
		ReadOnlySpan<byte> expected, 
		ReadOnlySpan<byte> actual,
		int length = -1)
	{
		if (length < 0)
		{
			Assert.Equal(expected.Length, actual.Length);
			length = expected.Length;
		}

		Assert.True(expected.Length <= length);
		Assert.True(actual.Length <= length);
		
		for (var i = 0; i < length; i++)
			if (expected[i] != actual[i])
				throw new Exception($"Expected '{expected[i]}' at {i}, got '{actual[i]}'");
	}
}
