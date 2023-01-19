using System;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;
using K4os.Text.BaseX.Codecs;

namespace Benchmarks.Showcase;

public class Showcase
{
	public enum AlgorithmType { Base16, Base64 }

	public enum OperationType { Encode, Decode }

	private static readonly BaseXCodec Default16 = new Base16Codec();
	private static readonly BaseXCodec Simd16 = new SimdBase16Codec();

	private static readonly BaseXCodec Default64 = new Base64Codec();
	private static readonly BaseXCodec Lookup64 = new LookupBase64Codec();
	private static readonly BaseXCodec Simd64 = new SimdBase64Codec();

	private byte[] _source;
	private char[] _encoded;
	private byte[] _decoded;

	[Params(AlgorithmType.Base16, AlgorithmType.Base64)]
	public AlgorithmType Algorithm { get; set; }

	[Params(OperationType.Encode, OperationType.Decode)]
	public OperationType Operation { get; set; }

	[Params(0x10000)]
	public int Length { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		_source = new byte[Length];
		_decoded = new byte[Length];
		new Random(1337).NextBytes(_source);
		
		var encodedLength = Algorithm switch {
			AlgorithmType.Base16 => Default16.EncodedLength(_source),
			AlgorithmType.Base64 => Default64.EncodedLength(_source),
			_ => throw new ArgumentOutOfRangeException(),
		};

		_encoded = new char[encodedLength];
		
		_ = Algorithm switch {
			AlgorithmType.Base16 => Default16.Encode(_source, _encoded),
			AlgorithmType.Base64 => Default64.Encode(_source, _encoded),
			_ => throw new ArgumentOutOfRangeException(),
		};
	}

	private static void NotImplemented() => throw new NotImplementedException();

	public void FrameworkEncode16() { _ = Convert.ToHexString(_source); }

	public void FrameworkDecode16() { _ = Convert.FromHexString(_encoded); }

	public void FrameworkEncode64() { Convert.TryToBase64Chars(_source, _encoded, out _); }

	public void FrameworkDecode64() { Convert.TryFromBase64Chars(_encoded, _decoded, out _); }

	public void CodecEncode(BaseXCodec codec) { codec.Encode(_source, _encoded); }

	public void CodecDecode(BaseXCodec codec) { codec.Decode(_encoded, _decoded); }

	[Benchmark(Baseline = true)]
	public void Framework()
	{
		switch (Operation, Algorithm)
		{
			case (OperationType.Encode, AlgorithmType.Base16):
				FrameworkEncode16();
				break;
			case (OperationType.Decode, AlgorithmType.Base16):
				FrameworkDecode16();
				break;
			case (OperationType.Encode, AlgorithmType.Base64):
				FrameworkEncode64();
				break;
			case (OperationType.Decode, AlgorithmType.Base64):
				FrameworkDecode64();
				break;
		}
	}

	[Benchmark]
	public void Default()
	{
		switch (Operation, Algorithm)
		{
			case (OperationType.Encode, AlgorithmType.Base16):
				CodecEncode(Default16);
				break;
			case (OperationType.Decode, AlgorithmType.Base16):
				CodecDecode(Default16);
				break;
			case (OperationType.Encode, AlgorithmType.Base64):
				CodecEncode(Default64);
				break;
			case (OperationType.Decode, AlgorithmType.Base64):
				CodecDecode(Default64);
				break;
		}
	}

	[Benchmark]
	public void Lookup()
	{
		switch (Operation, Algorithm)
		{
			case (OperationType.Encode, AlgorithmType.Base16):
				NotImplemented();
				break;
			case (OperationType.Decode, AlgorithmType.Base16):
				NotImplemented();
				break;
			case (OperationType.Encode, AlgorithmType.Base64):
				CodecEncode(Lookup64);
				break;
			case (OperationType.Decode, AlgorithmType.Base64):
				CodecDecode(Lookup64);
				break;
		}
	}

	[Benchmark]
	public void Simd()
	{
		switch (Operation, Algorithm)
		{
			case (OperationType.Encode, AlgorithmType.Base16):
				CodecEncode(Simd16);
				break;
			case (OperationType.Decode, AlgorithmType.Base16):
				CodecDecode(Simd16);
				break;
			case (OperationType.Encode, AlgorithmType.Base64):
				CodecEncode(Simd64);
				break;
			case (OperationType.Decode, AlgorithmType.Base64):
				CodecDecode(Simd64);
				break;
		}
	}
}
