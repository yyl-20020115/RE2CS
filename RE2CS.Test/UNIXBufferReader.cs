/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */


using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
/**
* A simple reader of lines from a UNIX character stream, like java.io.BufferedReader, but doesn't
* consider '\r' a line terminator.
*
* @author adonovan@google.com (Alan Donovan)
*/
namespace RE2CS.Tests;

public class UNIXBufferedReader :StreamReader
{
    public UNIXBufferedReader(Stream stream)
        :base(stream)
    {
    }

    public UNIXBufferedReader(Stream stream, bool detectEncodingFromByteOrderMarks)
        :base(stream, detectEncodingFromByteOrderMarks)
    {
    }

    public UNIXBufferedReader(Stream stream, Encoding encoding)
        :base(stream, encoding)
    {
    }
    public UNIXBufferedReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        :base(stream , encoding, detectEncodingFromByteOrderMarks)
    {
    }

    public UNIXBufferedReader(Stream stream, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        :base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize)
    {
    }
    public UNIXBufferedReader(Stream stream, Encoding? encoding = null, bool detectEncodingFromByteOrderMarks = true, int bufferSize = -1, bool leaveOpen = false)
        :base(stream, encoding, detectEncodingFromByteOrderMarks, bufferSize, leaveOpen)
    {
    }
    public UNIXBufferedReader(string path)
        :base(path)
    {
    }
    public UNIXBufferedReader(string path, FileStreamOptions options)
        :base(path, options)
    {
    }

    public UNIXBufferedReader(string path, bool detectEncodingFromByteOrderMarks)
        :base(path, detectEncodingFromByteOrderMarks)
    {
    }
    public UNIXBufferedReader(string path, Encoding encoding)
        :base(path, encoding)
    {
    }

    public UNIXBufferedReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks)
        :base(path, encoding, detectEncodingFromByteOrderMarks)
    {
    }
    public UNIXBufferedReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, int bufferSize)
        :base(path, encoding, detectEncodingFromByteOrderMarks, bufferSize)
    {
    }

    public UNIXBufferedReader(string path, Encoding encoding, bool detectEncodingFromByteOrderMarks, FileStreamOptions options)
        :base(path, encoding, detectEncodingFromByteOrderMarks)
    {
    }

    private char[] buf = new char[4096];
    private int buflen = 0; // length prefix of |buf| that is filled
    private int inext = 0; // index in buf of next char
    public override string? ReadLine()
    {
        StringBuilder s = null; // holds '\n'-free gulps of input
        int istart; // index of first char
        for (; ; )
        {
            // Should we refill the buffer?
            if (inext >= buflen)
            {
                int n = this.Read(buf, 0, buf.Length);
                if (n > 0)
                {
                    buflen = n;
                    inext = 0;
                }
                else
                {
                    return null;
                }
            }
            // Did we reach end-of-file?
            if (inext >= buflen)
            {
                return s != null && s.Length > 0 ? s.ToString() : null;
            }
            // Did we read a newline?
            int i;
            for (i = inext; i < buflen; i++)
            {
                if (buf[i] == '\n')
                {
                    istart = inext;
                    inext = i;
                    String str;
                    if (s == null)
                    {
                        str = new String(buf, istart, i - istart);
                    }
                    else
                    {
                        s.Append(buf, istart, i - istart);
                        str = s.ToString();
                    }
                    inext++;
                    return str;
                }
            }
            istart = inext;
            inext = i;
            if (s == null)
            {
                s = new StringBuilder(80);
            }
            s.Append(buf, istart, i - istart);
        }
    }
}
