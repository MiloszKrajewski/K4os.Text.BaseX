using System;
using BenchmarkDotNet.Attributes;
using BaselineCodec = K4os.Text.BaseX.Codecs.Base16Codec;
using ChallengerCodec = K4os.Text.BaseX.Codecs.Base16Codec;

namespace Benchmarks.Base16;

public class Encoder16
{
	private static BaselineCodec _baseline;
	private static ChallengerCodec _challenger;
	private byte[] _source;
	private char[] _target;

	[Params(16, 1337)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_baseline = new BaselineCodec();
		_challenger = new ChallengerCodec();
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_target = new char[_baseline.EncodedLength(_source)];
	}

	[Benchmark]
	public void Baseline() { _baseline.Encode(_source, _target); }

	[Benchmark]
	public void Challenger() { _challenger.Encode(_source, _target); }
}