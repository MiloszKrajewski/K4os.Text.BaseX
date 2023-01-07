## 0.0.6 (2023/01/06)
* IMPROVED: ShortGuid interface (added .Parse, .TryParse, .CanParse, .Create)
* IMPROVED: ShortGuid parsing (less allocations)
* IMPROVED: Better string allocation (affect all encoding)

## 0.0.5 (2023/01/04)
* IMPROVED: Base16 can use SIMD (SSE2/AVX2) instructions to speed up encoding/decoding

## 0.0.4 (2021/08/13)
* IMPROVED: Base85 is now faster

## 0.0.3 (2020/10/12)
* ADDED: Base85 codec
* ADDED: ShortGuid constructor can take "normal" Guid text representation as well

## 0.0.1 (2020/09/21)
* ADDED: Base64 codec
* ADDED: Base16 codec
* ADDED: ShortGuid
