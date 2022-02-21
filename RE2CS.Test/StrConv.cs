/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://golang.org/src/pkg/strconv/quote.go

// While this is a port of production Go source, it is currently
// only used by ExecTest, which is why it appears beneath javatests/.
using System;
using System.Text;
namespace RE2CS.Tests;

public static class Strconv
{

    // unquoteChar decodes the first character or byte in the escaped
    // string or character literal represented by the Go literal encoded
    // in UTF-16 in s.
    //
    // On success, it advances the UTF-16 cursor i[0] (an in/out
    // parameter) past the consumed codes and returns the decoded Unicode
    // code point or byte value.  On failure, it throws
    // IllegalArgumentException or StringIndexOutOfBoundsException
    //
    // |quote| specifies the type of literal being parsed
    // and therefore which escaped quote character is permitted.
    // If set to a single quote, it permits the sequence \' and disallows
    // unescaped '.
    // If set to a double quote, it permits \" and disallows unescaped ".
    // If set to zero, it does not permit either escape and allows both
    // quote characters to appear unescaped.
    private static int UnquoteChar(string s, int[] i, char quote)
    {
        int c = char.ConvertToUtf32(s,(i[0]));
        i[0] = s.OffsetByCodePoints(i[0], 1); // (throws if falls off end)

        // easy cases
        if (c == quote && (quote == '\'' || quote == '"'))
        {
            throw new InvalidOperationException("unescaped quotation mark in literal");
        }
        if (c != '\\')
        {
            return c;
        }

        // hard case: c is backslash
        c = char.ConvertToUtf32(s, (i[0]));
        i[0] = s.OffsetByCodePoints(i[0], 1); // (throws if falls off end)

        switch (c)
        {
            case 'a':
                return 0x07;
            case 'b':
                return '\b';
            case 'f':
                return '\f';
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case 'v':
                return 0x0B;
            case 'x':
            case 'u':
            case 'U':
                {
                    int n = 0;
                    switch (c)
                    {
                        case 'x':
                            n = 2;
                            break;
                        case 'u':
                            n = 4;
                            break;
                        case 'U':
                            n = 8;
                            break;
                    }
                    int v = 0;
                    for (int j = 0; j < n; j++)
                    {
                        int d = char.ConvertToUtf32(s, (i[0]));
                        i[0] = s.OffsetByCodePoints(i[0], 1); // (throws if falls off end)

                        int x = Utils.Unhex(d);
                        if (x == -1)
                        {
                            throw new InvalidOperationException("not a hex char: " + d);
                        }
                        v = (v << 4) | x;
                    }
                    if (c == 'x')
                    {
                        return v;
                    }
                    if (v > Unicode.MAX_RUNE)
                    {
                        throw new InvalidOperationException("Unicode code point out of range");
                    }
                    return v;
                }
            case '0':
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
                {
                    int v = c - '0';
                    for (int j = 0; j < 2; j++)
                    { // one digit already; two more
                        int d = char.ConvertToUtf32(s, (i[0]));
                        i[0] = s.OffsetByCodePoints(i[0], 1); // (throws if falls off end)

                        int x = d - '0';
                        if (x < 0 || x > 7)
                        {
                            throw new InvalidOperationException("illegal octal digit");
                        }
                        v = (v << 3) | x;
                    }
                    if (v > 255)
                    {
                        throw new InvalidOperationException("octal value out of range");
                    }
                    return v;
                }
            case '\\':
                return '\\';
            case '\'':
            case '"':
                if (c != quote)
                {
                    throw new InvalidOperationException("unnecessary backslash escape");
                }
                return c;
            default:
                throw new InvalidOperationException("unexpected character");
        }
    }

    // Unquote interprets s as a single-quoted, double-quoted,
    // or backquoted Go string literal, returning the string value
    // that s quotes.  (If s is single-quoted, it would be a Go
    // character literal; Unquote returns the corresponding
    // one-character string.)
    public static string Unquote(string s)
    {
        int n = s.Length;
        if (n < 2) {
            throw new InvalidOperationException("too short");
        }
        char quote = s[0];
        if (quote != s[n - 1]) {
            throw new InvalidOperationException("quotes don't match");
        }
        s = s.Substring(1, n - 1 - 1);
        if (quote == '`')
        {
            if (s.IndexOf('`') >= 0)
            {
                throw new InvalidOperationException("backquoted string contains '`'");
            }
            return s;
        }
        if (quote != '"' && quote != '\'')
        {
            throw new InvalidOperationException("invalid quotation mark");
        }
        if (s.IndexOf('\n') >= 0)
        {
            throw new InvalidOperationException("multiline string literal");
        }
        // Is it trivial?  Avoid allocation.
        if (s.IndexOf('\\') < 0 && s.IndexOf(quote) < 0)
        {
            if (quote == '"'
                || // "abc"
                s.CodePointCount(0, s.Length) == 1)
            { // 'a'
              // if s == "\\" then this return is wrong.
                return s;
            }
        }

        int[] i = { 0 }; // UTF-16 index, an in/out-parameter of unquoteChar.
        var buf = new StringBuilder();
        int len = s.Length;
        while (i[0] < len)
        {
            buf.Append(char.ConvertFromUtf32(UnquoteChar(s, i, quote)));
            if (quote == '\'' && i[0] != len)
            {
                throw new InvalidOperationException("single-quotation must be one char");
            }
        }

        return buf.ToString();
    }

}
