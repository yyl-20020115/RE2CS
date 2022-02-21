/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
/**
 * Some custom asserts and parametric tests.
 *
 * @author afrozm@google.com (Afroz Mohiuddin)
 */
using System.Text;
using NUnit.Framework;

namespace RE2CS.Tests;

[TestFixture]
public class ApiTestUtils
{

    /**
     * Tests that both RE2's and JDK's pattern class act as we expect them. The regular expression
     * {@code regexp} matches the string {@code match} and doesn't match {@code nonMatch}
     */
    public static void TestMatches(string regexp, string match, string nonMatch)
    {
        string errorString = "Pattern with regexp: " + regexp;
        AssertTrue(
            "JDK " + errorString + " doesn't match: " + match,
            System.Text.RegularExpressions.Regex.IsMatch(regexp, match));
        AssertFalse(
            "JDK " + errorString + " matches: " + nonMatch,
            System.Text.RegularExpressions.Regex.IsMatch(regexp, nonMatch));
        AssertTrue(errorString + " doesn't match: " + match, Pattern.Matches(regexp, match));
        AssertFalse(errorString + " matches: " + nonMatch, Pattern.Matches(regexp, nonMatch));

        AssertTrue(
            errorString + " doesn't match: " + match, Pattern.Matches(regexp, getUtf8Bytes(match)));
        AssertFalse(
            errorString + " matches: " + nonMatch, Pattern.Matches(regexp, getUtf8Bytes(nonMatch)));
    }
    public static void AssertTrue(bool v)
    {
        Assert.IsTrue(v);
    }
    public static void AssertFalse(bool v)
    {
        Assert.IsFalse(v);
    }
    public static void AssertTrue(string s, bool v)
    {
        Assert.IsTrue(v, s);
    }
    public static void AssertFalse(string s, bool v)
    {
        Assert.IsFalse(v, s);
    }

    // Test matches via a matcher.
    public static void TestMatcherMatches(string regexp, string match, string nonMatch)
    {
        TestMatcherMatches(regexp, match);
        TestMatcherNotMatches(regexp, nonMatch);
    }

    public static void TestMatcherMatches(string regexp, string match)
    {
        var p = new System.Text.RegularExpressions.Regex(regexp);

        AssertTrue(
            "JDK Pattern with regexp: " + regexp + " doesn't match: " + match,
            p.matcher(match).matches());
        Pattern pr = Pattern.Compile(regexp);
        AssertTrue(
            "Pattern with regexp: " + regexp + " doesn't match: " + match, pr.Matcher(match).Matches());
        AssertTrue(
            "Pattern with regexp: " + regexp + " doesn't match: " + match,
            pr.Matcher(getUtf8Bytes(match)).Matches());
    }

    public static void TestMatcherNotMatches(string regexp, string nonMatch)
    {
        var p = new System.Text.RegularExpressions.Regex(regexp);
        AssertFalse(
            "JDK Pattern with regexp: " + regexp + " matches: " + nonMatch,
            p.matcher(nonMatch).matches());
        Pattern pr = Pattern.Compile(regexp);
        AssertFalse(
            "Pattern with regexp: " + regexp + " matches: " + nonMatch, pr.Matcher(nonMatch).Matches());
        AssertFalse(
            "Pattern with regexp: " + regexp + " matches: " + nonMatch,
            pr.Matcher(getUtf8Bytes(nonMatch)).Matches());
    }

    /**
     * This takes a regex and it's compile time flags, a string that is expected to match the regex
     * and a string that is not expected to match the regex.
     *
     * We don't check for JDK compatibility here, since the flags are not in a 1-1 correspondence.
     *
     */
    public static void TestMatchesRE2(string regexp, int flags, string match, string nonMatch)
    {
        Pattern p = Pattern.Compile(regexp, flags);
        string errorString = "Pattern with regexp: " + regexp + " and flags: " + flags;
        AssertTrue(errorString + " doesn't match: " + match, p.Matches(match));
        AssertTrue(errorString + " doesn't match: " + match, p.Matches(getUtf8Bytes(match)));
        AssertFalse(errorString + " matches: " + nonMatch, p.Matches(nonMatch));
        AssertFalse(errorString + " matches: " + nonMatch, p.Matches(getUtf8Bytes(nonMatch)));
    }

    /**
     * Tests that both RE2 and JDK split the string on the regex in the same way, and that that way
     * matches our expectations.
     */
    public static void TestSplit(string regexp, string text, string[] expected)
    {
        TestSplit(regexp, text, 0, expected);
    }

    public static void TestSplit(string regexp, string text, int limit, string[] expected)
    {
        var p = new System.Text.RegularExpressions.Regex(regexp);
        Assert.AreEqual( p.Split(text, limit),expected);

        Assert.AreEqual(Pattern.Compile(regexp).Split(text, limit),expected);
    }

    // Helper methods for RE2Matcher's test.

    // Tests that both RE2 and JDK's Matchers do the same replaceFist.
    public static void TestReplaceAll(string orig, string regex, string repl, string actual)
    {
        Pattern p = Pattern.Compile(regex);
        string replaced;
        foreach (MatcherInput input in new[] { MatcherInput.Utf16(orig), MatcherInput.Utf8(orig) })
        {
            Matcher m = p.Matcher(input);
            replaced = m.ReplaceAll(repl);
            assertEquals(actual, replaced);
        }

        // JDK's
        java.util.regex.Pattern pj = java.util.regex.Pattern.compile(regex);
        java.util.regex.Matcher mj = pj.matcher(orig);
        replaced = mj.replaceAll(repl);
        assertEquals(actual, replaced);
    }

    // Tests that both RE2 and JDK's Matchers do the same replaceFist.
    public static void TestReplaceFirst(string orig, string regex, string repl, string actual)
    {
        Pattern p = Pattern.Compile(regex);
        string replaced;
        foreach (MatcherInput input in new[] { MatcherInput.Utf16(orig), MatcherInput.Utf8(orig) })
        {
            Matcher m = p.Matcher(orig);
            replaced = m.ReplaceFirst(repl);
            assertEquals(actual, replaced);
        }

        // JDK's
        java.util.regex.Pattern pj = java.util.regex.Pattern.compile(regex);
        java.util.regex.Matcher mj = pj.matcher(orig);
        replaced = mj.replaceFirst(repl);
        assertEquals(actual, replaced);
    }

    // Tests that both RE2 and JDK's Patterns/Matchers give the same groupCount.
    public static void testGroupCount(string pattern, int count)
    {
        // RE2
        Pattern p = Pattern.Compile(pattern);
        Matcher m = p.Matcher("x");
        Matcher m2 = p.Matcher(getUtf8Bytes("x"));
        
        assertEquals(count, p.GroupCount);
        assertEquals(count, m.GroupCount);
        assertEquals(count, m2.GroupCount);

        // JDK
        java.util.regex.Pattern pj = java.util.regex.Pattern.compile(pattern);
        java.util.regex.Matcher mj = pj.matcher("x");
        // java.util.regex.Pattern doesn't have group count in JDK.
        assertEquals(count, mj.groupCount());
    }

    public static void assertEquals(int v1, int v2)
    {
        Assert.AreEqual(v1, v2);
    }
    public static void assertEquals(string v1, string v2)
    {
        Assert.AreEqual(v1, v2);
    }

    public static void TestGroup(string text, string regexp, string[] output)
    {
        // RE2
        Pattern p = Pattern.Compile(regexp);
        foreach(MatcherInput input in new[] { MatcherInput.Utf16(text), MatcherInput.Utf8(text) })
        {
            Matcher matchString = p.matcher(input);
            AssertTrue(matchString.Find());
            assertEquals(output[0], matchString.Group());
            for (int i = 0; i < output.Length; i++)
            {
                assertEquals(output[i], matchString.Group(i));
            }
            assertEquals(output.Length - 1, matchString.GroupCount);
        }

        // JDK
        java.util.regex.Pattern pj = java.util.regex.Pattern.compile(regexp);
        java.util.regex.Matcher matchStringj = pj.matcher(text);
        // java.util.regex.Matcher matchBytes =
        //   p.matcher(text.getBytes(Charsets.UTF_8));
        AssertTrue(matchStringj.find());
        // assertEquals(true, matchBytes.find());
        assertEquals(output[0], matchStringj.group());
        // assertEquals(output[0], matchBytes.group());
        for (int i = 0; i < output.Length; i++)
        {
            assertEquals(output[i], matchStringj.group(i));
            // assertEquals(output[i], matchBytes.group(i));
        }
    }

    public static void TestFind(string text, string regexp, int start, string output)
    {
        // RE2
        Pattern p = Pattern.Compile(regexp);
        foreach (MatcherInput input in new []{ MatcherInput.Utf16(text), MatcherInput.Utf8(text) })
        {
            Matcher matchString = p.matcher(input);
            // RE2Matcher matchBytes = p.matcher(text.getBytes(Charsets.UTF_8));
            AssertTrue(matchString.Find(start));
            // assertTrue(matchBytes.find(start));
            assertEquals(output, matchString.Group());
            // assertEquals(output, matchBytes.group());
        }

        // JDK
        java.util.regex.Pattern pj = java.util.regex.Pattern.compile(regexp);
        java.util.regex.Matcher matchStringj = pj.matcher(text);
        AssertTrue(matchStringj.find(start));
        assertEquals(output, matchStringj.group());
    }

    public static void testFindNoMatch(string text, string regexp, int start)
    {
        // RE2
        Pattern p = Pattern.Compile(regexp);
        foreach (MatcherInput input in new[] { MatcherInput.Utf16(text), MatcherInput.Utf8(text) })
        {
            Matcher matchString = p.matcher(input);
            // RE2Matcher matchBytes = p.matcher(text.getBytes(Charsets.UTF_8));
            AssertFalse(matchString.Find(start));
            // assertFalse(matchBytes.find(start));
        }

        // JDK
        java.util.regex.Pattern pj = java.util.regex.Pattern.compile(regexp);
        java.util.regex.Matcher matchStringj = pj.matcher(text);
        AssertFalse(matchStringj.find(start));
    }

    public static void testInvalidGroup(string text, string regexp, int group)
    {
        Pattern p = Pattern.Compile(regexp);
        Matcher m = p.Matcher(text);
        m.Find();
        m.Group(group);
        fail(); // supposed to have exception by now
    }

    public static void VerifyLookingAt(string text, string regexp, bool output)
    {
        assertEquals(output, Pattern.Compile(regexp).Matcher(text).LookingAt());
        assertEquals(output, Pattern.Compile(regexp).Matcher(getUtf8Bytes(text)).LookingAt());
        assertEquals(output, java.util.regex.Pattern.compile(regexp).matcher(text).lookingAt());
    }

    private static byte[] getUtf8Bytes(string s)
    {
        return Encoding.UTF8.GetBytes(s);
    }
}
