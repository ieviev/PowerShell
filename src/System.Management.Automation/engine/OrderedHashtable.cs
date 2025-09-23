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

        
        /// <param name="capacity">The capacity.</param>
        public OrderedHashtable(int capacity) : base(capacity)
        {
            _orderedDictionary = new OrderedDictionary(capacity);
        }

        
        /// <param name="dictionary">The dictionary to use for initialization.</param>
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

        
        /// <param name="key">The key.</param>
        /// <returns>The value associated with the key.</returns>
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

        
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public override void Add(object key, object? value)
        {
            _orderedDictionary.Add(key, value);
        }

        
        public override void Clear()
        {
            _orderedDictionary.Clear();
        }

        
        /// <returns>A shallow clone of the hashtable.</returns>
        public override object Clone()
        {
            return new OrderedHashtable(_orderedDictionary);
        }

        
        /// <param name="key">The key to locate in the hashtable.</param>
        /// <returns>true if the hashtable contains an element with the specified key; otherwise, false.</returns>
        public override bool Contains(object key)
        {
            return _orderedDictionary.Contains(key);
        }

        
        /// <param name="key">The key to locate in the hashtable.</param>
        /// <returns>true if the hashtable contains an element with the specified key; otherwise, false.</returns>
        public override bool ContainsKey(object key)
        {
            return _orderedDictionary.Contains(key);
        }

        
        /// <param name="value">The value to locate in the hashtable.</param>
        /// <returns>true if the hashtable contains an element with the specified value; otherwise, false.</returns>
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

        
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from the hashtable. The array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public override void CopyTo(Array array, int arrayIndex)
        {
            _orderedDictionary.CopyTo(array, arrayIndex);
        }

        
        /// <returns>The enumerator.</returns>
        public override IDictionaryEnumerator GetEnumerator()
        {
            return _orderedDictionary.GetEnumerator();
        }

        
        /// <returns>The enumerator.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        
        /// <param name="key">The key to remove.</param>
        public override void Remove(object key)
        {
            _orderedDictionary.Remove(key);
        }
    }
}
