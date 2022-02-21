/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;

[TestFixture]
public class RE2ReplaceAllFunctionTest
{
    private static ReplaceFunction REPLACE_XSY = (string s) =>
    {
        return "x" + s + "y";
    };

    // Each row is (string pattern, input, output, ReplaceFunc replacement).
    // Conceptually the replacement func is a table column---but for now
    // it's always REPLACE_XSY.
    private static string[][] REPLACE_FUNC_TESTS = {
        new string[]{"[a-c]", "defabcdef", "defxayxbyxcydef"},
        new string[] { "[a-c]+", "defabcdef", "defxabcydef"},
        new string[] { "[a-c]*", "defabcdef", "xydxyexyfxabcydxyexyfxy"},
      };

    public static string[][] TestCases()
    {
        return REPLACE_FUNC_TESTS;
    }

    private readonly string pattern;
    private readonly string input;
    private readonly string expected;

    public RE2ReplaceAllFunctionTest(string pattern, string input, string expected)
    {
        this.pattern = pattern;
        this.input = input;
        this.expected = expected;
    }

    [Test]
    public void TestReplaceAllFunc()
    {
        RE2 re = null;
        try
        {
            re = RE2.Compile(pattern);
        }
        catch (PatternSyntaxException e)
        {
            Fail(string.Format("Unexpected error compiling {0}: {1}", pattern, e.Message));
        }
        var actual = re.ReplaceAllFunc(input, REPLACE_XSY, input.Length);
        if (!actual.Equals(expected))
        {
            Fail(
                string.Format(
                    "{0}.replaceAllFunc({1},{2}) = {3}; want {4}",
                    pattern,
                    input,
                    REPLACE_XSY,
                    actual,
                    expected));
        }
    }

    private void Fail(string p)
    {
        Assert.Fail(p);
    }
}
