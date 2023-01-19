using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX.Codecs;
using ReferenceCodec = Benchmarks.Reference.ReferenceBase85Codec;

namespace Benchmarks.Base85;

public class Codec85
{
	private ReferenceCodec _referenceCodec;
	private Base85Codec _challengerCoded;
	private byte[] _original;
	private char[] _encoded;
	private byte[] _decoded;

	[Params(16, 1337, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_referenceCodec = new ReferenceCodec();
		_challengerCoded = new Base85Codec();
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