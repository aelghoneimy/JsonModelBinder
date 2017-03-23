namespace JsonModelBinder
{
    using System.Collections.Generic;

    internal struct CacheKeyPair<TKey1, TKey2>
    {
        public readonly TKey1 Key1;
        public readonly TKey2 Key2;

        public CacheKeyPair(TKey1 key1, TKey2 key2)
        {
            Key1 = key1;
            Key2 = key2;
        }
    }

    internal class Cache<TKey, TValue> : Dictionary<TKey, TValue> where TValue : class
    {
        public new TValue this[TKey key]
        {
            get
            {
                try
                {
                    return base[key];
                }
                catch
                {
                    return null;
                }
            }

            set { base[key] = value; }
        }
    }

    internal class Cache<TKey1, TKey2, TValue> : Dictionary<CacheKeyPair<TKey1, TKey2>, TValue> where TValue: class
    {
        public new TValue this[CacheKeyPair<TKey1, TKey2> keyPair]
        {
            get
            {
                try
                {
                    return base[keyPair];
                }
                catch
                {
                    return null;
                }
            }

            set
            {
                base[keyPair] = value;
            }
        }

        public TValue this[TKey1 key1, TKey2 key2]
        {
            get
            {
                try
                {
                    return base[new CacheKeyPair<TKey1, TKey2>(key1, key2)];
                }
                catch
                {
                    return null;
                }
            }

            set
            {
                base[new CacheKeyPair<TKey1, TKey2>(key1, key2)] = value;
            }
        }

        public void Add(TKey1 key1, TKey2 key2, TValue value)
        {
            base[new CacheKeyPair<TKey1, TKey2>(key1, key2)] = value;
        }

        public bool ContainsKey(TKey1 key1, TKey2 key2)
        {
            return ContainsKey(new CacheKeyPair<TKey1, TKey2>(key1, key2));
        }

        public bool Remove(TKey1 key1, TKey2 key2)
        {
            return Remove(new CacheKeyPair<TKey1, TKey2>(key1, key2));
        }
    }
}