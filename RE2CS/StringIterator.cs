namespace RE2CS;

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
        int r = char.ConvertToUtf32(str, _pos);// str.codePointAt(Pos);
        _pos += (r > char.MaxValue ? 2 : 1);// new Rune(r).Utf16SequenceLength;//. Character.charCount(r);
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
