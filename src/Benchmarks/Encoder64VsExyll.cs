using System;
using BenchmarkDotNet.Attributes;
using Exyll;
using K4os.Text.BaseX;

namespace Benchmarks
{
	public class Encoder64VsExyll
	{
		private static Base64Encoder _exyll;
		private static Base64Codec _mine;
		private byte[] _source;

		[Params(16, 1337)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_mine = new Base64Codec();
			_exyll = Base64Encoder.Default;
			_source = new byte[Length];
			new Random().NextBytes(_source);
		}

		[Benchmark]
		public void Mine() { _ = _mine.Encode(_source); }

		[Benchmark]
		public void Framework() { _ = Convert.ToBase64String(_source); }

		[Benchmark]
		public void Exyll() { _ = _exyll.ToBase(_source); }
	}
}
