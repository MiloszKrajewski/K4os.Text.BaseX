using System;
using BenchmarkDotNet.Attributes;
using Exyll;
using K4os.Text.BaseX;
using K4os.Text.BaseX.Codecs;

namespace Benchmarks.Base64;

[MemoryDiagnoser]
public class Decoder64Vs
{
	private static readonly Base64Encoder ExyllCodec = Base64Encoder.Default;
	private static readonly BaseXCodec DefaultCodec = new Base64Codec();
	private static readonly BaseXCodec LookupCodec = new LookupBase64Codec();
	private static readonly BaseXCodec SimdCodec = new SimdBase64Codec();
	private byte[] _source;
	private string _encoded;
	private byte[] _decoded;

	[Params(16, 1337, 65536)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_encoded = Convert.ToBase64String(_source);
		_decoded = new byte[_source.Length];
	}

	[Benchmark]
	public void Base64_Span() { DefaultCodec.Decode(_encoded, _decoded); }
		
	[Benchmark]
	public void Base64_String() { _ = DefaultCodec.Decode(_encoded); }

	[Benchmark(Baseline = true)]
	public void Framework() { _ = Convert.FromBase64String(_encoded); }

//	[Benchmark]
//	public void Exyll() { _ = _exyll.FromBase(_encoded); }
}