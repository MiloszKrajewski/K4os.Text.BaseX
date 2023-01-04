using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;

namespace Benchmarks.Base16;

public class VsHexConverterEncoder
{
	private byte[] _original;
	private char[] _encoded;
	private SimdBase16Codec _vector;

	[Params(32, 0x100, 0x1000, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_vector = new SimdBase16Codec();
		_original = new byte[Length];
		_encoded = new char[Length * 2];

		new Random().NextBytes(_original);
	}

	[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
	// ReSharper disable once UnusedParameter.Local
	private static void NoOp<T>(T _) { }
	
	[Benchmark]
	public void Base16_Span() { _vector.Encode(_original, _encoded); }

	[Benchmark]
	public void Base16_String() { NoOp(_vector.Encode(_original)); }

	[Benchmark(Baseline = true)]
	public void HexConverter() { NoOp(Convert.ToHexString(_original)); }
}
