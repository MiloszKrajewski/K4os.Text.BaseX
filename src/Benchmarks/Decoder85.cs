using System;
using BenchmarkDotNet.Attributes;
using Benchmarks.Reference;
using K4os.Text.BaseX;

namespace Benchmarks
{
	using ReferenceCodec = ReferenceBase85Codec;
	using ChallengerCodec = Base85Codec;

	public class Decoder85
	{
		private ReferenceCodec _referenceCodec;
		private ChallengerCodec _challengerCoded;
		private byte[] _original;
		private char[] _encoded;
		private byte[] _decoded;

		// [Params(16, 1337, 0x10000)]
		[Params(1337)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_referenceCodec = new ReferenceBase85Codec();
			_challengerCoded = new ChallengerCodec();
			_original = new byte[Length];
			_encoded = new char[_referenceCodec.MaximumDecodedLength(_original.Length)];
			_decoded = new byte[_referenceCodec.DecodedLength(_encoded)];
			new Random(1234).NextBytes(_original);
			_referenceCodec.Encode(_original, _encoded);
		}

		[Benchmark]
		public void ReferenceEncode() { _referenceCodec.Encode(_original, _encoded); }
		
		[Benchmark]
		public void ChallengerEncode() { _challengerCoded.Encode(_original, _encoded); }

		[Benchmark]
		public void ReferenceDecode() { _referenceCodec.Decode(_encoded, _decoded); }

		[Benchmark]
		public void ChallengerDecode() { _challengerCoded.Decode(_encoded, _decoded); }
	}
}
