/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;
namespace RE2CS.Tests;

// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/prog_test.go

public class ProgTest
{
    private static string[][] COMPILE_TESTS = {
    new string[]{"a", "0       fail\n" + "1*      rune1 \"a\" -> 2\n" + "2       match\n"},
    new string[]{
      "[A-M][n-z]",
      "0       fail\n"
          + "1*      rune \"AM\" -> 2\n"
          + "2       rune \"nz\" -> 3\n"
          + "3       match\n"
    },
    new string[]{"", "0       fail\n" + "1*      nop -> 2\n" + "2       match\n"},
    new string[]{
      "a?",
      "0       fail\n" + "1       rune1 \"a\" -> 3\n" + "2*      alt -> 1, 3\n" + "3       match\n"
    },
    new string[]{
      "a??",
      "0       fail\n" + "1       rune1 \"a\" -> 3\n" + "2*      alt -> 3, 1\n" + "3       match\n"
    },
    new string[]{
      "a+",
      "0       fail\n" + "1*      rune1 \"a\" -> 2\n" + "2       alt -> 1, 3\n" + "3       match\n"
    },
    new string[]{
      "a+?",
      "0       fail\n" + "1*      rune1 \"a\" -> 2\n" + "2       alt -> 3, 1\n" + "3       match\n"
    },
    new string[]{
      "a*",
      "0       fail\n" + "1       rune1 \"a\" -> 2\n" + "2*      alt -> 1, 3\n" + "3       match\n"
    },
    new string[]{
      "a*?",
      "0       fail\n" + "1       rune1 \"a\" -> 2\n" + "2*      alt -> 3, 1\n" + "3       match\n"
    },
    new string[]{
      "a+b+",
      "0       fail\n"
          + "1*      rune1 \"a\" -> 2\n"
          + "2       alt -> 1, 3\n"
          + "3       rune1 \"b\" -> 4\n"
          + "4       alt -> 3, 5\n"
          + "5       match\n"
    },
    new string[]{
      "(a+)(b+)",
      "0       fail\n"
          + "1*      cap 2 -> 2\n"
          + "2       rune1 \"a\" -> 3\n"
          + "3       alt -> 2, 4\n"
          + "4       cap 3 -> 5\n"
          + "5       cap 4 -> 6\n"
          + "6       rune1 \"b\" -> 7\n"
          + "7       alt -> 6, 8\n"
          + "8       cap 5 -> 9\n"
          + "9       match\n"
    },
    new string[]{
      "a+|b+",
      "0       fail\n"
          + "1       rune1 \"a\" -> 2\n"
          + "2       alt -> 1, 6\n"
          + "3       rune1 \"b\" -> 4\n"
          + "4       alt -> 3, 6\n"
          + "5*      alt -> 1, 3\n"
          + "6       match\n"
    },
    new string[]{
      "A[Aa]",
      "0       fail\n"
          + "1*      rune1 \"A\" -> 2\n"
          + "2       rune \"A\"/i -> 3\n"
          + "3       match\n"
    },
    new string[]{
      "(?:(?:^).)",
      "0       fail\n" + "1*      empty 4 -> 2\n" + "2       anynotnl -> 3\n" + "3       match\n"
    },
    new string[]{
      "(?:|a)+",
      "0       fail\n"
          + "1       nop -> 4\n"
          + "2       rune1 \"a\" -> 4\n"
          + "3*      alt -> 1, 2\n"
          + "4       alt -> 3, 5\n"
          + "5       match\n"
    },
    new string[]{
      "(?:|a)*",
      "0       fail\n"
          + "1       nop -> 4\n"
          + "2       rune1 \"a\" -> 4\n"
          + "3       alt -> 1, 2\n"
          + "4       alt -> 3, 6\n"
          + "5*      alt -> 3, 6\n"
          + "6       match\n"
    },
  };

    private string input;
    private string expected;

    public static object[] GetParameters()
    {
        return COMPILE_TESTS;
    }

    public ProgTest(string input, string expected)
    {
        this.input = input;
        this.expected = expected;
    }

    [Test]
    public void TestCompile()
    {
        Regexp re = Parser.Parse(input, RE2.PERL);
        Program p = Compiler.CompileRegexp(re);
        string s = p.ToString();
        AssertEquals("compiled: " + input, expected, s);
    }

    private void AssertEquals(string message, string expected, string s)
    {
        Assert.AreEqual(expected, s, message);
    }
}