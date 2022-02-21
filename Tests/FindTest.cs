/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/find_test.go
//
// The original Go function names are documented because of the
// potential for confusion arising from systematic renamings
// (e.g. "string" -> "", "" -> "UTF8", "Test" -> "test", etc.)
using NUnit.Framework;

namespace RE2CS.Tests;

[TestFixture]
public class FindTest
{

    // For each pattern/text pair, what is the expected output of each
    // function?  We can derive the textual results from the indexed
    // results, the non-submatch results from the submatched results, the
    // single results from the 'all' results, and the string results from
    // the UTF-8 results. Therefore the table includes only the
    // findAllUTF8SubmatchIndex result.

    public class Test
    {
        // The n and x parameters construct a [][]int by extracting n
        // sequences from x.  This represents n matches with len(x)/n
        // submatches each.
        public Test(string pat, string text, int n, params int[] x)
        {
            this.pat = pat;
            this.text = text;
            this.textUTF8 = GoTestUtils.Utf8(text);
            this.matches = new int[n][];
            if (n > 0)
            {
                int runLength = x.Length / n;
                for (int j = 0, i = 0; i < n; i++)
                {
                    matches[i] = new int[runLength];
                    Array.Copy(x, j, matches[i], 0, runLength);
                    j += runLength;
                    if (j > x.Length)
                    {
                        Fail("invalid build entry");
                    }
                }
            }
        }

        void Fail(string s)
        {
            Assert.Fail(s);
        }
        public readonly string pat;
        public readonly string text;
        public readonly byte[] textUTF8;
        // Each element is an even-Length array of indices into textUTF8.  Not null.
        public readonly int[][] matches;

        public byte[] SubmatchBytes(int i, int j)
        {
            return Utils.Subarray(textUTF8, matches[i][2 * j], matches[i][2 * j + 1]);
        }

        public string SubmatchString(int i, int j)
        {
            return GoTestUtils.FromUTF8(SubmatchBytes(i, j)); // yikes
        }

        //@Override
        public override string ToString()
        {
            return string.Format("pat={0} text={1}", pat, text);
        }
    }

    // Used by RE2Test also.
    public static readonly Test[] FIND_TESTS = {
    new Test("", "", 1, 0, 0),
    new Test("^abcdefg", "abcdefg", 1, 0, 7),
    new Test("a+", "baaab", 1, 1, 4),
    new Test("abcd..", "abcdef", 1, 0, 6),
    new Test("a", "a", 1, 0, 1),
    new Test("x", "y", 0),
    new Test("b", "abc", 1, 1, 2),
    new Test(".", "a", 1, 0, 1),
    new Test(".*", "abcdef", 1, 0, 6),
    new Test("^", "abcde", 1, 0, 0),
    new Test("$", "abcde", 1, 5, 5),
    new Test("^abcd$", "abcd", 1, 0, 4),
    new Test("^bcd'", "abcdef", 0),
    new Test("^abcd$", "abcde", 0),
    new Test("a+", "baaab", 1, 1, 4),
    new Test("a*", "baaab", 3, 0, 0, 1, 4, 5, 5),
    new Test("[a-z]+", "abcd", 1, 0, 4),
    new Test("[^a-z]+", "ab1234cd", 1, 2, 6),
    new Test("[a\\-\\]z]+", "az]-bcz", 2, 0, 4, 6, 7),
    new Test("[^\\n]+", "abcd\n", 1, 0, 4),
    new Test("[日本語]+", "日本語日本語", 1, 0, 18),
    new Test("日本語+", "日本語", 1, 0, 9),
    new Test("日本語+", "日本語語語語", 1, 0, 18),
    new Test("()", "", 1, 0, 0, 0, 0),
    new Test("(a)", "a", 1, 0, 1, 0, 1),
    new Test("(.)(.)", "日a", 1, 0, 4, 0, 3, 3, 4),
    new Test("(.*)", "", 1, 0, 0, 0, 0),
    new Test("(.*)", "abcd", 1, 0, 4, 0, 4),
    new Test("(..)(..)", "abcd", 1, 0, 4, 0, 2, 2, 4),
    new Test("(([^xyz]*)(d))", "abcd", 1, 0, 4, 0, 4, 0, 3, 3, 4),
    new Test("((a|b|c)*(d))", "abcd", 1, 0, 4, 0, 4, 2, 3, 3, 4),
    new Test("(((a|b|c)*)(d))", "abcd", 1, 0, 4, 0, 4, 0, 3, 2, 3, 3, 4),
    new Test("\\a\\f\\n\\r\\t\\v", "\007\f\n\r\t\013", 1, 0, 6),
    new Test("[\\a\\f\\n\\r\\t\\v]+", "\007\f\n\r\t\013", 1, 0, 6),
    new Test("a*(|(b))c*", "aacc", 1, 0, 4, 2, 2, -1, -1),
    new Test("(.*).*", "ab", 1, 0, 2, 0, 2),
    new Test("[.]", ".", 1, 0, 1),
    new Test("/$", "/abc/", 1, 4, 5),
    new Test("/$", "/abc", 0),

    // multiple matches
    new Test(".", "abc", 3, 0, 1, 1, 2, 2, 3),
    new Test("(.)", "abc", 3, 0, 1, 0, 1, 1, 2, 1, 2, 2, 3, 2, 3),
    new Test(".(.)", "abcd", 2, 0, 2, 1, 2, 2, 4, 3, 4),
    new Test("ab*", "abbaab", 3, 0, 3, 3, 4, 4, 6),
    new Test("a(b*)", "abbaab", 3, 0, 3, 1, 3, 3, 4, 4, 4, 4, 6, 5, 6),

    // fixed bugs
    new Test("ab$", "cab", 1, 1, 3),
    new Test("axxb$", "axxcb", 0),
    new Test("data", "daXY data", 1, 5, 9),
    new Test("da(.)a$", "daXY data", 1, 5, 9, 7, 8),
    new Test("zx+", "zzx", 1, 1, 3),
    new Test("ab$", "abcab", 1, 3, 5),
    new Test("(aa)*$", "a", 1, 1, 1, -1, -1),
    new Test("(?:.|(?:.a))", "", 0),
    new Test("(?:A(?:A|a))", "Aa", 1, 0, 2),
    new Test("(?:A|(?:A|a))", "a", 1, 0, 1),
    new Test("(a){0}", "", 1, 0, 0, -1, -1),
    new Test("(?-s)(?:(?:^).)", "\n", 0),
    new Test("(?s)(?:(?:^).)", "\n", 1, 0, 1),
    new Test("(?:(?:^).)", "\n", 0),
    new Test("\\b", "x", 2, 0, 0, 1, 1),
    new Test("\\b", "xx", 2, 0, 0, 2, 2),
    new Test("\\b", "x y", 4, 0, 0, 1, 1, 2, 2, 3, 3),
    new Test("\\b", "xx yy", 4, 0, 0, 2, 2, 3, 3, 5, 5),
    new Test("\\B", "x", 0),
    new Test("\\B", "xx", 1, 1, 1),
    new Test("\\B", "x y", 0),
    new Test("\\B", "xx yy", 2, 1, 1, 4, 4),

    // RE2 tests
    new Test("[^\\S\\s]", "abcd", 0),
    new Test("[^\\S[:space:]]", "abcd", 0),
    new Test("[^\\D\\d]", "abcd", 0),
    new Test("[^\\D[:digit:]]", "abcd", 0),
    new Test("(?i)\\W", "x", 0),
    new Test("(?i)\\W", "k", 0),
    new Test("(?i)\\W", "s", 0),

    // can backslash-escape any punctuation
    new Test(
        "\\!\\\"\\#\\$\\%\\&\\'\\(\\)\\*\\+\\,\\-\\.\\/\\:\\;\\<\\=\\>\\?\\@\\[\\\\\\]\\^\\_\\{\\|\\}\\~",
        "!\"#$%&'()*+,-./:;<=>?@[\\]^_{|}~",
        1,
        0,
        31),
    new Test(
        "[\\!\\\"\\#\\$\\%\\&\\'\\(\\)\\*\\+\\,\\-\\.\\/\\:\\;\\<\\=\\>\\?\\@\\[\\\\\\]\\^\\_\\{\\|\\}\\~]+",
        "!\"#$%&'()*+,-./:;<=>?@[\\]^_{|}~",
        1,
        0,
        31),
    new Test("\\`", "`", 1, 0, 1),
    new Test("[\\`]+", "`", 1, 0, 1),

    // long set of matches
    new Test(
        ".",
        "qwertyuiopasdfghjklzxcvbnm1234567890",
        36,
        0,
        1,
        1,
        2,
        2,
        3,
        3,
        4,
        4,
        5,
        5,
        6,
        6,
        7,
        7,
        8,
        8,
        9,
        9,
        10,
        10,
        11,
        11,
        12,
        12,
        13,
        13,
        14,
        14,
        15,
        15,
        16,
        16,
        17,
        17,
        18,
        18,
        19,
        19,
        20,
        20,
        21,
        21,
        22,
        22,
        23,
        23,
        24,
        24,
        25,
        25,
        26,
        26,
        27,
        27,
        28,
        28,
        29,
        29,
        30,
        30,
        31,
        31,
        32,
        32,
        33,
        33,
        34,
        34,
        35,
        35,
        36),
    new Test("(|a)*", "aa", 3, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2),
  };

    public static Test[] TestCases() => FIND_TESTS;

    private readonly Test test;

    public FindTest(Test test) => this.test = test;

    // First the simple cases.

    [Test]
    public void TestFindUTF8()
    {
        RE2 re = RE2.Compile(test.pat);
        if (!re.ToString().Equals(test.pat))
        {
            Fail(string.Format("RE2.ToString() = \"{0}\"; should be \"{1}\"", re.ToString(), test.pat));
        }
        byte[] result = re.FindUTF8(test.textUTF8);
        if (test.matches.Length == 0 && GoTestUtils.Len(result) == 0)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("findUTF8: expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("findUTF8: expected match; got none: {1}", test));
        }
        else
        {
            byte[] expect = test.SubmatchBytes(0, 0);
            if (!Enumerable.SequenceEqual(expect, result))
            {
                Fail(
                    string.Format(
                        "findUTF8: expected {0}; got {1}: {2}",
                        GoTestUtils.FromUTF8(expect),
                        GoTestUtils.FromUTF8(result),
                        test));
            }
        }
    }

    [Test]
    public void TestFind()
    {
        string result = RE2.Compile(test.pat).Find(test.text);
        if (test.matches.Length == 0 && string.IsNullOrEmpty(result))
        {
            // ok
        }
        else if (test.matches.Length == 0 && !string.IsNullOrEmpty(result))
        {
            Fail(string.Format("find: expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && string.IsNullOrEmpty(result))
        {
            // Tricky because an empty result has two meanings:
            // no match or empty match.
            int[] match = test.matches[0];
            if (match[0] != match[1])
            {
                Fail(string.Format("find: expected match; got none: {0}", test));
            }
        }
        else
        {
            string expect = test.SubmatchString(0, 0);
            if (!expect.Equals(result))
            {
                Fail(string.Format("find: expected {0} got {1}: {2}", expect, result, test));
            }
        }
    }

    private void TestFindIndexCommon(
        string testName, Test test, int[] result, bool resultIndicesAreUTF8)
    {
        if (test.matches.Length == 0 && GoTestUtils.Len(result) == 0)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("{0}: expected no match; got one: {1}", testName, test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("{0}: expected match; got none: {1}", testName, test));
        }
        else
        {
            if (!resultIndicesAreUTF8)
            {
                result = GoTestUtils.Utf16IndicesToUtf8(result, test.text);
            }
            int[] expect = test.matches[0]; // UTF-8 indices
            if (expect[0] != result[0] || expect[1] != result[1])
            {
                Fail(
                    string.Format(
                        "{0}: expected {1} got {2}: {3}",
                        testName,
                        (expect),
                        (result),
                        test));
            }
        }
    }

    [Test]
    public void TestFindUTF8Index()
    {
        TestFindIndexCommon(
            "testFindUTF8Index", test, RE2.Compile(test.pat).FindUTF8Index(test.textUTF8), true);
    }

    [Test]
    public void TestFindIndex()
    {
        int[] result = RE2.Compile(test.pat).FindIndex(test.text);
        TestFindIndexCommon("testFindIndex", test, result, false);
    }

    // Now come the simple All cases.

    [Test]
    public void TestFindAllUTF8()
    {
        List<byte[]> result = RE2.Compile(test.pat).FindAllUTF8(test.textUTF8, -1);
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("findAllUTF8: expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            throw new Exception("findAllUTF8: expected match; got none: " + test);
        }
        else
        {
            if (test.matches.Length != result.Count)
            {
                Fail(
                    string.Format(
                        "findAllUTF8: expected {0} matches; got {1}: {2}",
                        test.matches.Length,
                        result.Count,
                        test));
            }
            for (int i = 0; i < test.matches.Length; i++)
            {
                byte[] expect = test.SubmatchBytes(i, 0);
                if (!Enumerable.SequenceEqual(expect, result[i]))
                {
                    Fail(
                        string.Format(
                            "findAllUTF8: match {0}: expected {0}; got {0}: {0}",
                            i / 2,
                            GoTestUtils.FromUTF8(expect),
                            GoTestUtils.FromUTF8(result[i]),
                            test));
                }
            }
        }
    }

    [Test]
    public void TestFindAll()
    {
        List<string> result = RE2.Compile(test.pat).FindAll(test.text, -1);
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("findAll: expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("findAll: expected match; got none: {0}", test));
        }
        else
        {
            if (test.matches.Length != result.Count)
            {
                Fail(
                    string.Format(
                        "findAll: expected {0} matches; got {1}: {2}",
                        test.matches.Length,
                        result.Count,
                        test));
            }
            for (int i = 0; i < test.matches.Length; i++)
            {
                string expect = test.SubmatchString(i, 0);
                if (!expect.Equals(result[i]))
                {
                    Fail(string.Format("findAll: expected {0}; got {0}: {0}",
                        expect, result, test));
                }
            }
        }
    }

    private void TestFindAllIndexCommon(
        string testName, Test test, List<int[]> result, bool resultIndicesAreUTF8)
    {
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("{0}: expected no match; got one: {1}", testName, test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("{0}: expected match; got none: {1}", testName, test));
        }
        else
        {
            if (test.matches.Length != result.Count)
            {
                Fail(
                    string.Format(
                        "{0}: expected {1} matches; got {2}: {3}",
                        testName,
                        test.matches.Length,
                        result.Count,
                        test));
            }
            for (int k = 0; k < test.matches.Length; k++)
            {
                int[] e = test.matches[k];
                int[] res = result[k];
                if (!resultIndicesAreUTF8)
                {
                    res = GoTestUtils.Utf16IndicesToUtf8(res, test.text);
                }
                if (e[0] != res[0] || e[1] != res[1])
                {
                    Fail(
                        string.Format(
                            "{0}: match {1}: expected {2}; got {3}: {4}",
                            testName,
                            k,
                            (e), // (only 1st two elements matter here)
                            (res),
                            test));
                }
            }
        }
    }

    [Test]
    public void TestFindAllUTF8Index()
    {
        TestFindAllIndexCommon(
            "testFindAllUTF8Index",
            test,
            RE2.Compile(test.pat).FindAllUTF8Index(test.textUTF8, -1),
            true);
    }

    [Test]
    public void TestFindAllIndex()
    {
        TestFindAllIndexCommon(
            "testFindAllIndex", test, RE2.Compile(test.pat).FindAllIndex(test.text, -1), false);
    }

    // Now come the Submatch cases.

    private void TestSubmatchBytes(string testName, FindTest.Test test, int n, byte[][] result)
    {
        int[] submatches = test.matches[n];
        if (submatches.Length != GoTestUtils.Len(result) * 2)
        {
            Fail(
                string.Format(
                    "{0} {1}: expected {2} submatches; got {3}: {4}",
                    testName,
                    n,
                    submatches.Length / 2,
                    GoTestUtils.Len(result),
                    test));
        }
        for (int k = 0; k < GoTestUtils.Len(result); k++)
        {
            if (submatches[k * 2] == -1)
            {
                if (result[k] != null)
                {
                    Fail(string.Format("{0} {1}: expected null got {2}: {3}", testName, n, result, test));
                }
                continue;
            }
            byte[] expect = test.SubmatchBytes(n, k);
            if (!Enumerable.SequenceEqual(expect, result[k]))
            {
                Fail(
                    string.Format(
                        "{0} {1}: expected {2}; got {3}: {4}",
                        testName,
                        n,
                        GoTestUtils.FromUTF8(expect),
                        GoTestUtils.FromUTF8(result[k]),
                        test));
            }
        }
    }

    [Test]
    public void TestFindUTF8Submatch()
    {
        byte[][] result = RE2.Compile(test.pat).FindUTF8Submatch(test.textUTF8);
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("expected match; got none: {0}", test));
        }
        else
        {
            TestSubmatchBytes("testFindUTF8Submatch", test, 0, result);
        }
    }

    // (Go: testSubmatchString)
    private void TestSubmatch(string testName, Test test, int n, string[] result)
    {
        int[] submatches = test.matches[n];
        if (submatches.Length != GoTestUtils.Len(result) * 2)
        {
            Fail(
                string.Format(
                    "{0} {1}: expected {2} submatches; got {3}: {4}",
                    testName,
                    n,
                    submatches.Length / 2,
                    GoTestUtils.Len(result),
                    test));
        }
        for (int k = 0; k < submatches.Length; k += 2)
        {
            if (submatches[k] == -1)
            {
                if (result[k / 2] != null && result[k / 2]!="")
                {
                    Fail(
                        string.Format(
                            "{0} {1}: expected null got {2}: {3}", testName, n, (result), test));
                }
                continue;
            }
            Console.Error.WriteLine(testName + "  " + test + " " + n + " " + k + " ");
            string expect = test.SubmatchString(n, k / 2);
            if (!expect.Equals(result[k / 2]))
            {
                Fail(
                    string.Format(
                        "{0} {1}: expected {2} got {3}: {4}",
                        testName,
                        n,
                        expect,
                        (result),
                        test));
            }
        }
    }

    // (Go: TestFindStringSubmatch)
    [Test]
    public void TestFindSubmatch()
    {
        string[] result = RE2.Compile(test.pat).FindSubmatch(test.text);
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("expected match; got none: {0}", test));
        }
        else
        {
            TestSubmatch("testFindSubmatch", test, 0, result);
        }
    }

    private void TestSubmatchIndices(
        string testName, Test test, int n, int[] result, bool resultIndicesAreUTF8)
    {
        int[] expect = test.matches[n];
        if (expect.Length != GoTestUtils.Len(result))
        {
            Fail(
                string.Format(
                    "{0} {1}: expected {2} matches; got {3}: {4}",
                    testName,
                    n,
                    expect.Length / 2,
                    GoTestUtils.Len(result) / 2,
                    test));
            return;
        }
        if (!resultIndicesAreUTF8)
        {
            result = GoTestUtils.Utf16IndicesToUtf8(result, test.text);
        }
        for (int k = 0; k < expect.Length; ++k)
        {
            if (expect[k] != result[k])
            {
                Fail(
                    string.Format(
                        "{0} {1}: submatch error: expected {2} got {3}: {4}",
                        testName,
                        n,
                        (expect),
                        (result),
                        test));
            }
        }
    }

    private void TestFindSubmatchIndexCommon(
        string testName, Test test, int[] result, bool resultIndicesAreUTF8)
    {
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("{0}: expected no match; got one: {1}", testName, test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("{0}: expected match; got none: {1}", testName, test));
        }
        else
        {
            TestSubmatchIndices(testName, test, 0, result, resultIndicesAreUTF8);
        }
    }

    [Test]
    public void TestFindUTF8SubmatchIndex()
    {
        TestFindSubmatchIndexCommon(
            "testFindSubmatchIndex",
            test,
            RE2.Compile(test.pat).FindUTF8SubmatchIndex(test.textUTF8),
            true);
    }

    // (Go: TestFindStringSubmatchIndex)
    [Test]
    public void TestFindSubmatchIndex()
    {
        TestFindSubmatchIndexCommon(
            "testFindStringSubmatchIndex",
            test,
            RE2.Compile(test.pat).FindSubmatchIndex(test.text),
            false);
    }

    // Now come the monster AllSubmatch cases.

    // (Go: TestFindAllSubmatch)
    [Test]
    public void TestFindAllUTF8Submatch()
    {
        List<byte[][]> result = RE2.Compile(test.pat).FindAllUTF8Submatch(test.textUTF8, -1);
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("expected no match; got one: {0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("expected match; got none: {0}", test));
        }
        else if (test.matches.Length != result.Count)
        {
            Fail(
                string.Format(
                    "expected {0} matches; got {1}: {2}", test.matches.Length, result.Count, test));
        }
        else
        {
            for (int k = 0; k < test.matches.Length; ++k)
            {
                TestSubmatchBytes("testFindAllSubmatch", test, k, result[k]);
            }
        }
    }

    // (Go: TestFindAllStringSubmatch)
    [Test]
    public void TestFindAllSubmatch()
    {
        List<string[]> result = RE2.Compile(test.pat).FindAllSubmatch(test.text, -1);
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("expected no match; got one:{0}", test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("expected match; got none: {0}", test));
        }
        else if (test.matches.Length != result.Count)
        {
            Fail(
                string.Format(
                    "expected {0} matches; got {1}: {2}", test.matches.Length, result.Count, test));
        }
        else
        {
            for (int k = 0; k < test.matches.Length; ++k)
            {
                TestSubmatch("testFindAllStringSubmatch", test, k, result[k]);
            }
        }
    }

    // (Go: testFindSubmatchIndex)
    private void TestFindAllSubmatchIndexCommon(
        string testName, Test test, List<int[]> result, bool resultIndicesAreUTF8)
    {
        if (test.matches.Length == 0 && result == null)
        {
            // ok
        }
        else if (test.matches.Length == 0 && result != null)
        {
            Fail(string.Format("{0}: expected no match; got one: {1}", testName, test));
        }
        else if (test.matches.Length > 0 && result == null)
        {
            Fail(string.Format("{0}: expected match; got none: {1}", testName, test));
        }
        else if (test.matches.Length != result.Count)
        {
            Fail(
                string.Format(
                    "{0}: expected {1} matches; got {2}: {3}",
                    testName,
                    test.matches.Length,
                    result.Count,
                    test));
        }
        else
        {
            for (int k = 0; k < test.matches.Length; ++k)
            {
                TestSubmatchIndices(testName, test, k, result[k], resultIndicesAreUTF8);
            }
        }
    }

    private void Fail(string p)
    {
        Assert.Fail(p);
    }

    // (Go: TestFindAllSubmatchIndex)
    [Test]
    public void TestFindAllUTF8SubmatchIndex()
    {
        TestFindAllSubmatchIndexCommon(
            "testFindAllUTF8SubmatchIndex",
            test,
            RE2.Compile(test.pat).FindAllUTF8SubmatchIndex(test.textUTF8, -1),
            true);
    }

    // (Go: TestFindAllStringSubmatchIndex)
    [Test]
    public void TestFindAllSubmatchIndex()
    {
        TestFindAllSubmatchIndexCommon(
            "testFindAllSubmatchIndex",
            test,
            RE2.Compile(test.pat).FindAllSubmatchIndex(test.text, -1),
            false);
    }

    // The find_test.go benchmarks are ported to Benchmarks.java.
}
