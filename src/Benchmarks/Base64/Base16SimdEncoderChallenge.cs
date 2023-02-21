using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX.Internal;

namespace Benchmarks.Base64;

public unsafe class Base64SimdEncoderChallenge
{
	private const int Ops = 100_000;
	private byte[] _original;
	private char[] _encoded;
	private byte[] _decoded;

	[Params(32, 1024, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_original = new byte[Length];
		_encoded = new char[Length * 4 / 3 + 32];
		_decoded = new byte[Length];
		new Random(Length).NextBytes(_original);
		Convert.ToBase64String(_original).AsSpan().CopyTo(_encoded);
	}

	[Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
	public void Baseline()
	{
		fixed (byte* source = _original)
		fixed (char* target = _encoded)
		{
			for (var i = 0; i < Ops; i++)
				Baseline(source, target);
		}
	}

	[Benchmark(OperationsPerInvoke = Ops)]
	public void Challenger()
	{
		fixed (byte* source = _original)
		fixed (char* target = _encoded)
		{
			for (var i = 0; i < Ops; i++) 
				Challenger(source, target);
		}
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Baseline(byte* source, char* target) => 
		SimdBase64.Encode_SSSE3(source, Length, target, Length * 4 / 3);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void Challenger(byte* source, char* target) =>
		SimdBase64.Encode_SSSE3(source, Length, target, Length * 4 / 3);
}