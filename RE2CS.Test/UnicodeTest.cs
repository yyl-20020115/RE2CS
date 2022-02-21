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

[TestClass]
public class UnicodeTest
{

    [TestMethod]
    public void TestFoldConstants()
    {
        int last = -1;
        for (int i = 0; i <= Unicode.MAX_RUNE; i++)
        {
            if(i>=char.MinValue && i<=char.MaxValue)
            {
                if (char.IsSurrogate((char)i)) continue;
            }
            int s = 0;
            try
            {
                s = Unicode.SimpleFold(i);
            }catch(Exception ex)
            {
                this.Fail(ex.Message);
            }
            if (s== i)
            {
                last = Unicode.MAX_FOLD;
                continue;
            }
            if (last == -1 && Unicode.MIN_FOLD != i)
            {
                Fail(string.Format("MIN_FOLD=#{0:X8} should be #{1:X8}", Unicode.MIN_FOLD, i));
            }
            last = i;
        }
        if (Unicode.MAX_FOLD != last)
        {
            Fail(string.Format("MAX_FOLD=#{0:X8} should be #{1:X8}", Unicode.MAX_FOLD, last));
        }
    }

    private void Fail(string v)
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
