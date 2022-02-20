/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
using System.Text;

namespace RE2CS.BenchMarks;

public class BenchmarkSubMatch
{

    //@Param({ "JDK", "RE2J"})
    private Implementations impl;

    //@Param({ "true", "false"})
    private bool binary;


    private Pattern pattern;

    byte[] bytes = readFile("google-maps-contact-info.html");
    //@Setup
    string html = String.Empty;
    public void setup()
    {
        html = Encoding.UTF8.GetString(bytes);
        pattern = Pattern.compile(impl, "([0-9]{3}-[0-9]{3}-[0-9]{4})");
    }

    //@Benchmark
    public void findPhoneNumbers()
    {
        Matcher matcher = binary ? pattern.matcher(bytes) : pattern.matcher(html);
        int count = 0;
        while (matcher.find())
        {
            matcher.group();
            count++;
        }
        if (count != 1)
        {
            throw new Exception("Expected to match one phone number.");
        }
    }

    private static byte[] readFile(String name)
    {
        return File.ReadAllBytes(name);
    }
}
