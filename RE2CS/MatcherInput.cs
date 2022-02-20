/*
 * Copyright (c) 2021 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace RE2CS;

public enum Encodings
{
    UTF_16,
    UTF_8,
}

/**
 * Abstract the representations of input text supplied to Matcher.
 */
public abstract class MatcherInput
{
    /**
     * Return the MatcherInput for UTF_16 encoding.
     */
    public static MatcherInput utf16(string charSequence)
    {
        return new Utf16MatcherInput(charSequence);
    }

    /**
     * Return the MatcherInput for UTF_8 encoding.
     */
    public static MatcherInput utf8(byte[] bytes)
    {
        return new Utf8MatcherInput(bytes);
    }

    /**
     * Return the MatcherInput for UTF_8 encoding.
     */
    public static MatcherInput utf8(string input)
    {
        return new Utf8MatcherInput(
            System.Text.Encoding.UTF8.GetBytes(input));
    }

    public abstract Encodings getEncoding();

    public abstract string asCharSequence();

    public abstract byte[] asBytes();

    public abstract int length();

    public class Utf8MatcherInput : MatcherInput
    {
        public byte[] bytes;

        public Utf8MatcherInput(byte[] bytes)
        {
            this.bytes = bytes;
        }

        public override Encodings getEncoding()
        {
            return Encodings.UTF_8;
        }

        public override string asCharSequence()
        {

            return System.Text.Encoding.UTF8.GetString(this.bytes);
        }
        public override byte[] asBytes()
        {
            return bytes;
        }
        public override int length()
        {
            return bytes.Length;
        }
    }

    public class Utf16MatcherInput : MatcherInput
    {
        public string charSequence;

        public Utf16MatcherInput(string charSequence)
        {
            this.charSequence = charSequence;
        }

        public override Encodings getEncoding()
        {
            return Encodings.UTF_16;
        }

        public override string asCharSequence()
        {
            return charSequence;
        }

        public override byte[] asBytes()
        {
            return System.Text.Encoding.Unicode.GetBytes(charSequence);
        }

        public override int length()
        {
            return charSequence.Length;
        }
    }
}
