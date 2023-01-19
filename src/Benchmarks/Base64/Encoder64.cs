using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;
using K4os.Text.BaseX.Codecs;

namespace Benchmarks.Base64;

public class Encoder64
{
	private static BaseXCodec _baseline;
	private static BaseXCodec _challenger;
	private byte[] _source;
	private char[] _target;

	[Params(16, 1337, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_baseline = new Base64Codec();
		_challenger = new SimdBase64Codec();
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_target = new char[_baseline.EncodedLength(_source)];
	}

	[Benchmark(Baseline = true)]
	public void Baseline() { _baseline.Encode(_source, _target); }

	[Benchmark]
	public void Challenger() { _challenger.Encode(_source, _target); }
}