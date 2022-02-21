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
    private volatile V _value;

    public AtomicReference(V initialValue = default)
    {
        _value = initialValue;
    }

    public V Value { get => _value; set => _value = value; }
    
    public bool compareAndSet(V expect, V update) {
        lock (this)
        {
            var same = false;
            if(same = (this._value == expect))
            {
                this._value = update;
            }
            return same;
        }
    }

    public override string ToString() => Value?.ToString() ?? "";
}
