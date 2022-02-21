/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RE2CS.Tests;
[TestClass]
public class RE2MatchTest
{

    [TestMethod]
    public void TestMatch()
    {
        for(int i = 0; i < FindTest.FIND_TESTS.Length; i++)
        {
            TestMatch(FindTest.FIND_TESTS[i]);
        }
        Assert.IsTrue(true);
    }
    public void TestMatch(FindTest.Test test)
    {
        RE2 re = RE2.Compile(test.pat);
        bool m = re.Match(test.text);
        if (m != (test.matches.Length > 0))
        {
            Fail(
                string.Format(
                    "RE2.match failure on {0}: {1} should be {2}", test, m, test.matches.Length > 0));
        }
        // now try bytes
        m = re.MatchUTF8(test.textUTF8);
        if (m != (test.matches.Length > 0))
        {
            Fail(
                string.Format(
                    "RE2.matchUTF8 failure on {0}: {1} should be {2}", test, m, test.matches.Length > 0));
        }
    }


    [TestMethod]
    public void TestMatchFunction()
    {
        for (int i = 0; i < FindTest.FIND_TESTS.Length; i++)
        {
            TestMatchFunction(FindTest.FIND_TESTS[i]);
        }
        Assert.IsTrue(true);
    }
    public void TestMatchFunction(FindTest.Test test)
    {
        bool m = RE2.Match(test.pat, test.text);
        if (m != (test.matches.Length > 0))
        {
            Fail(
                string.Format(
                    "RE2.match failure on {0}: {1} should be {2}", test, m, test.matches.Length > 0));
        }
    }
    private void Fail(string p)
    {
        Assert.Fail(p);
    }
}
