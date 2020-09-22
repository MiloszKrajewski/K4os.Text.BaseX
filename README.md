# K4os.Text.BaseX

[![NuGet Stats](https://img.shields.io/nuget/v/K4os.Text.BaseX.svg)](https://www.nuget.org/packages/K4os.Text.BaseX)

`K4os.Text.BaseX` is an implementation of Base16 and Base64 codecs for .NET/.NET core.
I've implemented them for two reasons...

## Base16

I couldn't find any good implementation of Base16/Hex conversion in .NET, while this is something I actually use quite a lot
and I've always implemented it in projects (`internal` helper methods). 

Honestly, if to search StackOverflow for 'byte[] to hex string' and top answer is:
```
var hex = BitConverter.ToString(data).Replace("-", string.Empty);
```
then something is really really wrong.

## Base64

With Base64 the story is a little bit different. .NET has very decent Base64 codec and I wouldn't need to do anything, but...
we needed url friendly version of Base64 which replaces `+` and `/` with `-` and `_` and both of them have special meaning in URLs. Also it wouldn't be wrong if were able to remove padding, and... `string.Replace(...)` is not fast.

But this one:
```
guid.ToByteArray().ToBase64().Replace("+", "-").Replace("-", "/").Replace("=", "");
```
is even slower.
If you do this from time to time it might enough but not in tight loop in the heart of your system.

So I've created Base64 codec which allows to configure what characters are going to be used for digits and what character should be used (if at all) for padding.

## ShortGuid

On top of this I added some simple implementation of [`ShortGuid`](https://www.madskristensen.net/blog/A-shorter-and-URL-friendly-GUID).

# Usage

TBD

# Build

```shell
paket install
fake build
```
