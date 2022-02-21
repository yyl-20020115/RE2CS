/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;
namespace RE2CS.Tests;


// Utilities to make JUnit act a little more like Go's "testing" package.
public static class GoTestUtils
{
    // Other utilities:

    public static int Len(object[] array) => array == null ? 0 : array.Length;

    public static int Len(int[] array) => array == null ? 0 : array.Length;

    public static int Len(byte[] array) => array == null ? 0 : array.Length;

    public static byte[] Utf8(string s) => Encoding.UTF8.GetBytes(s);// s.getBytes("UTF-8");

    // Beware: logically this operation can fail, but Java doesn't detect it.
    public static string FromUTF8(byte[] b) => Encoding.UTF8.GetString(b);// new string(b, "UTF-8");

    // Convert |idx16|, which are Java (UTF-16) string indices, into the
    // corresponding indices in the UTF-8 encoding of |text|.
    //
    // TODO(adonovan): eliminate duplication w.r.t. ExecTest.
    public static int[] Utf16IndicesToUtf8(int[] idx16, string text)
    {
        int[] idx8 = new int[idx16.Length];
        for (int i = 0; i < idx16.Length; ++i)
        {
            idx8[i] =
                idx16[i] == -1 ? -1 :
                    Encoding.UTF8.GetBytes(text.Substring(0, idx16[i]-0)).Length; // yikes
        }
        return idx8;
    }
}
