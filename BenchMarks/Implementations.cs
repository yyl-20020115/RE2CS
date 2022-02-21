/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace RE2CS.BenchMarks;

public enum Implementations
{
    JDK,
    RE2J
}

public abstract class Matcher
{
    public abstract bool find();

    public abstract bool matches();

    public abstract String group();

    public class Re2Matcher : Matcher
    {
        private readonly RE2CS.Matcher matcher;

        public Re2Matcher(RE2CS.Matcher matcher)
        {
            this.matcher = matcher;
        }

        public override bool find()
        {
            return matcher.Find();
        }

        public override bool matches()
        {
            return matcher.Matches();
        }

        public override String group()
        {
            return matcher.Group();
        }
    }

    public class JdkMatcher : Matcher
    {
        private System.Text.RegularExpressions.Regex matcher;
        private string s;
        public JdkMatcher(System.Text.RegularExpressions.Regex matcher, string s)
        {
            this.matcher = matcher;
            this.s= s;
        }

        public override bool find()
        {
            //TODO:
            throw new NotImplementedException();
        }

        public override bool matches()
        {
            //TODO:
            throw new NotImplementedException();
        }

        public override String group()
        {
            //TODO:
            throw new NotImplementedException();
        }
    }
}
public abstract class Pattern
{
    public static Pattern compile(Implementations impl, String pattern)
    {
        switch (impl)
        {
            case Implementations.JDK:
                return new JdkPattern(pattern);
            case Implementations.RE2J:
                return new Re2Pattern(pattern);
            default:
                throw new Exception();
        }
    }

    public abstract Matcher matcher(String str);

    public abstract Matcher matcher(byte[] bytes);

    public class JdkPattern : Pattern
    {

        private readonly System.Text.RegularExpressions.Regex pattern;

        public JdkPattern(String pattern)
        {
            this.pattern = new System.Text.RegularExpressions.Regex(pattern,
                System.Text.RegularExpressions.RegexOptions.Compiled);
        }

        public override Matcher matcher(String str)
        {
            return new Matcher.JdkMatcher(pattern,str);
        }

        public override Matcher matcher(byte[] bytes)
        {
            return new Matcher.JdkMatcher(pattern,
                Encoding.UTF8.GetString(bytes));
        }
    }

    public class Re2Pattern : Pattern
    {

        private readonly RE2CS.Pattern pattern;

        public Re2Pattern(String pattern)
        {
            this.pattern = RE2CS.Pattern.Compile(pattern);
        }

        public override Matcher matcher(String str)
        {
            return new Matcher.Re2Matcher(
                pattern.Matcher(str));
        }

        public override Matcher matcher(byte[] bytes)
        {
            return new Matcher.Re2Matcher(
                pattern.Matcher(bytes));
        }
    }
}
