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
 * A Prog is a compiled regular expression program.
 */
public class Program
{

    public Inst[] inst = new Inst[10];
    public int instSize = 0;
    public int start = 0; // index of start instruction
    public int numCap = 2; // number of CAPTURE insts in re
                    // 2 => implicit ( and ) for whole match $0

    // Constructs an empty program.
    public Program() { }

    // Returns the instruction at the specified pc.
    // Precondition: pc > 0 && pc < numInst().
    public Inst GetInst(int pc) => inst[pc];

    // Returns the number of instructions in this program.
    public int NumInst => instSize;

    // Adds a new instruction to this program, with operator |op| and |pc| equal
    // to |numInst()|.
    public void AddInst(int op)
    {
        if (this.instSize >= this.inst.Length)
        {
            var new_inst = new Inst[inst.Length<<1];
            inst.CopyTo(new_inst, 0);
            inst= new_inst;
        }
        inst[instSize] = new Inst(op);
        instSize++;
    }

    // skipNop() follows any no-op or capturing instructions and returns the
    // resulting instruction.
    public Inst SkipNop(int pc)
    {
        var i = inst[pc];
        while (i.op == Inst.NOP || i.op == Inst.CAPTURE)
        {
            i = inst[pc];
            pc = i._out;
        }
        return i;
    }

    // prefix() returns a pair of a literal string that all matches for the
    // regexp must start with, and a bool which is true if the prefix is the
    // entire match.  The string is returned by appending to |prefix|.
    public bool Prefix(StringBuilder prefix)
    {
        var i = SkipNop(start);

        // Avoid allocation of buffer if prefix is empty.
        if (!Inst.IsRuneOp(i.op) || i.runes.Length != 1)
        {
            return i.op == Inst.MATCH; // (Append "" to prefix)
        }

        // Have prefix; gather characters.
        while (Inst.IsRuneOp(i.op) && i.runes.Length == 1 && (i.arg & RE2.FOLD_CASE) == 0)
        {
            prefix.Append( new Rune(i.runes[0]).ToString()); // an int, not a byte.
            i = SkipNop(i._out);
        }
        return i.op == Inst.MATCH;
    }

    // startCond() returns the leading empty-width conditions that must be true
    // in any match.  It returns -1 (all bits set) if no matches are possible.
    public int StartCond()
    {
        int flag = 0; // bitmask of EMPTY_* flags
        int pc = start;
    loop:
        for (; ; )
        {
            var i = inst[pc];
            switch (i.op)
            {
                case Inst.EMPTY_WIDTH:
                    flag |= i.arg;
                    break;
                case Inst.FAIL:
                    return -1;
                case Inst.CAPTURE:
                case Inst.NOP:
                    break; // skip
                default:
                    goto loop;
            }
            pc = i._out;
        }
        return flag;
    }

    // --- Patch list ---

    // A patchlist is a list of instruction pointers that need to be filled in
    // (patched).  Because the pointers haven't been filled in yet, we can reuse
    // their storage to hold the list.  It's kind of sleazy, but works well in
    // practice.  See http://swtch.com/~rsc/regexp/regexp1.html for inspiration.

    // These aren't really pointers: they're integers, so we can reinterpret them
    // this way without using package unsafe.  A value l denotes p.inst[l>>1]._out
    // (l&1==0) or .arg (l&1==1).  l == 0 denotes the empty list, okay because we
    // start every program with a fail instruction, so we'll never want to point
    // at its output link.

    public int Next(int l)
    {
        var i = inst[l >> 1];
        if ((l & 1) == 0)
        {
            return i._out;
        }
        return i.arg;
    }

    public void Patch(int l, int val)
    {
        while (l != 0)
        {
            var i = inst[l >> 1];
            if ((l & 1) == 0)
            {
                l = i._out;
                i._out = val;
            }
            else
            {
                l = i.arg;
                i.arg = val;
            }
        }
    }

    public int Append(int l1, int l2)
    {
        if (l1 == 0)
        {
            return l2;
        }
        if (l2 == 0)
        {
            return l1;
        }
        int last = l1;
        for (; ; )
        {
            int _next = Next(last);
            if (_next == 0)
            {
                break;
            }
            last = _next;
        }
        var i = inst[last >> 1];
        if ((last & 1) == 0)
        {
            i._out = l2;
        }
        else
        {
            i.arg = l2;
        }
        return l1;
    }

    // ---

    public override string ToString()
    {
        var builder = new StringBuilder();
        for (int pc = 0; pc < instSize; ++pc)
        {
            int len = builder.Length;
            builder.Append(pc);
            if (pc == start)
            {
                builder.Append('*');
            }
            // Use spaces not tabs since they're not always preserved in
            // Google Java source, such as our tests.
            builder.Append("        ".Substring(builder.Length - len)).Append(inst[pc]).Append('\n');
        }
        return builder.ToString();
    }
}
