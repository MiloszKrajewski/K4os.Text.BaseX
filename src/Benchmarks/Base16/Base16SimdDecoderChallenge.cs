using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX.Internal;

namespace Benchmarks.Base16;

public unsafe class Base16SimdDecoderChallenge
{
	private const int Ops = 100_000;
	private byte[] _original;
	private char[] _encoded;
	private byte[] _decoded;

	private readonly sbyte[] _asciiMap = {
		(sbyte)'0', (sbyte)'1', (sbyte)'2', (sbyte)'3', (sbyte)'4',
		(sbyte)'5', (sbyte)'6', (sbyte)'7', (sbyte)'8', (sbyte)'9',
		(sbyte)'A', (sbyte)'B', (sbyte)'C', (sbyte)'D', (sbyte)'E', (sbyte)'F',
	};

	[Params(32, 1024, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_original = new byte[Length];
		_encoded = new char[Length * 2];
		_decoded = new byte[Length];
		new Random(Length).NextBytes(_original);
		Convert.ToHexString(_original).AsSpan().CopyTo(_encoded);
	}

	[Benchmark(Baseline = true, OperationsPerInvoke = Ops)]
	public void Baseline()
	{
		fixed (char* source = _encoded)
		fixed (byte* target = _decoded)
		fixed (sbyte* asciiMap = _asciiMap)
		{
			for (var i = 0; i < Ops; i++)
				Baseline(Length, source, target, asciiMap);
		}
	}

	[Benchmark(OperationsPerInvoke = Ops)]
	public void Challenger()
	{
		fixed (char* source = _encoded)
		fixed (byte* target = _decoded)
		fixed (sbyte* asciiMap = _asciiMap)
		{
			for (var i = 0; i < Ops; i++) 
				Challenger(Length, source, target, asciiMap);
		}
	}
	
//	[MethodImpl(MethodImplOptions.AggressiveInlining)]
//	private static void Baseline(int length, char* source, byte* target, sbyte* _) => 
//		SimdBase16_v2.Decode_SSE2(source, length * 2, target, length);
//
//	[MethodImpl(MethodImplOptions.AggressiveInlining)]
//	private static void Challenger(int length, char* source, byte* target, sbyte* _) =>
//		SimdBase16_v3.Decode_SSE2(source, length * 2, target, length);
	
//	[MethodImpl(MethodImplOptions.AggressiveInlining)]
//	private static void Baseline(int length, char* source, byte* target, sbyte* _) => 
//		SimdBase16_v2.Decode_SSSE3(source, length * 2, target, length);
//
//	[MethodImpl(MethodImplOptions.AggressiveInlining)]
//	private static void Challenger(int length, char* source, byte* target, sbyte* _) =>
//		SimdBase16_v3.Decode_SSSE3(source, length * 2, target, length);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Baseline(int length, char* source, byte* target, sbyte* nibbleToAscii) => 
		SimdBase16.Decode_AVX2(source, length * 2, target, length, nibbleToAscii);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static void Challenger(int length, char* source, byte* target, sbyte* nibbleToAscii) =>
		SimdBase16.Decode_AVX2(source, length * 2, target, length, nibbleToAscii);

}
