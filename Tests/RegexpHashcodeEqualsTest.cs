/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;


[TestFixture]
public class RegexpHashcodeEqualsTest
{
    public static IEnumerable<object[]> TestCases()
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

    public string a;

    public string b;

    public bool areEqual;

    public int mode;

    [Test]
    public void TestEquals()
    {
        Regexp ra = Parser.Parse(a, mode);
        Regexp rb = Parser.Parse(b, mode);
        if (areEqual)
        {
            Assert.AreEqual(ra, rb);
            Assert.AreEqual(ra.GetHashCode(), rb.GetHashCode());
        }
        else
        {
            Assert.AreNotEqual(ra, rb);
        }
    }
}
