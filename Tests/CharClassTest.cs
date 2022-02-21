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

    private static CharClass CreateCharClass(params int[] x) => new CharClass(x);

    private static int[] Ints(params int[] x) => x;

    private static int[] StringToRunes(string s) => s.EnumerateRunes().Select(r => r.Value).ToArray();// stringToRunes(s);

    private static void AssertClass(CharClass cc, params int[] expected)
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
    public void TestCleanClass()
    {
        AssertClass(CreateCharClass().CleanClass());

        AssertClass(CreateCharClass(10, 20, 10, 20, 10, 20).CleanClass(), 10, 20);

        AssertClass(CreateCharClass(10, 20).CleanClass(), 10, 20);

        AssertClass(CreateCharClass(10, 20, 20, 30).CleanClass(), 10, 30);

        AssertClass(CreateCharClass(10, 20, 30, 40, 20, 30).CleanClass(), 10, 40);

        AssertClass(CreateCharClass(0, 50, 20, 30).CleanClass(), 0, 50);

        AssertClass(
            CreateCharClass(10, 11, 13, 14, 16, 17, 19, 20, 22, 23).CleanClass(),
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

        AssertClass(
            CreateCharClass(13, 14, 10, 11, 22, 23, 19, 20, 16, 17).CleanClass(),
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

        AssertClass(
            CreateCharClass(13, 14, 10, 11, 22, 23, 19, 20, 16, 17).CleanClass(),
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

        AssertClass(CreateCharClass(13, 14, 10, 11, 22, 23, 19, 20, 16, 17, 5, 25).CleanClass(), 5, 25);

        AssertClass(CreateCharClass(13, 14, 10, 11, 22, 23, 19, 20, 16, 17, 12, 21).CleanClass(), 10, 23);

        AssertClass(CreateCharClass(0, Unicode.MAX_RUNE).CleanClass(), 0, Unicode.MAX_RUNE);

        AssertClass(CreateCharClass(0, 50).CleanClass(), 0, 50);

        AssertClass(CreateCharClass(50, Unicode.MAX_RUNE).CleanClass(), 50, Unicode.MAX_RUNE);
    }

    [Test]
    public void TestAppendLiteral()
    {
        AssertClass(CreateCharClass().AppendLiteral('a', 0), 'a', 'a');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral('a', 0), 'a', 'f');
        AssertClass(CreateCharClass('b', 'f').AppendLiteral('a', 0), 'a', 'f');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral('g', 0), 'a', 'g');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral('A', 0), 'a', 'f', 'A', 'A');

        AssertClass(CreateCharClass().AppendLiteral('A', RE2.FOLD_CASE), 'A', 'A', 'a', 'a');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral('a', RE2.FOLD_CASE), 'a', 'f', 'A', 'A');
        AssertClass(CreateCharClass('b', 'f').AppendLiteral('a', RE2.FOLD_CASE), 'a', 'f', 'A', 'A');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral('g', RE2.FOLD_CASE), 'a', 'g', 'G', 'G');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral('A', RE2.FOLD_CASE), 'a', 'f', 'A', 'A');

        // ' ' is beneath the MIN-MAX_FOLD range.
        AssertClass(CreateCharClass('a', 'f').AppendLiteral(' ', 0), 'a', 'f', ' ', ' ');
        AssertClass(CreateCharClass('a', 'f').AppendLiteral(' ', RE2.FOLD_CASE), 'a', 'f', ' ', ' ');
    }

    [Test]
    public void TestAppendFoldedRange()
    {
        // These cases are derived directly from the program logic:

        // Range is full: folding can't Add more.
        AssertClass(CreateCharClass().AppendFoldedRange(10, 0x10ff0), 10, 0x10ff0);

        // Range is outside folding possibilities.
        AssertClass(CreateCharClass().AppendFoldedRange(' ', '&'), ' ', '&');

        // [lo, MIN_FOLD - 1] needs no folding.  Only [...abc] suffix is folded.
        AssertClass(CreateCharClass().AppendFoldedRange(' ', 'C'), ' ', 'C', 'a', 'c');

        // [MAX_FOLD...] needs no folding
        AssertClass(
            CreateCharClass().AppendFoldedRange(0x10400, 0x104f0),
            0x10450,
            0x104f0,
            0x10400,
            0x10426, // lowercase Deseret
            0x10426,
            0x1044f); // uppercase Deseret, abutting.
    }

    [Test]
    public void TestAppendClass()
    {
        AssertClass(CreateCharClass().AppendClass(Ints('a', 'z')), 'a', 'z');
        AssertClass(CreateCharClass('a', 'f').AppendClass(Ints('c', 't')), 'a', 't');
        AssertClass(CreateCharClass('c', 't').AppendClass(Ints('a', 'f')), 'a', 't');

        AssertClass(
            CreateCharClass('d', 'e').AppendNegatedClass(Ints('b', 'f')), 'd', 'e', 0, 'a', 'g', Unicode.MAX_RUNE);
    }

    [Test]
    public void TestAppendFoldedClass()
    {
        // NB, local variable names use Unicode.
        // 0x17F is an old English long s (looks like an f) and folds to s.
        // 0x212A is the Kelvin symbol and folds to k.
        char ſ = (char)0x17F, K = (char)0x212A;

        AssertClass(CreateCharClass().AppendFoldedClass(Ints('a', 'z')), StringToRunes("akAK" + K + K + "lsLS" + ſ + ſ + "tzTZ"));

        AssertClass(
            CreateCharClass('a', 'f').AppendFoldedClass(Ints('c', 't')), StringToRunes("akCK" + K + K + "lsLS" + ſ + ſ + "ttTT"));

        AssertClass(CreateCharClass('c', 't').AppendFoldedClass(Ints('a', 'f')), 'c', 't', 'a', 'f', 'A', 'F');
    }

    [Test]
    public void TestNegateClass()
    {
        AssertClass(CreateCharClass().NegateClass(), '\0', Unicode.MAX_RUNE);
        AssertClass(CreateCharClass('A', 'Z').NegateClass(), '\0', '@', '[', Unicode.MAX_RUNE);
        AssertClass(CreateCharClass('A', 'Z', 'a', 'z').NegateClass(), '\0', '@', '[', '`', '{', Unicode.MAX_RUNE);
    }

    [Test]
    public void TestAppendTable()
    {
        AssertClass(
            CreateCharClass().AppendTable(new int[][] { Ints('a', 'z', 1), Ints('A', 'M', 4) }),
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
        AssertClass(
            CreateCharClass().AppendTable(new int[][] { Ints('Ā', 'Į', 2) }),
            StringToRunes("ĀĀĂĂĄĄĆĆĈĈĊĊČČĎĎĐĐĒĒĔĔĖĖĘĘĚĚĜĜĞĞĠĠĢĢĤĤĦĦĨĨĪĪĬĬĮĮ"));
        AssertClass(
            CreateCharClass().AppendTable(new int[][] { Ints('Ā' + 1, 'Į' + 1, 2) }),
            StringToRunes("āāăăąąććĉĉċċččďďđđēēĕĕėėęęěěĝĝğğġġģģĥĥħħĩĩīīĭĭįį"));

        AssertClass(
            CreateCharClass().AppendNegatedTable(new int[][] { Ints('b', 'f', 1) }), 0, 'a', 'g', Unicode.MAX_RUNE);
    }

    [Test]
    public void TestAppendGroup()
    {
        AssertClass(CreateCharClass().AppendGroup(CharGroup.PERL_GROUPS[("\\d")], false), '0', '9');
        AssertClass(
            CreateCharClass().AppendGroup(CharGroup.PERL_GROUPS[("\\D")], false), 0, '/', ':', Unicode.MAX_RUNE);
    }

    [Test]
    public void TestToString()
    {
        AssertEquals("[0xa 0xc-0x14]", CreateCharClass(10, 10, 12, 20).ToString());
    }
    public static void AssertEquals(string v1, string v2)
    {
        Assert.AreEqual(v1, v2);
    }

}
