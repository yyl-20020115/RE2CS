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
    private static RE2.ReplaceFunc REPLACE_XSY;
    //        new RE2.ReplaceFunc() {
    //          public string replace(string s)
    //    {
    //        return "x" + s + "y";
    //    }

    //        public string toString()
    //    {
    //        return "REPLACE_XSY";
    //    }
    //};

    // Each row is (string pattern, input, output, ReplaceFunc replacement).
    // Conceptually the replacement func is a table column---but for now
    // it's always REPLACE_XSY.
    private static string[][] REPLACE_FUNC_TESTS = {
    new string[]{"[a-c]", "defabcdef", "defxayxbyxcydef"},
    new string[] { "[a-c]+", "defabcdef", "defxabcydef"},
    new string[] { "[a-c]*", "defabcdef", "xydxyexyfxabcydxyexyfxy"},
  };

    public static string[][] testCases()
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
    public void testReplaceAllFunc()
    {
        RE2 re = null;
        try
        {
            re = RE2.Compile(pattern);
        }
        catch (PatternSyntaxException e)
        {
            fail(string.Format("Unexpected error compiling {0}: {1}", pattern, e.Message));
        }
        string actual = re.ReplaceAllFunc(input, REPLACE_XSY, input.Length);
        if (!actual.Equals(expected))
        {
            fail(
                string.Format(
                    "{0}.replaceAllFunc({1},{2}) = {3}; want {4}",
                    pattern,
                    input,
                    REPLACE_XSY,
                    actual,
                    expected));
        }
    }

    private void fail(string p)
    {
        Assert.Fail(p);
    }
}
