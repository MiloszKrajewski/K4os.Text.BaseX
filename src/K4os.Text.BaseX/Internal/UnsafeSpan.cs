using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace K4os.Text.BaseX.Internal;

internal readonly unsafe struct UnsafeSpan<T> where T: unmanaged
{
	private readonly IntPtr _pointer;
	private readonly int _length;
	
	public UnsafeSpan(void* pointer, int length)
	{
		_pointer = (IntPtr)pointer;
		_length = length;
	}

	private UnsafeSpan(Span<T> span)
	{
		_pointer = (IntPtr)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
		_length = span.Length;
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UnsafeSpan<T> FromPinned(Span<T> span) => 
		new(span);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator Span<T>(in UnsafeSpan<T> span) =>
		new(span._pointer.ToPointer(), span._length);
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static implicit operator ReadOnlySpan<T>(in UnsafeSpan<T> span) =>
		new(span._pointer.ToPointer(), span._length);
}
