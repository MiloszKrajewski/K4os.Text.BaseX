using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Benchmarks;

public class Vec128Encoder
{
	private static unsafe uint EncodeBlocks(
		char* map, byte* source, int sourceLength, char* target)
	{
		var blocks = (uint) sourceLength / 12;
		var limit = source + blocks * 12;

		var str = Sse2.LoadAlignedVector128((byte*)map);
		var ctr = Vector128.Create(2, 2, 1, 0, 5, 5, 4, 3, 8, 8, 7, 6, 11, 11, 10, 9).AsByte();
		Ssse3.Shuffle(str, ctr);

		var mask = Vector128.Create(0x3F000000);
		
		while (source < limit)
		{
			
			source += 3;
			target += 4;
		}

		return blocks * 4;
	}

}
