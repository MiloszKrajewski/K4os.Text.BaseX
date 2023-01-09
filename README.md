# K4os.Text.BaseX

[![NuGet Stats](https://img.shields.io/nuget/v/K4os.Text.BaseX.svg)](https://www.nuget.org/packages/K4os.Text.BaseX)

`K4os.Text.BaseX` is an implementation of Base16, Base64 and Base85 codecs for .NET/.NET core.
I've implemented them for few reasons...

## Base16

I couldn't find any good implementation of Base16/Hex conversion in .NET, while this is something I actually use quite a lot
and I've always implemented it in projects (`internal` helper methods). 

Honestly, if you search StackOverflow for 'byte[] to hex string' and [top answer](https://stackoverflow.com/questions/623104/byte-to-hex-string) is:
```
var hex = BitConverter.ToString(data).Replace("-", string.Empty);
```
then something is really really wrong.

Above approach is 100s times slower than `BaseX` implementation.

To be frank, Since .NET 5 there are very efficient `Convert.FromHexString` and `Convert.ToHexString` methods 
(actual implementation is in `HexConverter`, see 
[here](https://source.dot.net/#System.Net.Primitives/src/libraries/Common/src/System/HexConverter.cs)). 

|             Method | Length |            Mean | Ratio | Alloc Ratio |
|-------------------:|-------:|----------------:|------:|------------:|
|       HexConverter |     32 |        27.18 ns |  1.00 |        1.00 |
| ToStringAndReplace |     32 |       456.46 ns | 16.79 |        2.42 |
|                    |        |                 |       |             |
|       HexConverter |    256 |        95.96 ns |  1.00 |        1.00 |
| ToStringAndReplace |    256 |     3,307.41 ns | 34.41 |        2.49 |
|                    |        |                 |       |             |
|       HexConverter |   4096 |     1,224.75 ns |  1.00 |        1.00 |
| ToStringAndReplace |   4096 |    57,062.27 ns | 46.54 |        2.50 |
|                    |        |                 |       |             |
|       HexConverter |  65536 |    98,964.28 ns |  1.00 |        1.00 |
| ToStringAndReplace |  65536 | 1,277,662.01 ns | 13.32 |        2.50 |

As you can see, `HexConverter` is up to ~50x faster than `ToString().Replace()` and allocates 2.5x less memory.

So, since `HexConverter` was added in .NET 5 `BaseX` library is not that "revolutionary" anymore, 
but before .NET 5 is might be a life saver.

Same as `HexConverter`, it has SIMD (SSE2/SSSE3/AVX2) encoder implementations giving it roughly the same performance, 
although `HexConverter` always allocates a new string, while `BaseX` can store output in a provided `Span<char>` 
so it can be used with, for example, `IBufferWriter<char>`, avoiding any allocations. 

### Base16 encoding

|        Method | Length |          Mean | Ratio |
|--------------:|-------:|--------------:|------:|
|   Base16_Span |     32 |      12.98 ns |  0.49 |
| Base16_String |     32 |      26.82 ns |  1.01 |
|  HexConverter |     32 |      26.59 ns |  1.00 |
|               |        |               |       |
|   Base16_Span |    256 |      28.70 ns |  0.31 |
| Base16_String |    256 |      68.90 ns |  0.74 |
|  HexConverter |    256 |      93.39 ns |  1.00 |
|               |        |               |       |
|   Base16_Span |   4096 |     391.60 ns |  0.33 |
| Base16_String |   4096 |     783.06 ns |  0.66 |
|  HexConverter |   4096 |   1,180.64 ns |  1.00 |
|               |        |               |       |
|   Base16_Span |  65536 |   6,168.66 ns |  0.05 |
| Base16_String |  65536 | 120,121.02 ns |  0.91 |
|  HexConverter |  65536 | 128,228.19 ns |  1.00 |

As you can see, when encoding to `string` `BaseX` encoder is a little bit faster than `HexConverter` for large inputs, 
and roughly the same for small inputs. However, it is much faster when encoding to pre-allocated `Span<char>`, which 
open door to all no-allocation tricks (like mentioned before `IBufferWriter<char>`, `stackalloc`, etc). 
Yes, it is not fair comparison, as `HexConverter` does not have such mode, but at the same time that's exactly the point: 
`HexConverter` **does not have such mode**.

### Base16 decoding

`Base16` decoder is much faster though, up to 10x faster than `HexConverter` actually:

|        Method | Length |         Mean | Ratio |
|--------------:|-------:|-------------:|------:|
|   Base16_Span |     32 |     23.55 ns |  0.39 |
| Base16_String |     32 |     30.27 ns |  0.50 |
|  HexConverter |     32 |     60.20 ns |  1.00 |
|               |        |              |       |
|   Base16_Span |    256 |     47.02 ns |  0.12 |
| Base16_String |    256 |     62.10 ns |  0.15 |
|  HexConverter |    256 |    402.66 ns |  1.00 |
|               |        |              |       |
|   Base16_Span |   4096 |    493.34 ns |  0.08 |
| Base16_String |   4096 |    638.46 ns |  0.10 |
|  HexConverter |   4096 |  6,118.14 ns |  1.00 |
|               |        |              |       |
|   Base16_Span |  65536 |  7,626.52 ns |  0.08 |
| Base16_String |  65536 |  9,258.14 ns |  0.10 |
|  HexConverter |  65536 | 96,583.53 ns |  1.00 |

This is because `HexConverter` does not have SIMD decoder implementation (yet, I believe), while `BaseX` does.

**NOTE**: decoder is **very** tolerant to invalid input - it will not crash, but it will also not report any errors. Garbage in, garbage out, but no warning.  

## Base64

With Base64 the story is a little bit different. .NET has very decent Base64 codec and I wouldn't need to do anything, but...
we needed url friendly version of Base64 which replaces `+` and `/` with `-` and `_` (respectively) as both of them have 
special meaning in URLs. Also it wouldn't be wrong if were able to remove padding, and... `string.Replace(...)` is not fast.

But this one:
```
guid.ToByteArray().ToBase64().Replace("+", "-").Replace("/", "_").Replace("=", "");
```
is even slower.
If you do this from time to time it might be good enough, but not in tight loop in the heart of your system.

So I've created Base64 codec which allows to configure what characters are used as digits, 
and what character should be used (if at all) for padding. It is also a little bit faster (up to ~2x) 
and has allocation free mode.

### Base64 decoding

Performance-wise I can see some moderate improvement, between 30% to 50%, vs .NET default implementation.
Bigger the input, the better. On top of it allows to use `Span<byte>` to avoid allocations.

|        Method | Length |          Mean | Ratio |
|--------------:|-------:|--------------:|------:|
|   Base64_Span |     16 |      30.69 ns |  0.58 |
| Base64_String |     16 |      38.82 ns |  0.74 |
|     Framework |     16 |      52.64 ns |  1.00 |
|               |        |               |       |
|   Base64_Span |   1337 |     826.11 ns |  0.38 |
| Base64_String |   1337 |     926.92 ns |  0.43 |
|     Framework |   1337 |   2,171.19 ns |  1.00 |
|               |        |               |       |
|   Base64_Span |  65536 |  39,712.84 ns |  0.42 |
| Base64_String |  65536 |  42,912.79 ns |  0.46 |
|     Framework |  65536 |  93,885.76 ns |  1.00 |

### Base64 encoding

It seems that framework implementation of Base64 encoding is very fast for vary short strings.
I did take a look what I do differently, and it seems that it uses some internal method to allocate
new string: `string.FastAllocateString(int)`. Unfortunately, it is not exposed publicly, so I can't use it.

It is actually possible to do something similar, 

```csharp
var target = new string('\0', length);
fixed (char* targetP = target) 
    codec.Encode(source, new Span<char>(targetP, target.Length));
```

but I'm not sure if it is safe. You can do it yourself though if you're feeling lucky!

|        Method | Length |          Mean | Ratio |
|--------------:|-------:|--------------:|------:|
|   Base64_Span |     16 |      22.31 ns |  0.79 |
| Base64_String |     16 |      37.94 ns |  1.35 |
|     Framework |     16 |      28.11 ns |  1.00 |
|               |        |               |       |
|   Base64_Span |   1337 |     723.81 ns |  0.51 |
| Base64_String |   1337 |     874.75 ns |  0.63 |
|     Framework |   1337 |   1,408.38 ns |  1.00 |
|               |        |               |       |
|   Base64_Span |  65536 |  35,438.87 ns |  0.30 |
| Base64_String |  65536 |  94,941.04 ns |  0.81 |
|     Framework |  65536 | 118,610.53 ns |  1.00 |

For bigger strings `Base64` is faster (roughly 20% less time) as allocation speed means less, 
and as usual allocation free mode is much faster (as there is no allocation at all).

## Base85

I've implemented this one for completeness. I'm not using Base85 often, but it quite interesting concept.

Ascii85 is probably best codec if you care about size but you still want to be in "visible" range (I mean ASCII characters).
Unfortunately, this is not full implementation as it does not handle whitespaces inside encoded data, it expects
one contiguous string of characters, no spaces, tabs nor new lines.
On one hand it is an inconvenience but on the other hand it allowed to get much better performance 
(still not better than Base64, though, but Base64 will be hand to beat).

There were some decent implementations already. For example:  
I used [this one](https://faithlife.codes/blog/2012/02/ascii85-implementation-in-csharp/) in unit test 
to validate correctness of my implementation (note: not used in runtime, just unit tests). 
They are all (other solutions I've found) a little bit older though, they don't use `Span<byte>`, 
they do quite a lot of memory allocation, so I assumed I co do a little bit better.

Did I succeed? Well, this is debatable as one of design decisions for `Base16` and `Base64` totally backfired. 
I assumed that decoded length can be derived solely for length of encoded string, but `Base85` uses a form of
RLE compression so decoded length cannot be guessed without actually inspecting data (or you can use 
pessimistic estimation, but it will most likely inflate size 5-fold). I did fine (not great) 
tackling it, but definitely had one "oops!" moments here.       

## ShortGuid

On top of this I added some simple implementation of [`ShortGuid`](https://www.madskristensen.net/blog/A-shorter-and-URL-friendly-GUID).

# Usage

TBD

# Build

```shell
paket install
fake build
```
