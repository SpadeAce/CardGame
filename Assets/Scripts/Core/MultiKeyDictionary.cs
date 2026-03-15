using System;
using System.Collections.Generic;

public class MultiKeyDictionary<TKey1, TKey2, TValue> : Dictionary<TKey1, Dictionary<TKey2, TValue>>
{
    public TValue this[TKey1 key1, TKey2 key2]
    {
        get
        {
            if (!ContainsKey(key1) || !this[key1].ContainsKey(key2))
                throw new ArgumentOutOfRangeException();
            return base[key1][key2];
        }
        set
        {
            if (!ContainsKey(key1))
                this[key1] = new Dictionary<TKey2, TValue>();
            this[key1][key2] = value;
        }
    }

    public void Add(TKey1 key1, TKey2 key2, TValue value)
    {
        if(!ContainsKey(key1))
            this[key1] = new Dictionary<TKey2, TValue>();
        this[key1].Add(key2, value);
    }

    public bool ContainsKey(TKey1 key1, TKey2 key2)
    {
        return base.ContainsKey(key1) && this[key1].ContainsKey(key2);
    }
}

public class MultiKeyDictionary<TKey1, TKey2, TKey3, TValue> : Dictionary<TKey1, MultiKeyDictionary<TKey2, TKey3, TValue>>
{
    public TValue this[TKey1 key1, TKey2 key2, TKey3 key3]
    {
        get
        {
            return ContainsKey(key1) ? this[key1][key2, key3] : default(TValue);
        }
        set
        {
            if (!ContainsKey(key1))
                this[key1] = new MultiKeyDictionary<TKey2, TKey3, TValue>();
            this[key1][key2, key3] = value;
        }
    }

    public void Add(TKey1 key1, TKey2 key2, TKey3 key3, TValue value)
    {
        if (!ContainsKey(key1))
            this[key1] = new MultiKeyDictionary<TKey2, TKey3, TValue>();
        this[key1].Add(key2, key3, value);
    }

    public bool ContainKey(TKey1 key1, TKey2 key2, TKey3 key3)
    {
        return base.ContainsKey(key1) && this[key1].ContainsKey(key2, key3);
    }
}