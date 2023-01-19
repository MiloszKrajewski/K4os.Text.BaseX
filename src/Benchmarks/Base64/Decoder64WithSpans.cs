using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;
using K4os.Text.BaseX.Codecs;

namespace Benchmarks.Base64;

public class Decoder64WithSpans
{
	private static BaseXCodec _baseline;
	private static BaseXCodec _lookup;
	private static BaseXCodec _simd;
	private byte[] _source;
	private string _encoded;
	private byte[] _decoded;

	[Params(16, 1337, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_baseline = new Base64Codec();
		_lookup = new LookupBase64Codec();
		_simd = new SimdBase64Codec();
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_encoded = Convert.ToBase64String(_source);
		_decoded = new byte[_baseline.DecodedLength(_encoded)];
	}

	[Benchmark(Baseline = true)]
	public void Baseline() { _baseline.Decode(_encoded, _decoded); }
	
	[Benchmark]
	public void Lookup() { _lookup.Decode(_encoded, _decoded); }

	[Benchmark]
	public void Sse() { _simd.Decode(_encoded, _decoded); }
}