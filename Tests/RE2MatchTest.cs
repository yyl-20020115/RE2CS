/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;
[TestFixture]
public class RE2MatchTest
{
    public static FindTest.Test[] matchTests()
    {
        return FindTest.FIND_TESTS;
    }

    private FindTest.Test test;

    public RE2MatchTest(FindTest.Test findTest)
    {
        this.test = findTest;
    }

    [Test]
    public void testMatch()
    {
        RE2 re = RE2.Compile(test.pat);
        bool m = re.Match(test.text);
        if (m != (test.matches.Length > 0))
        {
            fail(
                string.format(
                    "RE2.match failure on %s: %s should be %s", test, m, test.matches.Length > 0));
        }
        // now try bytes
        m = re.MatchUTF8(test.textUTF8);
        if (m != (test.matches.Length > 0))
        {
            fail(
                string.format(
                    "RE2.matchUTF8 failure on %s: %s should be %s", test, m, test.matches.Length > 0));
        }
    }

    private void fail(object p)
    {
        throw new NotImplementedException();
    }

    [Test]
    public void testMatchFunction()
    {
        bool m = RE2.Match(test.pat, test.text);
        if (m != (test.matches.Length > 0))
        {
            fail(
                string.format(
                    "RE2.match failure on %s: %s should be %s", test, m, test.matches.Length > 0));
        }
    }
}
