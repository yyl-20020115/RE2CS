/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/prog.go

using System.Text;

namespace RE2CS;
/**
 * A single instruction in the regular expression virtual machine.
 *
 * @see http://swtch.com/~rsc/regexp/regexp2.html
 */
public class Inst
{
    public const int ALT = 1;
    public const int ALT_MATCH = 2;
    public const int CAPTURE = 3;
    public const int EMPTY_WIDTH = 4;
    public const int FAIL = 5;
    public const int MATCH = 6;
    public const int NOP = 7;
    public const int RUNE = 8;
    public const int RUNE1 = 9;
    public const int RUNE_ANY = 10;
    public const int RUNE_ANY_NOT_NL = 11;

    public int op;
    public int _out; // all but MATCH, FAIL
    public int arg; // ALT, ALT_MATCH, CAPTURE, EMPTY_WIDTH
    public int[] runes; // length==1 => exact match
                 // otherwise a list of [lo,hi] pairs.  hi is *inclusive*.
                 // REVIEWERS: why not half-open intervals?

    public Inst(int op)
    {
        this.op = op;
    }

    public static bool isRuneOp(int op)
    {
        return RUNE <= op && op <= RUNE_ANY_NOT_NL;
    }

    // MatchRune returns true if the instruction matches (and consumes) r.
    // It should only be called when op == InstRune.
    public bool matchRune(int r)
    {
        // Special case: single-rune slice is from literal string, not char
        // class.
        if (runes.Length == 1)
        {
            int r0 = runes[0];
            if (r == r0)
            {
                return true;
            }
            if ((arg & RE2.FOLD_CASE) != 0)
            {
                for (int r1 = Unicode.simpleFold(r0); r1 != r0; r1 = Unicode.simpleFold(r1))
                {
                    if (r == r1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        // Peek at the first few pairs.
        // Should handle ASCII well.
        for (int j = 0; j < runes.Length && j <= 8; j += 2)
        {
            if (r < runes[j])
            {
                return false;
            }
            if (r <= runes[j + 1])
            {
                return true;
            }
        }

        // Otherwise binary search.
        for (int lo = 0, hi = runes.Length / 2; lo < hi;)
        {
            int m = lo + (hi - lo) / 2;
            int c = runes[2 * m];
            if (c <= r)
            {
                if (r <= runes[2 * m + 1])
                {
                    return true;
                }
                lo = m + 1;
            }
            else
            {
                hi = m;
            }
        }
        return false;
    }
    public override string ToString()
    {
        switch (op)
        {
            case ALT:
                return "alt -> " + _out + ", " + arg;
            case ALT_MATCH:
                return "altmatch -> " + _out + ", " + arg;
            case CAPTURE:
                return "cap " + arg + " -> " + _out;
            case EMPTY_WIDTH:
                return "empty " + arg + " -> " + _out;
            case MATCH:
                return "match";
            case FAIL:
                return "fail";
            case NOP:
                return "nop -> " + _out;
            case RUNE:
                if (runes == null)
                {
                    return "rune <null>"; // can't happen
                }
                return "rune "
                    + escapeRunes(runes)
                    + (((arg & RE2.FOLD_CASE) != 0) ? "/i" : "")
                    + " -> "
                    + _out;
            case RUNE1:
                return "rune1 " + escapeRunes(runes) + " -> " + _out;
            case RUNE_ANY:
                return "any -> " + _out;
            case RUNE_ANY_NOT_NL:
                return "anynotnl -> " + _out;
            default:
                throw new InvalidOperationException("unhandled case in Inst.toString");
        }
    }

    // Returns an RE2 expression matching exactly |runes|.
    private static string escapeRunes(int[] runes)
    {
        var _out = new StringBuilder();
        _out.Append('"');
        foreach (int rune in runes)
        {
            Utils.escapeRune(_out, rune);
        }
        _out.Append('"');
        return _out.ToString();
    }
}
