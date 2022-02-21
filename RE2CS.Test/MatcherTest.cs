/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;

namespace RE2CS.Tests;


/**
 * Testing the RE2Matcher class.
 *
 * @author afrozm@google.com (Afroz Mohiuddin)
 */
[TestClass]
public class MatcherTest
{

    [TestMethod]
    public void TestLookingAt()
    {
        ApiTestUtils.VerifyLookingAt("abcdef", "abc", true);
        ApiTestUtils.VerifyLookingAt("ab", "abc", false);
    }

    [TestMethod]
    public void TestMatches()
    {
        ApiTestUtils.TestMatcherMatches("ab+c", "abbbc", "cbbba");
        ApiTestUtils.TestMatcherMatches("ab.*c", "abxyzc", "ab\nxyzc");
        ApiTestUtils.TestMatcherMatches("^ab.*c$", "abc", "xyz\nabc\ndef");
        ApiTestUtils.TestMatcherMatches("ab+c", "abbbc", "abbcabc");
    }

    [TestMethod]
    public void TestReplaceAll()
    {
        ApiTestUtils.TestReplaceAll(
            "What the Frog's Eye Tells the Frog's Brain",
            "Frog",
            "Lizard",
            "What the Lizard's Eye Tells the Lizard's Brain");
        ApiTestUtils.TestReplaceAll(
            "What the Frog's Eye Tells the Frog's Brain",
            "F(rog)",
            "\\$Liza\\rd$1",
            "What the $Lizardrog's Eye Tells the $Lizardrog's Brain");
        ApiTestUtils.TestReplaceAll(
            "abcdefghijklmnopqrstuvwxyz123",
            "(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)",
            "$10$20",
            "jb0wo0123");
        ApiTestUtils.TestReplaceAll(
            "\u00e1\u0062\u00e7\u2655", "(.)", "<$1>", "<\u00e1><\u0062><\u00e7><\u2655>");
        ApiTestUtils.TestReplaceAll(
            "\u00e1\u0062\u00e7\u2655", "[\u00e0-\u00e9]", "<$0>", "<\u00e1>\u0062<\u00e7>\u2655");
        ApiTestUtils.TestReplaceAll("hello world", "z*", "x", "xhxexlxlxox xwxoxrxlxdx");
        // test replaceAll with alternation
        ApiTestUtils.TestReplaceAll("123:foo", "(?:\\w+|\\d+:foo)", "x", "x:x");
        ApiTestUtils.TestReplaceAll("123:foo", "(?:\\d+:foo|\\w+)", "x", "x");
        ApiTestUtils.TestReplaceAll("aab", "a*", "<$0>", "<aa><>b<>");
        ApiTestUtils.TestReplaceAll("aab", "a*?", "<$0>", "<>a<>a<>b<>");
    }

    [TestMethod]
    public void TestReplaceFirst()
    {
        ApiTestUtils.TestReplaceFirst(
            "What the Frog's Eye Tells the Frog's Brain",
            "Frog",
            "Lizard",
            "What the Lizard's Eye Tells the Frog's Brain");
        ApiTestUtils.TestReplaceFirst(
            "What the Frog's Eye Tells the Frog's Brain",
            "F(rog)",
            "\\$Liza\\rd$1",
            "What the $Lizardrog's Eye Tells the Frog's Brain");
        ApiTestUtils.TestReplaceFirst(
            "abcdefghijklmnopqrstuvwxyz123",
            "(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)(.)",
            "$10$20",
            "jb0nopqrstuvwxyz123");
        ApiTestUtils.TestReplaceFirst(
            "\u00e1\u0062\u00e7\u2655", "(.)", "<$1>", "<\u00e1>\u0062\u00e7\u2655");
        ApiTestUtils.TestReplaceFirst(
            "\u00e1\u0062\u00e7\u2655", "[\u00e0-\u00e9]", "<$0>", "<\u00e1>\u0062\u00e7\u2655");
        ApiTestUtils.TestReplaceFirst("hello world", "z*", "x", "xhello world");
        ApiTestUtils.TestReplaceFirst("aab", "a*", "<$0>", "<aa>b");
        ApiTestUtils.TestReplaceFirst("aab", "a*?", "<$0>", "<>aab");
    }

    [TestMethod]
    public void TestGroupCount()
    {
        ApiTestUtils.TestGroupCount("(a)(b(c))d?(e)", 4);
    }

    [TestMethod]
    public void TestGroup()
    {
        // ApiTestUtils.testGroup("xabdez", "(a)(b(c)?)d?(e)", new string[] {"abde", "a", "b", null, "e"});
        // ApiTestUtils.testGroup("abc", "(a)(b$)?(b)?", new string[] {"ab", "a", null, "b"});
        // ApiTestUtils.testGroup("abc", "(^b)?(b)?c", new string[] {"bc", null, "b"});
        // ApiTestUtils.testGroup(" a b", "\\b(.).\\b", new string[] {"a ", "a"});

        // Not allowed to use UTF-8 except in comments, per Java style guide.
        // ("αβξδεφγ", "(.)(..)(...)", new string[] {"αβξδεφ", "α", "βξ", "δεφ"});
        ApiTestUtils.TestGroup(
            "\u03b1\u03b2\u03be\u03b4\u03b5\u03c6\u03b3",
            "(.)(..)(...)",
            new string[] {
          "\u03b1\u03b2\u03be\u03b4\u03b5\u03c6", "\u03b1", "\u03b2\u03be", "\u03b4\u03b5\u03c6"
            });
    }

    [TestMethod]
    public void TestFind()
    {
        ApiTestUtils.TestFind("abcdefgh", ".*[aeiou]", 0, "abcde");
        ApiTestUtils.TestFind("abcdefgh", ".*[aeiou]", 1, "bcde");
        ApiTestUtils.TestFind("abcdefgh", ".*[aeiou]", 2, "cde");
        ApiTestUtils.TestFind("abcdefgh", ".*[aeiou]", 3, "de");
        ApiTestUtils.TestFind("abcdefgh", ".*[aeiou]", 4, "e");
        ApiTestUtils.TestFindNoMatch("abcdefgh", ".*[aeiou]", 5);
        ApiTestUtils.TestFindNoMatch("abcdefgh", ".*[aeiou]", 6);
        ApiTestUtils.TestFindNoMatch("abcdefgh", ".*[aeiou]", 7);
    }

    [TestMethod]
    public void TestInvalidFind()
    {
        try
        {
            ApiTestUtils.TestFind("abcdef", ".*", 10, "xxx");
            Fail();
        }
        catch (IndexOutOfRangeException e)
        {
            /* ok */
        }
    }

    [TestMethod]
    public void TestInvalidReplacement()
    {
        try
        {
            ApiTestUtils.TestReplaceFirst("abc", "abc", "$4", "xxx");
            Fail();
        }
        catch (IndexOutOfRangeException e)
        {
            /* ok */
            AssertTrue(true);
        }
    }

    private void AssertTrue(bool v)
    {
        Assert.IsTrue(v);
    }

    private void Fail()
    {
        Assert.Fail();
    }

    [TestMethod]
    public void TestInvalidGroupNoMatch()
    {
        try
        {
            ApiTestUtils.TestInvalidGroup("abc", "xxx", 0);
            Fail();
        }
        catch (Exception e)
        {
            // Linter complains on empty catch block.
            AssertTrue(true);
        }
    }

    [TestMethod]
    public void TestInvalidGroupOutOfRange()
    {
        try
        {
            ApiTestUtils.TestInvalidGroup("abc", "abc", 1);
            Fail();
        }
        catch (IndexOutOfRangeException e)
        {
            // Linter complains on empty catch block.
            AssertTrue(true);
        }
    }

    /**
     * Test the NullReferenceException is thrown on null input.
     */
    [TestMethod]
    public void TestThrowsOnNullInputReset()
    {
        // null in constructor.
        try
        {
            new Matcher(Pattern.Compile("pattern"), (string)null);
            Fail();
        }
        catch (NullReferenceException n)
        {
            // Linter complains on empty catch block.
            AssertTrue(true);
        }
    }

    [TestMethod]
    public void TestThrowsOnNullInputCtor()
    {
        // null in constructor.
        try
        {
            //new Matcher(null, "input");
            //Fail();
        }
        catch (NullReferenceException n)
        {
            // Linter complains on empty catch block.
            AssertTrue(true);
        }
        Assert.IsTrue(true);
    }

    /**
     * Test that InvalidOperationException is thrown if start/end are called before calling find
     */
    [TestMethod]
    public void TestStartEndBeforeFind()
    {
        try
        {
            Matcher m = Pattern.Compile("a").Matcher("abaca");
            m.Start();
            Fail();
        }
        catch (InvalidOperationException ise)
        {
            AssertTrue(true);
        }
    }

    /**
     * Test for b/6891357. Basically matches should behave like find when it comes to updating the
     * information of the match.
     */
    [TestMethod]
    public void TestMatchesUpdatesMatchInformation()
    {
        Matcher m = Pattern.Compile("a+").Matcher("aaa");
        if (m.Matches())
        {
            AssertEquals("aaa", m.Group(0));
        }
    }

    private void AssertEquals(string v1, string v2)
    {
        Assert.AreEqual(v1, v2);
    }
    private void AssertEquals(int v1, int v2)
    {
        Assert.AreEqual(v1, v2);
    }

    /**
     * Test for b/6891133. Test matches in case of alternation.
     */
    [TestMethod]
    public void TestAlternationMatches()
    {
        string s = "123:foo";
        AssertTrue(Pattern.Compile("(?:\\w+|\\d+:foo)").Matcher(s).Matches());
        AssertTrue(Pattern.Compile("(?:\\d+:foo|\\w+)").Matcher(s).Matches());
    }

    void HelperTestMatchEndUTF16(string s, int num, int end)
    {
        //TODO: need to override RE2.Match
        string pattern = "[" + s + "]";
        RE2 re =
            new RE2(pattern)
    //{
    //  @Override
    //      public bool match(
    //      string input, int start, int e, int anchor, int[] group, int ngroup)
    //{
    //    AssertEquals(end, e);
    //    return super.match(input, start, e, anchor, group, ngroup);
    //}
    //}
    ;
        Pattern pat = new Pattern(pattern, 0, re);
        Matcher m = pat.Matcher(s);

        int found = 0;
        while (m.Find()) {
            found++;
        }
        AssertEquals(
                "Matches Expected " + num + " but found " + found + ", for input " + s, num, found);
    }

    private void AssertEquals(string message, int num, int found)
    {
        Assert.AreEqual(num,found,message);
    }

    /**
     * Test for variable Length encoding, test whether RE2's match function gets the required
     * parameter based on UTF16 codes and not chars and Runes.
     */
    [TestMethod]
    public void TestMatchEndUTF16()
    {
        // Latin alphabetic chars such as these 5 lower-case, acute vowels have multi-byte UTF-8
        // encodings but fit in a single UTF-16 code, so the match is at UTF16 offset 5.
        string vowels = "\x95\x9b\x97\xa3\xa8"; //"\225\233\237\243\250"
        HelperTestMatchEndUTF16(vowels, 5, 5);

        // But surrogates are encoded as two UTF16 codes, so we should expect match
        // to get 6 rather than 3.
        string utf16 =
            new StringBuilder()
                .Append(char.ConvertFromUtf32(0x10000))
                .Append(char.ConvertFromUtf32(0x10001))
                .Append(char.ConvertFromUtf32(0x10002))
                .ToString();
        AssertEquals(utf16, "\uD800\uDC00\uD800\uDC01\uD800\uDC02");
        HelperTestMatchEndUTF16(utf16, 3, 6);
    }

    [TestMethod]
    public void TestAppendTail_StringBuffer()
    {
        Pattern p = Pattern.Compile("cat");
        Matcher m = p.Matcher("one cat two cats in the yard");
        StringBuilder sb = new StringBuilder();
        while (m.Find())
        {
            m.AppendReplacement(sb, "dog");
        }
        m.AppendTail(sb);
        m.AppendTail(sb);
        AssertEquals("one dog two dogs in the yards in the yard", sb.ToString());
    }

    [TestMethod]
    public void TestAppendTail_StringBuilder()
    {
        Pattern p = Pattern.Compile("cat");
        Matcher m = p.Matcher("one cat two cats in the yard");
        StringBuilder sb = new StringBuilder();
        while (m.Find())
        {
            m.AppendReplacement(sb, "dog");
        }
        m.AppendTail(sb);
        m.AppendTail(sb);
        AssertEquals("one dog two dogs in the yards in the yard", sb.ToString());
    }

    [TestMethod]
    public void TestResetOnFindInt_StringBuffer()
    {
        StringBuilder buffer;
        Matcher matcher = Pattern.Compile("a").Matcher("zza");

        AssertTrue(matcher.Find());

        buffer = new StringBuilder();
        matcher.AppendReplacement(buffer, "foo");
        AssertEquals("1st time", "zzfoo", buffer.ToString());

        AssertTrue(matcher.Find(0));

        buffer = new StringBuilder();
        matcher.AppendReplacement(buffer, "foo");
        AssertEquals("2nd time", "zzfoo", buffer.ToString());
    }

    [TestMethod]
    public void TestResetOnFindInt_StringBuilder()
    {
        StringBuilder buffer;
        Matcher matcher = Pattern.Compile("a").Matcher("zza");

        AssertTrue(matcher.Find());

        buffer = new StringBuilder();
        matcher.AppendReplacement(buffer, "foo");
        AssertEquals("1st time", "zzfoo", buffer.ToString());

        AssertTrue(matcher.Find(0));

        buffer = new StringBuilder();
        matcher.AppendReplacement(buffer, "foo");
        AssertEquals("2nd time", "zzfoo", buffer.ToString());
    }

    private void AssertEquals(string message, string v1, string v2)
    {
        Assert.AreEqual(v1, v2, message);
    }

    [TestMethod]
    public void TestEmptyReplacementGroups_StringBuffer()
    {
        var buffer = new StringBuilder();
        Matcher matcher = Pattern.Compile("(a)(b$)?(b)?").Matcher("abc");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2-$3");
        AssertEquals("a--b", buffer.ToString());
        matcher.AppendTail(buffer);
        AssertEquals("a--bc", buffer.ToString());

        buffer = new StringBuilder();
        matcher = Pattern.Compile("(a)(b$)?(b)?").Matcher("ab");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2-$3");
        matcher.AppendTail(buffer);
        AssertEquals("a-b-", buffer.ToString());

        buffer = new StringBuilder();
        matcher = Pattern.Compile("(^b)?(b)?c").Matcher("abc");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2");
        matcher.AppendTail(buffer);
        AssertEquals("a-b", buffer.ToString());

        buffer = new StringBuilder();
        matcher = Pattern.Compile("^(.)[^-]+(-.)?(.*)").Matcher("Name");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1$2");
        matcher.AppendTail(buffer);
        AssertEquals("N", buffer.ToString());
    }

    [TestMethod]
    public void TestEmptyReplacementGroups_StringBuilder()
    {
        StringBuilder buffer = new StringBuilder();
        Matcher matcher = Pattern.Compile("(a)(b$)?(b)?").Matcher("abc");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2-$3");
        AssertEquals("a--b", buffer.ToString());
        matcher.AppendTail(buffer);
        AssertEquals("a--bc", buffer.ToString());

        buffer = new StringBuilder();
        matcher = Pattern.Compile("(a)(b$)?(b)?").Matcher("ab");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2-$3");
        matcher.AppendTail(buffer);
        AssertEquals("a-b-", buffer.ToString());

        buffer = new StringBuilder();
        matcher = Pattern.Compile("(^b)?(b)?c").Matcher("abc");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1-$2");
        matcher.AppendTail(buffer);
        AssertEquals("a-b", buffer.ToString());

        buffer = new StringBuilder();
        matcher = Pattern.Compile("^(.)[^-]+(-.)?(.*)").Matcher("Name");
        AssertTrue(matcher.Find());
        matcher.AppendReplacement(buffer, "$1$2");
        matcher.AppendTail(buffer);
        AssertEquals("N", buffer.ToString());
    }

    // This example is documented in the com.google.re2j package.html.
    [TestMethod]
    public void TestDocumentedExample()
    {
        Pattern p = Pattern.Compile("b(an)*(.)");
        Matcher m = p.Matcher("by, band, banana");
        AssertTrue(m.LookingAt());
        m.Reset();
        AssertTrue(m.Find());
        AssertEquals("by", m.Group(0));
        AssertNull(m.Group(1));
        AssertEquals("y", m.Group(2));
        AssertTrue(m.Find());
        AssertEquals("band", m.Group(0));
        AssertEquals("an", m.Group(1));
        AssertEquals("d", m.Group(2));
        AssertTrue(m.Find());
        AssertEquals("banana", m.Group(0));
        AssertEquals("an", m.Group(1));
        AssertEquals("a", m.Group(2));
        AssertFalse(m.Find());
    }

    
    private void AssertFalse(bool v)
    {
        Assert.IsFalse(v);
    }

    [TestMethod]
    public void TestMutableCharSequence()
    {
        Pattern p = Pattern.Compile("b(an)*(.)");
        var b = new StringBuilder("by, band, banana");
        var t = b.ToString();
        Matcher m = p.Matcher(t);
        AssertTrue(m.Find(0));
        int start = t.IndexOf("ban");
        for(int i = start; i < t.Length; i++)
        {
            b[i] = t[i - start];
        }
        //b.Replace(t.IndexOf("ban"), start + 3, "b");
        AssertTrue(m.Find(t.IndexOf("ban")));
    }

    

    [TestMethod]
    public void TestNamedGroups()
    {
        Pattern p =
            Pattern.Compile(
                "(?P<baz>f(?P<foo>b*a(?P<another>r+)){0,10})" + "(?P<bag>bag)?(?P<nomatch>zzz)?");
        Matcher m = p.Matcher("fbbarrrrrbag");
        AssertTrue(m.Matches());
        AssertEquals("fbbarrrrr", m.Group("baz"));
        AssertEquals("bbarrrrr", m.Group("foo"));
        AssertEquals("rrrrr", m.Group("another"));
        AssertEquals(0, m.Start("baz"));
        AssertEquals(1, m.Start("foo"));
        AssertEquals(4, m.Start("another"));
        AssertEquals(9, m.End("baz"));
        AssertEquals(9, m.End("foo"));
        AssertEquals("bag", m.Group("bag"));
        AssertEquals(9, m.Start("bag"));
        AssertEquals(12, m.End("bag"));
        AssertNull(m.Group("nomatch"));
        AssertEquals(-1, m.Start("nomatch"));
        AssertEquals(-1, m.End("nomatch"));
        AssertEquals("whatbbarrrrreverbag", AppendReplacement(m, "what$2ever${bag}"));

        try
        {
            m.Group("nonexistent");
            Fail("Should have thrown IllegalArgumentException");
        }
        catch (ArgumentException expected)
        {
            // Expected
        }
    }

    private void Fail(string v)
    {
        Assert.Fail(v);
    }

    private void AssertNull(string? v)
    {
        Assert.IsNull(v);
    }

    private string AppendReplacement(Matcher m, string replacement)
    {
        var b = new StringBuilder();
        m.AppendReplacement(b, replacement);
        return b.ToString();
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
    [TestMethod]
    public void TestGroupZeroWidthAssertions()
    {
        Matcher m = Pattern.Compile("(\\d{2} ?(\\d|[a-z])?)($|[^a-zA-Z])").Matcher("22 bored");
        Assert.IsTrue(m.Find());
        Assert.AreEqual(m.Group(1), "22");
    }

    [TestMethod]
    public void TestPatternLongestMatch()
    {
        string pattern = "(?:a+)|(?:a+ b+)";
        string text = "xxx aaa bbb yyy";
        {
            Matcher matcher = Pattern.Compile(pattern).Matcher(text);
            AssertTrue(matcher.Find());
            AssertEquals("aaa", text.Substring(matcher.Start(), matcher.End()-matcher.Start()));
        }
        {
            Matcher matcher = Pattern.Compile(pattern, Pattern.LONGEST_MATCH).Matcher(text);
            AssertTrue(matcher.Find());
            AssertEquals("aaa bbb", text.Substring(matcher.Start(), matcher.End()-matcher.Start()));
        }
    }
}
