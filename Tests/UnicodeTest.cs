/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using NUnit.Framework;

namespace RE2CS.Tests;

[TestFixture]
public class UnicodeTest
{

    [Test]
    public void testFoldConstants()
    {
        int last = -1;
        for (int i = 0; i <= Unicode.MAX_RUNE; i++)
        {
            if (Unicode.simpleFold(i) == i)
            {
                continue;
            }
            if (last == -1 && Unicode.MIN_FOLD != i)
            {
                fail(string.Format("MIN_FOLD=#{0:04X} should be #{1:04X}", Unicode.MIN_FOLD, i));
            }
            last = i;
        }
        if (Unicode.MAX_FOLD != last)
        {
            fail(string.Format("MAX_FOLD=#{0:04X} should be #{1:04X}", Unicode.MAX_FOLD, last));
        }
    }

    private void fail(string v)
    {
        Assert.Fail(v);
    }

    // TODO(adonovan): tests for:
    //
    // bool isUpper(int r);
    // bool isLower(int r);
    // bool isTitle(int r);
    // bool isPrint(int r);
    // int to(int _case, int r, int[][] caseRange);
    // int toUpper(int r);
    // int toLower(int r);
    // int simpleFold(int r);

}
