using System;
using BenchmarkDotNet.Attributes;

namespace Benchmarks
{
	using BaselineCodec = K4os.Text.BaseX.Base64Codec;
	using ChallengerCodec = K4os.Text.BaseX.Base64Codec;

	public class Decoder64
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
			_decoded = new byte[_baseline.DecodedLength(_encoded)];
		}

		[Benchmark]
		public void Baseline() { _baseline.Decode(_encoded, _decoded); }

		[Benchmark]
		public void Challenger() { _challenger.Decode(_encoded, _decoded); }
	}
}
