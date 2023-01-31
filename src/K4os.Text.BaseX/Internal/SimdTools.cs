#if NET5_0_OR_GREATER

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace K4os.Text.BaseX.Internal;

internal class SimdTools: SimdSettings
{
	protected const byte PERM_0101 = 0b01000100;
	protected const byte PERM_0213 = 0b11011000;
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int AdjustBeforeTransform(
		bool support, 
		int sourceLength, uint sourceBatchSize, 
		int targetLength, uint targetBatchSize, 
		uint vectorSize)
	{
		if (!support) return 0;

		var batches = Math.Min(
			SafeBatchCount(sourceLength, sourceBatchSize, vectorSize),
			SafeBatchCount(targetLength, targetBatchSize, vectorSize));
		
		return batches * (int)sourceBatchSize;
	}
    
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static int SafeBatchCount(int length, uint batchSize, uint vectorSize) =>
		length > 0 && (uint)length >= Math.Min(batchSize, vectorSize)
			? (int)SafeBatchCountImpl((uint)length, batchSize, vectorSize) 
			: 0;

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static uint SafeBatchCountImpl(uint length, uint batchSize, uint vectorSize)
	{
		var chunkCount = length / batchSize;
		if (batchSize == vectorSize) return chunkCount;

		var vectorsPerBatch = (batchSize + vectorSize - 1) / vectorSize;
		var limit = length - (vectorsPerBatch * vectorSize - batchSize);
		var overflow = chunkCount * batchSize - limit;
		if (overflow > 0) 
			chunkCount -= (overflow + batchSize - 1) / batchSize;
		return chunkCount;
	}	

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe Vector128<byte> LoadAscii128(char* source)
	{
		var chunk0 = Sse2.LoadVector128((byte*)(source + 0)).AsInt16();
		var chunk1 = Sse2.LoadVector128((byte*)(source + 8)).AsInt16();
		return Sse2.PackUnsignedSaturate(chunk0, chunk1);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe Vector256<byte> LoadAscii256(char* source)
	{
		var chunk0 = Avx.LoadVector256((byte*)(source + 0x00));
		var chunk1 = Avx.LoadVector256((byte*)(source + 0x10));
		var merged = Avx2.PackUnsignedSaturate(chunk0.AsInt16(), chunk1.AsInt16());
		return Avx2.Permute4x64(merged.AsInt64(), PERM_0213).AsByte();
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveAscii128(
		Vector128<sbyte> source, char* target)
	{
		Sse2.Store((sbyte*)(target + 0x00), Sse2.UnpackLow(source, Vector128<sbyte>.Zero));
		Sse2.Store((sbyte*)(target + 0x08), Sse2.UnpackHigh(source, Vector128<sbyte>.Zero));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Obsolete("Use it only when you measure performance")]
	protected static unsafe void SaveAscii128(
		Vector128<sbyte> source, char* target, Vector128<sbyte> zero)
	{
		Sse2.Store((sbyte*)(target + 0x00), Sse2.UnpackLow(source, zero));
		Sse2.Store((sbyte*)(target + 0x08), Sse2.UnpackHigh(source, zero));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveAscii256(Vector256<sbyte> source, char* target)
	{
		source = Avx2.Permute4x64(source.AsInt64(), PERM_0213).AsSByte();
		Avx.Store((sbyte*)(target + 0x00), Avx2.UnpackLow(source, Vector256<sbyte>.Zero));
		Avx.Store((sbyte*)(target + 0x10), Avx2.UnpackHigh(source, Vector256<sbyte>.Zero));
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	[Obsolete("Use it only when you measure performance")]
	protected static unsafe void SaveAscii256(
		Vector256<sbyte> source, char* target, Vector256<sbyte> zero)
	{
		source = Avx2.Permute4x64(source.AsInt64(), PERM_0213).AsSByte();
		Avx.Store((sbyte*)(target + 0x00), Avx2.UnpackLow(source, zero));
		Avx.Store((sbyte*)(target + 0x10), Avx2.UnpackHigh(source, zero));
	}

	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe Vector128<byte> LoadBytes128(byte* source) => 
		Sse2.LoadVector128(source);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe Vector256<byte> LoadBytes256(byte* source) =>
		Avx2.Permute4x64(Avx.LoadVector256(source).AsInt64(), PERM_0213).AsByte();

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveBytes128(Vector128<byte> bytes, byte* target) => 
		Sse2.Store(target, bytes);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected static unsafe void SaveBytes256(Vector256<byte> bytes, byte* target) => 
		Avx.Store(target, Avx2.Permute4x64(bytes.AsInt64(), PERM_0213).AsByte());
}

#endif
