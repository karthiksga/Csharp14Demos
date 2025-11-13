using System;
using System.Linq;

namespace FunctionalExtensions;

public static partial class SequenceExtensions
{
    // Instance-style helpers for string manipulation.
    extension(string value)
    {
        /// <summary>
        /// Removes all whitespace characters from the string.
        /// </summary>
        public string WithoutWhitespace => new string(value.Where(static c => !char.IsWhiteSpace(c)).ToArray());

        /// <summary>
        /// Splits the string on whitespace boundaries into words.
        /// </summary>
        public string[] Words => value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    // Static-style string operators for terse transformations.
    extension(string)
    {
        /// <summary>
        /// Indicates whether the string is null or whitespace.
        /// </summary>
        public static bool operator !(string value)
            => string.IsNullOrWhiteSpace(value);

        /// <summary>
        /// Trims leading and trailing whitespace from the string.
        /// </summary>
        public static string operator ~(string value)
            => value.Trim();

        /// <summary>
        /// Repeats the string <paramref name="count"/> times.
        /// </summary>
        public static string operator *(string value, int count)
        {
            if (count <= 0 || string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return string.Concat(Enumerable.Repeat(value, count));
        }

        /// <summary>
        /// Repeats the string <paramref name="count"/> times (commutative overload).
        /// </summary>
        public static string operator *(int count, string value)
            => value * count;

        /// <summary>
        /// Splits the string using the supplied character separator.
        /// </summary>
        public static string[] operator /(string value, char separator)
            => value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Splits the string using the supplied string separator.
        /// </summary>
        public static string[] operator /(string value, string separator)
            => value.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        /// <summary>
        /// Replaces occurrences of <c>replacement.Old</c> with <c>replacement.New</c>.
        /// </summary>
        public static string operator /(string value, (string Old, string New) replacement)
            => value.Replace(replacement.Old, replacement.New);

        /// <summary>
        /// Removes all occurrences of <paramref name="fragment"/>.
        /// </summary>
        public static string operator -(string value, string fragment)
            => value.Replace(fragment, string.Empty);

        /// <summary>
        /// Returns the trailing <paramref name="length"/> characters.
        /// </summary>
        public static string operator %(string value, int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            return length >= value.Length ? value : value[^length..];
        }

        /// <summary>
        /// Returns the leading <paramref name="length"/> characters.
        /// </summary>
        public static string operator <<(string value, int length)
        {
            if (length <= 0)
            {
                return string.Empty;
            }

            return length >= value.Length ? value : value[..length];
        }

        /// <summary>
        /// Skips the first <paramref name="length"/> characters.
        /// </summary>
        public static string operator >>(string value, int length)
        {
            if (length <= 0)
            {
                return value;
            }

            return length >= value.Length ? string.Empty : value[length..];
        }

        /// <summary>
        /// Returns the distinct characters common to both strings.
        /// </summary>
        public static string operator &(string left, string right)
            => new string(left.Where(right.Contains).Distinct().ToArray());

        /// <summary>
        /// Returns the characters unique to either string (symmetric difference).
        /// </summary>
        public static string operator ^(string left, string right)
            => new string(
                left.Where(c => !right.Contains(c))
                    .Concat(right.Where(c => !left.Contains(c)))
                    .ToArray());

        /// <summary>
        /// Applies a projection to the string using pipeline syntax.
        /// </summary>
        public static string operator |(string value, Func<string, string> projector)
            => projector(value);
    }
}
