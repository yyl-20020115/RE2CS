/*
 * Copyright (c) 2021 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace RE2CS;

public enum Encodings : uint
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
    public static MatcherInput Utf16(string charSequence) => new Utf16MatcherInput(charSequence);

    /**
     * Return the MatcherInput for UTF_8 encoding.
     */
    public static MatcherInput Utf8(byte[] bytes) => new Utf8MatcherInput(bytes);

    /**
     * Return the MatcherInput for UTF_8 encoding.
     */
    public static MatcherInput Utf8(string input) => new Utf8MatcherInput(
            System.Text.Encoding.UTF8.GetBytes(input));

    public abstract Encodings Encoding { get; }

    public abstract string AsCharSequence();

    public abstract byte[] AsBytes();

    public abstract int Length { get; }

    public class Utf8MatcherInput : MatcherInput
    {
        public readonly byte[] bytes;

        public Utf8MatcherInput(byte[] bytes) => this.bytes = bytes;

        public override Encodings Encoding => Encodings.UTF_8;

        public override string AsCharSequence() => System.Text.Encoding.UTF8.GetString(this.bytes);
        public override byte[] AsBytes() => bytes;
        public override int Length => bytes.Length;
    }

    public class Utf16MatcherInput : MatcherInput
    {
        public readonly string charSequence;

        public Utf16MatcherInput(string charSequence) => this.charSequence = charSequence;

        public override Encodings Encoding => Encodings.UTF_16;

        public override string AsCharSequence() => charSequence;

        public override byte[] AsBytes() => System.Text.Encoding.Unicode.GetBytes(charSequence);

        public override int Length => charSequence.Length;
    }
}
