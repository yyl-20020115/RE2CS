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
    public void testCompile()
    {
        Pattern p = Pattern.Compile("abc");
        assertEquals("abc", p.PatternText);
        assertEquals(0, p.Flags);
    }

    [Test]
    public void testCompileExceptionWithDuplicateGroups()
    {
        try
        {
            Pattern.Compile("(?P<any>.*)(?P<any>.*");
            fail();
        }
        catch (PatternSyntaxException e)
        {
            assertEquals("error parsing regexp: duplicate capture group name: `any`", e.Message);
        }
    }

    private void fail()
    {
        throw new NotImplementedException();
    }

    private void assertEquals(string v, string message)
    {
        throw new NotImplementedException();
    }

    [Test]
    public void testToString()
    {
        Pattern p = Pattern.Compile("abc");
        assertEquals("abc", p.toString());
    }

    [Test]
    public void testCompileFlags()
    {
        Pattern p = Pattern.Compile("abc", 5);
        assertEquals("abc", p.PatternText);
        assertEquals(5, p.Flags);
    }

    [Test]
    public void testSyntaxError()
    {
        try
        {
            Pattern.Compile("abc(");
            fail("should have thrown");
        }
        catch (PatternSyntaxException e)
        {
            assertEquals(-1, e.Index);
            assertNotSame("", e.Description);
            assertNotSame("", e.Message);
            assertEquals("abc(", e.Pattern);
        }
    }

    private void assertNotSame(string v1, string v2)
    {
        throw new NotImplementedException();
    }

    private void fail(string v)
    {
        throw new NotImplementedException();
    }

    [Test]
    public void testMatchesNoFlags()
    {
        ApiTestUtils.testMatches("ab+c", "abbbc", "cbbba");
        ApiTestUtils.testMatches("ab.*c", "abxyzc", "ab\nxyzc");
        ApiTestUtils.testMatches("^ab.*c$", "abc", "xyz\nabc\ndef");

        // Test quoted codepoints that require a surrogate pair. See https://github.com/google/re2j/issues/123.
        string source = new StringBuilder().appendCodePoint(110781).toString();
        ApiTestUtils.testMatches(source, source, "blah");
        ApiTestUtils.testMatches("\\Q" + source + "\\E", source, "blah");
    }

    [Test]
    public void testMatchesWithFlags()
    {
        ApiTestUtils.testMatchesRE2("ab+c", 0, "abbbc", "cbba");
        ApiTestUtils.testMatchesRE2("ab+c", Pattern.CASE_INSENSITIVE, "abBBc", "cbbba");
        ApiTestUtils.testMatchesRE2("ab.*c", 0, "abxyzc", "ab\nxyzc");
        ApiTestUtils.testMatchesRE2("ab.*c", Pattern.DOTALL, "ab\nxyzc", "aB\nxyzC");
        ApiTestUtils.testMatchesRE2(
            "ab.*c", Pattern.DOTALL | Pattern.CASE_INSENSITIVE, "aB\nxyzC", "z");
        ApiTestUtils.testMatchesRE2("^ab.*c$", 0, "abc", "xyz\nabc\ndef");

        ApiTestUtils.testMatchesRE2("^ab.*c$", Pattern.MULTILINE, "abc", "xyz\nabc\ndef");
        ApiTestUtils.testMatchesRE2("^ab.*c$", Pattern.MULTILINE, "abc", "");
        ApiTestUtils.testMatchesRE2("^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE, "ab\nc", "AB\nc");
        ApiTestUtils.testMatchesRE2(
            "^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE | Pattern.CASE_INSENSITIVE, "AB\nc", "z");
    }

    private void testFind(string regexp, int flag, string match, string nonMatch)
    {
        assertTrue(Pattern.Compile(regexp, flag).Matcher(match).Find());
        assertFalse(Pattern.Compile(regexp, flag).Matcher(nonMatch).Find());
    }

    [Test]
    public void testFind()
    {
        testFind("ab+c", 0, "xxabbbc", "cbbba");
        testFind("ab+c", Pattern.CASE_INSENSITIVE, "abBBc", "cbbba");
        testFind("ab.*c", 0, "xxabxyzc", "ab\nxyzc");
        testFind("ab.*c", Pattern.DOTALL, "ab\nxyzc", "aB\nxyzC");
        testFind("ab.*c", Pattern.DOTALL | Pattern.CASE_INSENSITIVE, "xaB\nxyzCz", "z");
        testFind("^ab.*c$", 0, "abc", "xyz\nabc\ndef");
        testFind("^ab.*c$", Pattern.MULTILINE, "xyz\nabc\ndef", "xyz\nab\nc\ndef");
        testFind("^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE, "xyz\nab\nc\ndef", "xyz\nAB\nc\ndef");
        testFind(
            "^ab.*c$",
            Pattern.DOTALL | Pattern.MULTILINE | Pattern.CASE_INSENSITIVE,
            "xyz\nAB\nc\ndef",
            "z");
    }

    [Test]
    public void testSplit()
    {
        ApiTestUtils.testSplit("/", "abcde", new string[] { "abcde" });
        ApiTestUtils.testSplit("/", "a/b/cc//d/e//", new string[] { "a", "b", "cc", "", "d", "e" });
        ApiTestUtils.testSplit("/", "a/b/cc//d/e//", 3, new string[] { "a", "b", "cc//d/e//" });
        ApiTestUtils.testSplit("/", "a/b/cc//d/e//", 4, new string[] { "a", "b", "cc", "/d/e//" });
        ApiTestUtils.testSplit("/", "a/b/cc//d/e//", 5, new string[] { "a", "b", "cc", "", "d/e//" });
        ApiTestUtils.testSplit("/", "a/b/cc//d/e//", 6, new string[] { "a", "b", "cc", "", "d", "e//" });
        ApiTestUtils.testSplit(
            "/", "a/b/cc//d/e//", 7, new string[] { "a", "b", "cc", "", "d", "e", "/" });
        ApiTestUtils.testSplit(
            "/", "a/b/cc//d/e//", 8, new string[] { "a", "b", "cc", "", "d", "e", "", "" });
        ApiTestUtils.testSplit(
            "/", "a/b/cc//d/e//", 9, new string[] { "a", "b", "cc", "", "d", "e", "", "" });

        // The tests below are listed at
        // http://docs.oracle.com/javase/1.5.0/docs/api/java/util/regex/Pattern.html#split(java.lang.CharSequence, int)

        string s = "boo:and:foo";
        string regexp1 = ":";
        string regexp2 = "o";

        ApiTestUtils.testSplit(regexp1, s, 2, new string[] { "boo", "and:foo" });
        ApiTestUtils.testSplit(regexp1, s, 5, new string[] { "boo", "and", "foo" });
        ApiTestUtils.testSplit(regexp1, s, -2, new string[] { "boo", "and", "foo" });
        ApiTestUtils.testSplit(regexp2, s, 5, new string[] { "b", "", ":and:f", "", "" });
        ApiTestUtils.testSplit(regexp2, s, -2, new string[] { "b", "", ":and:f", "", "" });
        ApiTestUtils.testSplit(regexp2, s, 0, new string[] { "b", "", ":and:f" });
        ApiTestUtils.testSplit(regexp2, s, new string[] { "b", "", ":and:f" });
    }

    [Test]
    public void testGroupCount()
    {
        // It is a simple delegation, but still test it.
        ApiTestUtils.testGroupCount("(.*)ab(.*)a", 2);
        ApiTestUtils.testGroupCount("(.*)(ab)(.*)a", 3);
        ApiTestUtils.testGroupCount("(.*)((a)b)(.*)a", 4);
        ApiTestUtils.testGroupCount("(.*)(\\(ab)(.*)a", 3);
        ApiTestUtils.testGroupCount("(.*)(\\(a\\)b)(.*)a", 3);
    }

    [Test]
    public void testNamedGroups()
    {
        assertNamedGroupsEquals(Collections.< string, Integer > emptyMap(), "hello");
        assertNamedGroupsEquals(Collections.< string, Integer > emptyMap(), "(.*)");
        assertNamedGroupsEquals(ImmutableMap.of("any", 1), "(?P<any>.*)");
        assertNamedGroupsEquals(ImmutableMap.of("foo", 1, "bar", 2), "(?P<foo>.*)(?P<bar>.*)");
    }

    private static void assertNamedGroupsEquals(Dictionary<string, Integer> expected, string pattern)
    {
        assertEquals(expected, Pattern.Compile(pattern).NamedGroups);
    }

    // See https://github.com/google/re2j/issues/93.
    [Test]
    public void testIssue93()
    {
        Pattern p1 = Pattern.Compile("(a.*?c)|a.*?b");
        Pattern p2 = Pattern.Compile("a.*?c|a.*?b");

        Matcher m1 = p1.Matcher("abc");
        m1.Find();
        Matcher m2 = p2.Matcher("abc");
        m2.Find();

        assertThat(m2.Group()).isEqualTo(m1.Group());
    }

    [Test]
    public void testQuote()
    {
        ApiTestUtils.testMatchesRE2(Pattern.Quote("ab+c"), 0, "ab+c", "abc");
    }

    private Pattern reserialize(Pattern object)
    {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        try
        {
            ObjectOutputStream out = new ObjectOutputStream(bytes);
      out.writeObject(object);
            ObjectInputStream in = new ObjectInputStream(new ByteArrayInputStream(bytes.toByteArray()));
            return (Pattern) in.readObject();
        }
        catch (IOException e)
        {
            throw new RuntimeException(e);
        }
        catch (ClassNotFoundException e)
        {
            throw new RuntimeException(e);
        }
    }

    private void assertSerializes(Pattern p)
    {
        Pattern reserialized = reserialize(p);
        assertEquals(p.PatternText, reserialized.PatternText);
        assertEquals(p.Flags, reserialized.Flags);
    }

    [Test]
    public void testSerialize()
    {
        assertSerializes(Pattern.Compile("ab+c"));
        assertSerializes(Pattern.Compile("^ab.*c$", Pattern.DOTALL | Pattern.MULTILINE));
        assertFalse(reserialize(Pattern.Compile("abc")).Matcher("def").Find());
    }

    [Test]
    public void testEquals()
    {
        Pattern pattern1 = Pattern.Compile("abc");
        Pattern pattern2 = Pattern.Compile("abc");
        Pattern pattern3 = Pattern.Compile("def");
        Pattern pattern4 = Pattern.Compile("abc", Pattern.CASE_INSENSITIVE);
        assertThat(pattern1).isEqualTo(pattern2);
        assertThat(pattern1).isNotEqualTo(pattern3);
        assertThat(pattern1.hashCode()).isEqualTo(pattern2.hashCode());
        assertThat(pattern1).isNotEqualTo(pattern4);
    }
}
