using System;
using Xunit;

namespace K4os.Text.BaseX.Test;

public class ShortGuidTests
{
	[Fact]
	public void ShortGuidLengthIsCorrect()
	{
		for (var i = 0; i < 1000; i++)
		{
			Assert.Equal(ShortGuid.Length, ShortGuid.NewGuid().Text.Length);
		}
	}

	[Theory]
	[InlineData("mDh9hTvzFUiAD5lsbMypxg", "857d3898-f33b-4815-800f-996c6ccca9c6")]
	[InlineData("DklL5wcMW0ycR9aQysDAsA", "e74b490e-0c07-4c5b-9c47-d690cac0c0b0")]
	[InlineData("hioLmCbN_U2xz7f6CqEfzQ", "980b2a86-cd26-4dfd-b1cf-b7fa0aa11fcd")]
	[InlineData("_____________________w", "ffffffff-ffff-ffff-ffff-ffffffffffff")]
	[InlineData("AAAAAAAAAAAAAAAAAAAAAA", "00000000-0000-0000-0000-000000000000")]
	public void WellKnownShortGuids(string shortGuid, string longGuid)
	{
		var s = new ShortGuid(shortGuid);
		var l = new Guid(longGuid);
		
		Assert.Equal(s.Guid, l);
		Assert.Equal(s.Text, shortGuid);
	}

    [Fact]
    public void RoundtripGuidShortGuid()
	{
		for (int i = 0; i < 1000; i++)
		{
			var l = Guid.NewGuid();
			var s1 = new ShortGuid(l.ToString());
			var s2 = new ShortGuid(l);
			Assert.Equal(s1.Text, s2.Text);
			Assert.Equal(s1.Guid, s2.Guid);
			Assert.Equal(l, s2.Guid);
			var s3 = new ShortGuid(s1.Text);
			Assert.Equal(s1.Text, s3.Text);
			Assert.Equal(l, s3.Guid);
		}
	}

}
