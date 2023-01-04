using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;

namespace Benchmarks.ShortGuids;

[MemoryDiagnoser]
public unsafe class ShortGuidDecoder
{
	private static readonly string Value = 
		new ShortGuid("12345678-1234-1234-1234-123456789012").Text;
	private static readonly BaseXCodec Codec = K4os.Text.BaseX.Base64.Url;
	
	[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
	// ReSharper disable once UnusedParameter.Local
	private static void NoOp<T>(T _) { }
	
	[Benchmark(Baseline = true)]
	public void Baseline()
	{
		var text = Value;
		var guid = new Guid(Codec.Decode(text));
		NoOp(guid);
	}
	
	[Benchmark]
	public void Constructor()
	{
		NoOp(new ShortGuid(Value));
	}

	[Benchmark]
	public void Challenger1()
	{
		var text = Value;
		var guid = Guid.Empty;
		var span = new Span<byte>(&guid, sizeof(Guid));
		Codec.Decode(text, span);
		NoOp(guid);
	}
}
