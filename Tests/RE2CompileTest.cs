/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;
[TestFixture]
public class RE2CompileTest
{
    // A list of regexp and expected error when calling RE2.compile. null implies that compile should
    // succeed.
  public static string[][] testData()
    {
        return new string[][] {
      new string[]{"", null},
      new string[]{".", null},
      new string[]{"^.$", null},
      new string[]{"a", null},
      new string[]{"a*", null},
      new string[]{"a+", null},
      new string[]{"a?", null},
      new string[]{"a|b", null},
      new string[]{"a*|b*", null},
      new string[]{"(a*|b)(c*|d)", null},
      new string[]{"[a-z]", null},
      new string[]{"[a-abc-c\\-\\]\\[]", null},
      new string[]{"[a-z]+", null},
      new string[]{"[abc]", null},
      new string[]{"[^1234]", null},
      new string[]{"[^\n]", null},
      new string[]{"\\!\\\\", null},
      new string[]{"abc]", null}, // Matches the closing bracket literally.
      new string[]{"a??", null},
      new string[]{"*", "missing argument to repetition operator: `*`"},
      new string[]{"+", "missing argument to repetition operator: `+`"},
      new string[]{"?", "missing argument to repetition operator: `?`"},
      new string[]{"(abc", "missing closing ): `(abc`"},
      new string[]{"abc)", "regexp/syntax: internal error: `stack underflow`"},
      new string[]{"x[a-z", "missing closing ]: `[a-z`"},
      new string[]{"[z-a]", "invalid character class range: `z-a`"},
      new string[]{"abc\\", "trailing backslash at end of expression"},
      new string[]{"a**", "invalid nested repetition operator: `**`"},
      new string[]{"a*+", "invalid nested repetition operator: `*+`"},
      new string[]{"\\x", "invalid escape sequence: `\\x`"},
      new string[]{"\\p", "invalid character class range: `\\p`"},
      new string[]{"\\p{", "invalid character class range: `\\p{`"}
    };
    }

    private string input;
  private string expectedError;

  public RE2CompileTest(string input, string expectedError)
    {
        this.input = input;
        this.expectedError = expectedError;
    }

    [Test]
  public void testCompile()
    {
        try
        {
            RE2.Compile(input);
            if (expectedError != null)
            {
                fail("RE2.compile(" + input + ") was successful, expected " + expectedError);
            }
        }
        catch (PatternSyntaxException e)
        {
            if (expectedError == null
                || !e.Message.Equals("error parsing regexp: " + expectedError))
            {
                fail("compiling " + input + "; unexpected error: " + e.Message);
            }
        }
    }

    private void fail(string m)
    {
        throw new NotImplementedException();
    }
}
