using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX.Codecs;
using K4os.Text.BaseX.Internal;

namespace Benchmarks.Base16;

public class SimdDecoder16
{
	private byte[] _original;
	private char[] _encoded;
	private byte[] _decoded;
	private Base16Codec _scalar;
	private Base16Codec _vector;

	[Params(32, 0x100, 0x1000, 0x10000)]
	public int Length { get; set; }
	
	[GlobalSetup]
	public void Setup()
	{
		_scalar = new Base16Codec();
		_vector = new SimdBase16Codec();
		_original = new byte[Length];
		_encoded = new char[Length * 2];
		_decoded = new byte[Length];
		
		new Random().NextBytes(_original);
		_scalar.Encode(_original, _encoded);
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		UpdateAlgorithm(SimdAlgorithm.Avx2);
	}

	private static void UpdateAlgorithm(SimdAlgorithm algorithm)
	{
		SimdSettings.AllowSse2 = algorithm >= SimdAlgorithm.Sse2;
		SimdSettings.AllowSsse3 = algorithm >= SimdAlgorithm.Ssse3;
		SimdSettings.AllowAvx2 = algorithm >= SimdAlgorithm.Avx2;
	}

	[Benchmark(Baseline = true)]
	public void Baseline()
	{
		UpdateAlgorithm(SimdAlgorithm.None);
		_scalar.Decode(_encoded, _decoded);
	}
	
	[Benchmark]
	public void Sse2()
	{
		UpdateAlgorithm(SimdAlgorithm.Sse2);
		_vector.Decode(_encoded, _decoded);
	}
	
	[Benchmark]
	public void Avx2()
	{
		UpdateAlgorithm(SimdAlgorithm.Avx2);
		_vector.Decode(_encoded, _decoded);
	}
}
