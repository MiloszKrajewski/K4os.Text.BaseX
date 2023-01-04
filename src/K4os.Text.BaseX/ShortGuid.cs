using System;

namespace K4os.Text.BaseX;

/// <summary>
/// Represents a globally unique identifier (GUID) with a shorter string value.
/// </summary>
public readonly struct ShortGuid: IComparable, IEquatable<ShortGuid>, IComparable<ShortGuid>
{
	/// <summary>A read-only instance of the ShortGuid class whose value is guaranteed to be all zeroes.</summary>
	public static readonly ShortGuid Empty = new(Guid.Empty);

	/// <summary>Length of ShortGuid string representation (base32 without padding).</summary>
	public const int Length = 22;

	private static readonly string EmptyText = Empty.Text;

	private readonly Guid _guid;
	private readonly string _text;

	/// <summary>Creates a ShortGuid from a string. It accepts both "normal" guid and
	/// base64 short guid.</summary>
	/// <param name="text">The encoded guid (as "normal" guid or base64 encoded one)</param>
	public ShortGuid(string text)
	{
		// if text is empty assume Guid.Empty (well, debatable)
		// if length is less than 32 assume short guid (no buts)
		// if not, try parse as normal guid and then, if it failed, try short guid
		if (string.IsNullOrEmpty(text))
			_guid = Guid.Empty;
		else if (text.Length < 32 || !Guid.TryParse(text, out _guid))
			_guid = Decode(text);

		// note, guid is always re-encoded even if it was provided as short guid
		// it allows to keep representation consistent
		_text = Encode(_guid);
	}

	/// <summary>Creates a ShortGuid from a Guid</summary>
	/// <param name="guid">The Guid to encode</param>
	public ShortGuid(Guid guid) => _text = Encode(_guid = guid);

	/// <summary>Gets the underlying Guid.</summary>
	public Guid Guid => _guid;

	/// <summary>Gets text representation of Guid.</summary>
	public string Text => _text ?? EmptyText;

	/// <summary>Returns the base64 encoded guid as a string</summary>
	/// <returns>Text representation of short guid (in ShortGuid form)</returns>
	public override string ToString() => Text;

	/// <summary>
	/// Returns a value indicating whether this instance and a 
	/// specified Object represent the same type and value.
	/// </summary>
	/// <param name="obj">The object to compare.</param>
	/// <returns><c>true</c> if objects are representing same Guid.</returns>
	public override bool Equals(object obj) =>
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
	public int CompareTo(object obj) =>
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
	private static unsafe string Encode(Guid guid)
	{
		#if NETSTANDARD2_1_OR_GREATER || NET5_0_OR_GREATER

		return string.Create(
			Length, (IntPtr)(&guid), (span, guidP) => {
				Base64.Url.Encode(new ReadOnlySpan<byte>((byte*)guidP, sizeof(Guid)), span);
			});

		#else
		
		return Base64.Url.Encode(new ReadOnlySpan<byte>(&guid, sizeof(Guid)));

		#endif
	}

	/// <summary>Decodes given base64 string to Guid.</summary>
	/// <param name="value">The base64 encoded string of a Guid</param>
	/// <returns>A new Guid.</returns>
	private static unsafe Guid Decode(string value)
	{
		if (string.IsNullOrEmpty(value))
			return Guid.Empty;

		Guid result;
		Base64.Url.Decode(value.AsSpan(), new Span<byte>(&result, sizeof(Guid)));
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
}
