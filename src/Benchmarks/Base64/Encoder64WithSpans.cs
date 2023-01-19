using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;
using BaselineCodec = K4os.Text.BaseX.Base64Codec;

namespace Benchmarks.Base64;

public class Encoder64WithSpans
{
	private static BaseXCodec _baseline;
	private static BaseXCodec _lookup;
	private static BaseXCodec _simd;
	private byte[] _source;
	private char[] _target;

	[Params(16, 1337, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_baseline = new BaselineCodec();
		_lookup = new LookupBase64Codec();
		_simd = new SimdBase64Codec();
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_target = new char[_baseline.EncodedLength(_source)];
	}

	[Benchmark(Baseline = true)]
	public void Baseline() { _baseline.Encode(_source, _target); }

	[Benchmark]
	public void Lookup() { _lookup.Encode(_source, _target); }
	
	[Benchmark]
	public void Sse() { _simd.Encode(_source, _target); }
}