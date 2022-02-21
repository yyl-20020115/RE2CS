/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/parse.go
namespace RE2CS;

public class CharGroup
{

    public readonly int Sign;
    public readonly int[] Cls;

    private CharGroup(int sign, int[] cls)
    {
        this.Sign = sign;
        this.Cls = cls;
    }

    private static readonly int[] Code1 = {
        /* \d */
        0x30, 0x39,
    };

    private static readonly int[] Code2 = {
        /* \s */
        0x9, 0xa, 0xc, 0xd, 0x20, 0x20,
    };

    private static readonly int[] Code3 = {
        /* \w */
        0x30, 0x39, 0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a,
    };

    private static readonly int[] Code4 = {
        /* [:alnum:] */
        0x30, 0x39, 0x41, 0x5a, 0x61, 0x7a,
    };

    private static readonly int[] Code5 = {
        /* [:alpha:] */
        0x41, 0x5a, 0x61, 0x7a,
    };

    private static readonly int[] Code6 = {
        /* [:ascii:] */
        0x0, 0x7f,
    };

    private static readonly int[] Code7 = {
        /* [:blank:] */
        0x9, 0x9, 0x20, 0x20,
    };

    private static readonly int[] Code8 = {
        /* [:cntrl:] */
        0x0, 0x1f, 0x7f, 0x7f,
    };

    private static readonly int[] Code9 = {
        /* [:digit:] */
        0x30, 0x39,
    };

    private static readonly int[] Code10 = {
        /* [:graph:] */
        0x21, 0x7e,
    };

    private static readonly int[] Code11 = {
        /* [:lower:] */
        0x61, 0x7a,
    };

    private static readonly int[] Code12 = {
        /* [:print:] */
        0x20, 0x7e,
    };

    private static readonly int[] Code13 = {
        /* [:punct:] */
        0x21, 0x2f, 0x3a, 0x40, 0x5b, 0x60, 0x7b, 0x7e,
    };

    private static readonly int[] Code14 = {
        /* [:space:] */
        0x9, 0xd, 0x20, 0x20,
    };

    private static readonly int[] Code15 = {
        /* [:upper:] */
        0x41, 0x5a,
    };

    private static readonly int[] Code16 = {
        /* [:word:] */
        0x30, 0x39, 0x41, 0x5a, 0x5f, 0x5f, 0x61, 0x7a,
    };

    private static readonly int[] Code17 = {
        /* [:xdigit:] */
        0x30, 0x39, 0x41, 0x46, 0x61, 0x66,
    };

    public static readonly Dictionary<string, CharGroup> PERL_GROUPS = new();
    public static readonly Dictionary<string, CharGroup> POSIX_GROUPS = new();

    static CharGroup()
    {
        PERL_GROUPS.Add("\\d", new (+1, Code1));
        PERL_GROUPS.Add("\\D", new (-1, Code1));
        PERL_GROUPS.Add("\\s", new (+1, Code2));
        PERL_GROUPS.Add("\\S", new (-1, Code2));
        PERL_GROUPS.Add("\\w", new (+1, Code3));
        PERL_GROUPS.Add("\\W", new (-1, Code3));
        POSIX_GROUPS.Add("[:alnum:]", new (+1, Code4));
        POSIX_GROUPS.Add("[:^alnum:]", new (-1, Code4));
        POSIX_GROUPS.Add("[:alpha:]", new (+1, Code5));
        POSIX_GROUPS.Add("[:^alpha:]", new (-1, Code5));
        POSIX_GROUPS.Add("[:ascii:]", new (+1, Code6));
        POSIX_GROUPS.Add("[:^ascii:]", new (-1, Code6));
        POSIX_GROUPS.Add("[:blank:]", new (+1, Code7));
        POSIX_GROUPS.Add("[:^blank:]", new (-1, Code7));
        POSIX_GROUPS.Add("[:cntrl:]", new (+1, Code8));
        POSIX_GROUPS.Add("[:^cntrl:]", new (-1, Code8));
        POSIX_GROUPS.Add("[:digit:]", new (+1, Code9));
        POSIX_GROUPS.Add("[:^digit:]", new (-1, Code9));
        POSIX_GROUPS.Add("[:graph:]", new (+1, Code10));
        POSIX_GROUPS.Add("[:^graph:]", new (-1, Code10));
        POSIX_GROUPS.Add("[:lower:]", new (+1, Code11));
        POSIX_GROUPS.Add("[:^lower:]", new (-1, Code11));
        POSIX_GROUPS.Add("[:print:]", new (+1, Code12));
        POSIX_GROUPS.Add("[:^print:]", new (-1, Code12));
        POSIX_GROUPS.Add("[:punct:]", new (+1, Code13));
        POSIX_GROUPS.Add("[:^punct:]", new (-1, Code13));
        POSIX_GROUPS.Add("[:space:]", new (+1, Code14));
        POSIX_GROUPS.Add("[:^space:]", new (-1, Code14));
        POSIX_GROUPS.Add("[:upper:]", new (+1, Code15));
        POSIX_GROUPS.Add("[:^upper:]", new (-1, Code15));
        POSIX_GROUPS.Add("[:word:]", new (+1, Code16));
        POSIX_GROUPS.Add("[:^word:]", new (-1, Code16));
        POSIX_GROUPS.Add("[:xdigit:]", new (+1, Code17));
        POSIX_GROUPS.Add("[:^xdigit:]", new (-1, Code17));
    }
}
