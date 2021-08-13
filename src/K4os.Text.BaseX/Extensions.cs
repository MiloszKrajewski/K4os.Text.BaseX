using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace K4os.Text.BaseX
{
	internal static class Extensions
	{
		private const int MIN_POOLED_LENGTH = 1024;

		public static T[] TryRent<T>(this ArrayPool<T> arrayPool, int sizeOfT, int arrayLength) =>
			arrayPool is null || arrayLength * sizeOfT < MIN_POOLED_LENGTH
				? null
				: arrayPool.Rent(arrayLength);

		public static void TryReturn<T>(this ArrayPool<T> arrayPool, T[] array)
		{
			if (array != null) arrayPool.Return(array);
		}

		public static T[] ValidateBounds<T>(this T[] array, int offset, int length)
		{
			if (array is null) throw new ArgumentException("Array is null");
			if (offset < 0 || offset > array.Length)
				throw new ArgumentException($"Offset {offset} is outside array bounds");
			if (offset + length > array.Length)
				throw new ArgumentException($"Length {length} is outside array bounds");

			return array;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint Mod85(this uint value) => 
			value - (uint)((value * 3233857729uL) >> 38) * 85;
	}
}
