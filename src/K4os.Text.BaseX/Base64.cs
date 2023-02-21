using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using K4os.Text.BaseX.Codecs;
using K4os.Text.BaseX.Internal;

namespace K4os.Text.BaseX;

/// <summary>Static class with helper and factory methods for Base64 codec.</summary>
public static class Base64
{
	private static Base64Codec? _serializer;

	internal const string Digits62 =
		"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

	/// <summary>Digits for default codec.</summary>
	public const string DefaultDigits = Digits62 + "+/";

	/// <summary>Digits for url codec.</summary>
	public const string UrlDigits = Digits62 + "-_";

	/// <summary>Default Base64 codec.</summary>
	public static Base64Codec Default { get; } = CreateDefaultCodec();

	/// <summary>URL friendly Base64 codec.</summary>
	public static Base64Codec Url { get; } = CreateUrlCodec();

	/// <summary>Default Base64 codec for serialization.
	/// This codec is potentially optimized for larger inputs, while <see cref="Default"/>
	/// if suppose to be general purpose.</summary>
	public static Base64Codec Serializer => GetOrCreateSerializerCodec();

	/// <summary>Converts byte array to Base64 string.</summary>
	/// <param name="decoded">Decoded buffer.</param>
	/// <returns>Base64 encoded string.</returns>
	public static string ToBase64(this byte[] decoded) => Default.Encode(decoded);

	/// <summary>Converts byte span to Base64 string.</summary>
	/// <param name="decoded">Decoded buffer.</param>
	/// <returns>Base64 encoded string.</returns>
	public static string ToBase64(this ReadOnlySpan<byte> decoded) => Default.Encode(decoded);

	/// <summary>Converts Base64 encoded string to byte array.</summary>
	/// <param name="encoded">Encoded string.</param>
	/// <returns>Decoded byte array.</returns>
	public static byte[] FromBase64(this string encoded) => Default.Decode(encoded);

	[SuppressMessage("ReSharper", "UnusedParameter.Local")]
	private static Base64Codec? TryCreateSimdCodec()
	{
		#if NET5_0_OR_GREATER
		if (SimdSettings.IsSimdSupported) 
			return new SimdBase64Codec();
		#endif
		return null;
	}

	private static Base64Codec CreateDefaultCodec() =>
		TryCreateSimdCodec() ??
		new Base64Codec(DefaultDigits, true, '=');

	private static Base64Codec CreateUrlCodec() => 
		new(UrlDigits, false, '=');

	private static Base64Codec CreateSerializerCodec()
	{
		#if NET5_0_OR_GREATER
		if (Default is SimdBase64Codec simd) return simd;
		#endif
		return IntPtr.Size < sizeof(ulong) ? Default : new LookupBase64Codec();
	}

	private static Base64Codec GetOrCreateSerializerCodec()
	{
		var serializer = Volatile.Read(ref _serializer);
		if (serializer is not null) return serializer;

		Interlocked.Exchange(ref _serializer, CreateSerializerCodec());
		return _serializer!;
	}
}
