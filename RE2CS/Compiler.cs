/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/compile.go

namespace RE2CS;
/**
 * Compiler from {@code Regexp} (RE2 abstract syntax) to {@code RE2} (compiled regular expression).
 *
 * The only entry point is {@link #compileRegexp}.
 */
public class Compiler
{
    /**
     * A fragment of a compiled regular expression program.
     *
     * @see http://swtch.com/~rsc/regexp/regexp1.html
     */
    public class Fragment
    {
        public readonly int i; // an instruction address (pc).
        public int _out; // a patch list; see explanation in Prog.java
        public bool nullable; // whether the fragment can match the empty string

        public Fragment(int i = 0, int _out = 0, bool nullable = false)
        {
            this.i = i;
            this._out = _out;
            this.nullable = nullable;
        }
    }
    private static readonly int[] ANY_RUNE_NOT_NL = { 0, '\n' - 1, '\n' + 1, Unicode.MAX_RUNE };
    private static readonly int[] ANY_RUNE = { 0, Unicode.MAX_RUNE };

    protected readonly Program program = new (); // Program being built

    protected Compiler()
    {
        this.NewInst(Inst.FAIL); // always the first instruction
    }

    public static Program CompileRegexp(Regexp re)
    {
        var c = new Compiler();
        var f = c.Compile(re);
        c.program.patch(f._out, c.NewInst(Inst.MATCH).i);
        c.program.start = f.i;
        return c.program;
    }

    private Fragment NewInst(int op)
    {
        // TODO(rsc): impose length limit.
        program.addInst(op);
        return new Fragment(program.numInst() - 1, 0, true);
    }

    // Returns a no-op fragment.  Sometimes unavoidable.
    private Fragment Nop()
    {
        var f = NewInst(Inst.NOP);
        f._out = f.i << 1;
        return f;
    }

    private Fragment Fail()
    {
        return new ();
    }

    // Given fragment a, returns (a) capturing as \n.
    // Given a fragment a, returns a fragment with capturing parens around a.
    private Fragment Cap(int arg)
    {
        var f = NewInst(Inst.CAPTURE);
        f._out = f.i << 1;
        program.getInst(f.i).arg = arg;
        if (program.numCap < arg + 1)
        {
            program.numCap = arg + 1;
        }
        return f;
    }

    // Given fragments a and b, returns ab; a|b
    private Fragment Cat(Fragment f1, Fragment f2)
    {
        // concat of failure is failure
        if (f1.i == 0 || f2.i == 0)
        {
            return Fail();
        }
        // TODO(rsc): elide nop
        program.patch(f1._out, f2.i);
        return new (f1.i, f2._out, f1.nullable && f2.nullable);
    }

    // Given fragments for a and b, returns fragment for a|b.
    private Fragment Alt(Fragment f1, Fragment f2)
    {
        // alt of failure is other
        if (f1.i == 0) return f2;
        if (f2.i == 0) return f1;
        var f = NewInst(Inst.ALT);
        var i = program.getInst(f.i);
        i._out = f1.i;
        i.arg = f2.i;
        f._out = program.Append(f1._out, f2._out);
        f.nullable = f1.nullable || f2.nullable;
        return f;
    }

    // loop returns the fragment for the main loop of a plus or star.
    // For plus, it can be used directly. with f1.i as the entry.
    // For star, it can be used directly when f1 can't match an empty string.
    // (When f1 can match an empty string, f1* must be implemented as (f1+)?
    // to get the priority match order correct.)
    private Fragment Loop(Fragment f1, bool nongreedy)
    {
        var f = NewInst(Inst.ALT);
        var i = program.getInst(f.i);
        if (nongreedy)
        {
            i.arg = f1.i;
            f._out = f.i << 1;
        }
        else
        {
            i._out = f1.i;
            f._out = f.i << 1 | 1;
        }
        program.patch(f1._out, f.i);
        return f;
    }

    // Given a fragment for a, returns a fragment for a? or a?? (if nongreedy)
    private Fragment Quest(Fragment f1, bool nongreedy)
    {
        var f = NewInst(Inst.ALT);
        var i = program.getInst(f.i);
        if (nongreedy)
        {
            i.arg = f1.i;
            f._out = f.i << 1;
        }
        else
        {
            i._out = f1.i;
            f._out = f.i << 1 | 1;
        }
        f._out = program.Append(f._out, f1._out);
        return f;
    }

    // Given a fragment a, returns a fragment for a* or a*? (if nongreedy)
    private Fragment Star(Fragment f1, bool nongreedy)
    {
        return f1.nullable ? Quest(Plus(f1, nongreedy), nongreedy) : Loop(f1, nongreedy);
    }

    // Given a fragment for a, returns a fragment for a+ or a+? (if nongreedy)
    private Fragment Plus(Fragment f1, bool nongreedy) => new Fragment(f1.i, Loop(f1, nongreedy)._out, f1.nullable);

    // op is a bitmask of EMPTY_* flags.
    private Fragment Empty(int op)
    {
        var f = NewInst(Inst.EMPTY_WIDTH);
        program.getInst(f.i).arg = op;
        f._out = f.i << 1;
        return f;
    }

    private Fragment Rune(int _rune, int flags) => Rune(new int[] { _rune }, flags);

    // flags : parser flags
    private Fragment Rune(int[] runes, int flags)
    {
        var f = NewInst(Inst.RUNE);
        f.nullable = false;
        var i = program.getInst(f.i);
        i.runes = runes;
        flags &= RE2.FOLD_CASE; // only relevant flag is FoldCase
        if (runes.Length != 1 || Unicode.simpleFold(runes[0]) == runes[0])
        {
            flags &= ~RE2.FOLD_CASE; // and sometimes not even that
        }
        i.arg = flags;
        f._out = f.i << 1;
        // Special cases for exec machine.
        if (((flags & RE2.FOLD_CASE) == 0 && runes.Length == 1)
            || (runes.Length == 2 && runes[0] == runes[1]))
        {
            i.op = Inst.RUNE1;
        }
        else if (runes.Length == 2 && runes[0] == 0 && runes[1] == Unicode.MAX_RUNE)
        {
            i.op = Inst.RUNE_ANY;
        }
        else if (runes.Length == 4
          && runes[0] == 0
          && runes[1] == '\n' - 1
          && runes[2] == '\n' + 1
          && runes[3] == Unicode.MAX_RUNE)
        {
            i.op = Inst.RUNE_ANY_NOT_NL;
        }
        return f;
    }

    private Fragment Compile(Regexp re)
    {
        switch (re.op)
        {
            case Regexp.Op.NO_MATCH:
                return Fail();
            case Regexp.Op.EMPTY_MATCH:
                return Nop();
            case Regexp.Op.LITERAL:
                if (re.runes.Length == 0)
                {
                    return Nop();
                }
                else
                {
                    Fragment? f = null;
                    foreach (int r in re.runes)
                    {
                        var f1 = Rune(r, re.flags);
                        f = (f == null) ? f1 : Cat(f, f1);
                    }
                    return f;
                }
            case Regexp.Op.CHAR_CLASS:
                return Rune(re.runes, re.flags);
            case Regexp.Op.ANY_CHAR_NOT_NL:
                return Rune(ANY_RUNE_NOT_NL, 0);
            case Regexp.Op.ANY_CHAR:
                return Rune(ANY_RUNE, 0);
            case Regexp.Op.BEGIN_LINE:
                return Empty(Utils.EMPTY_BEGIN_LINE);
            case Regexp.Op.END_LINE:
                return Empty(Utils.EMPTY_END_LINE);
            case Regexp.Op.BEGIN_TEXT:
                return Empty(Utils.EMPTY_BEGIN_TEXT);
            case Regexp.Op.END_TEXT:
                return Empty(Utils.EMPTY_END_TEXT);
            case Regexp.Op.WORD_BOUNDARY:
                return Empty(Utils.EMPTY_WORD_BOUNDARY);
            case Regexp.Op.NO_WORD_BOUNDARY:
                return Empty(Utils.EMPTY_NO_WORD_BOUNDARY);
            case Regexp.Op.CAPTURE:
                {
                    var bra = Cap(re.cap << 1);
                    var sub = Compile(re.subs[0]);
                    var ket = Cap(re.cap << 1 | 1);
                    return Cat(Cat(bra, sub), ket);
                }
            case Regexp.Op.STAR:
                return Star(Compile(re.subs[0]), (re.flags & RE2.NON_GREEDY) != 0);
            case Regexp.Op.PLUS:
                return Plus(Compile(re.subs[0]), (re.flags & RE2.NON_GREEDY) != 0);
            case Regexp.Op.QUEST:
                return Quest(Compile(re.subs[0]), (re.flags & RE2.NON_GREEDY) != 0);
            case Regexp.Op.CONCAT:
                if (re.subs.Length == 0)
                {
                    return Nop();
                }
                else
                {
                    Fragment? f = null;
                    foreach (Regexp sub in   re.subs)
                    {
                        var f1 = Compile(sub);
                        f = (f == null) ? f1 : Cat(f, f1);
                    }
                    return f;
                }
            case Regexp.Op.ALTERNATE:
                {
                    if (re.subs.Length == 0)
                    {
                        return Nop();
                    }
                    else
                    {
                        Fragment? f = null;
                        foreach (Regexp sub in re.subs)
                        {
                            var f1 = Compile(sub);
                            f = (f == null) ? f1 : Alt(f, f1);
                        }
                        return f;
                    }
                }
            default:
                throw new InvalidOperationException("regexp: unhandled case in compile");
        }
    }
}
