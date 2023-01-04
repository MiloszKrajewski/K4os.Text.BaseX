using System;
using BenchmarkDotNet.Attributes;

namespace Benchmarks.Base16
{
	using BaselineCodec = K4os.Text.BaseX.Base16Codec;
	using ChallengerCodec = K4os.Text.BaseX.Base16Codec;

	public class Decoder16
	{
		private static BaselineCodec _baseline;
		private static ChallengerCodec _challenger;
		private byte[] _source;
		private string _encoded;
		private byte[] _decoded;

		[Params(16, 1337)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_baseline = new BaselineCodec();
			_challenger = new ChallengerCodec();
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
