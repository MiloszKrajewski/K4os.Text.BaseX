using System;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Base16
{
	public class Encoder16WithStringCreate
	{
		private byte[] _source;
		private string _target;

		[Params(16, 1337)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_source = new byte[Length];
			new Random().NextBytes(_source);
		}

		[Benchmark]
		public void Twitter()
		{
			_target = FormatVarBinaryTwitter(_source);
		}

		[Benchmark]
		public void BaseX() { _target = FormatVarBinaryBaseX(_source); }

		private static string FormatVarBinaryTwitter(byte[] bytes)
		{
			if (bytes is null || bytes.Length <= 0) return "0x";

			return string.Create(
				bytes.Length * 2 + 2, bytes, (targetSpan, array) => {
					targetSpan[0] = '0';
					targetSpan[1] = 'x';
					var i = 2;
					foreach (var @byte in array)
					{
						targetSpan[i++] = ToUpperHexCharacter(@byte >> 4);
						targetSpan[i++] = ToUpperHexCharacter(@byte);
					}
				});
		}

		private static char ToUpperHexCharacter(int value)
		{
			value &= 0x0F;
			value += '0';
			if (value > '9')
				value += 'A' - ('9' + 1);

			return (char)value;
		}

		private static string FormatVarBinaryBaseX(byte[] bytes)
		{
			if (bytes is null || bytes.Length <= 0)
				return "0x";

			return string.Create(
				bytes.Length * 2 + 2, bytes, (targetSpan, array) => {
					targetSpan[0] = '0';
					targetSpan[1] = 'x';
					K4os.Text.BaseX.Base16.Upper.Encode(array, targetSpan[2..]);
				});
		}
	}
}
