/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace RE2CS;
/**
 * A stateful iterator that interprets a regex {@code Pattern} on a specific input. Its interface
 * mimics the JDK 1.4.2 {@code java.util.regex.Matcher}.
 *
 * <p>
 * Conceptually, a Matcher consists of four parts:
 * <ol>
 * <li>A compiled regular expression {@code Pattern}, set at construction and fixed for the lifetime
 * of the matcher.</li>
 *
 * <li>The remainder of the input string, set at construction or {@link #reset()} and advanced by
 * each match operation such as {@link #find}, {@link #matches} or {@link #lookingAt}.</li>
 *
 * <li>The current match information, accessible via {@link #start}, {@link #end}, and
 * {@link #group}, and updated by each match operation.</li>
 *
 * <li>The Append position, used and advanced by {@link #appendReplacement} and {@link #appendTail}
 * if performing a search and replace from the input to an external {@code StringBuffer}.
 *
 * </ol>
 *
 * <p>
 * See the <a href="package.html">package-level documentation</a> for an overview of how to use this
 * API.
 * </p>
 *
 * @author rsc@google.com (Russ Cox)
 */
public class Matcher
{
    // The pattern being matched.
    private readonly Pattern _pattern;

    // The group indexes, in [start, end) pairs.  Zeroth pair is overall match.
    private readonly int[] _groups;

    private readonly Dictionary<string, int> _namedGroups;

    // The number of submatches (groups) in the pattern.
    private readonly int _groupCount;

    private MatcherInput _matcherInput;

    // The input length in UTF16 codes.
    private int _inputLength;

    // The Append position: where the next Append should start.
    private int _appendPos;

    // Is there a current match?
    private bool _hasMatch;

    // Have we found the submatches (groups) of the current match?
    // group[0], group[1] are set regardless.
    private bool _hasGroups;

    // The anchor flag to use when repeating the match to find subgroups.
    private int _anchorFlag;

    public Matcher(Pattern pattern)
    {
        if (pattern == null)
        {
            throw new ArgumentNullException("pattern is null");
        }
        this._pattern = pattern;
        RE2 re2 = pattern.re2();
        _groupCount = re2.numberOfCapturingGroups();
        _groups = new int[2 + 2 * _groupCount];
        _namedGroups = re2.namedGroups;
    }

    /** Creates a new {@code Matcher} with the given pattern and input. */
    public Matcher(Pattern pattern, string input)
        : this(pattern)
    {
        reset(input);
    }

    public Matcher(Pattern pattern, MatcherInput input)
        : this(pattern)
    {
        reset(input);
    }

    /** Returns the {@code Pattern} associated with this {@code Matcher}. */
    public Pattern pattern()
    {
        return _pattern;
    }

    /**
     * Resets the {@code Matcher}, rewinding input and discarding any match information.
     *
     * @return the {@code Matcher} itself, for chained method calls
     */
    public Matcher reset()
    {
        _inputLength = _matcherInput.length();
        _appendPos = 0;
        _hasMatch = false;
        _hasGroups = false;
        return this;
    }

    /**
     * Resets the {@code Matcher} and changes the input.
     *
     * @param input the new input string
     * @return the {@code Matcher} itself, for chained method calls
     */
    public Matcher reset(string input)
    {
        return reset(MatcherInput.utf16(input));
    }

    /**
     * Resets the {@code Matcher} and changes the input.
     *
     * @param bytes utf8 bytes of the input string.
     * @return the {@code Matcher} itself, for chained method calls
     */
    public Matcher reset(byte[] bytes)
    {
        return reset(MatcherInput.utf8(bytes));
    }

    private Matcher reset(MatcherInput input)
    {
        if (input == null)
        {
            throw new ArgumentNullException("input is null");
        }
        _matcherInput = input;
        reset();
        return this;
    }

    /**
     * Returns the start position of the most recent match.
     *
     * @throws InvalidOperationException if there is no match
     */
    public int start()
    {
        return start(0);
    }

    /**
     * Returns the end position of the most recent match.
     *
     * @throws InvalidOperationException if there is no match
     */
    public int end()
    {
        return end(0);
    }

    /**
     * Returns the start position of a subgroup of the most recent match.
     *
     * @param group the group index; 0 is the overall match
     * @throws InvalidOperationException if there is no match
     * @throws IndexOutOfBoundsException if {@code group < 0} or {@code group > groupCount()}
     */
    public int start(int group)
    {
        loadGroup(group);
        return _groups[2 * group];
    }

    /**
     * Returns the start of the named group of the most recent match, or -1 if the group was not
     * matched.
     *
     * @param group the group name
     * @throws ArgumentException if no group with that name exists
     */
    public int start(string group)
    {
        if (!_namedGroups.TryGetValue(group,out var g))
        {
            throw new ArgumentException("group '" + group + "' not found");
        }
        return start(g);
    }

    /**
     * Returns the end position of a subgroup of the most recent match.
     *
     * @param group the group index; 0 is the overall match
     * @throws InvalidOperationException if there is no match
     * @throws IndexOutOfBoundsException if {@code group < 0} or {@code group > groupCount()}
     */
    public int end(int group)
    {
        loadGroup(group);
        return _groups[2 * group + 1];
    }

    /**
     * Returns the end of the named group of the most recent match, or -1 if the group was not
     * matched.
     *
     * @param group the group name
     * @throws ArgumentException if no group with that name exists
     */
    public int end(string group)
    {
        int g = _namedGroups[group];
        if (g == null)
        {
            throw new ArgumentException("group '" + group + "' not found");
        }
        return end(g);
    }

    /**
     * Returns the most recent match.
     *
     * @throws InvalidOperationException if there is no match
     */
    public string group()
    {
        return group(0);
    }

    /**
     * Returns the subgroup of the most recent match.
     *
     * @throws InvalidOperationException if there is no match
     * @throws IndexOutOfBoundsException if {@code group < 0} or {@code group > groupCount()}
     */
    public string group(int group)
    {
        int _start = start(group);
        int _end = end(group);
        if (_start < 0 && _end < 0)
        {
            // Means the subpattern didn't get matched at all.
            return null;
        }
        return substring(_start, _end);
    }

    /**
     * Returns the named group of the most recent match, or {@code null} if the group was not matched.
     *
     * @param group the group name
     * @throws ArgumentException if no group with that name exists
     */
    public string group(string _group)
    {
        if (!_namedGroups.ContainsKey(_group))
        {
            throw new ArgumentException("group '" + _group + "' not found");
        }
        return group(_namedGroups[_group]);
    }

    /**
     * Returns the number of subgroups in this pattern.
     *
     * @return the number of subgroups; the overall match (group 0) does not count
     */
    public int groupCount()
    {
        return _groupCount;
    }

    /** Helper: finds subgroup information if needed for group. */
    private void loadGroup(int group)
    {
        if (group < 0 || group > _groupCount)
        {
            throw new  IndexOutOfRangeException("Group index out of bounds: " + group);
        }
        if (!_hasMatch)
        {
            throw new InvalidOperationException("perhaps no match attempted");
        }
        if (group == 0 || _hasGroups)
        {
            return;
        }

        // Include the character after the matched text (if there is one).
        // This is necessary in the case of inputSequence abc and pattern
        // (a)(b$)?(b)? . If we do pass in the trailing c,
        // the groups evaluate to new string[] {"ab", "a", null, "b" }
        // If we don't, they evaluate to new string[] {"ab", "a", "b", null}
        // We know it won't affect the total matched because the previous call
        // to match included the extra character, and it was not matched then.
        int end = _groups[1] + 1;
        if (end > _inputLength)
        {
            end = _inputLength;
        }

        bool ok =
            _pattern.re2().match(_matcherInput, _groups[0], end, _anchorFlag, _groups, 1 + _groupCount);
        // Must match - hasMatch says that the last call with these
        // parameters worked just fine.
        if (!ok)
        {
            throw new InvalidOperationException("inconsistency in matching group data");
        }
        _hasGroups = true;
    }

    /**
     * Matches the entire input against the pattern (anchored start and end). If there is a match,
     * {@code matches} sets the match state to describe it.
     *
     * @return true if the entire input matches the pattern
     */
    public bool matches()
    {
        return genMatch(0, RE2.ANCHOR_BOTH);
    }

    /**
     * Matches the beginning of input against the pattern (anchored start). If there is a match,
     * {@code lookingAt} sets the match state to describe it.
     *
     * @return true if the beginning of the input matches the pattern
     */
    public bool lookingAt()
    {
        return genMatch(0, RE2.ANCHOR_START);
    }

    /**
     * Matches the input against the pattern (unanchored). The search begins at the end of the last
     * match, or else the beginning of the input. If there is a match, {@code find} sets the match
     * state to describe it.
     *
     * @return true if it finds a match
     */
    public bool find()
    {
        int start = 0;
        if (_hasMatch)
        {
            start = _groups[1];
            if (_groups[0] == _groups[1])
            { // empty match - nudge forward
                start++;
            }
        }
        return genMatch(start, RE2.UNANCHORED);
    }

    /**
     * Matches the input against the pattern (unanchored), starting at a specified position. If there
     * is a match, {@code find} sets the match state to describe it.
     *
     * @param start the input position where the search begins
     * @return true if it finds a match
     * @throws IndexOutOfBoundsException if start is not a valid input position
     */
    public bool find(int start)
    {
        if (start < 0 || start > _inputLength)
        {
            throw new  IndexOutOfRangeException("start index out of bounds: " + start);
        }
        reset();
        return genMatch(start, 0);
    }

    /** Helper: does match starting at start, with RE2 anchor flag. */
    private bool genMatch(int startByte, int anchor)
    {
        // TODO(rsc): Is matches/lookingAt supposed to reset the Append or input positions?
        // From the JDK docs, looks like no.
        bool ok = _pattern.re2().match(_matcherInput, startByte, _inputLength, anchor, _groups, 1);
        if (!ok)
        {
            return false;
        }
        _hasMatch = true;
        _hasGroups = false;
        _anchorFlag = anchor;

        return true;
    }

    /** Helper: return substring for [start, end). */
    public string substring(int start, int end)
    {
        // UTF_8 is matched in binary mode. So slice the bytes.
        if (_matcherInput.getEncoding() == Encodings.UTF_8)
        {
            return Encoding.UTF8.GetString(_matcherInput.asBytes(), start, end - start);
        }

        // This is fast for both StringBuilder and string.
        return _matcherInput.asCharSequence().Substring(start, end);
    }

    /** Helper for Pattern: return input length. */
    public int get_inputLength()
    {
        return _inputLength;
    }

    /**
     * Quotes '\' and '$' in {@code s}, so that the returned string could be used in
     * {@link #appendReplacement} as a literal replacement of {@code s}.
     *
     * @param s the string to be quoted
     * @return the quoted string
     */
    public static string quoteReplacement(string s)
    {
        if (s.IndexOf('\\') < 0 && s.IndexOf('$') < 0)
        {
            return s;
        }
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < s.Length; ++i)
        {
            char c = s[i];
            if (c == '\\' || c == '$')
            {
                sb.Append('\\');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    /**
     * Appends to {@code sb} two strings: the text from the Append position up to the beginning of the
     * most recent match, and then the replacement with submatch groups substituted for references of
     * the form {@code $n}, where {@code n} is the group number in decimal. It advances the Append
     * position to where the most recent match ended.
     *
     * <p>
     * To embed a literal {@code $}, use \$ (actually {@code "\\$"} with string escapes). The escape
     * is only necessary when {@code $} is followed by a digit, but it is always allowed. Only
     * {@code $} and {@code \} need escaping, but any character can be escaped.
     *
     * <p>
     * The group number {@code n} in {@code $n} is always at least one digit and expands to use more
     * digits as long as the resulting number is a valid group number for this pattern. To cut it off
     * earlier, escape the first digit that should not be used.
     *
     * @param sb the {@link StringBuffer} to Append to
     * @param replacement the replacement string
     * @return the {@code Matcher} itself, for chained method calls
     * @throws InvalidOperationException if there was no most recent match
     * @throws IndexOutOfBoundsException if replacement refers to an invalid group
     */
    public Matcher appendReplacement(StringBuffer sb, string replacement)
    {
        var result = new StringBuilder();
        appendReplacement(result, replacement);
        sb.Append(result);
        return this;
    }

    /**
     * Appends to {@code sb} two strings: the text from the Append position up to the beginning of the
     * most recent match, and then the replacement with submatch groups substituted for references of
     * the form {@code $n}, where {@code n} is the group number in decimal. It advances the Append
     * position to where the most recent match ended.
     *
     * <p>
     * To embed a literal {@code $}, use \$ (actually {@code "\\$"} with string escapes). The escape
     * is only necessary when {@code $} is followed by a digit, but it is always allowed. Only
     * {@code $} and {@code \} need escaping, but any character can be escaped.
     *
     * <p>
     * The group number {@code n} in {@code $n} is always at least one digit and expands to use more
     * digits as long as the resulting number is a valid group number for this pattern. To cut it off
     * earlier, escape the first digit that should not be used.
     *
     * @param sb the {@link StringBuilder} to Append to
     * @param replacement the replacement string
     * @return the {@code Matcher} itself, for chained method calls
     * @throws InvalidOperationException if there was no most recent match
     * @throws IndexOutOfBoundsException if replacement refers to an invalid group
     */
    public Matcher appendReplacement(StringBuilder sb, string replacement)
    {
        int s = start();
        int e = end();
        if (_appendPos < s)
        {
            sb.Append(substring(_appendPos, s));
        }
        _appendPos = e;
        appendReplacementInternal(sb, replacement);
        return this;
    }

    private void appendReplacementInternal(StringBuilder sb, string replacement)
    {
        int last = 0;
        int i = 0;
        int m = replacement.Length;
        for (; i < m - 1; i++)
        {
            if (replacement[i] == '\\')
            {
                if (last < i)
                {
                    sb.Append(replacement.Substring(last, i));
                }
                i++;
                last = i;
                continue;
            }
            if (replacement[i] == '$')
            {
                int c = replacement[i + 1];
                if ('0' <= c && c <= '9')
                {
                    int n = c - '0';
                    if (last < i)
                    {
                        sb.Append(replacement.Substring(last, i));
                    }
                    for (i += 2; i < m; i++)
                    {
                        c = replacement[i];
                        if (c < '0' || c > '9' || n * 10 + c - '0' > _groupCount)
                        {
                            break;
                        }
                        n = n * 10 + c - '0';
                    }
                    if (n > _groupCount)
                    {
                        throw new IndexOutOfRangeException("n > number of groups: " + n);
                    }
                    string _group = group(n);
                    if (_group != null)
                    {
                        sb.Append(_group);
                    }
                    last = i;
                    i--;
                    continue;
                }
                else if (c == '{')
                {
                    if (last < i)
                    {
                        sb.Append(replacement.Substring(last, i));
                    }
                    i++; // skip {
                    int j = i + 1;
                    while (j < replacement.Length
                        && replacement[j] != '}'
                        && replacement[j] != ' ')
                    {
                        j++;
                    }
                    if (j == replacement.Length || replacement[j] != '}')
                    {
                        throw new ArgumentException("named capture group is missing trailing '}'");
                    }
                    string groupName = replacement.Substring(i + 1, j);
                    sb.Append(group(groupName));
                    last = j + 1;
                }
            }
        }
        if (last < m)
        {
            sb.Append(replacement, last, m);
        }
    }

    /**
     * Appends to {@code sb} the substring of the input from the Append position to the end of the
     * input.
     *
     * @param sb the {@link StringBuffer} to Append to
     * @return the argument {@code sb}, for method chaining
     */
    public StringBuffer appendTail(StringBuffer sb)
    {
        sb.Append(substring(_appendPos, _inputLength));
        return sb;
    }

    /**
     * Appends to {@code sb} the substring of the input from the Append position to the end of the
     * input.
     *
     * @param sb the {@link StringBuilder} to Append to
     * @return the argument {@code sb}, for method chaining
     */
    public StringBuilder appendTail(StringBuilder sb)
    {
        sb.Append(substring(_appendPos, _inputLength));
        return sb;
    }

    /**
     * Returns the input with all matches replaced by {@code replacement}, interpreted as for
     * {@code appendReplacement}.
     *
     * @param replacement the replacement string
     * @return the input string with the matches replaced
     * @throws IndexOutOfBoundsException if replacement refers to an invalid group
     */
    public string replaceAll(string replacement)
    {
        return replace(replacement, true);
    }

    /**
     * Returns the input with the first match replaced by {@code replacement}, interpreted as for
     * {@code appendReplacement}.
     *
     * @param replacement the replacement string
     * @return the input string with the first match replaced
     * @throws IndexOutOfBoundsException if replacement refers to an invalid group
     */
    public string replaceFirst(string replacement)
    {
        return replace(replacement, false);
    }

    /** Helper: replaceAll/replaceFirst hybrid. */
    private string replace(string replacement, bool all)
    {
        reset();
        StringBuffer sb = new StringBuffer();
        while (find())
        {
            appendReplacement(sb, replacement);
            if (!all)
            {
                break;
            }
        }
        appendTail(sb);
        return sb.toString();
    }
}
