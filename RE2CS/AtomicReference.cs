/*
 * Copyright (c) 2020 The Go Authors. All rights reserved.
 *
 * Use of this source code is governed by a BSD-style
 * license that can be found in the LICENSE file.
 */
// Original Go source here:
// http://code.google.com/p/go/source/browse/src/pkg/regexp/syntax/parse.go

namespace RE2CS;

public class AtomicReference<V> where V : class
{
    private static long valueOffset;

    private volatile V value;

    public AtomicReference(V initialValue = default)
    {
        value = initialValue;
    }

    public V get() {
        return value;
    }

    public void set(V newValue)
    {
        value = newValue;
    }

    public bool compareAndSet(V expect, V update) {
        lock (this)
        {
            if(this.value == expect)
            {
                this.value = update;
                return true;
            }
            return false;
        }
    }


    public override string ToString()
    {
        return get().ToString();
    }

}
