/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/parse.go

namespace RE2CS;

public static class Characters
{
    public static int ToLowerCase(int codePoint)
        => (codePoint>=0 && codePoint<=char.MaxValue)
        ? (char.IsSurrogate((char)codePoint) 
        ? -1 : char.ToLower((char)codePoint)):        
        char.ConvertToUtf32(char.ConvertFromUtf32(codePoint).ToLower(), 0);

    public static int ToUpperCase(int codePoint)
        => (codePoint >= 0 && codePoint <= char.MaxValue)
        ? (char.IsSurrogate((char)codePoint)
        ? -1 : char.ToUpper((char)codePoint)) :
        char.ConvertToUtf32(char.ConvertFromUtf32(codePoint).ToUpper(), 0);

}
