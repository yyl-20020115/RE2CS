/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */


using System;
using System.IO;
/**
* A simple reader of lines from a UNIX character stream, like java.io.BufferedReader, but doesn't
* consider '\r' a line terminator.
*
* @author adonovan@google.com (Alan Donovan)
*/
namespace RE2CS.Tests;

public class UNIXBufferedReader :TextReader
{

    private readonly TextReader r;

    public UNIXBufferedReader(TextReader r) => this.r = r;

    public string? readLine() => r.ReadLine();

    public void close() => r.Close();

    // Unimplemented:

    public int read(char[] buf, int off, int len) => r.Read(buf, off, len);

    public int read() => r.Read();

    public long skip(long n) => throw new NotImplementedException();
    public bool ready() => throw new NotImplementedException();

    public bool markSupported() => throw new NotImplementedException();

    public void mark(int readAheadLimit) => throw new NotImplementedException();

    public void reset() => throw new NotImplementedException();
}
