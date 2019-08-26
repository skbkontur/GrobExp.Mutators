using System;
using System.Collections.Generic;
using System.Linq;

namespace Mutators.Tests.FunctionalTests
{
    public static class ArrayStringConverter
    {
        public static string[] ToArrayString(string str, int length, int maxCount)
        {
            if (string.IsNullOrEmpty(str))
                return null;
            if (length <= 0)
                throw new ArgumentException("Length should be positive", nameof(length));
            var result = str.SplitIntoPieces(length).Take(maxCount - 1).ToList();
            var piecesLength = result.Sum(x => x.Length);
            if (piecesLength < str.Length)
                result.Add(str.Substring(piecesLength));
            return result.ToArray();
        }

        public static IEnumerable<string> SplitIntoPieces(this string source, int pieceLength)
        {
            if (pieceLength <= 0)
                throw new ArgumentException($"pieceLength should be positive integer. '{pieceLength}' is invalid value", nameof(pieceLength));
            if (string.IsNullOrEmpty(source))
                yield break;
            for (var position = 0; position < source.Length; position += pieceLength)
            {
                var currentLength = Math.Min(pieceLength, source.Length - position);
                yield return source.Substring(position, currentLength);
            }
        }

        public static string ToString(string[] array)
        {
            if (array == null || array.Length == 0)
                return null;
            return string.Join("", array);
        }

        public static string ToString(string[] array, int startIndex, int length)
        {
            if (array == null || array.Length == 0)
                return null;
            if (startIndex < 0 || startIndex > array.Length || length < 0)
                return null;

            length = Math.Min(array.Length - startIndex, length);

            if (startIndex == 0 && length == array.Length)
                return ToString(array);

            var result = new string[length];
            Array.Copy(array, startIndex, result, 0, length);
            return ToString(result);
        }
    }
}