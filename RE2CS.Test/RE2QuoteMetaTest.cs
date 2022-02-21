/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RE2CS.Tests;

[TestClass]
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

    

    //private readonly string pattern;
    //private readonly string output;
    //private readonly string literal;
    //private readonly bool isLiteral;

 
    [TestMethod]
    public void TestQuoteMeta()
    {
        for(int i = 0; i < META_TESTS.Length; i++)
        {
            var test = META_TESTS[i];
            TestQuoteMeta(i, test[0], test[1], test[2]);
        }
        Assert.IsTrue(true);
    }
    public void TestQuoteMeta(int i,string pattern,string output,string literal)
    {
        // Verify that quoteMeta returns the expected string.
        string quoted = RE2.QuoteMeta(pattern);
        if (!quoted.Equals(output))
        {
            Fail(string.Format("RE2.quoteMeta(\"{0}\") = \"{1}\"; want \"{2}\"", pattern, quoted, output));
        }

        // Verify that the quoted string is in fact treated as expected
        // by compile -- i.e. that it matches the original, unquoted string.
        if (!string.IsNullOrEmpty(pattern))
        {
            RE2 re = null;
            try
            {
                re = RE2.Compile(quoted);
            }
            catch (PatternSyntaxException e)
            {
                Fail(
                    string.Format(
                        "Unexpected error compiling quoteMeta(\"{0}\"): {1}", pattern, e.Message));
            }
            string src = "abc" + pattern + "def";
            string repl = "xyz";
            string replaced = re.ReplaceAll(src, repl);
            string expected = "abcxyzdef";
            if (!replaced.Equals(expected))
            {
                Fail(
                    string.Format(
                        "quoteMeta(`{0}`).replace(`{1}`,`{2}`) = `{3}`; want `{4}`",
                        pattern,
                        src,
                        repl,
                        replaced,
                        expected));
            }
        }
    }

    [TestMethod]
    public void TestLiteralPrefix()
    {
        for (int i = 0; i < META_TESTS.Length; i++)
        {
            var test = META_TESTS[i];
            bool.TryParse(test[3], out var b);
            TestLiteralPrefix(i, test[0], test[1], test[2],b);
        }
        Assert.IsTrue(true);
    }
    public void TestLiteralPrefix(int i, string pattern, string output, string literal, bool isLiteral)
    {
        // Literal method needs to scan the pattern.
        RE2 re = RE2.Compile(pattern);
        if (re.prefixComplete != isLiteral)
        {
            Fail(
                string.Format(
                    "literalPrefix(\"{0}\") = {1}; want {2}", pattern, re.prefixComplete, isLiteral));
        }
        if (!re.prefix.Equals(literal))
        {
            Fail(
                string.Format(
                    "literalPrefix(\"{0}\") = \"{1}\"; want \"{2}\"", pattern, re.prefix, literal));
        }
    }

    private void Fail(string p)
    {
        Assert.Fail(p);
    }
}

