/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
namespace RE2CS.BenchMarks;

public class BenchmarkCompile
{

    //@Param({ "JDK", "RE2J"})
    private Implementations impl;

    //@Param({ "DATE", "EMAIL", "PHONE", "RANDOM", "SOCIAL", "STATES"})
    private UsefulRegex regex;

    //@Benchmark
    public void compile()
    {
        Pattern.compile(impl, regex.pattern);
    }

    public class UsefulRegex
    {
        public static string DATE = ("([0-9]{4})-?(1[0-2]|0[1-9])-?(3[01]|0[1-9]|[12][0-9])");
        public static string EMAIL = ("[\\w\\.]+@[\\w\\.]+");
        public static string PHONE = ("([0-9]{3})-([0-9]{3})-([0-9]{4})");
        public static string RANDOM = ("($+((((($+((a+a*)+(b+c))*)((cc)(b+b))+a)+((b+c*)+(c+c)))+a)+(c*a+($+(c+c)b))))+c");
        public static string SOCIAL = ("[0-8][0-9]{2}-[0-9]{2}-[0-9]{4}");
        public static string STATES = (
            "A[ZLRK]|C[TAO]|D[CE]|FL|GA|HI|I[ALND]|K[SY]|LA|M[ADEINOST]|"
                + "N[HCDEJMVY]|O[HKR]|PA|RI|S[CD]|T[XN]|UT|V[AT]|W[VAIY]");

        public readonly String pattern;

        UsefulRegex(String pattern)
        {
            this.pattern = pattern;
        }
    }
}
