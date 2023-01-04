using System;
using BenchmarkDotNet.Attributes;
using Exyll;
using K4os.Text.BaseX;

namespace Benchmarks.Base64
{
	public class Decoder64VsExyll
	{
		private static Base64Encoder _exyll;
		private static Base64Codec _mine;
		private byte[] _source;
		private string _encoded;

		[Params(16, 1337)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_mine = new Base64Codec();
			_exyll = Base64Encoder.Default;
			_source = new byte[Length];
			new Random().NextBytes(_source);
			_encoded = Convert.ToBase64String(_source);
		}

		[Benchmark]
		public void Mine() { _ = _mine.Decode(_encoded); }

		[Benchmark]
		public void Framework() { _ = Convert.FromBase64String(_encoded); }

		[Benchmark]
		public void Exyll() { _ = _exyll.FromBase(_encoded); }
	}
}
