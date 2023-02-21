using System;
using System.Buffers;

namespace K4os.Text.BaseX.Internal;

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
using System.Runtime.CompilerServices;
#endif

#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
// SpanAction already exists
#else
internal delegate void SpanAction<TItem, in TArguments>(
	Span<TItem> span, TArguments args);
#endif

internal delegate TResult SpanFunc<TItem, in TArguments, out TResult>(
	Span<TItem> span, TArguments args);

internal static class Polyfill
{
	private const int MAX_STACKALLOC_CHAR = 512;

	#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static string CreateFixedString<TArgs>(
		int length, TArgs arguments, SpanAction<char, TArgs> action) =>
		string.Create(length, arguments, action);

	#else

	public static unsafe string CreateFixedString<TArgs>(
		int length, TArgs arguments, SpanAction<char, TArgs> action)
	{
		if (length <= 0)
			return string.Empty;
		
		char[]? pooled = null;
		var target = length <= MAX_STACKALLOC_CHAR 
			? stackalloc char[MAX_STACKALLOC_CHAR] 
			: pooled = ArrayPool<char>.Shared.Rent(length);
		
		action(target, arguments);
		
		string result;
		fixed (char* targetP = target)
			result = new string(targetP, 0, length);
		
		if (pooled is not null)
			ArrayPool<char>.Shared.Return(pooled);
		
		return result;
	}

	#endif

	public static unsafe string CreateVariableString<TArgs>(
		int length, TArgs arguments, SpanFunc<char, TArgs, int> action)
	{
		if (length <= 0)
			return string.Empty;
		
		char[]? pooled = null;
		var target = length <= MAX_STACKALLOC_CHAR 
			? stackalloc char[MAX_STACKALLOC_CHAR] 
			: pooled = ArrayPool<char>.Shared.Rent(length);
		
		var used = action(target, arguments);
		
		string result;
		fixed (char* targetP = target)
			result = used > 0 ? new string(targetP, 0, used) : string.Empty;
		
		if (pooled is not null)
			ArrayPool<char>.Shared.Return(pooled);
		
		return result;
	}
}
