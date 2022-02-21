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
 * Testing the RE2Matcher class.
 *
 * @author afrozm@google.com (Afroz Mohiuddin)
 */
[TestFixture]
public class MatcherTest
{

    [Test]
    public void testLookingAt()
    {
        ApiTestUtils.verifyLookingAt("abcdef", "abc", true);
        ApiTestUtils.verifyLookingAt("ab", "abc", false);
    }

    [Test]
    public void testMatches()
    {
        ApiTestUtils.testMatcherMatches("ab+c", "abbbc", "cbbba");
        ApiTestUtils.testMatcherMatches("ab.*c", "abxyzc", "ab\nxyzc");
        ApiTestUtils.testMatcherMatches("^ab.*c$", "abc", "xyz\nabc\ndef");
        ApiTestUtils.testMatcherMatches("ab+c", "abbbc", "abbcabc");
    }

    [Test]
    public void testReplaceAll()
    {
        ApiTestUtils.testReplaceAll(
            "What the Frog's Eye Tells the Frog's Brain",
            "Frog",
            "Lizard",
            "What the Lizard's Eye Tells the Lizard's Brain");
        ApiTestUtils.testReplaceAll(
            "What the Frog's Eye Tells the Frog's Brain",
            "F(rog)",
            "\\$Liza\\rd$1",
            "What the $Lizardrog's Eye Tells the $Lizardrog's Brain");
        ApiTestUtils.testReplaceAll(
            "abcdefghijklmnopqrstuvwxyz123",
            "(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)",
            "$10$20",
            "jb0wo0123");
        ApiTestUtils.testReplaceAll(
            "\u00e1\u0062\u00e7\u2655", "(.)", "<$1>", "<\u00e1><\u0062><\u00e7><\u2655>");
        ApiTestUtils.testReplaceAll(
            "\u00e1\u0062\u00e7\u2655", "[\u00e0-\u00e9]", "<$0>", "<\u00e1>\u0062<\u00e7>\u2655");
        ApiTestUtils.testReplaceAll("hello world", "z*", "x", "xhxexlxlxox xwxoxrxlxdx");
        // test replaceAll with alternation
        ApiTestUtils.testReplaceAll("123:foo", "(?:\\w+|\\d+:foo)", "x", "x:x");
        ApiTestUtils.testReplaceAll("123:foo", "(?:\\d+:foo|\\w+)", "x", "x");
        ApiTestUtils.testReplaceAll("aab", "a*", "<$0>", "<aa><>b<>");
        ApiTestUtils.testReplaceAll("aab", "a*?", "<$0>", "<>a<>a<>b<>");
    }

    [Test]
    public void testReplaceFirst()
    {
        ApiTestUtils.testReplaceFirst(
            "What the Frog's Eye Tells the Frog's Brain",
            "Frog",
            "Lizard",
            "What the Lizard's Eye Tells the Frog's Brain");
        ApiTestUtils.testReplaceFirst(
            "What the Frog's Eye Tells the Frog's Brain",
            "F(rog)",
            "\\$Liza\\rd$1",
            "What the $Lizardrog's Eye Tells the Frog's Brain");
        ApiTestUtils.testReplaceFirst(
            "abcdefghijklmnopqrstuvwxyz123",
            "(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)",
            "$10$20",
            "jb0nopqrstuvwxyz123");
        ApiTestUtils.testReplaceFirst(
            "\u00e1\u0062\u00e7\u2655", "(.)", "<$1>", "<\u00e1>\u0062\u00e7\u2655");
        ApiTestUtils.testReplaceFirst(
            "\u00e1\u0062\u00e7\u2655", "[\u00e0-\u00e9]", "<$0>", "<\u00e1>\u0062\u00e7\u2655");
        ApiTestUtils.testReplaceFirst("hello world", "z*", "x", "xhello world");
        ApiTestUtils.testReplaceFirst("aab", "a*", "<$0>", "<aa>b");
        ApiTestUtils.testReplaceFirst("aab", "a*?", "<$0>", "<>aab");
    }

    [Test]
    public void testGroupCount()
    {
        ApiTestUtils.testGroupCount("(a)(b(c))d?(e)", 4);
    }

    [Test]
    public void testGroup()
    {
        // ApiTestUtils.testGroup("xabdez", "(a)(b(c)?)d?(e)", new string[] {"abde", "a", "b", null, "e"});
        // ApiTestUtils.testGroup("abc", "(a)(b$)?(b)?", new string[] {"ab", "a", null, "b"});
        // ApiTestUtils.testGroup("abc", "(^b)?(b)?c", new string[] {"bc", null, "b"});
        // ApiTestUtils.testGroup(" a b", "\\b(.).\\b", new string[] {"a ", "a"});

        // Not allowed to use UTF-8 except in comments, per Java style guide.
        // ("αβξδεφγ", "(.)(..)(...)", new string[] {"αβξδεφ", "α", "βξ", "δεφ"});
        ApiTestUtils.testGroup(
            "\u03b1\u03b2\u03be\u03b4\u03b5\u03c6\u03b3",
            "(.)(..)(...)",
            new string[] {
          "\u03b1\u03b2\u03be\u03b4\u03b5\u03c6", "\u03b1", "\u03b2\u03be", "\u03b4\u03b5\u03c6"
            });
    }

    [Test]
    public void testFind()
    {
        ApiTestUtils.testFind("abcdefgh", ".*[aeiou]", 0, "abcde");
        ApiTestUtils.testFind("abcdefgh", ".*[aeiou]", 1, "bcde");
        ApiTestUtils.testFind("abcdefgh", ".*[aeiou]", 2, "cde");
        ApiTestUtils.testFind("abcdefgh", ".*[aeiou]", 3, "de");
        ApiTestUtils.testFind("abcdefgh", ".*[aeiou]", 4, "e");
        ApiTestUtils.testFindNoMatch("abcdefgh", ".*[aeiou]", 5);
        ApiTestUtils.testFindNoMatch("abcdefgh", ".*[aeiou]", 6);
        ApiTestUtils.testFindNoMatch("abcdefgh", ".*[aeiou]", 7);
    }

    [Test]
    public void testInvalidFind()
    {
        try
        {
            ApiTestUtils.testFind("abcdef", ".*", 10, "xxx");
            fail();
        }
        catch (IndexOutOfRangeException e)
        {
            /* ok */
        }
    }

    [Test]
    public void testInvalidReplacement()
    {
        try
        {
            ApiTestUtils.testReplaceFirst("abc", "abc", "$4", "xxx");
            fail();
        }
        catch (IndexOutOfRangeException e)
        {
            /* ok */
            assertTrue(true);
        }
    }

    private void assertTrue(bool v)
    {
        throw new NotImplementedException();
    }

    private void fail()
    {
        throw new NotImplementedException();
    }

    [Test]
    public void testInvalidGroupNoMatch()
    {
        try
        {
            ApiTestUtils.testInvalidGroup("abc", "xxx", 0);
            fail();
        }
        catch (Exception e)
        {
            // Linter complains on empty catch block.
            assertTrue(true);
        }
    }

    [Test]
    public void testInvalidGroupOutOfRange()
    {
        try
        {
            ApiTestUtils.testInvalidGroup("abc", "abc", 1);
            fail();
        }
        catch (IndexOutOfRangeException e)
        {
            // Linter complains on empty catch block.
            assertTrue(true);
        }
    }

    /**
     * Test the NullReferenceException is thrown on null input.
     */
    [Test]
    public void testThrowsOnNullInputReset()
    {
        // null in constructor.
        try
        {
            new Matcher(Pattern.compile("pattern"), (string)null);
            fail();
        }
        catch (NullReferenceException n)
        {
            // Linter complains on empty catch block.
            assertTrue(true);
        }
    }

    [Test]
    public void testThrowsOnNullInputCtor()
    {
        // null in constructor.
        try
        {
            new Matcher(null, "input");
            fail();
        }
        catch (NullReferenceException n)
        {
            // Linter complains on empty catch block.
            assertTrue(true);
        }
    }

    /**
     * Test that InvalidOperationException is thrown if start/end are called before calling find
     */
    [Test]
    public void testStartEndBeforeFind()
    {
        try
        {
            Matcher m = Pattern.compile("a").matcher("abaca");
            m.Start();
            fail();
        }
        catch (InvalidOperationException ise)
        {
            assertTrue(true);
        }
    }

    /**
     * Test for b/6891357. Basically matches should behave like find when it comes to updating the
     * information of the match.
     */
    [Test]
    public void testMatchesUpdatesMatchInformation()
    {
        Matcher m = Pattern.compile("a+").matcher("aaa");
        if (m.Matches())
        {
            assertEquals("aaa", m.Group(0));
        }
    }

    private void assertEquals(string v1, string v2)
    {
        throw new NotImplementedException();
    }

    /**
     * Test for b/6891133. Test matches in case of alternation.
     */
    [Test]
    public void testAlternationMatches()
    {
        string s = "123:foo";
        assertTrue(Pattern.compile("(?:\\w+|\\d+:foo)").matcher(s).Matches());
        assertTrue(Pattern.compile("(?:\\d+:foo|\\w+)").matcher(s).Matches());
    }

    void helperTestMatchEndUTF16(string s, int num, int end)
    {
        string pattern = "[" + s + "]";
        RE2 re =
            new RE2(pattern)
    //{
    //  @Override
    //      public bool match(
    //      CharSequence input, int start, int e, int anchor, int[] group, int ngroup)
    //{
    //    assertEquals(end, e);
    //    return super.match(input, start, e, anchor, group, ngroup);
    //}
    //}
    ;
        Pattern pat = new Pattern(pattern, 0, re);
        Matcher m = pat.matcher(s);

        int found = 0;
        while (m.Find()) {
            found++;
        }
        assertEquals(
                "Matches Expected " + num + " but found " + found + ", for input " + string, num, found);
    }

    /**
     * Test for variable Length encoding, test whether RE2's match function gets the required
     * parameter based on UTF16 codes and not chars and Runes.
     */
    [Test]
    public void testMatchEndUTF16()
    {
        // Latin alphabetic chars such as these 5 lower-case, acute vowels have multi-byte UTF-8
        // encodings but fit in a single UTF-16 code, so the match is at UTF16 offset 5.
        string vowels = "\225\233\237\243\250";
        helperTestMatchEndUTF16(vowels, 5, 5);

        // But surrogates are encoded as two UTF16 codes, so we should expect match
        // to get 6 rather than 3.
        string utf16 =
            new StringBuilder()
                .appendCodePoint(0x10000)
                .appendCodePoint(0x10001)
                .appendCodePoint(0x10002)
                .toString();
        assertEquals(utf16, "\uD800\uDC00\uD800\uDC01\uD800\uDC02");
        helperTestMatchEndUTF16(utf16, 3, 6);
    }

    [Test]
    public void testAppendTail_StringBuffer()
    {
        Pattern p = Pattern.compile("cat");
        Matcher m = p.matcher("one cat two cats in the yard");
        StringBuffer sb = new StringBuffer();
        while (m.Find())
        {
            m.appendReplacement(sb, "dog");
        }
        m.appendTail(sb);
        m.appendTail(sb);
        assertEquals("one dog two dogs in the yards in the yard", sb.toString());
    }

    [Test]
    public void testAppendTail_StringBuilder()
    {
        Pattern p = Pattern.compile("cat");
        Matcher m = p.matcher("one cat two cats in the yard");
        StringBuilder sb = new StringBuilder();
        while (m.Find())
        {
            m.AppendReplacement(sb, "dog");
        }
        m.AppendTail(sb);
        m.AppendTail(sb);
        assertEquals("one dog two dogs in the yards in the yard", sb.toString());
    }

    [Test]
    public void testResetOnFindInt_StringBuffer()
    {
        StringBuffer buffer;
        Matcher matcher = Pattern.compile("a").matcher("zza");

        assertTrue(matcher.Find());

        buffer = new StringBuffer();
        matcher.appendReplacement(buffer, "foo");
        assertEquals("1st time", "zzfoo", buffer.toString());

        assertTrue(matcher.Find(0));

        buffer = new StringBuffer();
        matcher.appendReplacement(buffer, "foo");
        assertEquals("2nd time", "zzfoo", buffer.toString());
    }

    [Test]
    public void testResetOnFindInt_StringBuilder()
    {
        StringBuilder buffer;
        Matcher matcher = Pattern.compile("a").matcher("zza");

        assertTrue(matcher.Find());

        buffer = new StringBuilder();
        matcher.AppendReplacement(buffer, "foo");
        assertEquals("1st time", "zzfoo", buffer.toString());

        assertTrue(matcher.Find(0));

        buffer = new StringBuilder();
        matcher.AppendReplacement(buffer, "foo");
        assertEquals("2nd time", "zzfoo", buffer.toString());
    }

    [Test]
    public void testEmptyReplacementGroups_StringBuffer()
    {
        StringBuffer buffer = new StringBuffer();
        Matcher matcher = Pattern.compile("(a)(b$)?(b)?").matcher("abc");
        assertTrue(matcher.Find());
        matcher.appendReplacement(buffer, "$1-$2-$3");
        assertEquals("a--b", buffer.toString());
        matcher.appendTail(buffer);
        assertEquals("a--bc", buffer.toString());

        buffer = new StringBuffer();
        matcher = Pattern.compile("(a)(b$)?(b)?").matcher("ab");
        assertTrue(matcher.Find());
        matcher.appendReplacement(buffer, "$1-$2-$3");
        matcher.appendTail(buffer);
        assertEquals("a-b-", buffer.toString());

        buffer = new StringBuffer();
        matcher = Pattern.compile("(^b)?(b)?c").matcher("abc");
        assertTrue(matcher.Find());
        matcher.appendReplacement(buffer, "$1-$2");
        matcher.appendTail(buffer);
        assertEquals("a-b", buffer.toString());

        buffer = new StringBuffer();
        matcher = Pattern.compile("^(.)[^-]+(-.)?(.*)").matcher("Name");
        assertTrue(matcher.Find());
        matcher.appendReplacement(buffer, "$1$2");
        matcher.appendTail(buffer);
        assertEquals("N", buffer.toString());
    }

    [Test]
    public void testEmptyReplacementGroups_StringBuilder()
    {
        StringBuilder buffer = new StringBuilder();
        Matcher matcher = Pattern.compile("(a)(b$)?(b)?").matcher("abc");
        assertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2-$3");
        assertEquals("a--b", buffer.toString());
        matcher.AppendTail(buffer);
        assertEquals("a--bc", buffer.toString());

        buffer = new StringBuilder();
        matcher = Pattern.compile("(a)(b$)?(b)?").matcher("ab");
        assertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2-$3");
        matcher.AppendTail(buffer);
        assertEquals("a-b-", buffer.toString());

        buffer = new StringBuilder();
        matcher = Pattern.compile("(^b)?(b)?c").matcher("abc");
        assertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2");
        matcher.AppendTail(buffer);
        assertEquals("a-b", buffer.toString());

        buffer = new StringBuilder();
        matcher = Pattern.compile("^(.)[^-]+(-.)?(.*)").matcher("Name");
        assertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1$2");
        matcher.AppendTail(buffer);
        assertEquals("N", buffer.toString());
    }

    // This example is documented in the com.google.re2j package.html.
    [Test]
    public void testDocumentedExample()
    {
        Pattern p = Pattern.compile("b(an)*(.)");
        Matcher m = p.matcher("by, band, banana");
        assertTrue(m.LookingAt());
        m.Reset();
        assertTrue(m.Find());
        assertEquals("by", m.Group(0));
        assertNull(m.Group(1));
        assertEquals("y", m.Group(2));
        assertTrue(m.Find());
        assertEquals("band", m.Group(0));
        assertEquals("an", m.Group(1));
        assertEquals("d", m.Group(2));
        assertTrue(m.Find());
        assertEquals("banana", m.Group(0));
        assertEquals("an", m.Group(1));
        assertEquals("a", m.Group(2));
        assertFalse(m.Find());
    }

    [Test]
    public void testMutableCharSequence()
    {
        Pattern p = Pattern.compile("b(an)*(.)");
        StringBuilder b = new StringBuilder("by, band, banana");
        Matcher m = p.matcher(b);
        assertTrue(m.Find(0));
        int start = b.indexOf("ban");
        b.replace(b.indexOf("ban"), start + 3, "b");
        assertTrue(m.find(b.indexOf("ban")));
    }

    [Test]
    public void testNamedGroups()
    {
        Pattern p =
            Pattern.compile(
                "(?P<baz>f(?P<foo>b*a(?P<another>r+)){0,10})" + "(?P<bag>bag)?(?P<nomatch>zzz)?");
        Matcher m = p.matcher("fbbarrrrrbag");
        assertTrue(m.Matches());
        assertEquals("fbbarrrrr", m.Group("baz"));
        assertEquals("bbarrrrr", m.Group("foo"));
        assertEquals("rrrrr", m.Group("another"));
        assertEquals(0, m.start("baz"));
        assertEquals(1, m.start("foo"));
        assertEquals(4, m.start("another"));
        assertEquals(9, m.End("baz"));
        assertEquals(9, m.End("foo"));
        assertEquals("bag", m.Group("bag"));
        assertEquals(9, m.start("bag"));
        assertEquals(12, m.End("bag"));
        assertNull(m.Group("nomatch"));
        assertEquals(-1, m.start("nomatch"));
        assertEquals(-1, m.End("nomatch"));
        assertEquals("whatbbarrrrreverbag", appendReplacement(m, "what$2ever${bag}"));

        try
        {
            m.Group("nonexistent");
            fail("Should have thrown IllegalArgumentException");
        }
        catch (IllegalArgumentException expected)
        {
            // Expected
        }
    }

    private string appendReplacement(Matcher m, string replacement)
    {
        StringBuilder b = new StringBuilder();
        m.AppendReplacement(b, replacement);
        return b.toString();
    }

    // See https://github.com/google/re2j/issues/96.
    // Ensures that RE2J generates the correct zero-width assertions (e.g. END_LINE, END_TEXT) when matching on
    // a Substring of a larger input. For example:
    //
    // pattern: (\d{2} ?(\d|[a-z])?)($|[^a-zA-Z])
    // input: "22 bored"
    //
    // pattern.find(input) is true matcher.group(0) will contain "22 b". When retrieving group(1) from this matcher,
    // RE2J re-matches the group, but only considers "22 b" as the input. If it incorrectly treats 'b' as END_OF_LINE
    // and END_OF_TEXT, then group(1) will contain "22 b" when it should actually contain "22".
    [Test]
    public void testGroupZeroWidthAssertions()
    {
        Matcher m = Pattern.compile("(\\d{2} ?(\\d|[a-z])?)($|[^a-zA-Z])").matcher("22 bored");
        Truth.assertThat(m.Find()).isTrue();
        Truth.assertThat(m.Group(1)).isEqualTo("22");
    }

    [Test]
    public void testPatternLongestMatch()
    {
        string pattern = "(?:a+)|(?:a+ b+)";
        string text = "xxx aaa bbb yyy";
        {
            Matcher matcher = Pattern.compile(pattern).matcher(text);
            assertTrue(matcher.Find());
            assertEquals("aaa", text.Substring(matcher.Start(), matcher.End()));
        }
        {
            Matcher matcher = Pattern.compile(pattern, Pattern.LONGEST_MATCH).matcher(text);
            assertTrue(matcher.Find());
            assertEquals("aaa bbb", text.Substring(matcher.Start(), matcher.End()));
        }
    }
}
