using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;

namespace Benchmarks;

public class Encoder16SseTest
{
	private static Base16Codec _baseline;
	private byte[] _source;
	private char[] _target;
	private readonly byte[] _charMap = new byte[16];

	[Params(32, 0x100, 0x1000, 0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_baseline = new Base16Codec();
		_source = new byte[Length];
		new Random().NextBytes(_source);
		_target = new char[_baseline.EncodedLength(_source)];
		Sse2Base16Encoder.BuildDigitMap('0', 'A', _charMap);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static char ToUpperHexCharacter(int value)
	{
		value &= 0x0F;
		value += '0';
		if (value > '9')
			value += 'A' - ('9' + 1);

		return (char)value;
	}
	
	private static void NaiveEncode(byte[] source, char[] target)
	{
		var length = source.Length;
		var i = 0;
		var o = 0;
		while (i < length)
		{
			target[o++] = ToUpperHexCharacter(source[i] >> 4);
			target[o++] = ToUpperHexCharacter(source[i]);
			i++;
		}
	}

	[Benchmark]
	public void Naive() { NaiveEncode(_source, _target); }

	[Benchmark(Baseline = true)]
	public void Baseline() { _baseline.Encode(_source, _target); }

	[Benchmark]
	public void Sse2() { Sse2Base16Encoder.Encode_SSE2(_source, _target, _charMap); }
	
	[Benchmark]
	public void Ssse3() { Sse2Base16Encoder.Encode_SSSE3(_source, _target, _charMap); }
	
	[Benchmark]
	public void Avx2() { Sse2Base16Encoder.Encode_AVX2(_source, _target, _charMap); }
}
