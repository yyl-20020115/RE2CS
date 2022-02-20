using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RE2CS.Super;

/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */

/** GWT supersource for {@link Character#toLowerCase}. */
public static class Characters
{
    public static int toLowerCase(int codePoint) 
        => char.ConvertToUtf32(char.ConvertFromUtf32(codePoint).ToLower(), 0);

    public static int toUpperCase(int codePoint) 
        => char.ConvertToUtf32(char.ConvertFromUtf32(codePoint).ToUpper(), 0);
}

