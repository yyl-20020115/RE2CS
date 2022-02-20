/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/simplify_test.go
using NUnit.Framework;
namespace RE2CS.Tests;


[TestFixture]
public class SimplifyTest
{

    private static readonly string[][] SIMPLIFY_TESTS = new string[][]{
    // Already-simple constructs
    new string[]{"a", "a"},
    new string[]{"ab", "ab"},
    new string[]{"a|b", "[a-b]"},
    new string[]{"ab|cd", "ab|cd"},
    new string[]{"(ab)*", "(ab)*"},
    new string[]{"(ab)+", "(ab)+"},
    new string[]{"(ab)?", "(ab)?"},
    new string[]{".", "(?s:.)"},
    new string[]{"^", "^"},
    new string[]{"$", "$"},
    new string[]{"[ac]", "[ac]"},
    new string[]{"[^ac]", "[^ac]"},

    // Posix character classes
    new string[]{"[[:alnum:]]", "[0-9A-Za-z]"},
    new string[]{"[[:alpha:]]", "[A-Za-z]"},
    new string[]{"[[:blank:]]", "[\\t ]"},
    new string[]{"[[:cntrl:]]", "[\\x00-\\x1f\\x7f]"},
    new string[]{"[[:digit:]]", "[0-9]"},
    new string[]{"[[:graph:]]", "[!-~]"},
    new string[]{"[[:lower:]]", "[a-z]"},
    new string[]{"[[:print:]]", "[ -~]"},
    new string[]{"[[:punct:]]", "[!-/:-@\\[-`\\{-~]"},
    new string[]{"[[:space:]]", "[\\t-\\r ]"},
    new string[]{"[[:upper:]]", "[A-Z]"},
    new string[]{"[[:xdigit:]]", "[0-9A-Fa-f]"},

    // Perl character classes
    new string[]{"\\d", "[0-9]"},
    new string[]{"\\s", "[\\t-\\n\\f-\\r ]"},
    new string[]{"\\w", "[0-9A-Z_a-z]"},
    new string[]{"\\D", "[^0-9]"},
    new string[]{"\\S", "[^\\t-\\n\\f-\\r ]"},
    new string[]{"\\W", "[^0-9A-Z_a-z]"},
    new string[]{"[\\d]", "[0-9]"},
    new string[]{"[\\s]", "[\\t-\\n\\f-\\r ]"},
    new string[]{"[\\w]", "[0-9A-Z_a-z]"},
    new string[]{"[\\D]", "[^0-9]"},
    new string[]{"[\\S]", "[^\\t-\\n\\f-\\r ]"},
    new string[]{"[\\W]", "[^0-9A-Z_a-z]"},

    // Posix repetitions
    new string[]{"a{1}", "a"},
    new string[]{"a{2}", "aa"},
    new string[]{"a{5}", "aaaaa"},
    new string[]{"a{0,1}", "a?"},
    // The next three are illegible because Simplify inserts (?:)
    // parens instead of () parens to avoid creating extra
    // captured subexpressions.  The comments show a version with fewer parens.
    new string[]{"(a){0,2}", "(?:(a)(a)?)?"}, //       (aa?)?
    new string[]{"(a){0,4}", "(?:(a)(?:(a)(?:(a)(a)?)?)?)?"}, //   (a(a(aa?)?)?)?
    new string[]{"(a){2,6}", "(a)(a)(?:(a)(?:(a)(?:(a)(a)?)?)?)?"}, // aa(a(a(aa?)?)?)?
    new string[]{"a{0,2}", "(?:aa?)?"}, //       (aa?)?
    new string[]{"a{0,4}", "(?:a(?:a(?:aa?)?)?)?"}, //   (a(a(aa?)?)?)?
    new string[]{"a{2,6}", "aa(?:a(?:a(?:aa?)?)?)?"}, // aa(a(a(aa?)?)?)?
    new string[]{"a{0,}", "a*"},
    new string[]{"a{1,}", "a+"},
    new string[]{"a{2,}", "aa+"},
    new string[]{"a{5,}", "aaaaa+"},

    // Test that operators simplify their arguments.
    new string[]{"(?:a{1,}){1,}", "a+"},
    new string[]{"(a{1,}b{1,})", "(a+b+)"},
    new string[]{"a{1,}|b{1,}", "a+|b+"},
    new string[]{"(?:a{1,})*", "(?:a+)*"},
    new string[]{"(?:a{1,})+", "a+"},
    new string[]{"(?:a{1,})?", "(?:a+)?"},
    new string[]{"", "(?:)"},
    new string[]{"a{0}", "(?:)"},

    // Character class simplification
    new string[]{"[ab]", "[a-b]"},
    new string[]{"[a-za-za-z]", "[a-z]"},
    new string[]{"[A-Za-zA-Za-z]", "[A-Za-z]"},
    new string[]{"[ABCDEFGH]", "[A-H]"},
    new string[]{"[AB-CD-EF-GH]", "[A-H]"},
    new string[]{"[W-ZP-XE-R]", "[E-Z]"},
    new string[]{"[a-ee-gg-m]", "[a-m]"},
    new string[]{"[a-ea-ha-m]", "[a-m]"},
    new string[]{"[a-ma-ha-e]", "[a-m]"},
    new string[]{"[a-zA-Z0-9 -~]", "[ -~]"},

    // Empty character classes
    new string[]{"[^[:cntrl:][:^cntrl:]]", "[^\\x00-\\x{10FFFF}]"},

    // Full character classes
    new string[]{"[[:cntrl:][:^cntrl:]]", "(?s:.)"},

    // Unicode case folding.
    new string[]{"(?i)A", "(?i:A)"},
    new string[]{"(?i)a", "(?i:A)"},
    new string[]{"(?i)[A]", "(?i:A)"},
    new string[]{"(?i)[a]", "(?i:A)"},
    new string[]{"(?i)K", "(?i:K)"},
    new string[]{"(?i)k", "(?i:K)"},
    new string[]{"(?i)\\x{212a}", "(?i:K)"},
    new string[]{"(?i)[K]", "[Kk\u212A]"},
    new string[]{"(?i)[k]", "[Kk\u212A]"},
    new string[]{"(?i)[\\x{212a}]", "[Kk\u212A]"},
    new string[]{"(?i)[a-z]", "[A-Za-z\u017F\u212A]"},
    new string[]{"(?i)[\\x00-\\x{FFFD}]", "[\\x00-\uFFFD]"},
    new string[]{"(?i)[\\x00-\\x{10FFFF}]", "(?s:.)"},

    // Empty string as a regular expression.
    // The empty string must be preserved inside parens in order
    // to make submatches work right, so these tests are less
    // interesting than they might otherwise be.  string inserts
    // explicit (?:) in place of non-parenthesized empty strings,
    // to make them easier to spot for other parsers.
    new string[]{"(a|b|)", "([a-b]|(?:))"},
    new string[]{"(|)", "()"},
    new string[]{"a()", "a()"},
    new string[]{"(()|())", "(()|())"},
    new string[]{"(a|)", "(a|(?:))"},
    new string[]{"ab()cd()", "ab()cd()"},
    new string[]{"()", "()"},
    new string[]{"()*", "()*"},
    new string[]{"()+", "()+"},
    new string[]{"()?", "()?"},
    new string[]{"(){0}", "(?:)"},
    new string[]{"(){1}", "()"},
    new string[]{"(){1,}", "()+"},
    new string[]{"(){0,2}", "(?:()()?)?"},
    new string[]{"(?:(a){0})", "(?:)"},
    };

    public static Object[] getParameters()
    {
        return SIMPLIFY_TESTS;
    }

    private readonly string input;
    private readonly string expected;

    public SimplifyTest(string input, string expected)
    {
        this.input = input;
        this.expected = expected;
    }

    [Test]
    public void testSimplify()
    {
        Regexp re = Parser.parse(input, RE2.MATCH_NL | (RE2.PERL & ~RE2.ONE_LINE));
        string s = Simplify.simplify(re).ToString();
        assertEquals(string.Format("simplify({0})", input), expected, s);
    }

    private void assertEquals(string message, string expected, string s)
    {
        Assert.AreEqual(expected,s,message);
    }
}
