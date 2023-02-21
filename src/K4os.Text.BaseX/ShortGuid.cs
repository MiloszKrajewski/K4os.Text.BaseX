using System;
using K4os.Text.BaseX.Codecs;
using K4os.Text.BaseX.Internal;

namespace K4os.Text.BaseX;

/// <summary>
/// Represents a globally unique identifier (GUID) with a shorter string value.
/// </summary>
public readonly struct ShortGuid: IComparable, IEquatable<ShortGuid>, IComparable<ShortGuid>
{
	private static readonly Base64Codec Codec = Base64.Url;
	
	/// <summary>Length of ShortGuid string representation (base32 without padding).</summary>
	public const int Length = 22;

	/// <summary>A read-only instance of the ShortGuid class whose value is guaranteed to be all zeroes.</summary>
	public static readonly ShortGuid Empty = new(Guid.Empty);
	
	private static readonly string EmptyText = Empty.Text;

	private readonly Guid _guid;
	private readonly string? _text;

	/// <summary>Creates a ShortGuid from a string. It accepts both "normal" guid and
	/// base64 short guid.</summary>
	/// <param name="text">The encoded guid (as "normal" guid or base64 encoded one)</param>
	public ShortGuid(string text) { ParseShortGuid(text, out _guid, out _text); }

	/// <summary>Creates a ShortGuid from a Guid</summary>
	/// <param name="guid">The Guid to encode</param>
	public ShortGuid(Guid guid) => _text = Encode(_guid = guid);

	/// <summary>Creates a ShortGuid from a Guid as text. Does not validate anything.</summary>
	/// <param name="guid">Guid.</param>
	/// <param name="text">Text.</param>
	private ShortGuid(Guid guid, string text)
	{
		_guid = guid;
		_text = text;
	}

	/// <summary>Gets the underlying Guid.</summary>
	public Guid Guid => _guid;

	/// <summary>Gets text representation of Guid.</summary>
	public string Text => _text ?? EmptyText;

	/// <summary>Returns the base64 encoded guid as a string</summary>
	/// <returns>Text representation of short guid (in ShortGuid form)</returns>
	public override string ToString() => Text;

	/// <summary>Returns a value indicating whether given text can be parsed to <see cref="ShortGuid"/>.</summary>
	/// <param name="text">Text with ShortGuid or Guid.</param>
	/// <returns>Value indicating whether given text can be parsed.</returns>
	public static bool CanParse(string text) => Validate(text) != ShortGuidFormat.Invalid;

	/// <summary>Tries to parsed text as <see cref="ShortGuid"/>.</summary>
	/// <param name="text">Text with ShortGuid or Guid.</param>
	/// <returns><see cref="ShortGuid"/> or <c>null</c>.</returns>
	public static ShortGuid? TryParse(string? text) =>
		TryParseShortGuid(text, out var guid, out text, false) 
			? new ShortGuid(guid, text!) 
			: default(ShortGuid?);

	/// <summary>Parsed text as <see cref="ShortGuid"/>, throws exception if not valid Guid/ShortGuid.</summary>
	/// <param name="text">Text with ShortGuid or Guid.</param>
	/// <returns><see cref="ShortGuid"/>.</returns>
	public static ShortGuid Parse(string text)
	{
		ParseShortGuid(text, out var guid, out text);
		return new ShortGuid(guid, text);
	}

	/// <summary>Convert Guid to <see cref="ShortGuid"/>.</summary>
	/// <param name="guid">Guid.</param>
	/// <returns><see cref="ShortGuid"/>.</returns>
	public static ShortGuid Create(Guid guid) => new(guid);

	/// <summary>
	/// Returns a value indicating whether this instance and a 
	/// specified Object represent the same type and value.
	/// </summary>
	/// <param name="obj">The object to compare.</param>
	/// <returns><c>true</c> if objects are representing same Guid.</returns>
	public override bool Equals(object? obj) =>
		obj switch {
			ShortGuid sg => _guid.Equals(sg._guid),
			Guid g => _guid.Equals(g),
			string s => Text.Equals(s),
			_ => false,
		};

	/// <summary>Compares ShortGuid with other object. Handles ShortGuid, Guid or ShortGuid's
	/// string representation.</summary>
	/// <param name="obj">The object to compare.</param>
	/// <returns>A signed number indicating the relative values of this instance and
	/// <paramref name="obj" />.</returns>
	public int CompareTo(object? obj) =>
		obj switch {
			ShortGuid sg => _guid.CompareTo(sg._guid),
			Guid g => _guid.CompareTo(g),
			string s => _guid.CompareTo(Decode(s)),
			_ => 0,
		};

	/// <summary>Returns the HashCode for underlying Guid.</summary>
	/// <returns>Guid's HashCode.</returns>
	public override int GetHashCode() => _guid.GetHashCode();

	/// <summary>Checks if two ShortGuids are equals.</summary>
	/// <param name="other">Other Guid.</param>
	/// <returns><c>true</c> if Guids are equals; <c>false</c> otherwise</returns>
	public bool Equals(ShortGuid other) => _guid.Equals(other._guid);

	/// <summary>Compares two ShortGuids are equals.</summary>
	/// <param name="other">Other Guid.</param>
	/// <returns><c>true</c> if Guids are equals; <c>false</c> otherwise</returns>
	public int CompareTo(ShortGuid other) => _guid.CompareTo(other._guid);

	/// <summary>
	/// Initialises a new instance of the ShortGuid class
	/// </summary>
	/// <returns>New <see cref="ShortGuid"/></returns>
	public static ShortGuid NewGuid() => new(Guid.NewGuid());

	/// <summary>Encodes the given Guid as a base64 string that is 22 characters long.
	/// </summary>
	/// <param name="guid">The Guid to encode</param>
	/// <returns>Encoded short GUID as string.</returns>
	private static unsafe string Encode(Guid guid) =>
		Polyfill.CreateFixedString(
			Length, (IntPtr)(&guid), static (span, guidP) => {
				Codec.Encode(new ReadOnlySpan<byte>((byte*)guidP, sizeof(Guid)), span);
			});

	/// <summary>Decodes given base64 string to Guid.</summary>
	/// <param name="value">The base64 encoded string of a Guid</param>
	/// <returns>A new Guid.</returns>
	private static unsafe Guid Decode(string? value)
	{
		if (string.IsNullOrEmpty(value))
			return Guid.Empty;

		Guid result;
		Codec.Decode(value.AsSpan(), new Span<byte>(&result, sizeof(Guid)));
		return result;
	}

	/// <summary>Determines if both ShortGuids have the same underlying Guid value.</summary>
	/// <param name="x">Guid X</param>
	/// <param name="y">Guid Y</param>
	/// <returns><c>true</c> if Guids are equal, <c>false</c> otherwise.</returns>
	public static bool operator ==(ShortGuid x, ShortGuid y) => x._guid == y._guid;

	/// <summary>Determines if both ShortGuids do not have the same underlying Guid value.</summary>
	/// <param name="x">Guid X</param>
	/// <param name="y">Guid Y</param>
	/// <returns><c>true</c> if Guids are not equal, <c>false</c> otherwise.</returns>
	public static bool operator !=(ShortGuid x, ShortGuid y) => !(x == y);

	/// <summary>Implicitly converts the ShortGuid to it's string equivalent</summary>
	/// <param name="guid">ShortGuid</param>
	/// <returns>String.</returns>
	public static implicit operator string(ShortGuid guid) => guid.Text;

	/// <summary>Implicitly converts the ShortGuid to it's Guid equivalent</summary>
	/// <param name="guid">ShortGuid</param>
	/// <returns>Guid.</returns>
	public static implicit operator Guid(ShortGuid guid) => guid._guid;

	/// <summary>Implicitly converts the string to a ShortGuid</summary>
	/// <param name="text">String representation of Guid.</param>
	/// <returns>ShortGuid.</returns>
	public static implicit operator ShortGuid(string text) => new(text);

	/// <summary>Implicitly converts the Guid to a ShortGuid</summary>
	/// <param name="guid">Guid.</param>
	/// <returns>ShortGuid</returns>
	public static implicit operator ShortGuid(Guid guid) => new(guid);
	
	private static void ParseShortGuid(
		string input, out Guid guid, out string text) =>
		TryParseShortGuid(input, out guid, out text!, true);

	private static bool TryParseShortGuid(
		string? input,
		out Guid guid, out string? text,
		bool failOrError)
	{
		// if text is empty assume Guid.Empty (well, debatable)
		// is text looks like short guid assume short guid
		// if not, try parse as normal guid 
		// if still nothing throw exception
		if (string.IsNullOrWhiteSpace(input))
		{
			guid = Guid.Empty;
			text = EmptyText;
			return true;
		}

		var format = Validate(input);

		if (format == ShortGuidFormat.Strict)
		{
			guid = Decode(input);
			text = input;
			return true;
		}

		if (format == ShortGuidFormat.Valid)
		{
			guid = Decode(input);
		}
		else if (!Guid.TryParse(input, out guid))
		{
			if (failOrError) ThrowCannotParseGuid();
			text = null;
			return false;
		}

		// this is slowing it down, it would be much better to pass ShortGuids as Strict
		text = Encode(guid);
		return true;
	}

	private static void ThrowCannotParseGuid() =>
		throw new ArgumentException("Provided value is neither Guid nor ShortGuid");

	private enum ShortGuidFormat { Invalid, Valid, Strict }

	private static ShortGuidFormat Validate(string? text)
	{
		if (text is null) return ShortGuidFormat.Invalid;

		var span = Codec.StripPadding(text.AsSpan());
		return
			span.Length != Length || Codec.ErrorIndex(span) >= 0 ? ShortGuidFormat.Invalid :
			text.Length == Length ? ShortGuidFormat.Strict :
			ShortGuidFormat.Valid;
	}
}
