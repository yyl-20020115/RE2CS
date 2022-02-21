/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RE2CS.Tests;

[TestClass]
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


    [TestMethod]
    public void TestReplaceAllFunc()
    {
    //private readonly string pattern;
    //private readonly string input;
    //private readonly string expected;
        for(int i = 0; i < REPLACE_FUNC_TESTS.Length; i++)
        {
            var t = REPLACE_FUNC_TESTS[i];
            TestReplaceAllFunc(i, t[0], t[1], t[2]);
        }
        Assert.IsTrue(true);
    }
    public void TestReplaceAllFunc(int i,string pattern,string input,string expected)
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
