/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace RE2CS.BenchMarks;

public class BenchmarkFullMatch
{

    //@Param({ "JDK", "RE2J"})
    private Implementations impl;

    //@Param({ "true", "false"})
    private bool binary;

    private Pattern pattern;

    private static String password = "password";
    private byte[] password_bytes = Encoding.UTF8.GetBytes(password);

    private static String l0ngpassword = "l0ngpassword";
    private byte[] l0ngpassword_bytes = Encoding.UTF8.GetBytes(l0ngpassword);

    //@Setup
    public void setup()
    {
        pattern =
            Pattern.compile(
                impl, "12345|123123|qwerty|mypass|abcdefg|hello|secret|admin|root|password");
    }

    //@Benchmark
    public void matched()
    {
        Matcher matcher =
            binary ? pattern.matcher(password_bytes) 
            : pattern.matcher(password);
        bool matches = matcher.matches();
        if (!matches)
        {
            throw new Exception();
        }
        //bh.consume(matches);
    }

    //@Benchmark
    public void notMatched()
    {
        Matcher matcher =
            binary ? pattern.matcher(l0ngpassword_bytes) : pattern.matcher(l0ngpassword);
        bool matches = matcher.matches();
        if (matches)
        {
            throw new Exception();
        }
        //bh.consume(matches);
    }
}
