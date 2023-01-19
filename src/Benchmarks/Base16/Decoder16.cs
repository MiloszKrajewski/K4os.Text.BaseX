using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;
using K4os.Text.BaseX.Codecs;

namespace Benchmarks.Base16
{
	public class Decoder16
	{
		private static BaseXCodec _baseline;
		private static BaseXCodec _challenger;
		private byte[] _source;
		private string _encoded;
		private byte[] _decoded;

		[Params(16, 1337)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_baseline = new Base16Codec();
			_challenger = new SimdBase16Codec();
			_source = new byte[Length];
			new Random().NextBytes(_source);
			_encoded = Convert.ToBase64String(_source);
			_decoded = new byte[Length];
		}

		[Benchmark]
		public void Baseline() { _baseline.Decode(_encoded, _decoded); }

		[Benchmark]
		public void Challenger() { _challenger.Decode(_encoded, _decoded); }
	}
}
