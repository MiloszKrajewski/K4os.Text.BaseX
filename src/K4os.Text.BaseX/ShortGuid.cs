using System;

namespace K4os.Text.BaseX
{
	/// <summary>
	/// Represents a globally unique identifier (GUID) with a shorter string value.
	/// </summary>
	public readonly struct ShortGuid: IComparable, IEquatable<ShortGuid>, IComparable<ShortGuid>
	{
		/// <summary>A read-only instance of the ShortGuid class whose value is guaranteed to be all zeroes.</summary>
		public static readonly ShortGuid Empty = new ShortGuid(Guid.Empty);

		private readonly Guid _guid;
		private readonly string _text;

		/// <summary>Creates a ShortGuid from a base64 encoded string</summary>
		/// <param name="text">The encoded guid as a base64 string</param>
		public ShortGuid(string text) => _guid = Decode(_text = text);

		/// <summary>Creates a ShortGuid from a Guid</summary>
		/// <param name="guid">The Guid to encode</param>
		public ShortGuid(Guid guid) => _text = Encode(_guid = guid);

		/// <summary>Gets the underlying Guid.</summary>
		public Guid Guid => _guid;

		/// <summary>Gets text representation of Guid.</summary>
		public string Text => _text;

		/// <summary>Returns the base64 encoded guid as a string</summary>
		/// <returns>Text representation of short guid (in ShortGuid form)</returns>
		public override string ToString() => _text;

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
				string s => _text.Equals(s),
				_ => false
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
				_ => 0
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
		/// <returns></returns>
		public static ShortGuid NewGuid() => new ShortGuid(Guid.NewGuid());

		/// <summary>Encodes the given Guid as a base64 string that is 22 characters long.
		/// </summary>
		/// <param name="guid">The Guid to encode</param>
		/// <returns></returns>
		private static string Encode(Guid guid) =>
			Base64.Url.Encode(guid.ToByteArray());

		/// <summary>Decodes given base64 string to Guid.</summary>
		/// <param name="value">The base64 encoded string of a Guid</param>
		/// <returns>A new Guid</returns>
		private static Guid Decode(string value) =>
			string.IsNullOrEmpty(value) ? Guid.Empty : new Guid(Base64.Url.Decode(value));

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
		public static implicit operator string(ShortGuid guid) => guid._text;

		/// <summary>Implicitly converts the ShortGuid to it's Guid equivalent</summary>
		/// <param name="guid">ShortGuid</param>
		/// <returns>Guid.</returns>
		public static implicit operator Guid(ShortGuid guid) => guid._guid;

		/// <summary>Implicitly converts the string to a ShortGuid</summary>
		/// <param name="text">String representation of Guid.</param>
		/// <returns>ShortGuid.</returns>
		public static implicit operator ShortGuid(string text) => new ShortGuid(text);

		/// <summary>Implicitly converts the Guid to a ShortGuid</summary>
		/// <param name="guid">Guid.</param>
		/// <returns>ShortGuid</returns>
		public static implicit operator ShortGuid(Guid guid) => new ShortGuid(guid);
	}
}
