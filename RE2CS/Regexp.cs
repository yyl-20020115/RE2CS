/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/regexp.go

using System.Text;

namespace RE2CS;

/**
 * Regular expression abstract syntax tree. Produced by parser, used by compiler. NB, this
 * corresponds to {@code syntax.regexp} in the Go implementation; Go's {@code regexp} is called
 * {@code RE2} in Java.
 */
public class Regexp
{
    public enum Op : uint
    {
        NO_MATCH, // Matches no strings.
        EMPTY_MATCH, // Matches empty string.
        LITERAL, // Matches runes[] sequence
        CHAR_CLASS, // Matches Runes interpreted as range pair list
        ANY_CHAR_NOT_NL, // Matches any character except '\n'
        ANY_CHAR, // Matches any character
        BEGIN_LINE, // Matches empty string at end of line
        END_LINE, // Matches empty string at end of line
        BEGIN_TEXT, // Matches empty string at beginning of text
        END_TEXT, // Matches empty string at end of text
        WORD_BOUNDARY, // Matches word boundary `\b`
        NO_WORD_BOUNDARY, // Matches word non-boundary `\B`
        CAPTURE, // Capturing subexpr with index cap, optional name name
        STAR, // Matches subs[0] zero or more times.
        PLUS, // Matches subs[0] one or more times.
        QUEST, // Matches subs[0] zero or one times.
        REPEAT, // Matches subs[0] [min, max] times; max=-1 => no limit.
        CONCAT, // Matches concatenation of subs[]
        ALTERNATE, // Matches union of subs[]

        // Pseudo ops, used internally by Parser for parsing stack:
        LEFT_PAREN,
        VERTICAL_BAR

    }
    public static bool IsPseudo(Op op)
    {
        return op >= Op.LEFT_PAREN;
    }

    public static readonly Regexp[] EMPTY_SUBS = Array.Empty<Regexp>();

    public Op op; // operator
    public int flags; // bitmap of parse flags
    public Regexp[] subs; // subexpressions, if any.  Never null.
                          // subs[0] is used as the freelist.
    public int[]? runes; // matched runes, for LITERAL, CHAR_CLASS
    public int min, max; // min, max for REPEAT
    public int cap; // capturing index, for CAPTURE
    public string name; // capturing name, for CAPTURE
    public Dictionary<string, int>? namedGroups; // map of group name -> capturing index
                                                // Do update copy ctor when adding new fields!

    public Regexp(Op op)
    {
        this.op = op;
    }

    // Shallow copy constructor.public 
    public Regexp(Regexp that)
    {
        this.op = that.op;
        this.flags = that.flags;
        this.subs = that.subs;
        this.runes = that.runes;
        this.min = that.min;
        this.max = that.max;
        this.cap = that.cap;
        this.name = that.name;
        this.namedGroups = that.namedGroups;
    }

    public void Reinit()
    {
        this.flags = 0;
        subs = EMPTY_SUBS;
        runes = null;
        cap = min = max = 0;
        name = null;
    }

    public override string ToString()
    {
        var builder = new StringBuilder();
        AppendTo(builder);
        return builder.ToString();
    }

    private static void QuoteIfHyphen(StringBuilder builder, int rune)
    {
        if (rune == '-') builder.Append('\\');
    }

    // appendTo() appends the Perl syntax for |this| regular expression to |_out|.
    private void AppendTo(StringBuilder builder)
    {
        switch (op)
        {
            case Op.NO_MATCH:
                builder.Append("[^\\x00-\\x{10FFFF}]");
                break;
            case Op.EMPTY_MATCH:
                builder.Append("(?:)");
                break;
            case Op.STAR:
            case Op.PLUS:
            case Op.QUEST:
            case Op.REPEAT:
                {
                    var sub = subs[0];
                    if (sub.op > Op.CAPTURE
                        || (sub.op == Op.LITERAL && sub.runes.Length > 1))
                    {
                        builder.Append("(?:");
                        sub.AppendTo(builder);
                        builder.Append(')');
                    }
                    else
                    {
                        sub.AppendTo(builder);
                    }
                    switch (op)
                    {
                        case Op.STAR:
                            builder.Append('*');
                            break;
                        case Op.PLUS:
                            builder.Append('+');
                            break;
                        case Op.QUEST:
                            builder.Append('?');
                            break;
                        case Op.REPEAT:
                            builder.Append('{').Append(min);
                            if (min != max)
                            {
                                builder.Append(',');
                                if (max >= 0)
                                {
                                    builder.Append(max);
                                }
                            }
                            builder.Append('}');
                            break;
                    }
                    if ((flags & RE2.NON_GREEDY) != 0)
                    {
                        builder.Append('?');
                    }
                    break;
                }
            case Op.CONCAT:
                foreach (Regexp sub in subs)
                {
                    if (sub.op == Op.ALTERNATE)
                    {
                        builder.Append("(?:");
                        sub.AppendTo(builder);
                        builder.Append(')');
                    }
                    else
                    {
                        sub.AppendTo(builder);
                    }
                }
                break;
            case Op.ALTERNATE:
                {
                    string sep = "";
                    foreach (Regexp sub in subs)
                    {
                        builder.Append(sep);
                        sep = "|";
                        sub.AppendTo(builder);
                    }
                    break;
                }
            case Op.LITERAL:
                if ((flags & RE2.FOLD_CASE) != 0)
                {
                    builder.Append("(?i:");
                }
                foreach (int rune in runes)
                {
                    Utils.EscapeRune(builder, rune);
                }
                if ((flags & RE2.FOLD_CASE) != 0)
                {
                    builder.Append(')');
                }
                break;
            case Op.ANY_CHAR_NOT_NL:
                builder.Append("(?-s:.)");
                break;
            case Op.ANY_CHAR:
                builder.Append("(?s:.)");
                break;
            case Op.CAPTURE:
                if (string.IsNullOrEmpty( name ))
                {
                    builder.Append('(');
                }
                else
                {
                    builder.Append("(?P<");
                    builder.Append(name);
                    builder.Append(">");
                }
                if (subs[0].op != Op.EMPTY_MATCH)
                {
                    subs[0].AppendTo(builder);
                }
                builder.Append(')');
                break;
            case Op.BEGIN_TEXT:
                builder.Append("\\A");
                break;
            case Op.END_TEXT:
                if ((flags & RE2.WAS_DOLLAR) != 0)
                {
                    builder.Append("(?-m:$)");
                }
                else
                {
                    builder.Append("\\z");
                }
                break;
            case Op.BEGIN_LINE:
                builder.Append('^');
                break;
            case Op.END_LINE:
                builder.Append('$');
                break;
            case Op.WORD_BOUNDARY:
                builder.Append("\\b");
                break;
            case Op.NO_WORD_BOUNDARY:
                builder.Append("\\B");
                break;
            case Op.CHAR_CLASS:
                if (runes.Length % 2 != 0)
                {
                    builder.Append("[invalid char class]");
                    break;
                }
                builder.Append('[');
                if (runes.Length == 0)
                {
                    builder.Append("^\\x00-\\x{10FFFF}");
                }
                else if (runes[0] == 0 && runes[runes.Length - 1] == Unicode.MAX_RUNE)
                {
                    // Contains 0 and MAX_RUNE.  Probably a negated class.
                    // Print the gaps.
                    builder.Append('^');
                    for (int i = 1; i < runes.Length - 1; i += 2)
                    {
                        int lo = runes[i] + 1;
                        int hi = runes[i + 1] - 1;
                        QuoteIfHyphen(builder, lo);
                        Utils.EscapeRune(builder, lo);
                        if (lo != hi)
                        {
                            builder.Append('-');
                            QuoteIfHyphen(builder, hi);
                            Utils.EscapeRune(builder, hi);
                        }
                    }
                }
                else
                {
                    for (int i = 0; i < runes.Length; i += 2)
                    {
                        int lo = runes[i];
                        int hi = runes[i + 1];
                        QuoteIfHyphen(builder, lo);
                        Utils.EscapeRune(builder, lo);
                        if (lo != hi)
                        {
                            builder.Append('-');
                            QuoteIfHyphen(builder, hi);
                            Utils.EscapeRune(builder, hi);
                        }
                    }
                }
                builder.Append(']');
                break;
            default: // incl. pseudos
                builder.Append(op);
                break;
        }
    }

    // maxCap() walks the regexp to find the maximum capture index.
    public int MaxCap()
    {
        int m = 0;
        if (op == Op.CAPTURE)
        {
            m = cap;
        }
        if (subs != null)
        {
            foreach (Regexp sub in subs)
            {
                int n = sub.MaxCap();
                if (m < n)
                {
                    m = n;
                }
            }
        }
        return m;
    }

    public override int GetHashCode()
    {
        int hashcode = op.GetHashCode();
        switch (op)
        {
            case Op.END_TEXT:
                hashcode += 31 * (flags & RE2.WAS_DOLLAR);
                break;
            case Op.LITERAL:
            case Op.CHAR_CLASS:
                hashcode += 31 * Utils.GetHashCode(runes);
                break;
            case Op.ALTERNATE:
            case Op.CONCAT:
                hashcode += 31 * Utils.GetHashCode(subs);
                break;
            case Op.STAR:
            case Op.PLUS:
            case Op.QUEST:
                hashcode += 31 * (flags & RE2.NON_GREEDY) + 31 * subs[0].GetHashCode();
                break;
            case Op.REPEAT:
                hashcode += 31 * min + 31 * max + 31 * subs[0].GetHashCode();
                break;
            case Op.CAPTURE:
                hashcode += 31 * cap + 31 * (name != null ? name.GetHashCode() : 0) + 31 * subs[0].GetHashCode();
                break;
        }
        return hashcode;
    }

    // Equals() returns true if this and that have identical structure.
    public override bool Equals(object? that)
    {
        if (!(that is Regexp y))
        {
            return false;
        }
        var x = this;
        if (x.op != y.op)
        {
            return false;
        }
        switch (x.op)
        {
            case Op.END_TEXT:
                // The parse flags remember whether this is \z or \Z.
                if ((x.flags & RE2.WAS_DOLLAR) != (y.flags & RE2.WAS_DOLLAR))
                {
                    return false;
                }
                break;
            case Op.LITERAL:
            case Op.CHAR_CLASS:
                if (!Enumerable.SequenceEqual(x.runes, y.runes))
                {
                    return false;
                }
                break;
            case Op.ALTERNATE:
            case Op.CONCAT:
                if (x.subs.Length != y.subs.Length)
                {
                    return false;
                }
                for (int i = 0; i < x.subs.Length; ++i)
                {
                    if (!x.subs[i].Equals(y.subs[i]))
                    {
                        return false;
                    }
                }
                break;
            case Op.STAR:
            case Op.PLUS:
            case Op.QUEST:
                if ((x.flags & RE2.NON_GREEDY) != (y.flags & RE2.NON_GREEDY)
                    || !x.subs[0].Equals(y.subs[0]))
                {
                    return false;
                }
                break;
            case Op.REPEAT:
                if ((x.flags & RE2.NON_GREEDY) != (y.flags & RE2.NON_GREEDY)
                    || x.min != y.min
                    || x.max != y.max
                    || !x.subs[0].Equals(y.subs[0]))
                {
                    return false;
                }
                break;
            case Op.CAPTURE:
                if (x.cap != y.cap
                    || (x.name == null ? y.name != null : !x.name.Equals(y.name))
                    || !x.subs[0].Equals(y.subs[0]))
                {
                    return false;
                }
                break;
        }
        return true;
    }
}
