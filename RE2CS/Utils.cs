/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Collections;
using System.Text;

namespace RE2CS;

/**
 * Various constants and helper utilities.
 */
public static class Utils
{

    public static readonly int[] EMPTY_INTS = Array.Empty<int>();

    // Returns true iff |c| is an ASCII letter or decimal digit.
    public static bool isalnum(int c) 
        => ('0' <= c && c <= '9') || ('A' <= c && c <= 'Z') || ('a' <= c && c <= 'z');

    // If |c| is an ASCII hex digit, returns its value, otherwise -1.
    public static int unhex(int c)
    {
        if ('0' <= c && c <= '9')
        {
            return c - '0';
        }
        if ('a' <= c && c <= 'f')
        {
            return c - 'a' + 10;
        }
        if ('A' <= c && c <= 'F')
        {
            return c - 'A' + 10;
        }
        return -1;
    }

    private const string METACHARACTERS = "\\.+*?()|[]{}^$";

    // Appends a RE2 literal to |out| for rune |rune|,
    // with regexp metacharacters escaped.
    public static void EscapeRune(StringBuilder result, int rune)
    {
        if (Unicode.isPrint(rune))
        {
            var r = new Rune(rune);
            var m = METACHARACTERS.EnumerateRunes().ToList();
            if (m.IndexOf(r) >= 0)
            {
                result.Append('\\');
            }
            result.Append(r.ToString());
            return;
        }

        switch (rune)
        {
            case '"':
                result.Append("\\\"");
                break;
            case '\\':
                result.Append("\\\\");
                break;
            case '\t':
                result.Append("\\t");
                break;
            case '\n':
                result.Append("\\n");
                break;
            case '\r':
                result.Append("\\r");
                break;
            case '\b':
                result.Append("\\b");
                break;
            case '\f':
                result.Append("\\f");
                break;
            default:
                {
                    var s = string.Format("{0:x}",rune);
                    if (rune < 0x100)
                    {
                        result.Append("\\x");
                        if (s.Length == 1)
                        {
                            result.Append('0');
                        }
                        result.Append(s);
                    }
                    else
                    {
                        result.Append("\\x{").Append(s).Append('}');
                    }
                    break;
                }
        }
    }

    // Returns the array of runes in the specified Java UTF-16 string.
    public static int[] stringToRunes(string str) => str.EnumerateRunes().Select(r => r.Value).ToArray();

    // Returns the Java UTF-16 string containing the single rune |r|.
    public static string runeToString(int r) => new Rune(r).ToString();

    // Returns a new copy of the specified subarray.
    public static int[] subarray(int[] array, int start, int end)
    {
        int[] r = new int[end - start];
        for (int i = start; i < end; ++i)
        {
            r[i - start] = array[i];
        }
        return r;
    }

    // Returns a new copy of the specified subarray.
    public static byte[] subarray(byte[] array, int start, int end)
    {
        byte[] r = new byte[end - start];
        for (int i = start; i < end; ++i)
        {
            r[i - start] = array[i];
        }
        return r;
    }

    // Returns the index of the first occurrence of array |target| within
    // array |source| after |fromIndex|, or -1 if not found.
    public static int indexOf(byte[] source, byte[] target, int fromIndex)
    {
        if (fromIndex >= source.Length)
        {
            return target.Length == 0 ? source.Length : -1;
        }
        if (fromIndex < 0)
        {
            fromIndex = 0;
        }
        if (target.Length == 0)
        {
            return fromIndex;
        }

        byte first = target[0];
        for (int i = fromIndex, max = source.Length - target.Length; i <= max; i++)
        {
            // Look for first byte.
            if (source[i] != first)
            {
                while (++i <= max && source[i] != first) { }
            }

            // Found first byte, now look at the rest of v2.
            if (i <= max)
            {
                int j = i + 1;
                int end = j + target.Length - 1;
                for (int k = 1; j < end && source[j] == target[k]; j++, k++) { }

                if (j == end)
                {
                    return i; // found whole array
                }
            }
        }
        return -1;
    }

    // isWordRune reports whether r is consider a ``word character''
    // during the evaluation of the \b and \B zero-width assertions.
    // These assertions are ASCII-only: the word characters are [A-Za-z0-9_].
    public static bool isWordRune(int r)
    {
        return (('A' <= r && r <= 'Z') || ('a' <= r && r <= 'z') || ('0' <= r && r <= '9') || r == '_');
    }

    //// EMPTY_* flags

    public const int EMPTY_BEGIN_LINE = 0x01;
    public const int EMPTY_END_LINE = 0x02;
    public const int EMPTY_BEGIN_TEXT = 0x04;
    public const int EMPTY_END_TEXT = 0x08;
    public const int EMPTY_WORD_BOUNDARY = 0x10;
    public const int EMPTY_NO_WORD_BOUNDARY = 0x20;
    public const int EMPTY_ALL = -1; // (impossible)

    // emptyOpContext returns the zero-width assertions satisfied at the position
    // between the runes r1 and r2, a bitmask of EMPTY_* flags.
    // Passing r1 == -1 indicates that the position is at the beginning of the
    // text.
    // Passing r2 == -1 indicates that the position is at the end of the text.
    // TODO(adonovan): move to Machine.
    public static int emptyOpContext(int r1, int r2)
    {
        int op = 0;
        if (r1 < 0)
        {
            op |= EMPTY_BEGIN_TEXT | EMPTY_BEGIN_LINE;
        }
        if (r1 == '\n')
        {
            op |= EMPTY_BEGIN_LINE;
        }
        if (r2 < 0)
        {
            op |= EMPTY_END_TEXT | EMPTY_END_LINE;
        }
        if (r2 == '\n')
        {
            op |= EMPTY_END_LINE;
        }
        if (isWordRune(r1) != isWordRune(r2))
        {
            op |= EMPTY_WORD_BOUNDARY;
        }
        else
        {
            op |= EMPTY_NO_WORD_BOUNDARY;
        }
        return op;
    }

    public static int GetHashCode(IStructuralEquatable s)
    {
        return s.GetHashCode(EqualityComparer<int>.Default);
    }

    public static int CodePointBefore(this string s, int i)
    {
        if (i > 0)
        {
            char c = s[--i];
            if (char.IsLowSurrogate(c))
            {
                char d = s[--i];
                if (char.IsHighSurrogate(d))
                {
                    return char.ConvertToUtf32(d, c);
                }
            }
            return c;
        }
        return -1;
    }
}
