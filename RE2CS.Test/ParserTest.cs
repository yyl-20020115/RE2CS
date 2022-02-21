/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/parse_test.go
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace RE2CS.Tests;


/**
 * @author adonovan@google.com (Alan Donovan)
 */
[TestClass]
public class ParserTest
{

    public delegate bool RunePredicate(int rune);

    private static RunePredicate IS_UPPER = (int r) => Unicode.IsUpper(r);


    private static RunePredicate IS_UPPER_FOLD = (int r) =>
    {
        if (Unicode.IsUpper(r))
        {
            return true;
        }
        for (int c = Unicode.SimpleFold(r); c != r; c = Unicode.SimpleFold(c))
        {
            if (Unicode.IsUpper(c))
            {
                return true;
            }
        }
        return false;
    };

    private static Dictionary<Regexp.Op, string> OP_NAMES = new();

    static ParserTest()
    {
        OP_NAMES.Add(Regexp.Op.NO_MATCH, "no");
        OP_NAMES.Add(Regexp.Op.EMPTY_MATCH, "emp");
        OP_NAMES.Add(Regexp.Op.LITERAL, "lit");
        OP_NAMES.Add(Regexp.Op.CHAR_CLASS, "cc");
        OP_NAMES.Add(Regexp.Op.ANY_CHAR_NOT_NL, "dnl");
        OP_NAMES.Add(Regexp.Op.ANY_CHAR, "dot");
        OP_NAMES.Add(Regexp.Op.BEGIN_LINE, "bol");
        OP_NAMES.Add(Regexp.Op.END_LINE, "eol");
        OP_NAMES.Add(Regexp.Op.BEGIN_TEXT, "bot");
        OP_NAMES.Add(Regexp.Op.END_TEXT, "eot");
        OP_NAMES.Add(Regexp.Op.WORD_BOUNDARY, "wb");
        OP_NAMES.Add(Regexp.Op.NO_WORD_BOUNDARY, "nwb");
        OP_NAMES.Add(Regexp.Op.CAPTURE, "cap");
        OP_NAMES.Add(Regexp.Op.STAR, "star");
        OP_NAMES.Add(Regexp.Op.PLUS, "plus");
        OP_NAMES.Add(Regexp.Op.QUEST, "que");
        OP_NAMES.Add(Regexp.Op.REPEAT, "rep");
        OP_NAMES.Add(Regexp.Op.CONCAT, "cat");
        OP_NAMES.Add(Regexp.Op.ALTERNATE, "alt");
    }

    private static int TEST_FLAGS = RE2.MATCH_NL | RE2.PERL_X | RE2.UNICODE_GROUPS;

    private static readonly string[][] PARSE_TESTS = {
    // Base cases
    new string[]{"a", "lit{a}"},
    new string[]{ "a.", "cat{lit{a}dot{}}"},
    new string[]{ "a.b", "cat{lit{a}dot{}lit{b}}"},
    new string[]{ "ab", "str{ab}"},
    new string[]{ "a.b.c", "cat{lit{a}dot{}lit{b}dot{}lit{c}}"},
    new string[]{ "abc", "str{abc}"},
    new string[]{ "a|^", "alt{lit{a}bol{}}"},
    new string[]{ "a|b", "cc{0x61-0x62}"},
    new string[]{ "(a)", "cap{lit{a}}"},
    new string[]{ "(a)|b", "alt{cap{lit{a}}lit{b}}"},
    new string[]{ "a*", "star{lit{a}}"},
    new string[]{ "a+", "plus{lit{a}}"},
    new string[]{ "a?", "que{lit{a}}"},
    new string[]{ "a{2}", "rep{2,2 lit{a}}"},
    new string[]{ "a{2,3}", "rep{2,3 lit{a}}"},
    new string[]{ "a{2,}", "rep{2,-1 lit{a}}"},
    new string[]{ "a*?", "nstar{lit{a}}"},
    new string[]{ "a+?", "nplus{lit{a}}"},
    new string[]{ "a??", "nque{lit{a}}"},
    new string[]{ "a{2}?", "nrep{2,2 lit{a}}"},
    new string[]{ "a{2,3}?", "nrep{2,3 lit{a}}"},
    new string[]{ "a{2,}?", "nrep{2,-1 lit{a}}"},
    // Malformed { } are treated as literals.
    new string[]{ "x{1001", "str{x{1001}"},
    new string[]{ "x{9876543210", "str{x{9876543210}"},
    new string[]{ "x{9876543210,", "str{x{9876543210,}"},
    new string[]{ "x{2,1", "str{x{2,1}"},
    new string[]{ "x{1,9876543210", "str{x{1,9876543210}"},
    new string[]{ "", "emp{}"},
    new string[]{ "|", "emp{}"}, // alt{emp{}emp{}} but got factored
    new string[]{ "|x|", "alt{emp{}lit{x}emp{}}"},
    new string[]{ ".", "dot{}"},
    new string[]{ "^", "bol{}"},
    new string[]{ "$", "eol{}"},
    new string[]{ "\\|", "lit{|}"},
    new string[]{ "\\(", "lit{(}"},
    new string[]{ "\\)", "lit{)}"},
    new string[]{ "\\*", "lit{*}"},
    new string[]{ "\\+", "lit{+}"},
    new string[]{ "\\?", "lit{?}"},
    new string[]{ "{", "lit{{}"},
    new string[]{ "}", "lit{}}"},
    new string[]{ "\\.", "lit{.}"},
    new string[]{ "\\^", "lit{^}"},
    new string[]{ "\\$", "lit{$}"},
    new string[]{ "\\\\", "lit{\\}"},
    new string[]{ "[ace]", "cc{0x61 0x63 0x65}"},
    new string[]{ "[abc]", "cc{0x61-0x63}"},
    new string[]{ "[a-z]", "cc{0x61-0x7a}"},
    new string[]{ "[a]", "lit{a}"},
    new string[]{ "\\-", "lit{-}"},
    new string[]{ "-", "lit{-}"},
    new string[]{ "\\_", "lit{_}"},
    new string[]{ "abc", "str{abc}"},
    new string[]{ "abc|def", "alt{str{abc}str{def}}"},
    new string[]{ "abc|def|ghi", "alt{str{abc}str{def}str{ghi}}"},

    // Posix and Perl extensions
    new string[]{ "[[:lower:]]", "cc{0x61-0x7a}"},
    new string[]{ "[a-z]", "cc{0x61-0x7a}"},
    new string[]{ "[^[:lower:]]", "cc{0x0-0x60 0x7b-0x10ffff}"},
    new string[]{ "[[:^lower:]]", "cc{0x0-0x60 0x7b-0x10ffff}"},
    new string[]{ "(?i)[[:lower:]]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"},
    new string[]{ "(?i)[a-z]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"},
    new string[]{ "(?i)[^[:lower:]]", "cc{0x0-0x40 0x5b-0x60 0x7b-0x17e 0x180-0x2129 0x212b-0x10ffff}"},
    new string[]{ "(?i)[[:^lower:]]", "cc{0x0-0x40 0x5b-0x60 0x7b-0x17e 0x180-0x2129 0x212b-0x10ffff}"},
    new string[]{ "\\d", "cc{0x30-0x39}"},
    new string[]{ "\\D", "cc{0x0-0x2f 0x3a-0x10ffff}"},
    new string[]{ "\\s", "cc{0x9-0xa 0xc-0xd 0x20}"},
    new string[]{ "\\S", "cc{0x0-0x8 0xb 0xe-0x1f 0x21-0x10ffff}"},
    new string[]{ "\\w", "cc{0x30-0x39 0x41-0x5a 0x5f 0x61-0x7a}"},
    new string[]{ "\\W", "cc{0x0-0x2f 0x3a-0x40 0x5b-0x5e 0x60 0x7b-0x10ffff}"},
    new string[]{ "(?i)\\w", "cc{0x30-0x39 0x41-0x5a 0x5f 0x61-0x7a 0x17f 0x212a}"},
    new string[]{ "(?i)\\W", "cc{0x0-0x2f 0x3a-0x40 0x5b-0x5e 0x60 0x7b-0x17e 0x180-0x2129 0x212b-0x10ffff}"},
    new string[]{ "[^\\\\]", "cc{0x0-0x5b 0x5d-0x10ffff}"},
    //  { "\\C", "byte{}" },  // probably never

    // Unicode, negatives, and a double negative.
    new string[]{ "\\p{Braille}", "cc{0x2800-0x28ff}"},
    new string[]{ "\\P{Braille}", "cc{0x0-0x27ff 0x2900-0x10ffff}"},
    new string[]{ "\\p{^Braille}", "cc{0x0-0x27ff 0x2900-0x10ffff}"},
    new string[]{ "\\P{^Braille}", "cc{0x2800-0x28ff}"},
    new string[]{ "\\pZ", "cc{0x20 0xa0 0x1680 0x180e 0x2000-0x200a 0x2028-0x2029 0x202f 0x205f 0x3000}"},
    new string[]{ "[\\p{Braille}]", "cc{0x2800-0x28ff}"},
    new string[]{ "[\\P{Braille}]", "cc{0x0-0x27ff 0x2900-0x10ffff}"},
    new string[]{ "[\\p{^Braille}]", "cc{0x0-0x27ff 0x2900-0x10ffff}"},
    new string[]{ "[\\P{^Braille}]", "cc{0x2800-0x28ff}"},
    new string[]{ "[\\pZ]", "cc{0x20 0xa0 0x1680 0x180e 0x2000-0x200a 0x2028-0x2029 0x202f 0x205f 0x3000}"},
    new string[]{ "\\p{Lu}", MakeCharClass(IS_UPPER)},
    new string[]{ "[\\p{Lu}]", MakeCharClass(IS_UPPER)},
    new string[]{ "(?i)[\\p{Lu}]", MakeCharClass(IS_UPPER_FOLD)},
    new string[]{ "\\p{Any}", "dot{}"},
    new string[]{ "\\p{^Any}", "cc{}"},

    // Hex, octal.
    new string[]{ "[\\012-\\234]\\141", "cat{cc{0xa-0x9c}lit{a}}"},
    new string[]{ "[\\x{41}-\\x7a]\\x61", "cat{cc{0x41-0x7a}lit{a}}"},

    // More interesting regular expressions.
    new string[]{ "a{,2}", "str{a{,2}}"},
    new string[]{ "\\.\\^\\$\\\\", "str{.^$\\}"},
    new string[]{ "[a-zABC]", "cc{0x41-0x43 0x61-0x7a}"},
    new string[]{ "[^a]", "cc{0x0-0x60 0x62-0x10ffff}"},
    new string[]{ "[α-ε☺]", "cc{0x3b1-0x3b5 0x263a}"}, // utf-8
    new string[]{ "a*{", "cat{star{lit{a}}lit{{}}"},

    // Test precedences
    new string[]{ "(?:ab)*", "star{str{ab}}"},
    new string[]{ "(ab)*", "star{cap{str{ab}}}"},
    new string[]{ "ab|cd", "alt{str{ab}str{cd}}"},
    new string[]{ "a(b|c)d", "cat{lit{a}cap{cc{0x62-0x63}}lit{d}}"},

    // Test flattening.
    new string[]{ "(?:a)", "lit{a}"},
    new string[]{ "(?:ab)(?:cd)", "str{abcd}"},
    new string[]{ "(?:a+b+)(?:c+d+)", "cat{plus{lit{a}}plus{lit{b}}plus{lit{c}}plus{lit{d}}}"},
    new string[]{ "(?:a+|b+)|(?:c+|d+)", "alt{plus{lit{a}}plus{lit{b}}plus{lit{c}}plus{lit{d}}}"},
    new string[]{ "(?:a|b)|(?:c|d)", "cc{0x61-0x64}"},
    new string[]{ "a|.", "dot{}"},
    new string[]{ ".|a", "dot{}"},
    new string[]{ "(?:[abc]|A|Z|hello|world)", "alt{cc{0x41 0x5a 0x61-0x63}str{hello}str{world}}"},
    new string[]{ "(?:[abc]|A|Z)", "cc{0x41 0x5a 0x61-0x63}"},

    // Test Perl quoted literals
    new string[]{ "\\Q+|*?{[\\E", "str{+|*?{[}"},
    new string[]{ "\\Q+\\E+", "plus{lit{+}}"},
    new string[]{ "\\Qab\\E+", "cat{lit{a}plus{lit{b}}}"},
    new string[]{ "\\Q\\\\E", "lit{\\}"},
    new string[]{ "\\Q\\\\\\E", "str{\\\\}"},

    // Test Perl \A and \z
    new string[]{ "(?m)^", "bol{}"},
    new string[]{ "(?m)$", "eol{}"},
    new string[]{ "(?-m)^", "bot{}"},
    new string[]{ "(?-m)$", "eot{}"},
    new string[]{ "(?m)\\A", "bot{}"},
    new string[]{ "(?m)\\z", "eot{\\z}"},
    new string[]{ "(?-m)\\A", "bot{}"},
    new string[]{ "(?-m)\\z", "eot{\\z}"},

    // Test named captures
    new string[]{ "(?P<name>a)", "cap{name:lit{a}}"},

    // Case-folded literals
    new string[]{ "[Aa]", "litfold{A}"},
    new string[]{ "[\\x{100}\\x{101}]", "litfold{Ā}"},
    new string[]{ "[Δδ]", "litfold{Δ}"},

    // Strings
    new string[]{ "abcde", "str{abcde}"},
    new string[]{ "[Aa][Bb]cd", "cat{strfold{AB}str{cd}}"},

    // Factoring.
    new string[]{
    "abc|abd|aef|bcx|bcy",
      "alt{cat{lit{a}alt{cat{lit{b}cc{0x63-0x64}}str{ef}}}cat{str{bc}cc{0x78-0x79}}}"
    },
    new string[]{
    "ax+y|ax+z|ay+w",
      "cat{lit{a}alt{cat{plus{lit{x}}lit{y}}cat{plus{lit{x}}lit{z}}cat{plus{lit{y}}lit{w}}}}"
    },

    // Bug fixes.

    new string[]{ "(?:.)", "dot{}"},
    new string[]{ "(?:x|(?:xa))", "cat{lit{x}alt{emp{}lit{a}}}"},
    new string[]{ "(?:.|(?:.a))", "cat{dot{}alt{emp{}lit{a}}}"},
    new string[]{ "(?:A(?:A|a))", "cat{lit{A}litfold{A}}"},
    new string[]{ "(?:A|a)", "litfold{A}"},
    new string[]{ "A|(?:A|a)", "litfold{A}"},
    new string[]{ "(?s).", "dot{}"},
    new string[]{ "(?-s).", "dnl{}"},
    new string[]{ "(?:(?:^).)", "cat{bol{}dot{}}"},
    new string[]{ "(?-s)(?:(?:^).)", "cat{bol{}dnl{}}"},
    new string[]{ "[\\x00-\\x{10FFFF}]", "dot{}"},
    new string[]{ "[^\\x00-\\x{10FFFF}]", "cc{}"},
    new string[]{ "(?:[a][a-])", "cat{lit{a}cc{0x2d 0x61}}"},

    // RE2 prefix_tests
    new string[]{ "abc|abd", "cat{str{ab}cc{0x63-0x64}}"},
    new string[]{ "a(?:b)c|abd", "cat{str{ab}cc{0x63-0x64}}"},
    new string[]{
    "abc|abd|aef|bcx|bcy",
      "alt{cat{lit{a}alt{cat{lit{b}cc{0x63-0x64}}str{ef}}}" + "cat{str{bc}cc{0x78-0x79}}}"
    },
    new string[]{ "abc|x|abd", "alt{str{abc}lit{x}str{abd}}"},
    new string[]{ "(?i)abc|ABD", "cat{strfold{AB}cc{0x43-0x44 0x63-0x64}}"},
    new string[]{ "[ab]c|[ab]d", "cat{cc{0x61-0x62}cc{0x63-0x64}}"},
    new string[]{ ".c|.d", "cat{dot{}cc{0x63-0x64}}"},
    new string[]{ "x{2}|x{2}[0-9]", "cat{rep{2,2 lit{x}}alt{emp{}cc{0x30-0x39}}}"},
    new string[]{ "x{2}y|x{2}[0-9]y", "cat{rep{2,2 lit{x}}alt{lit{y}cat{cc{0x30-0x39}lit{y}}}}"},
    new string[]{ "a.*?c|a.*?b", "cat{lit{a}alt{cat{nstar{dot{}}lit{c}}cat{nstar{dot{}}lit{b}}}}"},
  };

    // TODO(adonovan): Add some tests for:
    // - ending a regexp with "\\"
    // - Java UTF-16 things.

    [TestMethod]
    public void TestParseSimple()
    {
        TestParseDump(PARSE_TESTS, TEST_FLAGS);
    }

    private static string[][] FOLDCASE_TESTS = {
    new string[]{ "AbCdE", "strfold{ABCDE}"},
    new string[]{ "[Aa]", "litfold{A}"},
    new string[]{ "a", "litfold{A}"},

    // 0x17F is an old English long s (looks like an f) and folds to s.
    // 0x212A is the Kelvin symbol and folds to k.
    new string[]{ "A[F-g]", "cat{litfold{A}cc{0x41-0x7a 0x17f 0x212a}}"}, // [Aa][A-z...]
    new string[]{ "[[:upper:]]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"},
    new string[]{ "[[:lower:]]", "cc{0x41-0x5a 0x61-0x7a 0x17f 0x212a}"}
};

    [TestMethod]
    public void TestParseFoldCase()
    {
        TestParseDump(FOLDCASE_TESTS, RE2.FOLD_CASE);
    }

    private static string[][] LITERAL_TESTS = {
        new string[]{ "(|)^$.[*+?]{5,10},\\", "str{(|)^$.[*+?]{5,10},\\}"},
    };

    [TestMethod]
    public void TestParseLiteral()
    {
        TestParseDump(LITERAL_TESTS, RE2.LITERAL);
    }

    private static readonly string[][] MATCHNL_TESTS = {
    new string[]{ ".", "dot{}"},
    new string[]{ "\n", "lit{\n}"},
    new string[]{ "[^a]", "cc{0x0-0x60 0x62-0x10ffff}"},
    new string[]{ "[a\\n]", "cc{0xa 0x61}"},
      };

    [TestMethod]
    public void TestParseMatchNL()
    {
        TestParseDump(MATCHNL_TESTS, RE2.MATCH_NL);
    }

    private static string[][] NOMATCHNL_TESTS = {
    new string[]{ ".", "dnl{}"},
    new string[]{ "\n", "lit{\n}"},
    new string[]{ "[^a]", "cc{0x0-0x9 0xb-0x60 0x62-0x10ffff}"},
    new string[]{ "[a\\n]", "cc{0xa 0x61}"},
      };

    [TestMethod]
    public void TestParseNoMatchNL()
    {
        TestParseDump(NOMATCHNL_TESTS, 0);
    }

    // Test Parse -> Dump.
    private void TestParseDump(string[][] tests, int flags)
    {
        foreach (string[] test in tests) {
            try
            {
                Regexp re = Parser.Parse(test[0], flags);
                string d = Dump(re);
                Assert.AreEqual(d, test[1], "parse/dump of " + test[0]);
                //Truth.assertWithMessage("parse/dump of " + test[0]).that(d).isEqualTo(test[1]);
            }
            catch (PatternSyntaxException e)
            {
                throw new Exception("Parsing failed: " + test[0], e);
            }
        }
    }

    // dump prints a string representation of the regexp showing
    // the structure explicitly.
    private static string Dump(Regexp re)
    {
        var b = new StringBuilder();
        DumpRegexp(b, re);
        return b.ToString();
    }

    // dumpRegexp writes an encoding of the syntax tree for the regexp |re|
    // to |b|.  It is used during testing to distinguish between parses that
    // might print the same using re's ToString() method.
    private static void DumpRegexp(StringBuilder b, Regexp re)
    {
        string name = OP_NAMES[re.op];
        if (name == null)
        {
            b.Append("op").Append(re.op);
        }
        else
        {
            switch (re.op)
            {
                case Regexp.Op.STAR:
                case Regexp.Op.PLUS:
                case Regexp.Op.QUEST:
                case Regexp.Op.REPEAT:
                    if ((re.flags & RE2.NON_GREEDY) != 0)
                    {
                        b.Append('n');
                    }
                    b.Append(name);
                    break;
                case Regexp.Op.LITERAL:
                    if (re.runes.Length > 1)
                    {
                        b.Append("str");
                    }
                    else
                    {
                        b.Append("lit");
                    }
                    if ((re.flags & RE2.FOLD_CASE) != 0)
                    {
                        foreach (int r in re.runes) {
                            if (Unicode.SimpleFold(r) != r)
                            {
                                b.Append("fold");
                                break;
                            }
                        }
                    }
                    break;
                default:
                    b.Append(name);
                    break;
            }
        }
        b.Append('{');
        switch (re.op)
        {
            case Regexp.Op.END_TEXT:
                if ((re.flags & RE2.WAS_DOLLAR) == 0)
                {
                    b.Append("\\z");
                }
                break;
            case Regexp.Op.LITERAL:
                foreach (int r in re.runes)
                {
                    //b.appendCodePoint(r);
                    b.Append(char.ConvertFromUtf32(r));
                }
                break;
            case Regexp.Op.CONCAT:
            case Regexp.Op.ALTERNATE:
                foreach (Regexp sub in re.subs)
                {
                    DumpRegexp(b, sub);
                }
                break;
            case Regexp.Op.STAR:
            case Regexp.Op.PLUS:
            case Regexp.Op.QUEST:
                DumpRegexp(b, re.subs[0]);
                break;
            case Regexp.Op.REPEAT:
                b.Append(re.min).Append(',').Append(re.max).Append(' ');
                DumpRegexp(b, re.subs[0]);
                break;
            case Regexp.Op.CAPTURE:
                if (re.name != null && !string.IsNullOrEmpty( re.name))
                {
                    b.Append(re.name);
                    b.Append(':');
                }
                DumpRegexp(b, re.subs[0]);
                break;
            case Regexp.Op.CHAR_CLASS:
                {
                    string sep = "";
                    for (int i = 0; i < re.runes.Length; i += 2)
                    {
                        b.Append(sep);
                        sep = " ";
                        int lo = re.runes[i], hi = re.runes[i + 1];
                        if (lo == hi)
                        {
                            b.Append(string.Format("{0:X4}", lo));
                        }
                        else
                        {
                            b.Append(string.Format("{0:X4}-{1:X4}", lo, hi));
                        }
                    }
                    break;
                }
        }
        b.Append('}');
    }

    private static string MakeCharClass(RunePredicate f)
    {
        var re = new Regexp(Regexp.Op.CHAR_CLASS);
        List<int> runes = new();
        int lo = -1;
        for (int i = 0; i <= Unicode.MAX_RUNE; i++)
        {
            if (f(i))
            {
                if (lo < 0)
                {
                    lo = i;
                }
            }
            else
            {
                if (lo >= 0)
                {
                    runes.Add(lo);
                    runes.Add(i - 1);
                    lo = -1;
                }
            }
        }
        if (lo >= 0)
        {
            runes.Add(lo);
            runes.Add(Unicode.MAX_RUNE);
        }
        re.runes = new int[runes.Count];
        int j = 0;
        foreach (int i in runes) {
            re.runes[j++] = i;
        }
        return Dump(re);
    }

    [TestMethod]
    public void TestAppendRangeCollapse()
    {
        // AppendRange should collapse each of the new ranges
        // into the earlier ones (it looks back two ranges), so that
        // the slice never grows very large.
        // Note that we are not calling cleanClass.
        var cc = new CharClass();
        // Add 'A', 'a', 'B', 'b', etc.
        for (int i = 'A'; i <= 'Z'; i++)
        {
            cc.AppendRange(i, i);
            cc.AppendRange(i + 'a' - 'A', i + 'a' - 'A');
        }
        AssertEquals("AZaz", RunesToString(cc.ToArray()));
    }

    // Converts an array of Unicode runes to a Java UTF-16 string.
    private static string RunesToString(int[] runes)
    {
        var builder = new StringBuilder();
        foreach (int rune in runes) {
            builder.Append(char.ConvertFromUtf32(rune));
        }
        return builder.ToString();
    }

    private static readonly string[] INVALID_REGEXPS = {
    "(",
    ")",
    "(a",
    "(a|b|",
    "(a|b",
    "[a-z",
    "([a-z)",
    "x{1001}",
    "x{9876543210}",
    "x{2,1}",
    "x{1,9876543210}",
    // Java string literals can't contain Invalid UTF-8.
    // "\\xff",
    // "[\xff]",
    // "[\\\xff]",
    // "\\\xff",
    "(?P<name>a",
    "(?P<name>",
    "(?P<name",
    "(?P<x y>a)",
    "(?P<>a)",
    "[a-Z]",
    "(?i)[a-Z]",
    "a{100000}",
    "a{100000,}",
    // Group names may not be repeated
    "(?P<foo>bar)(?P<foo>baz)",
    "\\x", // https://github.com/google/re2j/issues/103
    "\\xv", // https://github.com/google/re2j/issues/103
  };

    private static readonly string[] ONLY_PERL = {
    "[a-b-c]",
    "\\Qabc\\E",
    "\\Q*+?{[\\E",
    "\\Q\\\\E",
    "\\Q\\\\\\E",
    "\\Q\\\\\\\\E",
    "\\Q\\\\\\\\\\E",
    "(?:a)",
    "(?P<name>a)",
    };

    private static readonly string[] ONLY_POSIX = {
    "a++", "a**", "a?*", "a+*", "a{1}*", ".{1}{2}.{3}",
    };

    [TestMethod]
    public void TestParseInvalidRegexps()
    {
        foreach (string regexp in INVALID_REGEXPS) {
            try
            {
                var re = Parser.Parse(regexp, RE2.PERL);
                Fail("Parsing (PERL) " + regexp + " should have failed, instead got " + Dump(re));
            }
            catch (PatternSyntaxException e)
            {
                /* ok */
            }
            try
            {
                Regexp re = Parser.Parse(regexp, RE2.POSIX);
                Fail("parsing (POSIX) " + regexp + " should have failed, instead got " + Dump(re));
            }
            catch (PatternSyntaxException e)
            {
                /* ok */
            }
        }
        foreach (string regexp in ONLY_PERL) {
            Parser.Parse(regexp, RE2.PERL);
            try
            {
                Regexp re = Parser.Parse(regexp, RE2.POSIX);
                Fail("parsing (POSIX) " + regexp + " should have failed, instead got " + Dump(re));
            }
            catch (PatternSyntaxException e)
            {
                /* ok */
            }
        }
        foreach (string regexp in ONLY_POSIX) {
            try
            {
                Regexp re = Parser.Parse(regexp, RE2.PERL);
                Fail("parsing (PERL) " + regexp + " should have failed, instead got " + Dump(re));
            }
            catch (PatternSyntaxException e)
            {
                /* ok */
            }
            Parser.Parse(regexp, RE2.POSIX);
        }
    }

    private void Fail(string v)
    {
        Assert.Fail(v);
    }

    [TestMethod]
    public void TestToStringEquivalentParse() {
        foreach (string[] tt in PARSE_TESTS) {
            Regexp re = Parser.Parse(tt[0], TEST_FLAGS);
            string d = Dump(re);
            AssertEquals(d, tt[1]); // (already ensured by testParseSimple)

            string s = re.ToString();
            if (!s.Equals(tt[0]))
            {
                // If ToString didn't return the original regexp,
                // it must have found one with fewer parens.
                // Unfortunately we can't check the Length here, because
                // ToString produces "\\{" for a literal brace,
                // but "{" is a shorter equivalent in some contexts.
                Regexp nre = Parser.Parse(s, TEST_FLAGS);
                string nd = Dump(nre);
                AssertEquals(string.Format("parse({0}) -> {1}", tt[0], s), d, nd);

                string ns = nre.ToString();
                AssertEquals(string.Format("parse({0}) -> {1}", tt[0], s), s, ns);
            }
        }
    }

    private void AssertEquals(string d, string v)
    {
        Assert.AreEqual(d, v);  
    }

    private void AssertEquals(string message, string d, string nd)
    {
        Assert.AreEqual(d, nd, message);
    }
}
