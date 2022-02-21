/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace RE2CS;
/**
 * A compiled representation of an RE2 regular expression, mimicking the
 * {@code java.util.regex.Pattern} API.
 *
 * <p>
 * The matching functions take {@code string} arguments instead of the more general Java
 * {@code string} since the latter doesn't provide UTF-16 decoding.
 *
 * <p>
 * See the <a href='package.html'>package-level documentation</a> for an overview of how to use this
 * API.
 * </p>
 *
 * @author rsc@google.com (Russ Cox)
 */
public class Pattern
{
    /** Flag: case insensitive matching. */
    public const int CASE_INSENSITIVE = 1;

    /** Flag: dot ({@code .}) matches all characters, including newline. */
    public const int DOTALL = 2;

    /**
     * Flag: multiline matching: {@code ^} and {@code $} match at beginning and end of line, not just
     * beginning and end of input.
     */
    public const int MULTILINE = 4;

    /**
     * Flag: Unicode groups (e.g. {@code \p\ Greek\} ) will be syntax errors.
     */
    public const int DISABLE_UNICODE_GROUPS = 8;

    /**
     * Flag: matches longest possible string.
     */
    public const int LONGEST_MATCH = 16;

    // The pattern string at construction time.
    private readonly string _pattern;

    // The flags at construction time.
    private readonly int _flags;

    // The compiled RE2 regexp.
    private readonly RE2 _re2;

    // This is visible for testing.
    public Pattern(string pattern, int flags, RE2 re2)
    {
        this._pattern = pattern ?? throw new ArgumentNullException(nameof(pattern));
        this._re2 = re2 ?? throw new ArgumentNullException(nameof(re2));
        this._flags = flags;
    }

    /**
     * Releases memory used by internal caches associated with this pattern. Does not change the
     * observable behaviour. Useful for tests that detect memory leaks via allocation tracking.
     */
    public void Reset() => _re2.Reset();

    /**
     * Returns the flags used in the constructor.
     */
    public int Flags => _flags;

    /**
     * Returns the pattern used in the constructor.
     */
    public string PatternText => _pattern;

    public RE2 Re2 => _re2;

    /**
     * Creates and returns a new {@code Pattern} corresponding to compiling {@code regex} with the
     * default flags (0).
     *
     * @param regex the regular expression
     * @throws PatternSyntaxException if the pattern is malformed
     */
    public static Pattern Compile(string regex) => Compile(regex, regex, 0);

    /**
     * Creates and returns a new {@code Pattern} corresponding to compiling {@code regex} with the
     * given {@code flags}.
     *
     * @param regex the regular expression
     * @param flags bitwise OR of the flag constants {@code CASE_INSENSITIVE}, {@code DOTALL}, and
     * {@code MULTILINE}
     * @throws PatternSyntaxException if the regular expression is malformed
     * @throws IllegalArgumentException if an unknown flag is given
     */
    public static Pattern Compile(string regex, int flags)
    {
        var flregex = regex;
        if ((flags & CASE_INSENSITIVE) != 0)
        {
            flregex = "(?i)" + flregex;
        }
        if ((flags & DOTALL) != 0)
        {
            flregex = "(?s)" + flregex;
        }
        if ((flags & MULTILINE) != 0)
        {
            flregex = "(?m)" + flregex;
        }
        if ((flags & ~(MULTILINE | DOTALL | CASE_INSENSITIVE | DISABLE_UNICODE_GROUPS | LONGEST_MATCH))
            != 0)
        {
            throw new ArgumentException(
                "Flags should only be a combination "
                    + "of MULTILINE, DOTALL, CASE_INSENSITIVE, DISABLE_UNICODE_GROUPS, LONGEST_MATCH");
        }
        return Compile(flregex, regex, flags);
    }

    /**
     * Helper: create new Pattern with given regex and flags. Flregex is the regex with flags applied.
     */
    private static Pattern Compile(string flregex, string regex, int flags)
    {
        int re2Flags = RE2.PERL;
        if ((flags & DISABLE_UNICODE_GROUPS) != 0)
        {
            re2Flags &= ~RE2.UNICODE_GROUPS;
        }
        return new Pattern(
            regex, flags, RE2.CompileImpl(flregex, re2Flags, (flags & LONGEST_MATCH) != 0));
    }

    /**
     * Matches a string against a regular expression.
     *
     * @param regex the regular expression
     * @param input the input
     * @return true if the regular expression matches the entire input
     * @throws PatternSyntaxException if the regular expression is malformed
     */
    public static bool Matches(string regex, string input) => Compile(regex).Matcher(input).Matches();

    public static bool Matches(string regex, byte[] input) => Compile(regex).Matcher(input).Matches();

    public bool Matches(string input) => this.Matcher(input).Matches();

    public bool Matches(byte[] input) => this.Matcher(input).Matches();

    /**
     * Creates a new {@code Matcher} matching the pattern against the input.
     *
     * @param input the input string
     */
    public Matcher Matcher(string input) => new Matcher(this, input);

    public Matcher Matcher(byte[] input) => new Matcher(this, MatcherInput.Utf8(input));

    // This is visible for testing.
    public Matcher Matcher(MatcherInput input) => new Matcher(this, input);

    /**
     * Splits input around instances of the regular expression. It returns an array giving the strings
     * that occur before, between, and after instances of the regular expression. Empty strings that
     * would occur at the end of the array are omitted.
     *
     * @param input the input string to be split
     * @return the split strings
     */

    /**
     * Splits input around instances of the regular expression. It returns an array giving the strings
     * that occur before, between, and after instances of the regular expression.
     *
     * <p>
     * If {@code limit <= 0}, there is no limit on the size of the returned array. If
     * {@code limit == 0}, empty strings that would occur at the end of the array are omitted. If
     * {@code limit > 0}, at most limit strings are returned. The final string contains the remainder
     * of the input, possibly including additional matches of the pattern.
     *
     * @param input the input string to be split
     * @param limit the limit
     * @return the split strings
     */
    public string[] Split(string input, int limit = 0) => Split(new Matcher(this, input), limit);

    /** Helper: run split on m's input. */
    private string[] Split(Matcher m, int limit)
    {
        int matchCount = 0;
        int arraySize = 0;
        int last = 0;
        while (m.Find())
        {
            matchCount++;
            if (limit != 0 || last < m.Start())
            {
                arraySize = matchCount;
            }
            last = m.End();
        }
        if (last < m.InputLength || limit != 0)
        {
            matchCount++;
            arraySize = matchCount;
        }

        int trunc = 0;
        if (limit > 0 && arraySize > limit)
        {
            arraySize = limit;
            trunc = 1;
        }
        string[] array = new string[arraySize];
        int i = 0;
        last = 0;
        m.Reset();
        while (m.Find() && i < arraySize - trunc)
        {
            array[i++] = m.Substring(last, m.Start());
            last = m.End();
        }
        if (i < arraySize)
        {
            array[i] = m.Substring(last, m.InputLength);
        }
        return array;
    }

    /**
     * Returns a literal pattern string for the specified string.
     *
     * <p>
     * This method produces a string that can be used to create a <code>Pattern</code> that would
     * match the string <code>s</code> as if it were a literal pattern.
     * </p>
     * Metacharacters or escape sequences in the input sequence will be given no special meaning.
     *
     * @param s The string to be literalized
     * @return A literal string replacement
     */
    public static string Quote(string s) => RE2.QuoteMeta(s);


    public override string ToString() => _pattern;

    /**
     * Returns the number of capturing groups in this matcher's pattern. Group zero denotes the entire
     * pattern and is excluded from this count.
     *
     * @return the number of capturing groups in this pattern
     */
    public int GroupCount => _re2.NumberOfCapturingGroups;

    /**
     * Return a map of the capturing groups in this matcher's pattern, where key is the name and value
     * is the index of the group in the pattern.
     */
    public Dictionary<string, int> NamedGroups => (_re2.namedGroups);

    object ReadResolve()
    {
        // The deserialized version will be missing the RE2 instance, so we need to create a new,
        // compiled version.
        return Pattern.Compile(_pattern, _flags);
    }

    public override bool Equals(object? o)
    {
        if (this == o)
        {
            return true;
        }
        if (o == null || this.GetType() != o.GetType())
        {
            return false;
        }

        return o is Pattern other && Flags == other.Flags && _pattern.Equals(other.PatternText);
    }

    public override int GetHashCode()
    {
        int result = _pattern.GetHashCode();
        result = 31 * result + _flags;
        return result;
    }

}
