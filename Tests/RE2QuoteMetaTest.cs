/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;

[TestFixture]
public class RE2QuoteMetaTest
{
    // (pattern, output, literal, isLiteral)
    private static string[][] META_TESTS = {
    new string[]{"", "", "", "true"},
    new string[]{"foo", "foo", "foo", "true"},
    // has meta but no operator:
    new string[]{"foo\\.\\$", "foo\\\\\\.\\\\\\$", "foo.$", "true"},
    // has escaped operators and real operators:
    new string[]{"foo.\\$", "foo\\.\\\\\\$", "foo", "false"},
    new string[]{
      "!@#$%^&*()_+-=[{]}\\|,<.>/?~",
      "!@#\\$%\\^&\\*\\(\\)_\\+-=\\[\\{\\]\\}\\\\\\|,<\\.>/\\?~",
      "!@#",
      "false"
    },
  };

    public static string[][] testCases()
    {
        return META_TESTS;
    }

    private readonly string pattern;
    private readonly string output;
    private readonly string literal;
    private readonly bool isLiteral;

    public RE2QuoteMetaTest(string pattern, string output, string literal, string isLiteral)
    {
        this.pattern = pattern;
        this.output = output;
        this.literal = literal;
        this.isLiteral = Boolean.parseBoolean(isLiteral);
    }

    [Test]
    public void testQuoteMeta()
    {
        // Verify that quoteMeta returns the expected string.
        string quoted = RE2.quoteMeta(pattern);
        if (!quoted.Equals(output))
        {
            fail(string.format("RE2.quoteMeta(\"%s\") = \"%s\"; want \"%s\"", pattern, quoted, output));
        }

        // Verify that the quoted string is in fact treated as expected
        // by compile -- i.e. that it matches the original, unquoted string.
        if (!pattern.isEmpty())
        {
            RE2 re = null;
            try
            {
                re = RE2.compile(quoted);
            }
            catch (PatternSyntaxException e)
            {
                fail(
                    string.format(
                        "Unexpected error compiling quoteMeta(\"%s\"): %s", pattern, e.Message));
            }
            string src = "abc" + pattern + "def";
            string repl = "xyz";
            string replaced = re.replaceAll(src, repl);
            string expected = "abcxyzdef";
            if (!replaced.Equals(expected))
            {
                fail(
                    string.format(
                        "quoteMeta(`%s`).replace(`%s`,`%s`) = `%s`; want `%s`",
                        pattern,
                        src,
                        repl,
                        replaced,
                        expected));
            }
        }
    }

    [Test]
    public void testLiteralPrefix()
    {
        // Literal method needs to scan the pattern.
        RE2 re = RE2.compile(pattern);
        if (re.prefixComplete != isLiteral)
        {
            fail(
                string.format(
                    "literalPrefix(\"%s\") = %s; want %s", pattern, re.prefixComplete, isLiteral));
        }
        if (!re.prefix.Equals(literal))
        {
            fail(
                string.format(
                    "literalPrefix(\"%s\") = \"%s\"; want \"%s\"", pattern, re.prefix, literal));
        }
    }

    private void fail(string p)
    {
        Assert.Fail(p);
    }
}

