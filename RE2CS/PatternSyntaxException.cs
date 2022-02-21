/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

namespace RE2CS;

/**
 * An exception thrown by the parser if the pattern was invalid.
 *
 * Following {@code java.util.regex.PatternSyntaxException}, this is an unchecked exception.
 */
public class PatternSyntaxException : Exception
{
    private readonly string error; // the nature of the error
    private readonly string input; // the partial input at the point of error.

    public PatternSyntaxException(string error, string input = "")
          : base("error parsing regexp: " + error + (String.IsNullOrEmpty(input) ? "": ": `" + input + "`"))
    {
        this.error = error;
        this.input = input;
    }

    /**
     * Retrieves the error index.
     *
     * @return The approximate index in the pattern of the error, or <tt>-1</tt> if the index is not
     * known
     */
    public int Index => -1;

    /**
     * Retrieves the description of the error.
     *
     * @return The description of the error
     */
    public string Description => error;

    /**
     * Retrieves the erroneous regular-expression pattern.
     *
     * @return The erroneous pattern
     */
    public string Pattern => input;
}
