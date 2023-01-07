using System;
using BenchmarkDotNet.Attributes;
using Exyll;
using K4os.Text.BaseX;

namespace Benchmarks.Base64
{
	[MemoryDiagnoser]
	public class Encoder64VsExyll
	{
		private static Base64Encoder _exyll;
		private static Base64Codec _mine;
		private byte[] _source;
		private char[] _target;

		[Params(16, 1337, 65536)]
		public int Length { get; set; }

		[GlobalSetup]
		public void Setup()
		{
			_mine = new Base64Codec();
			_exyll = Base64Encoder.Default;
			_source = new byte[Length];
			new Random().NextBytes(_source);
			_target = new char[_mine.MaximumEncodedLength(_source.Length)];
		}

		[Benchmark]
		public void Base64_Span() { _mine.Encode(_source, _target); }

		[Benchmark]
		public unsafe void Base64_Span_Ex()
		{
			var length = _target.Length;
			var target = new string('\0', length);
			fixed (char* pTarget = target) _mine.Encode(_source, new Span<char>(pTarget, length));
			_ = target;
		}

		[Benchmark]
		public void Base64_String() { _ = _mine.Encode(_source); }
		
		[Benchmark(Baseline = true)]
		public void Framework() { _ = Convert.ToBase64String(_source); }

//		[Benchmark]
//		public void Exyll() { _ = _exyll.ToBase(_source); }
	}
}
