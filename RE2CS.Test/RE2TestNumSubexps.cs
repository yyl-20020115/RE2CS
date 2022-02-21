/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RE2CS.Tests;

[TestClass]
public class RE2TestNumSubexps
{
    private static readonly string[][] NUM_SUBEXP_CASES = {
    new string[]{"", "0"},
    new string[]{".*", "0"},
    new string[]{"abba", "0"},
    new string[]{"ab(b)a", "1"},
    new string[]{"ab(.*)a", "1"},
    new string[]{"(.*)ab(.*)a", "2"},
    new string[]{"(.*)(ab)(.*)a", "3"},
    new string[]{"(.*)((a)b)(.*)a", "4"},
    new string[]{"(.*)(\\(ab)(.*)a", "3"},
    new string[]{"(.*)(\\(a\\)b)(.*)a", "3"},
    };

    public static string[][] TestCases() => NUM_SUBEXP_CASES;

    private readonly string input;
    private readonly int expected;

    public RE2TestNumSubexps(string input, string expected)
    {
        this.input = input;
        int.TryParse(expected, out this.expected);
    }

    [TestMethod]
    public void TestNumSubexp()
    {
        AssertEquals(
        "numberOfCapturingGroups(" + input + ")",
        expected,
        RE2.Compile(input).NumberOfCapturingGroups);
    }
    public static void AssertEquals(string message,int v1, int v2)
    {
        Assert.AreEqual(v1, v2,message);
    }

}
