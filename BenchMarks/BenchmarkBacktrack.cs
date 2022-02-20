/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace RE2CS.BenchMarks;

public class BenchmarkBacktrack
{

    //@Param({ "JDK", "RE2J"})
    private Implementations impl;

    //@Param({ "5", "10", "15", "20"})
    private int repeats;

    private Pattern pattern;

    //@Setup
    public void setup()
    {
        pattern = Pattern.compile(impl, repeat("a?", repeats) + repeat("a", repeats));
    }

    //@Benchmark
    public void matched()
    {
        Matcher matcher = pattern.matcher(repeat("a", repeats));
        bool matches = matcher.matches();
        if (!matches)
        {
            throw new Exception();
        }
        //
    }

    private String repeat(String str, int n)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < n; i++)
        {
            sb.Append(str);
        }
        return sb.ToString();
    }
}
