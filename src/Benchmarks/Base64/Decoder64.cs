using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;
using K4os.Text.BaseX.Codecs;
using BaselineCodec = K4os.Text.BaseX.Codecs.Base64Codec;

namespace Benchmarks.Base64;

public class Decoder64
{
	private static BaseXCodec _baseline;
	private static BaseXCodec _challenger;
	private byte[] _source;
	private string _encoded;
	private byte[] _decoded;

	[Params(16, 1337, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_baseline = new BaselineCodec();
		_challenger = new SimdBase64Codec();
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_encoded = Convert.ToBase64String(_source);
		_decoded = new byte[_baseline.DecodedLength(_encoded)];
	}

	[Benchmark(Baseline = true)]
	public void Baseline() { _baseline.Decode(_encoded, _decoded); }

	[Benchmark]
	public void Challenger() { _challenger.Decode(_encoded, _decoded); }
}