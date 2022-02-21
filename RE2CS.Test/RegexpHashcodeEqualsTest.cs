/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace RE2CS.Tests;


[TestClass]
public class RegexpHashcodeEqualsTest
{
    public static IList<object[]> TestCases()
    {
        return new List<object[]>(
          new object[][] {
          new object[]{"abc", "abc", true, RE2.POSIX},
          new object[]{"abc", "def", false, RE2.POSIX},
          new object[]{"(abc)", "(a)(b)(c)", false, RE2.POSIX},
          new object[]{"a|$", "a|$", true, RE2.POSIX},
          new object[]{"abc|def", "def|abc", false, RE2.POSIX},
          new object[]{"a?", "b?", false, RE2.POSIX},
          new object[]{"a?", "a?", true, RE2.POSIX},
          new object[]{"a{1,3}", "a{1,3}", true, RE2.POSIX},
          new object[]{"a{2,3}", "a{1,3}", false, RE2.POSIX},
          new object[]{"^((?P<foo>what)a)$", "^((?P<foo>what)a)$", true, RE2.PERL},
          new object[]{"^((?P<foo>what)a)$", "^((?P<bar>what)a)$", false, RE2.PERL},
            });
    }

 
    [TestMethod]
    public void TestEquals()
    {
        var cases = TestCases();
        for(int i = 0; i < cases.Count; i++) {
            var c = cases[i];
            this.TestEquals(i, c[0] as string, c[1] as string, (bool)c[2], (int)c[3]);
        }
    }
    public void TestEquals(int i,string a,string b,bool areEqual, int mode)
    {
        var ra = Parser.Parse(a, mode);
        var rb = Parser.Parse(b, mode);
        if (areEqual)
        {
            Assert.AreEqual(ra, rb,"TestCase="+i);
            Assert.AreEqual(ra.GetHashCode(), rb.GetHashCode(), "TestCase=" + i);
        }
        else
        {
            Assert.AreNotEqual(ra, rb, "TestCase=" + i);
            Assert.AreNotEqual(ra.GetHashCode(), rb.GetHashCode(), "TestCase=" + i);
        }
    }
}
