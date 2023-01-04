using System;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using K4os.Text.BaseX;

namespace Benchmarks.ShortGuids;

[MemoryDiagnoser]
public unsafe class ShortGuidEncoder
{
	private static readonly Guid Value = new("12345678-1234-1234-1234-123456789012");
	private static readonly BaseXCodec Codec = K4os.Text.BaseX.Base64.Url;

	[MethodImpl(MethodImplOptions.NoOptimization | MethodImplOptions.NoInlining)]
	// ReSharper disable once UnusedParameter.Local
	private static void NoOp<T>(T _) { }

	[Benchmark(Baseline = true)]
	public void Baseline()
	{
		var guid = Value;
		var text = Codec.Encode(guid.ToByteArray());
		NoOp(text);
	}
	
	[Benchmark]
	public void Constructor()
	{
		NoOp(new ShortGuid(Value));
	}

	[Benchmark]
	public void Challenger1()
	{
		var guid = Value;
		var text = Codec.Encode(new ReadOnlySpan<byte>(&guid, sizeof(Guid)));
		NoOp(text);
	}

	[Benchmark]
	public void Challenger2()
	{
		var guid = Value;
		var text = string.Create(
			ShortGuid.Length,
			(IntPtr)(&guid), (span, guidP) => {
				Codec.Encode(new ReadOnlySpan<byte>((byte*)guidP, sizeof(Guid)), span);
			});
		NoOp(text);
	}
}
