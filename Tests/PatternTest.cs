/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;
using System.Text;

namespace RE2CS.Tests;
/**
 * This class checks that the behaviour of Pattern and JDK's Pattern are same, and we expect them
 * that way too.
 *
 * @author afrozm@google.com (Afroz Mohiuddin)
 */
public class PatternTest
{

    [Test]
    public void TestCompile()
    {
        Pattern p = Pattern.Compile("abc");
        AssertEquals("abc", p.PatternText);
        AssertEquals(0, p.Flags);
    }

    [Test]
    public void TestCompileExceptionWithDuplicateGroups()
    {
        try
        {
            Pattern.Compile("(?P<any>.*)(?P<any>.*");
            Fail();
        }
        catch (PatternSyntaxException e)
        {
            AssertEquals("error parsing regexp: duplicate capture group name: `any`", e.Message);
        }
    }

    private void Fail()
    {
        Assert.Fail();
    }
    private void AssertEquals(string v1, string v2)
    {
        Assert.AreEqual(v1, v2);
    }

    private void AssertEquals(int v1,int v2)
    {
        Assert.AreEqual(v1, v2);
    }

    [Test]
    public void TestToString()
    {
        Pattern p = Pattern.Compile("abc");
        AssertEquals("abc", p.ToString());
    }

    [Test]
    public void TestCompileFlags()
    {
        Pattern p = Pattern.Compile("abc", 5);
        AssertEquals("abc", p.PatternText);
        AssertEquals(5, p.Flags);
    }

    [Test]
    public void TestSyntaxError()
    {
        try
        {
            Pattern.Compile("abc(");
            Fail("should have thrown");
        }
        catch (PatternSyntaxException e)
        {
            AssertEquals(-1, e.Index);
            AssertNotSame("", e.Description);
            AssertNotSame("", e.Message);
            AssertEquals("abc(", e.Pattern);
        }
    }

    private void AssertNotSame(string v1, string v2)
    {
        Assert.AreNotSame(v1, v2);
    }

    private void Fail(string v)
    {
        Assert.Fail(v);
    }

    [Test]
    public void TestMatchesNoFlags()
    {
        ApiTestUtils.TestMatches("ab+c", "abbbc", "cbbba");
        ApiTestUtils.TestMatches("ab.*c", "abxyzc", "ab\nxyzc");
        ApiTestUtils.TestMatches("^ab.*c$", "abc", "xyz\nabc\ndef");

        // Test quoted codepoints that require a surrogate pair. See https://github.com/google/re2j/issues/123.
        string source = char.ConvertFromUtf32(110781);
        ApiTestUtils.TestMatches(source, source, "blah");
        ApiTestUtils.TestMatches("\\Q" + source + "\\E", source, "blah");
    }

    [Test]
    public void TestMatchesWithFlags()
    {
        ApiTestUtils.TestMatchesRE2("ab+c", 0, "abbbc", "cbba");
        ApiTestUtils.TestMatchesRE2("ab+c", Pattern.CASE_INSENSITIVE, "abBBc", "cbbba");
        ApiTestUtils.TestMatchesRE2("ab.*c", 0, "abxyzc", "ab\nxyzc");
        ApiTestUtils.TestMatchesRE2("ab.*c", Pattern.DOTALL, "ab\nxyzc", "aB\nxyzC");
        ApiTestUtils.TestMatchesRE2(
            "ab.*c", Pattern.DOTALL | Pattern.CASE_INSENSITIVE, "aB\nxyzC", "z");
        ApiTestUtils.TestMatchesRE2("^ab.*c$", 0, "abc", "xyz\nabc\ndef");

        ApiTestUtils.TestMatchesRE2("^ab.*c$", Pattern.MULTILINE, "abc", "xyz\nabc\ndef");
        ApiTestUtils.TestMatchesRE2("^ab.*c$", Pattern.MULTILINE, "abc", "");
        ApiTestUtils.TestMatchesRE2("^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE, "ab\nc", "AB\nc");
        ApiTestUtils.TestMatchesRE2(
            "^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE | Pattern.CASE_INSENSITIVE, "AB\nc", "z");
    }

    private void TestFind(string regexp, int flag, string match, string nonMatch)
    {
        AssertTrue(Pattern.Compile(regexp, flag).Matcher(match).Find());
        AssertFalse(Pattern.Compile(regexp, flag).Matcher(nonMatch).Find());
    }

    private void AssertFalse(bool v)
    {
        Assert.IsFalse(v);
    }

    private void AssertTrue(bool v)
    {
        Assert.IsTrue(v);
    }

    [Test]
    public void TestFind()
    {
        TestFind("ab+c", 0, "xxabbbc", "cbbba");
        TestFind("ab+c", Pattern.CASE_INSENSITIVE, "abBBc", "cbbba");
        TestFind("ab.*c", 0, "xxabxyzc", "ab\nxyzc");
        TestFind("ab.*c", Pattern.DOTALL, "ab\nxyzc", "aB\nxyzC");
        TestFind("ab.*c", Pattern.DOTALL | Pattern.CASE_INSENSITIVE, "xaB\nxyzCz", "z");
        TestFind("^ab.*c$", 0, "abc", "xyz\nabc\ndef");
        TestFind("^ab.*c$", Pattern.MULTILINE, "xyz\nabc\ndef", "xyz\nab\nc\ndef");
        TestFind("^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE, "xyz\nab\nc\ndef", "xyz\nAB\nc\ndef");
        TestFind(
            "^ab.*c$",
            Pattern.DOTALL | Pattern.MULTILINE | Pattern.CASE_INSENSITIVE,
            "xyz\nAB\nc\ndef",
            "z");
    }

    [Test]
    public void TestSplit()
    {
        ApiTestUtils.TestSplit("/", "abcde", new string[] { "abcde" });
        ApiTestUtils.TestSplit("/", "a/b/cc//d/e//", new string[] { "a", "b", "cc", "", "d", "e" });
        ApiTestUtils.TestSplit("/", "a/b/cc//d/e//", 3, new string[] { "a", "b", "cc//d/e//" });
        ApiTestUtils.TestSplit("/", "a/b/cc//d/e//", 4, new string[] { "a", "b", "cc", "/d/e//" });
        ApiTestUtils.TestSplit("/", "a/b/cc//d/e//", 5, new string[] { "a", "b", "cc", "", "d/e//" });
        ApiTestUtils.TestSplit("/", "a/b/cc//d/e//", 6, new string[] { "a", "b", "cc", "", "d", "e//" });
        ApiTestUtils.TestSplit(
            "/", "a/b/cc//d/e//", 7, new string[] { "a", "b", "cc", "", "d", "e", "/" });
        ApiTestUtils.TestSplit(
            "/", "a/b/cc//d/e//", 8, new string[] { "a", "b", "cc", "", "d", "e", "", "" });
        ApiTestUtils.TestSplit(
            "/", "a/b/cc//d/e//", 9, new string[] { "a", "b", "cc", "", "d", "e", "", "" });

        // The tests below are listed at
        // http://docs.oracle.com/javase/1.5.0/docs/api/java/util/regex/Pattern.html#split(java.lang.CharSequence, int)

        string s = "boo:and:foo";
        string regexp1 = ":";
        string regexp2 = "o";

        ApiTestUtils.TestSplit(regexp1, s, 2, new string[] { "boo", "and:foo" });
        ApiTestUtils.TestSplit(regexp1, s, 5, new string[] { "boo", "and", "foo" });
        ApiTestUtils.TestSplit(regexp1, s, -2, new string[] { "boo", "and", "foo" });
        ApiTestUtils.TestSplit(regexp2, s, 5, new string[] { "b", "", ":and:f", "", "" });
        ApiTestUtils.TestSplit(regexp2, s, -2, new string[] { "b", "", ":and:f", "", "" });
        ApiTestUtils.TestSplit(regexp2, s, 0, new string[] { "b", "", ":and:f" });
        ApiTestUtils.TestSplit(regexp2, s, new string[] { "b", "", ":and:f" });
    }

    [Test]
    public void TestGroupCount()
    {
        // It is a simple delegation, but still test it.
        ApiTestUtils.testGroupCount("(.*)ab(.*)a", 2);
        ApiTestUtils.testGroupCount("(.*)(ab)(.*)a", 3);
        ApiTestUtils.testGroupCount("(.*)((a)b)(.*)a", 4);
        ApiTestUtils.testGroupCount("(.*)(\\(ab)(.*)a", 3);
        ApiTestUtils.testGroupCount("(.*)(\\(a\\)b)(.*)a", 3);
    }

    [Test]
    public void TestNamedGroups()
    {
        AssertNamedGroupsEquals(new Dictionary<string, int>(), "hello");
        AssertNamedGroupsEquals(new Dictionary<string, int>(), "(.*)");
        AssertNamedGroupsEquals(new Dictionary<string, int>() { ["any"]=1}, "(?P<any>.*)");
        AssertNamedGroupsEquals(new Dictionary<string, int>() { ["foo"] = 1,["bar"]=2 }, "(?P<foo>.*)(?P<bar>.*)");
    }

    private static void AssertNamedGroupsEquals(Dictionary<string, int> expected, string pattern)
    {
        Assert.AreEqual(expected, Pattern.Compile(pattern).NamedGroups);
    }
    // See https://github.com/google/re2j/issues/93.
    [Test]
    public void TestIssue93()
    {
        Pattern p1 = Pattern.Compile("(a.*?c)|a.*?b");
        Pattern p2 = Pattern.Compile("a.*?c|a.*?b");

        Matcher m1 = p1.Matcher("abc");
        m1.Find();
        Matcher m2 = p2.Matcher("abc");
        m2.Find();
        Assert.AreEqual(m2.Group(), m1.Group());
    }

    [Test]
    public void TestQuote()
    {
        ApiTestUtils.TestMatchesRE2(Pattern.Quote("ab+c"), 0, "ab+c", "abc");
    }

    private Pattern Reserialize(Pattern o)
    {
        return o;
    }

    private void AssertSerializes(Pattern p)
    {
        Pattern reserialized = Reserialize(p);
        AssertEquals(p.PatternText, reserialized.PatternText);
        AssertEquals(p.Flags, reserialized.Flags);
    }

    [Test]
    public void TestSerialize()
    {
        AssertSerializes(Pattern.Compile("ab+c"));
        AssertSerializes(Pattern.Compile("^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE));
        AssertFalse(Reserialize(Pattern.Compile("abc")).Matcher("def").Find());
    }

    [Test]
    public void TestEquals()
    {
        Pattern pattern1 = Pattern.Compile("abc");
        Pattern pattern2 = Pattern.Compile("abc");
        Pattern pattern3 = Pattern.Compile("def");
        Pattern pattern4 = Pattern.Compile("abc", Pattern.CASE_INSENSITIVE);
        Assert.AreEqual(pattern1, pattern2);
        Assert.AreNotEqual(pattern1, pattern2);
        Assert.AreEqual(pattern1.GetHashCode(), pattern2.GetHashCode());
        Assert.AreNotEqual(pattern1.GetHashCode(), pattern2.GetHashCode());

    }
}
