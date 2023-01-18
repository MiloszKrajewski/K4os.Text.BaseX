using System;
using BenchmarkDotNet.Attributes;
using Exyll;
using K4os.Text.BaseX;

namespace Benchmarks.Base64;

[MemoryDiagnoser]
public class Decoder64Vs
{
	private static Base64Encoder _exyll;
	private static Base64Codec _mine;
	private byte[] _source;
	private string _encoded;
	private byte[] _decoded;

	[Params(16, 1337, 65536)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_mine = new Base64Codec();
		_exyll = Base64Encoder.Default;
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_encoded = Convert.ToBase64String(_source);
		_decoded = new byte[_source.Length];
	}

	[Benchmark]
	public void Base64_Span() { _mine.Decode(_encoded, _decoded); }
		
	[Benchmark]
	public void Base64_String() { _ = _mine.Decode(_encoded); }

	[Benchmark(Baseline = true)]
	public void Framework() { _ = Convert.FromBase64String(_encoded); }

//	[Benchmark]
//	public void Exyll() { _ = _exyll.FromBase(_encoded); }
}