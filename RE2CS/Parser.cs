/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/parse.go

// TODO(adonovan):
// - Eliminate allocations (new int[], new Regexp[], new ArrayList) by
//   recycling old arrays on a freelist.
using System.Text;

namespace RE2CS;

/**
 * A parser of regular expression patterns.
 *
 * The only public entry point is {@link #parse(string pattern, int flags)}.
 */
public class Parser
{
    // Unexpected error
    private const string ERR_INTERNAL_ERROR = "regexp/syntax: internal error";

    // Parse errors
    private const string ERR_INVALID_CHAR_CLASS = "invalid character class";
    private const string ERR_INVALID_CHAR_RANGE = "invalid character class range";
    private const string ERR_INVALID_ESCAPE = "invalid escape sequence";
    private const string ERR_INVALID_NAMED_CAPTURE = "invalid named capture";
    private const string ERR_INVALID_PERL_OP = "invalid or unsupported Perl syntax";
    private const string ERR_INVALID_REPEAT_OP = "invalid nested repetition operator";
    private const string ERR_INVALID_REPEAT_SIZE = "invalid repeat count";
    private const string ERR_MISSING_BRACKET = "missing closing ]";
    private const string ERR_MISSING_PAREN = "missing closing )";
    private const string ERR_MISSING_REPEAT_ARGUMENT = "missing argument to repetition operator";
    private const string ERR_TRAILING_BACKSLASH = "trailing backslash at end of expression";
    private const string ERR_DUPLICATE_NAMED_CAPTURE = "duplicate capture group name";

    // Hack to expose ArrayList.removeRange().
    
    private readonly string wholeRegexp;
    // Flags control the behavior of the parser and record information about
    // regexp context.
    private int flags; // parse mode flags

    // Stack of parsed expressions.
    private readonly Stack<Regexp> stack = new ();
    private Regexp? free;
    private int numCap = 0; // number of capturing groups seen
    private readonly Dictionary<string, int> namedGroups = new();

    public Parser(string wholeRegexp, int flags)
    {
        this.wholeRegexp = wholeRegexp;
        this.flags = flags;
    }

    // Allocate a Regexp, from the free list if possible.
    private Regexp NewRegexp(Regexp.Op op)
    {
        var re = free;
        if (re != null && re.subs != null && re.subs.Length > 0)
        {
            free = re.subs[0];
            re.Reinit();
            re.op = op;
        }
        else
        {
            re = new (op);
        }
        return re;
    }

    private void Reuse(Regexp re)
    {
        if (re.subs != null && re.subs.Length > 0)
        {
            re.subs[0] = free;
        }
        free = re;
    }

    // Parse stack manipulation.

    private Regexp Pop() => stack.Pop();

    private Regexp[] PopToPseudo()
    {
        var result = new List<Regexp>();
        while (stack.Count > 0 && ! Regexp.IsPseudo(stack.Peek().op))
        {           
            result.Add(stack.Pop());
        }
        result.Reverse();
        return result.ToArray();
    }

    // push pushes the regexp re onto the parse stack and returns the regexp.
    // Returns null for a CHAR_CLASS that can be merged with the top-of-stack.
    private Regexp Push(Regexp re)
    {
        if (re.op == Regexp.Op.CHAR_CLASS && re.runes.Length == 2 && re.runes[0] == re.runes[1])
        {
            // Collapse range [x-x] -> single rune x.
            if (MaybeConcat(re.runes[0], flags & ~RE2.FOLD_CASE))
            {
                return null;
            }
            re.op = Regexp.Op.LITERAL;
            re.runes = new int[] { re.runes[0] };
            re.flags = flags & ~RE2.FOLD_CASE;
        }
        else if ((re.op == Regexp.Op.CHAR_CLASS
              && re.runes.Length == 4
              && re.runes[0] == re.runes[1]
              && re.runes[2] == re.runes[3]
              && Unicode.SimpleFold(re.runes[0]) == re.runes[2]
              && Unicode.SimpleFold(re.runes[2]) == re.runes[0])
          || (re.op == Regexp.Op.CHAR_CLASS
              && re.runes.Length == 2
              && re.runes[0] + 1 == re.runes[1]
              && Unicode.SimpleFold(re.runes[0]) == re.runes[1]
              && Unicode.SimpleFold(re.runes[1]) == re.runes[0]))
        {
            // Case-insensitive rune like [Aa] or [Δδ].
            if (MaybeConcat(re.runes[0], flags | RE2.FOLD_CASE))
            {
                return null;
            }

            // Rewrite as (case-insensitive) literal.
            re.op = Regexp.Op.LITERAL;
            re.runes = new int[] { re.runes[0] };
            re.flags = flags | RE2.FOLD_CASE;
        }
        else
        {
            // Incremental concatenation.
            MaybeConcat(-1, 0);
        }

        stack.Push(re);
        return re;
    }

    // maybeConcat implements incremental concatenation
    // of literal runes into string nodes.  The parser calls this
    // before each push, so only the top fragment of the stack
    // might need processing.  Since this is called before a push,
    // the topmost literal is no longer subject to operators like *
    // (Otherwise ab* would turn into (ab)*.)
    // If (r >= 0 and there's a node left over, maybeConcat uses it
    // to push r with the given flags.
    // maybeConcat reports whether r was pushed.
    private bool MaybeConcat(int r, int flags)
    {
        int n = stack.Count;
        if (n < 2)
        {
            return false;
        }
        Regexp re1 = stack.Pop();
        Regexp re2 = stack.Pop();
        stack.Push(re2);
        stack.Push(re1);
        if (re1.op != Regexp.Op.LITERAL
            || re2.op != Regexp.Op.LITERAL
            || (re1.flags & RE2.FOLD_CASE) != (re2.flags & RE2.FOLD_CASE))
        {
            return false;
        }

        // Push re1 into re2.
        re2.runes = ConcatRunes(re2.runes, re1.runes);

        // Reuse re1 if possible.
        if (r >= 0)
        {
            re1.runes = new int[] { r };
            re1.flags = flags;
            return true;
        }

        Pop();
        Reuse(re1);
        return false; // did not push r
    }

    // newLiteral returns a new LITERAL Regexp with the given flags
    private Regexp NewLiteral(int r, int flags)
    {
        var re = NewRegexp(Regexp.Op.LITERAL);
        re.flags = flags;
        if ((flags & RE2.FOLD_CASE) != 0)
        {
            r = MinFoldRune(r);
        }
        re.runes = new int[] { r };
        return re;
    }

    // minFoldRune returns the minimum rune fold-equivalent to r.
    private static int MinFoldRune(int r)
    {
        if (r < Unicode.MIN_FOLD || r > Unicode.MAX_FOLD)
        {
            return r;
        }
        int min = r;
        int r0 = r;
        for (r = Unicode.SimpleFold(r); r != r0; r = Unicode.SimpleFold(r))
        {
            if (min > r)
            {
                min = r;
            }
        }
        return min;
    }

    // literal pushes a literal regexp for the rune r on the stack
    // and returns that regexp.
    private void Literal(int r)
    {
        Push(NewLiteral(r, flags));
    }

    // op pushes a regexp with the given op onto the stack
    // and returns that regexp.
    private Regexp Op(Regexp.Op op)
    {
        var re = NewRegexp(op);
        re.flags = flags;
        return Push(re);
    }

    // repeat replaces the top stack element with itself repeated according to
    // op, min, max.  beforePos is the start position of the repetition operator.
    // Pre: t is positioned after the initial repetition operator.
    // Post: t advances past an optional perl-mode '?', or stays Add.
    //       Or, it fails with PatternSyntaxException.
    private void Repeat(
        Regexp.Op op, int min, int max, int beforePos, StringIterator t, int lastRepeatPos)
    {
        int flags = this.flags;
        if ((flags & RE2.PERL_X) != 0) {
            if (t.HasMore&& t.LookingAt('?'))
            {
                t.Skip(1); // '?'
                flags ^= RE2.NON_GREEDY;
            }
            if (lastRepeatPos != -1)
            {
                // In Perl it is not allowed to stack repetition operators:
                // a** is a syntax error, not a doubled star, and a++ means
                // something else entirely, which we don't support!
                throw new PatternSyntaxException(ERR_INVALID_REPEAT_OP, t.From(lastRepeatPos));
            }
        }
        int n = stack.Count;
        if (n == 0) {
            throw new PatternSyntaxException(ERR_MISSING_REPEAT_ARGUMENT, t.From(beforePos));
        }
        var sub = stack.Peek();
        if (Regexp.IsPseudo(sub.op))
        {
            throw new PatternSyntaxException(ERR_MISSING_REPEAT_ARGUMENT, t.From(beforePos));
        }
        Regexp re = NewRegexp(op);
        re.min = min;
        re.max = max;
        re.flags = flags;
        re.subs = new Regexp[] { sub };
        stack.Pop();
        stack.Push(re);
        //stack.set(n - 1, re);
    }

    // concat replaces the top of the stack (above the topmost '|' or '(') with
    // its concatenation.
    private Regexp Concat()
    {
        MaybeConcat(-1, 0);

        // Scan down to find pseudo-operator | or (.
        var subs = PopToPseudo();

        // Empty concatenation is special case.
        if (subs.Length == 0)
        {
            return Push(NewRegexp(Regexp.Op.EMPTY_MATCH));
        }

        return Push(Collapse(subs, Regexp.Op.CONCAT));
    }

    // alternate replaces the top of the stack (above the topmost '(') with its
    // alternation.
    private Regexp Alternate()
    {
        // Scan down to find pseudo-operator (.
        // There are no | above (.
        var subs = PopToPseudo();

        // Make sure top class is clean.
        // All the others already are (see swapVerticalBar).
        if (subs.Length > 0)
        {
            CleanAlt(subs[subs.Length - 1]);
        }

        // Empty alternate is special case
        // (shouldn't happen but easy to handle).
        if (subs.Length == 0)
        {
            return Push(NewRegexp(Regexp.Op.NO_MATCH));
        }

        return Push(Collapse(subs, Regexp.Op.ALTERNATE));
    }

    // cleanAlt cleans re for eventual inclusion in an alternation.
    private void CleanAlt(Regexp re)
    {
        if (re.op == Regexp.Op.CHAR_CLASS)
        {
            re.runes = new CharClass(re.runes).CleanClass().ToArray();
            if (re.runes.Length == 2 && re.runes[0] == 0 && re.runes[1] == Unicode.MAX_RUNE)
            {
                re.runes = null;
                re.op = Regexp.Op.ANY_CHAR;
            }
            else if (re.runes.Length == 4
              && re.runes[0] == 0
              && re.runes[1] == '\n' - 1
              && re.runes[2] == '\n' + 1
              && re.runes[3] == Unicode.MAX_RUNE)
            {
                re.runes = null;
                re.op = Regexp.Op.ANY_CHAR_NOT_NL;
            }
        }
    }

    // collapse returns the result of applying op to subs[start:end].
    // If (sub contains op nodes, they all get hoisted up
    // so that there is never a concat of a concat or an
    // alternate of an alternate.
    private Regexp Collapse(Regexp[] subs, Regexp.Op op)
    {
        if (subs.Length == 1)
        {
            return subs[0];
        }
        // Concatenate subs iff op is same.
        // Compute Length in first pass.
        int len = 0;
        foreach (Regexp sub in subs)
        {
            len += (sub.op == op) ? sub.subs.Length : 1;
        }
        Regexp[] newsubs = new Regexp[len];
        int i = 0;
        foreach (Regexp sub in subs)
        {
            if (sub.op == op)
            {
                Array.Copy(sub.subs, 0, newsubs, i, sub.subs.Length);
                i += sub.subs.Length;
                Reuse(sub);
            }
            else
            {
                newsubs[i++] = sub;
            }
        }
        Regexp re = NewRegexp(op);
        re.subs = newsubs;

        if (op == Regexp.Op.ALTERNATE)
        {
            re.subs = Factor(re.subs, re.flags);
            if (re.subs.Length == 1)
            {
                Regexp old = re;
                re = re.subs[0];
                Reuse(old);
            }
        }
        return re;
    }

    // factor factors common prefixes from the alternation list sub.  It
    // returns a replacement list that reuses the same storage and frees
    // (passes to p.reuse) any removed *Regexps.
    //
    // For example,
    //     ABC|ABD|AEF|BCX|BCY
    // simplifies by literal prefix extraction to
    //     A(B(C|D)|EF)|BC(X|Y)
    // which simplifies by character class introduction to
    //     A(B[CD]|EF)|BC[XY]
    //
    private Regexp[] Factor(Regexp[] array, int flags)
    {
        if (array.Length < 2)
        {
            return array;
        }

        // The following code is subtle, because it's a literal Java
        // translation of code that makes clever use of Go "slices".
        // A slice is a triple (array, offset, Length), and the Go
        // implementation uses two slices, |sub| and |out| backed by the
        // same array.  In Java, we have to be explicit about all of these
        // variables, so:
        //
        // Go    Java
        // sub   (array, s, lensub)
        // out   (array, 0, lenout)   // (always a prefix of |array|)
        //
        // In the comments we'll use the logical notation of go slices, e.g. sub[i]
        // even though the Java code will read array[s + i].

        int s = 0; // offset of first |sub| within array.
        int lensub = array.Length; // = len(sub)
        int lenout = 0; // = len(out)

        // Round 1: Factor out common literal prefixes.
        // Note: (str, strlen) and (istr, istrlen) are like Go slices
        // onto a prefix of some Regexp's runes array (hence offset=0).
        int[] str = null;
        int strlen = 0;
        int strflags = 0;
        int start = 0;
        for (int i = 0; i <= lensub; i++)
        {
            // Invariant: the Regexps that were in sub[0:start] have been
            // used or marked for reuse, and the slice space has been reused
            // for out (len <= start).
            //
            // Invariant: sub[start:i] consists of regexps that all begin
            // with str as modified by strflags.
            int[] istr = null;
            int istrlen = 0;
            int iflags = 0;
            if (i < lensub)
            {
                // NB, we inlined Go's leadingString() since Java has no pair return.
                Regexp re = array[s + i];
                if (re.op == Regexp.Op.CONCAT && re.subs.Length > 0)
                {
                    re = re.subs[0];
                }
                if (re.op == Regexp.Op.LITERAL)
                {
                    istr = re.runes;
                    istrlen = re.runes.Length;
                    iflags = re.flags & RE2.FOLD_CASE;
                }
                // istr is the leading literal string that re begins with.
                // The string refers to storage in re or its children.

                if (iflags == strflags)
                {
                    int same = 0;
                    while (same < strlen && same < istrlen && str[same] == istr[same])
                    {
                        same++;
                    }
                    if (same > 0)
                    {
                        // Matches at least one rune in current range.
                        // Keep going around.
                        strlen = same;
                        continue;
                    }
                }
            }

            // Found end of a run with common leading literal string:
            // sub[start:i] all begin with str[0:strlen], but sub[i]
            // does not even begin with str[0].
            //
            // Factor out common string and append factored expression to out.
            if (i == start)
            {
                // Nothing to do - run of Length 0.
            }
            else if (i == start + 1)
            {
                // Just one: don't bother factoring.
                array[lenout++] = array[s + start];
            }
            else
            {
                // Construct factored form: prefix(suffix1|suffix2|...)
                Regexp prefix = NewRegexp(Regexp.Op.LITERAL);
                prefix.flags = strflags;
                prefix.runes = Utils.Subarray(str, 0, strlen);

                for (int j = start; j < i; j++)
                {
                    array[s + j] = RemoveLeadingString(array[s + j], strlen);
                }
                // Recurse.
                Regexp suffix = Collapse(Subarray(array, s + start, s + i), Regexp.Op.ALTERNATE);
                Regexp re = NewRegexp(Regexp.Op.CONCAT);
                re.subs = new Regexp[] { prefix, suffix };
                array[lenout++] = re;
            }

            // Prepare for next iteration.
            start = i;
            str = istr;
            strlen = istrlen;
            strflags = iflags;
        }
        // In Go: sub = out
        lensub = lenout;
        s = 0;

        // Round 2: Factor out common complex prefixes,
        // just the first piece of each concatenation,
        // whatever it is.  This is good enough a lot of the time.
        start = 0;
        lenout = 0;
        Regexp first = null;
        for (int i = 0; i <= lensub; i++)
        {
            // Invariant: the Regexps that were in sub[0:start] have been
            // used or marked for reuse, and the slice space has been reused
            // for out (lenout <= start).
            //
            // Invariant: sub[start:i] consists of regexps that all begin with
            // ifirst.
            Regexp ifirst = null;
            if (i < lensub)
            {
                ifirst = LeadingRegexp(array[s + i]);
                if (first != null
                    && first.Equals(ifirst)
                    && (IsCharClass(first)
                        || (first.op == Regexp.Op.REPEAT
                            && first.min == first.max
                            && IsCharClass(first.subs[0]))))
                {
                    continue;
                }
            }

            // Found end of a run with common leading regexp:
            // sub[start:i] all begin with first but sub[i] does not.
            //
            // Factor out common regexp and append factored expression to out.
            if (i == start)
            {
                // Nothing to do - run of Length 0.
            }
            else if (i == start + 1)
            {
                // Just one: don't bother factoring.
                array[lenout++] = array[s + start];
            }
            else
            {
                // Construct factored form: prefix(suffix1|suffix2|...)
                Regexp prefix = first;
                for (int j = start; j < i; j++)
                {
                    bool reuse = j != start; // prefix came from sub[start]
                    array[s + j] = RemoveLeadingRegexp(array[s + j], reuse);
                }
                // recurse
                Regexp suffix = Collapse(Subarray(array, s + start, s + i), Regexp.Op.ALTERNATE);
                Regexp re = NewRegexp(Regexp.Op.CONCAT);
                re.subs = new Regexp[] { prefix, suffix };
                array[lenout++] = re;
            }

            // Prepare for next iteration.
            start = i;
            first = ifirst;
        }
        // In Go: sub = out
        lensub = lenout;
        s = 0;

        // Round 3: Collapse runs of single literals into character classes.
        start = 0;
        lenout = 0;
        for (int i = 0; i <= lensub; i++)
        {
            // Invariant: the Regexps that were in sub[0:start] have been
            // used or marked for reuse, and the slice space has been reused
            // for out (lenout <= start).
            //
            // Invariant: sub[start:i] consists of regexps that are either
            // literal runes or character classes.
            if (i < lensub && IsCharClass(array[s + i]))
            {
                continue;
            }

            // sub[i] is not a char or char class;
            // emit char class for sub[start:i]...
            if (i == start)
            {
                // Nothing to do - run of Length 0.
            }
            else if (i == start + 1)
            {
                array[lenout++] = array[s + start];
            }
            else
            {
                // Make new char class.
                // Start with most complex regexp in sub[start].
                int max = start;
                for (int j = start + 1; j < i; j++)
                {
                    Regexp subMax = array[s + max], subJ = array[s + j];
                    if (subMax.op < subJ.op
                        || (subMax.op == subJ.op && subMax.runes.Length < subJ.runes.Length))
                    {
                        max = j;
                    }
                }
                // swap sub[start], sub[max].
                Regexp tmp = array[s + start];
                array[s + start] = array[s + max];
                array[s + max] = tmp;

                for (int j = start + 1; j < i; j++)
                {
                    MergeCharClass(array[s + start], array[s + j]);
                    Reuse(array[s + j]);
                }
                CleanAlt(array[s + start]);
                array[lenout++] = array[s + start];
            }

            // ... and then emit sub[i].
            if (i < lensub)
            {
                array[lenout++] = array[s + i];
            }
            start = i + 1;
        }
        // In Go: sub = out
        lensub = lenout;
        s = 0;

        // Round 4: Collapse runs of empty matches into a single empty match.
        start = 0;
        lenout = 0;
        for (int i = 0; i < lensub; ++i)
        {
            if (i + 1 < lensub
                && array[s + i].op == Regexp.Op.EMPTY_MATCH
                && array[s + i + 1].op == Regexp.Op.EMPTY_MATCH)
            {
                continue;
            }
            array[lenout++] = array[s + i];
        }
        // In Go: sub = out
        lensub = lenout;
        s = 0;

        return Subarray(array, s, lensub);
    }

    // removeLeadingString removes the first n leading runes
    // from the beginning of re.  It returns the replacement for re.
    private Regexp RemoveLeadingString(Regexp re, int n)
    {
        if (re.op == Regexp.Op.CONCAT && re.subs.Length > 0)
        {
            // Removing a leading string in a concatenation
            // might simplify the concatenation.
            Regexp sub = RemoveLeadingString(re.subs[0], n);
            re.subs[0] = sub;
            if (sub.op == Regexp.Op.EMPTY_MATCH)
            {
                Reuse(sub);
                switch (re.subs.Length)
                {
                    case 0:
                    case 1:
                        // Impossible but handle.
                        re.op = Regexp.Op.EMPTY_MATCH;
                        re.subs = null;
                        break;
                    case 2:
                        {
                            Regexp old = re;
                            re = re.subs[1];
                            Reuse(old);
                            break;
                        }
                    default:
                        re.subs = Subarray(re.subs, 1, re.subs.Length);
                        break;
                }
            }
            return re;
        }

        if (re.op == Regexp.Op.LITERAL)
        {
            re.runes = Utils.Subarray(re.runes, n, re.runes.Length);
            if (re.runes.Length == 0)
            {
                re.op = Regexp.Op.EMPTY_MATCH;
            }
        }
        return re;
    }

    // leadingRegexp returns the leading regexp that re begins with.
    // The regexp refers to storage in re or its children.
    private static Regexp LeadingRegexp(Regexp re)
    {
        if (re.op == Regexp.Op.EMPTY_MATCH)
        {
            return null;
        }
        if (re.op == Regexp.Op.CONCAT && re.subs.Length > 0)
        {
            Regexp sub = re.subs[0];
            if (sub.op == Regexp.Op.EMPTY_MATCH)
            {
                return null;
            }
            return sub;
        }
        return re;
    }

    // removeLeadingRegexp removes the leading regexp in re.
    // It returns the replacement for re.
    // If reuse is true, it passes the removed regexp (if no longer needed) to
    // reuse.
    private Regexp RemoveLeadingRegexp(Regexp re, bool _reuse)
    {
        if (re.op == Regexp.Op.CONCAT && re.subs.Length > 0)
        {
            if (_reuse)
            {
                Reuse(re.subs[0]);
            }
            re.subs = Subarray(re.subs, 1, re.subs.Length);
            switch (re.subs.Length)
            {
                case 0:
                    re.op = Regexp.Op.EMPTY_MATCH;
                    re.subs = Regexp.EMPTY_SUBS;
                    break;
                case 1:
                    Regexp old = re;
                    re = re.subs[0];
                    Reuse(old);
                    break;
            }
            return re;
        }
        if (_reuse)
        {
            Reuse(re);
        }
        return NewRegexp(Regexp.Op.EMPTY_MATCH);
    }

    private static Regexp LiteralRegexp(string s, int flags)
    {
        var re = new Regexp(Regexp.Op.LITERAL);
        re.flags = flags;
        re.runes = Utils.StringToRunes(s);
        return re;
    }

    // Parsing.

    // StringIterator: a stream of runes with an opaque cursor, permitting
    // rewinding.  The units of the cursor are not specified beyond the
    // fact that ASCII characters are single width.  (Cursor positions
    // could be UTF-8 byte indices, UTF-16 code indices or rune indices.)
    //
    // In particular, be careful with:
    // - skip(int): only use this to advance over ASCII characters
    //   since these always have a width of 1.
    // - skip(string): only use this to advance over strings which are
    //   known to be at the current position, e.g. due to prior call to
    //   lookingAt().
    // Only use pop() to advance over possibly non-ASCII runes.
    public class StringIterator
    {
        private readonly string str; // a stream of UTF-16 codes
        private int _pos = 0; // current position in UTF-16 string

        public StringIterator(string str)
        {
            this.str = str;
        }

        // Returns the cursor position.  Do not interpret the result!
        public int Pos => _pos;

        // Resets the cursor position to a previous value returned by pos().
        public void RewindTo(int pos) => this._pos = pos;

        // Returns true unless the stream is exhausted.
        public bool HasMore => _pos < str.Length;

        // Returns the rune at the cursor position.
        // Precondition: |more()|.
        public int Peek()
        {
            if(_pos>=str.Length)
            {
                return -1;
            }
            return char.ConvertToUtf32(str, _pos);// str[_pos];
        }
        // Advances the cursor by |n| positions, which must be ASCII runes.
        //
        // (In practise, this is only ever used to skip over regexp
        // metacharacters that are ASCII, so there is no numeric difference
        // between indices into  UTF-8 bytes, UTF-16 codes and runes.)
        public void Skip(int n) => _pos += n;

        // Advances the cursor by the number of cursor positions in |s|.
        public void SkipString(string s) => _pos += s.Length;

        // Returns the rune at the cursor position, and advances the cursor
        // past it.  Precondition: |more()|.
        public int Pop()
        {
            int r = char.ConvertToUtf32(str,_pos);// str.codePointAt(Pos);
            _pos += new Rune(r).Utf16SequenceLength;//. Character.charCount(r);
            return r;
        }

        // Equivalent to both peek() == c but more efficient because we
        // don't support surrogates.  Precondition: |more()|.
        public bool LookingAt(char c) => str[_pos] == c;

        // Equivalent to rest().StartsWith(s).
        public bool LookingAt(string s) => Rest().StartsWith(s);

        // Returns the rest of the pattern as a Java UTF-16 string.
        public string Rest() => str.Substring(_pos);

        // Returns the Substring from |beforePos| to the current position.
        // |beforePos| must have been previously returned by |pos()|.
        public string From(int beforePos) => str.Substring(beforePos, _pos - beforePos);

        public override string ToString() => Rest();
    }

    /**
     * Parse regular expression pattern {@var pattern} with mode flags {@var flags}.
     */
    public static Regexp Parse(string pattern, int flags) 
        => new Parser(pattern, flags).ParseInternal();

    private Regexp ParseInternal()
    {
        if ((flags & RE2.LITERAL) != 0) {
            // Trivial parser for literal string.
            return LiteralRegexp(wholeRegexp, flags);
        }

        // Otherwise, must do real work.
        int lastRepeatPos = -1, min = -1, max = -1;
        var t = new StringIterator(wholeRegexp);
        while (t.HasMore)
        {
            int repeatPos = -1;
        bigswitch:
            switch (t.Peek())
            {
                default:
                    Literal(t.Pop());
                    break;

                case '(':
                    if ((flags & RE2.PERL_X) != 0 && t.LookingAt("(?"))
                    {
                        // Flag changes and non-capturing groups.
                        ParsePerlFlags(t);
                        break;
                    }
                    Op(Regexp.Op.LEFT_PAREN).cap = ++numCap;
                    t.Skip(1); // '('
                    break;

                case '|':
                    ParseVerticalBar();
                    t.Skip(1); // '|'
                    break;

                case ')':
                    ParseRightParen();
                    t.Skip(1); // ')'
                    break;

                case '^':
                    if ((flags & RE2.ONE_LINE) != 0)
                    {
                        Op(Regexp.Op.BEGIN_TEXT);
                    }
                    else
                    {
                        Op(Regexp.Op.BEGIN_LINE);
                    }
                    t.Skip(1); // '^'
                    break;

                case '$':
                    if ((flags & RE2.ONE_LINE) != 0)
                    {
                        Op(Regexp.Op.END_TEXT).flags |= RE2.WAS_DOLLAR;
                    }
                    else
                    {
                        Op(Regexp.Op.END_LINE);
                    }
                    t.Skip(1); // '$'
                    break;

                case '.':
                    if ((flags & RE2.DOT_NL) != 0)
                    {
                        Op(Regexp.Op.ANY_CHAR);
                    }
                    else
                    {
                        Op(Regexp.Op.ANY_CHAR_NOT_NL);
                    }
                    t.Skip(1); // '.'
                    break;

                case '[':
                    ParseClass(t);
                    break;

                case '*':
                case '+':
                case '?':
                    {
                        repeatPos = t.Pos;
                        Regexp.Op op = Regexp.Op.ANY_CHAR;
                        switch (t.Pop())
                        {
                            case '*':
                                op = Regexp.Op.STAR;
                                break;
                            case '+':
                                op = Regexp.Op.PLUS;
                                break;
                            case '?':
                                op = Regexp.Op.QUEST;
                                break;
                        }
                        Repeat(op, min, max, repeatPos, t, lastRepeatPos);
                        // (min and max are now dead.)
                        break;
                    }
                case '{':
                    {
                        repeatPos = t.Pos;
                        int minMax = ParseRepeat(t);
                        if (minMax < 0)
                        {
                            // If the repeat cannot be parsed, { is a literal.
                            t.RewindTo(repeatPos);
                            Literal(t.Pop()); // '{'
                            break;
                        }
                        min = minMax >> 16;
                        max = (short)(minMax & 0xffff); // sign extend
                        Repeat(Regexp.Op.REPEAT, min, max, repeatPos, t, lastRepeatPos);
                        break;
                    }

                case '\\':
                    {
                        int savedPos = t.Pos;
                        t.Skip(1); // '\\'
                        if ((flags & RE2.PERL_X) != 0 && t.HasMore)
                        {
                            int c = t.Pop();
                            switch (c)
                            {
                                case 'A':
                                    Op(Regexp.Op.BEGIN_TEXT);
                                    goto outswitch;
                                case 'b':
                                    Op(Regexp.Op.WORD_BOUNDARY);
                                    goto outswitch;
                                case 'B':
                                    Op(Regexp.Op.NO_WORD_BOUNDARY);
                                    goto outswitch;
                                case 'C':
                                    // any byte; not supported
                                    throw new PatternSyntaxException(ERR_INVALID_ESCAPE, "\\C");
                                case 'Q':
                                    {
                                        // \Q ... \E: the ... is always literals
                                        string lit = t.Rest();
                                        int i = lit.IndexOf("\\E");
                                        if (i >= 0)
                                        {
                                            lit = lit.Substring(0, i - 0);
                                        }
                                        t.SkipString(lit);
                                        t.SkipString("\\E");
                                        for (int j = 0; j < lit.Length;)
                                        {
                                            int codepoint = char.ConvertToUtf32(lit, j);// lit.codePointAt(j);
                                            Literal(codepoint);
                                            j += new Rune(codepoint).Utf16SequenceLength;// Character.charCount(codepoint);
                                        }
                                        goto outswitch;
                                    }
                                case 'z':
                                    Op(Regexp.Op.END_TEXT);
                                    goto outswitch;
                                default:
                                    t.RewindTo(savedPos);
                                    break;
                            }
                        }

                        var re = NewRegexp(Regexp.Op.CHAR_CLASS);
                        re.flags = flags;

                        // Look for Unicode character group like \p{Han}
                        if (t.LookingAt("\\p") || t.LookingAt("\\P"))
                        {
                            var cc2 = new CharClass();
                            if (ParseUnicodeClass(t, cc2))
                            {
                                re.runes = cc2.ToArray();
                                Push(re);
                                goto outswitch;
                            }
                        }

                        // Perl character class escape.
                        var cc = new CharClass();
                        if (ParsePerlClassEscape(t, cc))
                        {
                            re.runes = cc.ToArray();
                            Push(re);
                            goto outswitch;
                        }

                        t.RewindTo(savedPos);
                        Reuse(re);

                        // Ordinary single-character escape.
                        Literal(ParseEscape(t));
                        break;
                    }
            }
        outswitch:
            lastRepeatPos = repeatPos;
        }

        Concat();
        if (SwapVerticalBar())
        {
            Pop(); // pop vertical bar
        }
        Alternate();

        int n = stack.Count;
        if (n != 1)
        {
            throw new PatternSyntaxException(ERR_MISSING_PAREN, wholeRegexp);
        }
        stack.Peek().namedGroups = namedGroups;
        return stack.Peek();
    }

    // parseRepeat parses {min} (max=min) or {min,} (max=-1) or {min,max}.
    // If |t| is not of that form, it returns -1.
    // If |t| has the right form but the values are negative or too big,
    // it returns -2.
    // On success, returns a nonnegative number encoding min/max in the
    // high/low signed halfwords of the result.  (Note: min >= 0; max may
    // be -1.)
    //
    // On success, advances |t| beyond the repeat; otherwise |t.pos()| is
    // undefined.
    private static int ParseRepeat(StringIterator t)
    {
        int start = t.Pos;
        if (!t.HasMore|| !t.LookingAt('{')) {
            return -1;
        }
        t.Skip(1); // '{'
        int min = ParseInt(t); // (can be -2)
        if (min == -1) {
            return -1;
        }
        if (!t.HasMore) {
            return -1;
        }
        int max;
        if (!t.LookingAt(',')) {
            max = min;
        } else {
            t.Skip(1); // ','
            if (!t.HasMore)
            {
                return -1;
            }
            if (t.LookingAt('}'))
            {
                max = -1;
            }
            else if ((max = ParseInt(t)) == -1)
            { // (can be -2)
                return -1;
            }
        }
        if (!t.HasMore|| !t.LookingAt('}')) {
            return -1;
        }
        t.Skip(1); // '}'
        if (min < 0 || min > 1000 || max == -2 || max > 1000 || (max >= 0 && min > max)) {
            // Numbers were negative or too big, or max is present and min > max.
            throw new PatternSyntaxException(ERR_INVALID_REPEAT_SIZE, t.From(start));
        }
        return (min << 16) | (max & 0xffff); // success
    }

    // parsePerlFlags parses a Perl flag setting or non-capturing group or both,
    // like (?i) or (?: or (?i:.
    // Pre: t at "(?".  Post: t after ")".
    // Sets numCap.
    private void ParsePerlFlags(StringIterator t)
    {
        int startPos = t.Pos;

        // Check for named captures, first introduced in Python's regexp library.
        // As usual, there are three slightly different syntaxes:
        //
        //   (?P<name>expr)   the original, introduced by Python
        //   (?<name>expr)    the .NET alteration, adopted by Perl 5.10
        //   (?'name'expr)    another .NET alteration, adopted by Perl 5.10
        //
        // Perl 5.10 gave in and implemented the Python version too,
        // but they claim that the last two are the preferred forms.
        // PCRE and languages based on it (specifically, PHP and Ruby)
        // support all three as well.  EcmaScript 4 uses only the Python form.
        //
        // In both the open source world (via Code Search) and the
        // Google source tree, (?P<expr>name) is the dominant form,
        // so that's the one we implement.  One is enough.
        string s = t.Rest();
        if (s.StartsWith("(?P<")) {
            // Pull out name.
            int end = s.IndexOf('>');
            if (end < 0)
            {
                throw new PatternSyntaxException(ERR_INVALID_NAMED_CAPTURE, s);
            }
            string name = s.Substring(4, end-4); // "name"
            t.SkipString(name);
            t.Skip(5); // "(?P<>"
            if (!IsValidCaptureName(name))
            {
                throw new PatternSyntaxException(
                    ERR_INVALID_NAMED_CAPTURE, s.Substring(0, end - 0)); // "(?P<name>"
            }
            // Like ordinary capture, but named.
            Regexp re = Op(Regexp.Op.LEFT_PAREN);
            re.cap = ++numCap;
            if (namedGroups.ContainsKey(name))
            {
                throw new PatternSyntaxException(ERR_DUPLICATE_NAMED_CAPTURE, name);
            }
            namedGroups.Add(name, numCap);
            re.name = name;
            return;
        }

        // Non-capturing group.  Might also twiddle Perl flags.
        t.Skip(2); // "(?"
        int flags = this.flags;
        int sign = +1;
        bool sawFlag = false;
    loop:
        while (t.HasMore) {
            int c = t.Pop();
            switch (c)
            {
                default:
                    goto exit;
                // Flags.
                case 'i':
                    flags |= RE2.FOLD_CASE;
                    sawFlag = true;
                    break;
                case 'm':
                    flags &= ~RE2.ONE_LINE;
                    sawFlag = true;
                    break;
                case 's':
                    flags |= RE2.DOT_NL;
                    sawFlag = true;
                    break;
                case 'U':
                    flags |= RE2.NON_GREEDY;
                    sawFlag = true;
                    break;

                // Switch to negation.
                case '-':
                    if (sign < 0)
                    {
                        goto exit;
                    }
                    sign = -1;
                    // Invert flags so that | above turn into &~ and vice versa.
                    // We'll invert flags again before using it below.
                    flags = ~flags;
                    sawFlag = false;
                    break;

                // End of flags, starting group or not.
                case ':':
                case ')':
                    if (sign < 0)
                    {
                        if (!sawFlag)
                        {
                            goto exit;
                        }
                        flags = ~flags;
                    }
                    if (c == ':')
                    {
                        // Open new group
                        Op(Regexp.Op.LEFT_PAREN);
                    }
                    this.flags = flags;
                    return;
            }
        }
    exit:
        throw new PatternSyntaxException(ERR_INVALID_PERL_OP, t.From(startPos));
    }

    // isValidCaptureName reports whether name
    // is a valid capture name: [A-Za-z0-9_]+.
    // PCRE limits names to 32 bytes.
    // Python rejects names starting with digits.
    // We don't enforce either of those.
    private static bool IsValidCaptureName(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }
        for (int i = 0; i < name.Length; ++i)
        {
            char c = name[i];
            if (c != '_' && !Utils.Isalnum(c))
            {
                return false;
            }
        }
        return true;
    }

    // parseInt parses a nonnegative decimal integer.
    // -1 => bad format.  -2 => format ok, but integer overflow.
    private static int ParseInt(StringIterator t)
    {
        int start = t.Pos;
        int c;
        while (t.HasMore&& (c = t.Peek()) >= '0' && c <= '9')
        {
            t.Skip(1); // digit
        }
        string n = t.From(start);
        if (string.IsNullOrEmpty(n) || (n.Length > 1 && n[0] == '0'))
        { // disallow leading zeros
            return -1; // bad format
        }
        if (n.Length> 8)
        {
            return -2; // overflow
        }

        return int.TryParse(n, out var r) ? r : -1;//  Integer.valueOf(n, 10); // can't fail
    }

    // can this be represented as a character class?
    // single-rune literal string, char class, ., and .|\n.
    private static bool IsCharClass(Regexp re) => ((re.op == Regexp.Op.LITERAL && re.runes.Length == 1)
            || re.op == Regexp.Op.CHAR_CLASS
            || re.op == Regexp.Op.ANY_CHAR_NOT_NL
            || re.op == Regexp.Op.ANY_CHAR);

    // does re match r?
    private static bool MatchRune(Regexp re, int r)
    {
        switch (re.op)
        {
            case Regexp.Op.LITERAL:
                return re.runes.Length == 1 && re.runes[0] == r;
            case Regexp.Op.CHAR_CLASS:
                for (int i = 0; i < re.runes.Length; i += 2)
                {
                    if (re.runes[i] <= r && r <= re.runes[i + 1])
                    {
                        return true;
                    }
                }
                return false;
            case Regexp.Op.ANY_CHAR_NOT_NL:
                return r != '\n';
            case Regexp.Op.ANY_CHAR:
                return true;
        }
        return false;
    }

    // parseVerticalBar handles a | in the input.
    private void ParseVerticalBar()
    {
        Concat();

        // The concatenation we just parsed is on top of the stack.
        // If it sits above an opVerticalBar, swap it below
        // (things below an opVerticalBar become an alternation).
        // Otherwise, push a new vertical bar.
        if (!SwapVerticalBar())
        {
            Op(Regexp.Op.VERTICAL_BAR);
        }
    }

    // mergeCharClass makes dst = dst|src.
    // The caller must ensure that dst.Op >= src.Op,
    // to reduce the amount of copying.
    private static void MergeCharClass(Regexp dst, Regexp src)
    {
        switch (dst.op)
        {
            case Regexp.Op.ANY_CHAR:
                // src doesn't add anything.
                break;
            case Regexp.Op.ANY_CHAR_NOT_NL:
                // src might add \n
                if (MatchRune(src, '\n'))
                {
                    dst.op = Regexp.Op.ANY_CHAR;
                }
                break;
            case Regexp.Op.CHAR_CLASS:
                // src is simpler, so either literal or char class
                if (src.op == Regexp.Op.LITERAL)
                {
                    dst.runes = new CharClass(dst.runes).AppendLiteral(src.runes[0], src.flags).ToArray();
                }
                else
                {
                    dst.runes = new CharClass(dst.runes).AppendClass(src.runes).ToArray();
                }
                break;
            case Regexp.Op.LITERAL:
                // both literal
                if (src.runes[0] == dst.runes[0] && src.flags == dst.flags)
                {
                    break;
                }
                dst.op = Regexp.Op.CHAR_CLASS;
                dst.runes =
                    new CharClass()
                        .AppendLiteral(dst.runes[0], dst.flags)
                        .AppendLiteral(src.runes[0], src.flags)
                        .ToArray();
                break;
        }
    }

    // If the top of the stack is an element followed by an opVerticalBar
    // swapVerticalBar swaps the two and returns true.
    // Otherwise it returns false.
    private bool SwapVerticalBar()
    {
        // If above and below vertical bar are literal or char class,
        // can merge into a single char class.
        int n = stack.Count;

        Regexp re1 = null;
        Regexp re2 = null;
        Regexp re3 = null;
        if (n >= 3)
        {
            re1 = stack.Pop();
            re2 = stack.Pop();
            re3 = stack.Peek();

            if (re2.op == Regexp.Op.VERTICAL_BAR
                && IsCharClass(re1)
                && IsCharClass(re3))
            {
                // Make re3 the more complex of the two.
                if (re1.op > re3.op)
                {
                    var tmp = re3;
                    re3 = re1;
                    re1 = tmp;
                    stack.Pop();
                    stack.Push(re3);
                    //stack.set(n - 3, re3);
                }
                stack.Push(re2);
                stack.Push(re1);

                MergeCharClass(re3, re1);
                Reuse(re1);
                Pop();
                return true;
            }
            stack.Push(re2);
            stack.Push(re1);

        }

        if (n >= 2)
        {
            re1 = stack.Pop();
            re2 = stack.Pop();
            if (re2.op == Regexp.Op.VERTICAL_BAR)
            {
                if (n >= 3)
                {
                    // Now out of reach.
                    // Clean opportunistically.
                    CleanAlt(stack.Peek());

                }
                stack.Push(re1);
                stack.Push(re2);
                //stack.set(n - 2, re1);
                //stack.set(n - 1, re2);
                return true;
            }
            stack.Push(re2);
            stack.Push(re1);
        }
        return false;
    }

    // parseRightParen handles a ')' in the input.
    private void ParseRightParen()
    {
        Concat();
        if (SwapVerticalBar()) {
            Pop(); // pop vertical bar
        }
        Alternate();

        int n = stack.Count;
        if (n < 2) {
            throw new PatternSyntaxException(ERR_INTERNAL_ERROR, "stack underflow");
        }
        var re1 = Pop();
        var re2 = Pop();
        if (re2.op != Regexp.Op.LEFT_PAREN) {
            throw new PatternSyntaxException(ERR_MISSING_PAREN, wholeRegexp);
        }
        // Restore flags at time of paren.
        this.flags = re2.flags;
        if (re2.cap == 0) {
            // Just for grouping.
            Push(re1);
        } else {
            re2.op = Regexp.Op.CAPTURE;
            re2.subs = new Regexp[] { re1 };
            Push(re2);
        }
    }
    // parseEscape parses an escape sequence at the beginning of s
    // and returns the rune.
    // Pre: t at '\\'.  Post: after escape.
    private static int ParseEscape(StringIterator t)
    {
        int startPos = t.Pos;
        t.Skip(1); // '\\'
        if (!t.HasMore) {
            throw new PatternSyntaxException(ERR_TRAILING_BACKSLASH);
        }
        int c = t.Pop();
    bigswitch:
        switch (c) {
            default:
                if (!Utils.Isalnum(c))
                {
                    // Escaped non-word characters are always themselves.
                    // PCRE is not quite so rigorous: it accepts things like
                    // \q, but we don't.  We once rejected \_, but too many
                    // programs and people insist on using it, so allow \_.
                    return c;
                }
                break;

            // Octal escapes.
            case '1':
            case '2':
            case '3':
            case '4':
            case '5':
            case '6':
            case '7':
                // Single non-zero digit is a backreference; not supported
                if (!t.HasMore|| t.Peek() < '0' || t.Peek() > '7')
                {
                    break;
                }
                goto for_zero;
            /* fallthrough */
            case '0':
            for_zero:
                {
                    // Consume up to three octal digits; already have one.
                    int r = c - '0';
                    for (int i = 1; i < 3; i++)
                    {
                        if (!t.HasMore || t.Peek() < '0' || t.Peek() > '7')
                        {
                            break;
                        }
                        r = r * 8 + t.Peek() - '0';
                        t.Skip(1); // digit
                    }
                    return r;
                }

            // Hexadecimal escapes.
            case 'x':
                if (!t.HasMore)
                {
                    break;
                }
                c = t.Pop();
                if (c == '{')
                {
                    // Any number of digits in braces.
                    // Perl accepts any text at all; it ignores all text
                    // after the first non-hex digit.  We require only hex digits,
                    // and at least one.
                    int nhex = 0;
                    int r = 0;
                    for (; ; )
                    {
                        if (!t.HasMore)
                        {
                            goto outswitch;
                        }
                        c = t.Pop();
                        if (c == '}')
                        {
                            break;
                        }
                        int v = Utils.Unhex(c);
                        if (v < 0)
                        {
                            goto outswitch;
                        }
                        r = r * 16 + v;
                        if (r > Unicode.MAX_RUNE)
                        {
                            goto outswitch;
                        }
                        nhex++;
                    }
                    if (nhex == 0)
                    {
                        goto outswitch;
                    }
                    return r;
                }

                // Easy case: two hex digits.
                int x = Utils.Unhex(c);
                if (!t.HasMore)
                {
                    break;
                }
                c = t.Pop();
                int y = Utils.Unhex(c);
                if (x < 0 || y < 0)
                {
                    break;
                }
                return x * 16 + y;

            // C escapes.  There is no case 'b', to avoid misparsing
            // the Perl word-boundary \b as the C backspace \b
            // when in POSIX mode.  In Perl, /\b/ means word-boundary
            // but /[\b]/ means backspace.  We don't support that.
            // If you want a backspace, embed a literal backspace
            // character or use \x08.
            case 'a':
                return 7; // No \a in Java
            case 'f':
                return '\f';
            case 'n':
                return '\n';
            case 'r':
                return '\r';
            case 't':
                return '\t';
            case 'v':
                return 11; // No \v in Java
        }
    outswitch:
        throw new PatternSyntaxException(ERR_INVALID_ESCAPE, t.From(startPos));
    }

    // parseClassChar parses a character class character and returns it.
    // wholeClassPos is the position of the start of the entire class "[...".
    // Pre: t at class char; Post: t after it.
    private static int ParseClassChar(StringIterator t, int wholeClassPos)
    {
        if (!t.HasMore) {
            throw new PatternSyntaxException(ERR_MISSING_BRACKET, t.From(wholeClassPos));
        }

        // Allow regular escape sequences even though
        // many need not be escaped in this context.
        if (t.LookingAt('\\')) {
            return ParseEscape(t);
        }

        return t.Pop();
    }

    // parsePerlClassEscape parses a leading Perl character class escape like \d
    // from the beginning of |t|.  If one is present, it appends the characters
    // to cc and returns true.  The iterator is advanced past the escape
    // on success, undefined on failure, in which case false is returned.
    private bool ParsePerlClassEscape(StringIterator t, CharClass cc)
    {
        int beforePos = t.Pos;
        if ((flags & RE2.PERL_X) == 0
            || !t.HasMore            || t.Pop() != '\\'
            || // consume '\\'
            !t.HasMore)
        {
            return false;
        }
        t.Pop(); // e.g. advance past 'd' in "\\d"
        var k = t.From(beforePos);
        if (!CharGroup.PERL_GROUPS.TryGetValue(k,out var g))
        {
            return false;
        }
        if (g == null)
        {
            return false;
        }
        cc.AppendGroup(g, (flags & RE2.FOLD_CASE) != 0);
        return true;
    }

    // parseNamedClass parses a leading POSIX named character class like
    // [:alnum:] from the beginning of t.  If one is present, it appends the
    // characters to cc, advances the iterator, and returns true.
    // Pre: t at "[:".  Post: t after ":]".
    // On failure (no class of than name), throws PatternSyntaxException.
    // On misparse, returns false; t.pos() is undefined.
    private bool ParseNamedClass(StringIterator t, CharClass cc)
    {
        // (Go precondition check deleted.)
        string cls = t.Rest();
        int i = cls.IndexOf(":]");
        if (i < 0) {
            return false;
        }
        string name = cls.Substring(0, i + 2-0); // "[:alnum:]"
        t.SkipString(name);
        if (!CharGroup.POSIX_GROUPS.TryGetValue(name,out var g))
        {
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, name);
        }
        cc.AppendGroup(g, (flags & RE2.FOLD_CASE) != 0);
        return true;
    }

    // RangeTables are represented as int[][], a list of triples (start, end,
    // stride).
    private static int[][] ANY_TABLE = new int[][]{
        new int[]{0, Unicode.MAX_RUNE, 1},
      };

    // unicodeTable() returns the Unicode RangeTable identified by name
    // and the table of additional fold-equivalent code points.
    // Returns null if |name| does not identify a Unicode character range.
    private static Pair<int[][], int[][]> UnicodeTable(string name)
    {
        // Special case: "Any" means any.
        if (name.Equals("Any"))
        {
            return Pair<int[][], int[][]>.of(ANY_TABLE, ANY_TABLE);
        }
        int[][] table = UnicodeTables.CATEGORIES[name];
        if (table != null)
        {
            return Pair<int[][], int[][]>.of(table, UnicodeTables.FOLD_CATEGORIES[name]);
        }
        table = UnicodeTables.SCRIPTS[name];
        if (table != null)
        {
            return Pair<int[][], int[][]>.of(table, UnicodeTables.FOLD_SCRIPT[name]);
        }
        return null;
    }

    // parseUnicodeClass() parses a leading Unicode character class like \p{Han}
    // from the beginning of t.  If one is present, it appends the characters to
    // to |cc|, advances |t| and returns true.
    //
    // Returns false if such a pattern is not present or UNICODE_GROUPS
    // flag is not enabled; |t.pos()| is not advanced in this case.
    // Indicates error by throwing PatternSyntaxException.
    private bool ParseUnicodeClass(StringIterator t, CharClass cc)
    {
        int startPos = t.Pos;
        if ((flags & RE2.UNICODE_GROUPS) == 0 || (!t.LookingAt("\\p") && !t.LookingAt("\\P"))) {
            return false;
        }
        t.Skip(1); // '\\'
                   // Committed to parse or throw exception.
        int sign = +1;
        int c = t.Pop(); // 'p' or 'P'
        if (c == 'P') {
            sign = -1;
        }
        if (!t.HasMore) {
            t.RewindTo(startPos);
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, t.Rest());
        }
        c = t.Pop();
        string name;
        if (c != '{') {
            // Single-letter name.
            name = Utils.RuneToString(c);
        } else {
            // Name is in braces.
            string rest = t.Rest();
            int end = rest.IndexOf('}');
            if (end < 0)
            {
                t.RewindTo(startPos);
                throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, t.Rest());
            }
            name = rest.Substring(0, end - 0); // e.g. "Han"
            t.SkipString(name);
            t.Skip(1); // '}'
                       // Don't use skip(end) because it assumes UTF-16 coding, and
                       // StringIterator doesn't guarantee that.
        }

        // Group can have leading negation too.
        //  \p{^Han} == \P{Han}, \P{^Han} == \p{Han}.
        if (!string.IsNullOrEmpty(name) && name[0] == '^') {
            sign = -sign;
            name = name.Substring(1);
        }

        var pair = UnicodeTable(name);
        if (pair == null) {
            throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, t.From(startPos));
        }
        var tab = pair.first;
        var fold = pair.second; // fold-equivalent table

        // Variation of CharClass.appendGroup() for tables.
        if ((flags & RE2.FOLD_CASE) == 0 || fold == null) {
            cc.AppendTableWithSign(tab, sign);
        } else {
            // Merge and clean tab and fold in a temporary buffer.
            // This is necessary for the negative case and just tidy
            // for the positive case.
            int[] tmp = new CharClass().AppendTable(tab).AppendTable(fold).CleanClass().ToArray();
            cc.AppendClassWithSign(tmp, sign);
        }
        return true;
    }

    // parseClass parses a character class and pushes it onto the parse stack.
    //
    // NOTES:
    // Pre: at '['; Post: after ']'.
    // Mutates stack.  Advances iterator.  May throw.
    private void ParseClass(StringIterator t)
    {
        int startPos = t.Pos;
        t.Skip(1); // '['
        var re = NewRegexp(Regexp.Op.CHAR_CLASS);
        re.flags = flags;
        var cc = new CharClass();

        int sign = +1;
        if (t.HasMore&& t.LookingAt('^'))
        {
            sign = -1;
            t.Skip(1); // '^'

            // If character class does not match \n, add it here,
            // so that negation later will do the right thing.
            if ((flags & RE2.CLASS_NL) == 0)
            {
                cc.AppendRange('\n', '\n');
            }
        }

        bool first = true; // ']' and '-' are okay as first char in class
        while (!t.HasMore|| t.Peek() != ']' || first)
        {
            // POSIX: - is only okay unescaped as first or last in class.
            // Perl: - is okay anywhere.
            if (t.HasMore&& t.LookingAt('-') && (flags & RE2.PERL_X) == 0 && !first)
            {
                string s = t.Rest();
                if (s.Equals("-") || !s.StartsWith("-]"))
                {
                    t.RewindTo(startPos);
                    throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, t.Rest());
                }
            }
            first = false;

            int beforePos = t.Pos;

            // Look for POSIX [:alnum:] etc.
            if (t.LookingAt("[:"))
            {
                if (ParseNamedClass(t, cc))
                {
                    continue;
                }
                t.RewindTo(beforePos);
            }

            // Look for Unicode character group like \p{Han}.
            if (ParseUnicodeClass(t, cc))
            {
                continue;
            }

            // Look for Perl character class symbols (extension).
            if (ParsePerlClassEscape(t, cc))
            {
                continue;
            }
            t.RewindTo(beforePos);

            // Single character or simple range.
            int lo = ParseClassChar(t, startPos);
            int hi = lo;
            if (t.HasMore&& t.LookingAt('-'))
            {
                t.Skip(1); // '-'
                if (t.HasMore&& t.LookingAt(']'))
                {
                    // [a-] means (a|-) so check for final ].
                    t.Skip(-1);
                }
                else
                {
                    hi = ParseClassChar(t, startPos);
                    if (hi < lo)
                    {
                        throw new PatternSyntaxException(ERR_INVALID_CHAR_RANGE, t.From(beforePos));
                    }
                }
            }
            if ((flags & RE2.FOLD_CASE) == 0)
            {
                cc.AppendRange(lo, hi);
            }
            else
            {
                cc.AppendFoldedRange(lo, hi);
            }
        }
        t.Skip(1); // ']'

        cc.CleanClass();
        if (sign < 0)
        {
            cc.NegateClass();
        }
        re.runes = cc.ToArray();
        Push(re);
    }

    //// Utilities

    // Returns a new copy of the specified subarray.
    public static Regexp[] Subarray(Regexp[] array, int start, int end)
    {
        var r = new Regexp[end - start];
        for (int i = start; i < end; ++i)
        {
            r[i - start] = array[i];
        }
        return r;
    }

    public class Pair<F, S>
    {
        public readonly F first;
        public readonly S second;

        public Pair(F first, S second)
        {
            this.first = first;
            this.second = second;
        }

        public static Pair<F, S> of(F first, S second) => new Pair<F, S>(first, second);
    }

    private static int[] ConcatRunes(int[] x, int[] y)
    {
        int[] z = new int[x.Length + y.Length];
        Array.Copy(x, 0, z, 0, x.Length);
        Array.Copy(y, 0, z, x.Length, y.Length);
        return z;
    }
}
