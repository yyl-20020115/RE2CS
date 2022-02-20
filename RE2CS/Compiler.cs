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
    public class Frag
    {
        public readonly int i; // an instruction address (pc).
        public int _out; // a patch list; see explanation in Prog.java
        public bool nullable; // whether the fragment can match the empty string

        public Frag()
            : this(0, 0) { }

        public Frag(int i)
            : this(i, 0) { }

        public Frag(int i, int _out)
            : this(i, _out, false) { }

        public Frag(int i, int _out, bool nullable)
        {
            this.i = i;
            this._out = _out;
            this.nullable = nullable;
        }
    }

    private readonly Prog prog = new (); // Program being built

    private Compiler()
    {
        newInst(Inst.FAIL); // always the first instruction
    }

    public static Prog compileRegexp(Regexp re)
    {
        var c = new Compiler();
        var f = c.compile(re);
        c.prog.patch(f._out, c.newInst(Inst.MATCH).i);
        c.prog.start = f.i;
        return c.prog;
    }

    private Frag newInst(int op)
    {
        // TODO(rsc): impose length limit.
        prog.addInst(op);
        return new Frag(prog.numInst() - 1, 0, true);
    }

    // Returns a no-op fragment.  Sometimes unavoidable.
    private Frag nop()
    {
        Frag f = newInst(Inst.NOP);
        f._out = f.i << 1;
        return f;
    }

    private Frag fail()
    {
        return new Frag();
    }

    // Given fragment a, returns (a) capturing as \n.
    // Given a fragment a, returns a fragment with capturing parens around a.
    private Frag cap(int arg)
    {
        Frag f = newInst(Inst.CAPTURE);
        f._out = f.i << 1;
        prog.getInst(f.i).arg = arg;
        if (prog.numCap < arg + 1)
        {
            prog.numCap = arg + 1;
        }
        return f;
    }

    // Given fragments a and b, returns ab; a|b
    private Frag cat(Frag f1, Frag f2)
    {
        // concat of failure is failure
        if (f1.i == 0 || f2.i == 0)
        {
            return fail();
        }
        // TODO(rsc): elide nop
        prog.patch(f1._out, f2.i);
        return new Frag(f1.i, f2._out, f1.nullable && f2.nullable);
    }

    // Given fragments for a and b, returns fragment for a|b.
    private Frag alt(Frag f1, Frag f2)
    {
        // alt of failure is other
        if (f1.i == 0)
        {
            return f2;
        }
        if (f2.i == 0)
        {
            return f1;
        }
        Frag f = newInst(Inst.ALT);
        Inst i = prog.getInst(f.i);
        i._out = f1.i;
        i.arg = f2.i;
        f._out = prog.Append(f1._out, f2._out);
        f.nullable = f1.nullable || f2.nullable;
        return f;
    }

    // loop returns the fragment for the main loop of a plus or star.
    // For plus, it can be used directly. with f1.i as the entry.
    // For star, it can be used directly when f1 can't match an empty string.
    // (When f1 can match an empty string, f1* must be implemented as (f1+)?
    // to get the priority match order correct.)
    private Frag loop(Frag f1, bool nongreedy)
    {
        Frag f = newInst(Inst.ALT);
        Inst i = prog.getInst(f.i);
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
        prog.patch(f1._out, f.i);
        return f;
    }

    // Given a fragment for a, returns a fragment for a? or a?? (if nongreedy)
    private Frag quest(Frag f1, bool nongreedy)
    {
        Frag f = newInst(Inst.ALT);
        Inst i = prog.getInst(f.i);
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
        f._out = prog.Append(f._out, f1._out);
        return f;
    }

    // Given a fragment a, returns a fragment for a* or a*? (if nongreedy)
    private Frag star(Frag f1, bool nongreedy)
    {
        if (f1.nullable)
        {
            return quest(plus(f1, nongreedy), nongreedy);
        }
        return loop(f1, nongreedy);
    }

    // Given a fragment for a, returns a fragment for a+ or a+? (if nongreedy)
    private Frag plus(Frag f1, bool nongreedy)
    {
        return new Frag(f1.i, loop(f1, nongreedy)._out, f1.nullable);
    }

    // op is a bitmask of EMPTY_* flags.
    private Frag empty(int op)
    {
        Frag f = newInst(Inst.EMPTY_WIDTH);
        prog.getInst(f.i).arg = op;
        f._out = f.i << 1;
        return f;
    }

    private Frag rune(int _rune, int flags)
    {
        return rune(new int[] { _rune }, flags);
    }

    // flags : parser flags
    private Frag rune(int[] runes, int flags)
    {
        Frag f = newInst(Inst.RUNE);
        f.nullable = false;
        Inst i = prog.getInst(f.i);
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

    private static readonly int[] ANY_RUNE_NOT_NL = { 0, '\n' - 1, '\n' + 1, Unicode.MAX_RUNE };
    private static readonly int[] ANY_RUNE = { 0, Unicode.MAX_RUNE };

    private Frag compile(Regexp re)
    {
        switch (re.op)
        {
            case Regexp.Op.NO_MATCH:
                return fail();
            case Regexp.Op.EMPTY_MATCH:
                return nop();
            case Regexp.Op.LITERAL:
                if (re.runes.Length == 0)
                {
                    return nop();
                }
                else
                {
                    Frag f = null;
                    foreach (int r in re.runes)
                    {
                        Frag f1 = rune(r, re.flags);
                        f = (f == null) ? f1 : cat(f, f1);
                    }
                    return f;
                }
            case Regexp.Op.CHAR_CLASS:
                return rune(re.runes, re.flags);
            case Regexp.Op.ANY_CHAR_NOT_NL:
                return rune(ANY_RUNE_NOT_NL, 0);
            case Regexp.Op.ANY_CHAR:
                return rune(ANY_RUNE, 0);
            case Regexp.Op.BEGIN_LINE:
                return empty(Utils.EMPTY_BEGIN_LINE);
            case Regexp.Op.END_LINE:
                return empty(Utils.EMPTY_END_LINE);
            case Regexp.Op.BEGIN_TEXT:
                return empty(Utils.EMPTY_BEGIN_TEXT);
            case Regexp.Op.END_TEXT:
                return empty(Utils.EMPTY_END_TEXT);
            case Regexp.Op.WORD_BOUNDARY:
                return empty(Utils.EMPTY_WORD_BOUNDARY);
            case Regexp.Op.NO_WORD_BOUNDARY:
                return empty(Utils.EMPTY_NO_WORD_BOUNDARY);
            case Regexp.Op.CAPTURE:
                {
                    Frag bra = cap(re.cap << 1), sub = compile(re.subs[0]), ket = cap(re.cap << 1 | 1);
                    return cat(cat(bra, sub), ket);
                }
            case Regexp.Op.STAR:
                return star(compile(re.subs[0]), (re.flags & RE2.NON_GREEDY) != 0);
            case Regexp.Op.PLUS:
                return plus(compile(re.subs[0]), (re.flags & RE2.NON_GREEDY) != 0);
            case Regexp.Op.QUEST:
                return quest(compile(re.subs[0]), (re.flags & RE2.NON_GREEDY) != 0);
            case Regexp.Op.CONCAT:
                if (re.subs.Length == 0)
                {
                    return nop();
                }
                else
                {
                    Frag f = null;
                    foreach (Regexp sub in   re.subs)
                    {
                        Frag f1 = compile(sub);
                        f = (f == null) ? f1 : cat(f, f1);
                    }
                    return f;
                }
            case Regexp.Op.ALTERNATE:
                {
                    if (re.subs.Length == 0)
                    {
                        return nop();
                    }
                    else
                    {
                        Frag f = null;
                        foreach (Regexp sub in re.subs)
                        {
                            var f1 = compile(sub);
                            f = (f == null) ? f1 : alt(f, f1);
                        }
                        return f;
                    }
                }
            default:
                throw new InvalidOperationException("regexp: unhandled case in compile");
        }
    }
}
