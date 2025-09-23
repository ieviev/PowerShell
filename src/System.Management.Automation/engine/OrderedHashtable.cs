// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.Serialization;

#nullable enable

namespace System.Management.Automation
{
    
    public sealed class OrderedHashtable : Hashtable, IEnumerable
    {
        private readonly OrderedDictionary _orderedDictionary;

        
        public OrderedHashtable()
        {
            _orderedDictionary = new OrderedDictionary();
        }

        
        public OrderedHashtable(int capacity) : base(capacity)
        {
            _orderedDictionary = new OrderedDictionary(capacity);
        }

        
        public OrderedHashtable(IDictionary dictionary)
        {
            _orderedDictionary = new OrderedDictionary(dictionary.Count);
            foreach (DictionaryEntry entry in dictionary)
            {
                _orderedDictionary.Add(entry.Key, entry.Value);
            }
        }

        
        public override int Count
        {
            get
            {
                return _orderedDictionary.Count;
            }
        }

        
        public override bool IsFixedSize
        {
            get
            {
                return false;
            }
        }

        
        public override bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        
        public override bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        
        public override ICollection Keys
        {
            get
            {
                return _orderedDictionary.Keys;
            }
        }

        
        public override ICollection Values
        {
            get
            {
                return _orderedDictionary.Values;
            }
        }

        
        public override object? this[object key]
        {
            get
            {
                return _orderedDictionary[key];
            }

            set
            {
                _orderedDictionary[key] = value;
            }
        }

        
        public override void Add(object key, object? value)
        {
            _orderedDictionary.Add(key, value);
        }

        
        public override void Clear()
        {
            _orderedDictionary.Clear();
        }

        
        public override object Clone()
        {
            return new OrderedHashtable(_orderedDictionary);
        }

        
        public override bool Contains(object key)
        {
            return _orderedDictionary.Contains(key);
        }

        
        public override bool ContainsKey(object key)
        {
            return _orderedDictionary.Contains(key);
        }

        
        public override bool ContainsValue(object? value)
        {
            foreach (DictionaryEntry entry in _orderedDictionary)
            {
                if (Equals(entry.Value, value))
                {
                    return true;
                }
            }

            return false;
        }

        
        public override void CopyTo(Array array, int arrayIndex)
        {
            _orderedDictionary.CopyTo(array, arrayIndex);
        }

        
        public override IDictionaryEnumerator GetEnumerator()
        {
            return _orderedDictionary.GetEnumerator();
        }

        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        
        public override void Remove(object key)
        {
            _orderedDictionary.Remove(key);
        }
    }
}
