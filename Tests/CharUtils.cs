namespace RE2CS.Tests;

public static class CharUtils
{
    public static int OffsetByCodePoints(this string s, int index, int codePointOffset)
    {
        int start = 0;
        int count = s.Length;

        int x = index;
        if (codePointOffset >= 0)
        {
            int limit = start + count;
            int i;
            for (i = 0; x < limit && i < codePointOffset; i++)
            {
                if (char.IsHighSurrogate(s[x++]) && x < limit &&
                    char.IsLowSurrogate(s[x]))
                {
                    x++;
                }
            }
            if (i < codePointOffset)
            {
                throw new IndexOutOfRangeException();
            }
        }
        else
        {
            int i;
            for (i = codePointOffset; x > start && i < 0; i++)
            {
                if (char.IsLowSurrogate(s[--x]) && x > start &&
                    char.IsHighSurrogate(s[x - 1]))
                {
                    x--;
                }
            }
            if (i < 0)
            {
                throw new IndexOutOfRangeException();
            }
        }
        return x;
    }
    public static int CodePointCount(this string s, int start, int end)
    {
        int offset = start, count = end - start;
        int endIndex = offset + count;
        int n = count;
        for (int i = offset; i < endIndex;)
        {
            if (char.IsHighSurrogate(s[i++]) && i < endIndex &&
                char.IsLowSurrogate(s[i]))
            {
                n--;
                i++;
            }
        }
        return n;
    }

}
