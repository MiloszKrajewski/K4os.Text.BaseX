using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace K4os.Text.BaseX.Test;

public class Playground
{
	[Fact]
	public unsafe void CreatingSameStringCreatesNewInstance()
	{
		var list = new List<string>();
		for (var i = 0; i < 10000; i++)
			list.Add(new string('x', 1));
		for (var i = 0; i < 10000; i++)
		{
			var s = list[i];
			fixed (char* p = s)
			{
				Assert.Equal('x', *p);
				*p = 'y';
			}
		}
	}

	[Fact]
	public void Base64Mapping12()
	{
		var buffer = new byte[3];
		string? expected = null;

		for (var a = 0; a <= 255; a++)
		for (var b = 0; b <= 255; b++)
		for (var c = 0; c <= 255; c++)
		{
			if (c == 0) expected = null;

			buffer[0] = (byte)a;
			buffer[1] = (byte)b;
			buffer[2] = (byte)c;

			var text = Convert.ToBase64String(buffer).Substring(0, 2);
			expected ??= text;

			Assert.Equal(expected, text);
		}
	}

	[Fact]
	public void Base64Mapping23()
	{
		var buffer = new byte[3];
		string? expected = null;

		for (var a = 0; a <= 255; a++)
		for (var b = 0; b <= 255; b++)
		for (var c = 0; c <= 255; c++)
		{
			if (c == 0) expected = null;

			buffer[0] = (byte)c;
			buffer[1] = (byte)a;
			buffer[2] = (byte)b;

			var text = Convert.ToBase64String(buffer).Substring(2, 2);
			expected ??= text;

			Assert.Equal(expected, text);
		}
	}
}
