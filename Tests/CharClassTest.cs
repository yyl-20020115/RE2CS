/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;

[TestFixture]
public class CharClassTest
{

    private static CharClass cc(params int[] x)
    {
        return new CharClass(x);
    }

    private static int[] i(params int[] x)
    {
        return x;
    }

    private static int[] s(string s)
    {
        return s.EnumerateRunes().Select(r => r.Value).ToArray();// stringToRunes(s);
    }

    private static void assertClass(CharClass cc, params int[] expected)
    {
        var actual = cc.ToArray();
        if (!Enumerable.SequenceEqual(actual, expected))
        {
            throw new Exception(
                "Incorrect CharClass value:\n"
                    + "Expected: "
                    + expected
                    + "\n"
                    + "Actual:   "
                    + actual);
        }
    }
    [Test]
    public void testCleanClass()
    {
        assertClass(cc().CleanClass());

        assertClass(cc(10, 20, 10, 20, 10, 20).CleanClass(), 10, 20);

        assertClass(cc(10, 20).CleanClass(), 10, 20);

        assertClass(cc(10, 20, 20, 30).CleanClass(), 10, 30);

        assertClass(cc(10, 20, 30, 40, 20, 30).CleanClass(), 10, 40);

        assertClass(cc(0, 50, 20, 30).CleanClass(), 0, 50);

        assertClass(
            cc(10, 11, 13, 14, 16, 17, 19, 20, 22, 23).CleanClass(),
            10,
            11,
            13,
            14,
            16,
            17,
            19,
            20,
            22,
            23);

        assertClass(
            cc(13, 14, 10, 11, 22, 23, 19, 20, 16, 17).CleanClass(),
            10,
            11,
            13,
            14,
            16,
            17,
            19,
            20,
            22,
            23);

        assertClass(
            cc(13, 14, 10, 11, 22, 23, 19, 20, 16, 17).CleanClass(),
            10,
            11,
            13,
            14,
            16,
            17,
            19,
            20,
            22,
            23);

        assertClass(cc(13, 14, 10, 11, 22, 23, 19, 20, 16, 17, 5, 25).CleanClass(), 5, 25);

        assertClass(cc(13, 14, 10, 11, 22, 23, 19, 20, 16, 17, 12, 21).CleanClass(), 10, 23);

        assertClass(cc(0, Unicode.MAX_RUNE).CleanClass(), 0, Unicode.MAX_RUNE);

        assertClass(cc(0, 50).CleanClass(), 0, 50);

        assertClass(cc(50, Unicode.MAX_RUNE).CleanClass(), 50, Unicode.MAX_RUNE);
    }

    [Test]
    public void testAppendLiteral()
    {
        assertClass(cc().AppendLiteral('a', 0), 'a', 'a');
        assertClass(cc('a', 'f').AppendLiteral('a', 0), 'a', 'f');
        assertClass(cc('b', 'f').AppendLiteral('a', 0), 'a', 'f');
        assertClass(cc('a', 'f').AppendLiteral('g', 0), 'a', 'g');
        assertClass(cc('a', 'f').AppendLiteral('A', 0), 'a', 'f', 'A', 'A');

        assertClass(cc().AppendLiteral('A', RE2.FOLD_CASE), 'A', 'A', 'a', 'a');
        assertClass(cc('a', 'f').AppendLiteral('a', RE2.FOLD_CASE), 'a', 'f', 'A', 'A');
        assertClass(cc('b', 'f').AppendLiteral('a', RE2.FOLD_CASE), 'a', 'f', 'A', 'A');
        assertClass(cc('a', 'f').AppendLiteral('g', RE2.FOLD_CASE), 'a', 'g', 'G', 'G');
        assertClass(cc('a', 'f').AppendLiteral('A', RE2.FOLD_CASE), 'a', 'f', 'A', 'A');

        // ' ' is beneath the MIN-MAX_FOLD range.
        assertClass(cc('a', 'f').AppendLiteral(' ', 0), 'a', 'f', ' ', ' ');
        assertClass(cc('a', 'f').AppendLiteral(' ', RE2.FOLD_CASE), 'a', 'f', ' ', ' ');
    }

    [Test]
    public void testAppendFoldedRange()
    {
        // These cases are derived directly from the program logic:

        // Range is full: folding can't add more.
        assertClass(cc().AppendFoldedRange(10, 0x10ff0), 10, 0x10ff0);

        // Range is outside folding possibilities.
        assertClass(cc().AppendFoldedRange(' ', '&'), ' ', '&');

        // [lo, MIN_FOLD - 1] needs no folding.  Only [...abc] suffix is folded.
        assertClass(cc().AppendFoldedRange(' ', 'C'), ' ', 'C', 'a', 'c');

        // [MAX_FOLD...] needs no folding
        assertClass(
            cc().AppendFoldedRange(0x10400, 0x104f0),
            0x10450,
            0x104f0,
            0x10400,
            0x10426, // lowercase Deseret
            0x10426,
            0x1044f); // uppercase Deseret, abutting.
    }

    [Test]
    public void testAppendClass()
    {
        assertClass(cc().AppendClass(i('a', 'z')), 'a', 'z');
        assertClass(cc('a', 'f').AppendClass(i('c', 't')), 'a', 't');
        assertClass(cc('c', 't').AppendClass(i('a', 'f')), 'a', 't');

        assertClass(
            cc('d', 'e').AppendNegatedClass(i('b', 'f')), 'd', 'e', 0, 'a', 'g', Unicode.MAX_RUNE);
    }

    [Test]
    public void testAppendFoldedClass()
    {
        // NB, local variable names use Unicode.
        // 0x17F is an old English long s (looks like an f) and folds to s.
        // 0x212A is the Kelvin symbol and folds to k.
        char ſ = (char)0x17F, K = (char)0x212A;

        assertClass(cc().AppendFoldedClass(i('a', 'z')), s("akAK" + K + K + "lsLS" + ſ + ſ + "tzTZ"));

        assertClass(
            cc('a', 'f').AppendFoldedClass(i('c', 't')), s("akCK" + K + K + "lsLS" + ſ + ſ + "ttTT"));

        assertClass(cc('c', 't').AppendFoldedClass(i('a', 'f')), 'c', 't', 'a', 'f', 'A', 'F');
    }

    [Test]
    public void testNegateClass()
    {
        assertClass(cc().NegateClass(), '\0', Unicode.MAX_RUNE);
        assertClass(cc('A', 'Z').NegateClass(), '\0', '@', '[', Unicode.MAX_RUNE);
        assertClass(cc('A', 'Z', 'a', 'z').NegateClass(), '\0', '@', '[', '`', '{', Unicode.MAX_RUNE);
    }

    [Test]
    public void testAppendTable()
    {
        assertClass(
            cc().AppendTable(new int[][] { i('a', 'z', 1), i('A', 'M', 4) }),
            'a',
            'z',
            'A',
            'A',
            'E',
            'E',
            'I',
            'I',
            'M',
            'M');
        assertClass(
            cc().AppendTable(new int[][] { i('Ā', 'Į', 2) }),
            s("ĀĀĂĂĄĄĆĆĈĈĊĊČČĎĎĐĐĒĒĔĔĖĖĘĘĚĚĜĜĞĞĠĠĢĢĤĤĦĦĨĨĪĪĬĬĮĮ"));
        assertClass(
            cc().AppendTable(new int[][] { i('Ā' + 1, 'Į' + 1, 2) }),
            s("āāăăąąććĉĉċċččďďđđēēĕĕėėęęěěĝĝğğġġģģĥĥħħĩĩīīĭĭįį"));

        assertClass(
            cc().AppendNegatedTable(new int[][] { i('b', 'f', 1) }), 0, 'a', 'g', Unicode.MAX_RUNE);
    }

    [Test]
    public void testAppendGroup()
    {
        assertClass(cc().AppendGroup(CharGroup.PERL_GROUPS[("\\d")], false), '0', '9');
        assertClass(
            cc().AppendGroup(CharGroup.PERL_GROUPS[("\\D")], false), 0, '/', ':', Unicode.MAX_RUNE);
    }

    [Test]
    public void testToString()
    {
        assertEquals("[0xa 0xc-0x14]", cc(10, 10, 12, 20).ToString());
    }
    public static void assertEquals(string v1, string v2)
    {
        Assert.AreEqual(v1, v2);
    }

}
