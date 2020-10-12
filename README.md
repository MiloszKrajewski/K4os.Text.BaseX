# K4os.Text.BaseX

[![NuGet Stats](https://img.shields.io/nuget/v/K4os.Text.BaseX.svg)](https://www.nuget.org/packages/K4os.Text.BaseX)

`K4os.Text.BaseX` is an implementation of Base16, Base64 and Base85 codecs for .NET/.NET core.
I've implemented them for few reasons...

## Base16

I couldn't find any good implementation of Base16/Hex conversion in .NET, while this is something I actually use quite a lot
and I've always implemented it in projects (`internal` helper methods). 

Honestly, if you search StackOverflow for 'byte[] to hex string' and top answer is:
```
var hex = BitConverter.ToString(data).Replace("-", string.Empty);
```
then something is really really wrong.

## Base64

With Base64 the story is a little bit different. .NET has very decent Base64 codec and I wouldn't need to do anything, but...
we needed url friendly version of Base64 which replaces `+` and `/` with `-` and `_` (respectively) as both of them have special meaning in URLs. Also it wouldn't be wrong if were able to remove padding, and... `string.Replace(...)` is not fast.

But this one:
```
guid.ToByteArray().ToBase64().Replace("+", "-").Replace("/", "_").Replace("=", "");
```
is even slower.
If you do this from time to time it might be good enough, but not in tight loop in the heart of your system.

So I've created Base64 codec which allows to configure what characters are used as digits, and what character should be used (if at all) for padding.

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
