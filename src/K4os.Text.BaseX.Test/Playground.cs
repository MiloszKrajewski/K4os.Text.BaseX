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
}
