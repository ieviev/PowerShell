// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis; // for fxcop
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    #region DataAddedEventArgs

    
    public sealed class DataAddedEventArgs : EventArgs
    {
        #region Private Data

        #endregion

        #region Constructor

        
        /// <param name="psInstanceId">
        /// PowerShell InstanceId which added this data.
        /// Guid.Empty, if the data is not added by a PowerShell
        /// instance.
        /// </param>
        /// <param name="index">
        /// Index at which the data is added.
        /// </param>
        internal DataAddedEventArgs(Guid psInstanceId, int index)
        {
            PowerShellInstanceId = psInstanceId;
            Index = index;
        }

        #endregion

        #region Properties

        
        public int Index { get; }

        
        public Guid PowerShellInstanceId { get; }

        #endregion
    }

    #endregion

    
    public sealed class DataAddingEventArgs : EventArgs
    {
        #region Private Data

        #endregion

        #region Constructor

        
        /// <param name="psInstanceId">
        /// PowerShell InstanceId which added this data.
        /// Guid.Empty, if the data is not added by a PowerShell
        /// instance.
        /// </param>
        /// <param name="itemAdded">
        /// The actual item about to be added.
        /// </param>
        internal DataAddingEventArgs(Guid psInstanceId, object itemAdded)
        {
            PowerShellInstanceId = psInstanceId;
            ItemAdded = itemAdded;
        }

        #endregion

        #region Properties

        
        public object ItemAdded { get; }

        
        public Guid PowerShellInstanceId { get; }

        #endregion
    }

    #region PSDataCollection

    
    public class PSDataCollection<T> : IList<T>, ICollection<T>, IEnumerable<T>, IList, ICollection, IEnumerable, IDisposable, ISerializable
    {
        #region Private Data

        private readonly IList<T> _data;
        private ManualResetEvent _readWaitHandle;
        private bool _isOpen = true;
        private bool _releaseOnEnumeration;
        private bool _isEnumerated;
        // a counter to keep track of active PowerShell instances
        // using this buffer.
        private int _refCount;

        private bool _isDisposed = false;

        
        private bool _blockingEnumerator = false;

        
        private bool _refCountIncrementedForBlockingEnumerator = false;

        private int _countNewData = 0;
        private int _dataAddedFrequency = 1;
        private Guid _sourceGuid = Guid.Empty;

        #endregion

        #region Public Constructors

        
        public PSDataCollection() : this(new List<T>())
        {
        }

        
        /// <param name="items">
        /// Items used to initialize the collection
        /// </param>
        /// <remarks>
        /// This constructor is useful when the user wants to use an IEnumerable as an input to one of the PowerShell.BeginInvoke overloads.
        /// The invocation doesn't complete until Complete() is called on the PSDataCollection; this constructor does the Complete() on
        /// behalf of the user.
        /// </remarks>
        public PSDataCollection(IEnumerable<T> items) : this(new List<T>(items))
        {
            this.Complete();
        }

        
        /// <param name="capacity">
        /// The number of elements that the new buffer can initially
        /// store.
        /// </param>
        /// <remarks>
        /// Capacity is the number of elements that the PSDataCollection can
        /// store before resizing is required.
        /// </remarks>
        public PSDataCollection(int capacity) : this(new List<T>(capacity))
        {
        }

        #endregion

        #region type converters

        
        /// <param name="valueToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(bool valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        
        /// <param name="valueToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(string valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        
        /// <param name="valueToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(int valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        
        /// <param name="valueToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(byte valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        private static PSDataCollection<T> CreateAndInitializeFromExplicitValue(object valueToConvert)
        {
            PSDataCollection<T> psdc = new PSDataCollection<T>();
            psdc.Add(LanguagePrimitives.ConvertTo<T>(valueToConvert));
            psdc.Complete();
            return psdc;
        }

        
        /// <param name="valueToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(Hashtable valueToConvert)
        {
            PSDataCollection<T> psdc = new PSDataCollection<T>();
            psdc.Add(LanguagePrimitives.ConvertTo<T>(valueToConvert));
            psdc.Complete();
            return psdc;
        }

        
        /// <param name="valueToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(T valueToConvert)
        {
            PSDataCollection<T> psdc = new PSDataCollection<T>();
            psdc.Add(LanguagePrimitives.ConvertTo<T>(valueToConvert));
            psdc.Complete();
            return psdc;
        }

        
        /// <param name="arrayToConvert">The value to convert.</param>
        /// <returns>New collection of value, marked as Complete.</returns>
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(object[] arrayToConvert)
        {
            PSDataCollection<T> psdc = new PSDataCollection<T>();
            if (arrayToConvert != null)
            {
                foreach (var ae in arrayToConvert)
                {
                    psdc.Add(LanguagePrimitives.ConvertTo<T>(ae));
                }
            }

            psdc.Complete();
            return psdc;
        }

        #endregion

        #region Internal Constructor

        
        /// <param name="listToUse">
        /// buffer where the elements are stored
        /// </param>
        /// <remarks>
        /// Using this constructor will make the data buffer a wrapper on
        /// top of the <paramref name="listToUse"/>, which provides synchronized
        /// access.
        /// </remarks>
        internal PSDataCollection(IList<T> listToUse)
        {
            _data = listToUse;
        }

        
        /// <param name="info">Serialization information for this instance.</param>
        /// <param name="context">The streaming context for this instance.</param>
        protected PSDataCollection(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }

            if (!(info.GetValue("Data", typeof(IList<T>)) is IList<T> listToUse))
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }

            _data = listToUse;

            _blockingEnumerator = info.GetBoolean("BlockingEnumerator");
            _dataAddedFrequency = info.GetInt32("DataAddedCount");
            EnumeratorNeverBlocks = info.GetBoolean("EnumeratorNeverBlocks");
            _isOpen = info.GetBoolean("IsOpen");
        }

        #endregion

        #region PSDataCollection Specific Public Methods / Properties

        
        public event EventHandler<DataAddingEventArgs> DataAdding;

        
        public event EventHandler<DataAddedEventArgs> DataAdded;

        
        public event EventHandler Completed;

        
        public bool IsOpen
        {
            get
            {
                lock (SyncObject)
                {
                    return _isOpen;
                }
            }
        }

        
        public int DataAddedCount
        {
            get
            {
                return _dataAddedFrequency;
            }

            set
            {
                bool raiseDataAdded = false;
                lock (SyncObject)
                {
                    _dataAddedFrequency = value;
                    if (_countNewData >= _dataAddedFrequency)
                    {
                        raiseDataAdded = true;
                        _countNewData = 0;
                    }
                }

                if (raiseDataAdded)
                {
                    // We should raise the event outside of the lock
                    // as the call is made into 3rd party code
                    RaiseDataAddedEvent(_lastPsInstanceId, _lastIndex);
                }
            }
        }

        
        public bool SerializeInput
        {
            get
            {
                return _serializeInput;
            }

            set
            {
                if (typeof(T) != typeof(PSObject))
                {
                    // If you drop this constraint, GetSerializedInput must be updated.
                    throw new NotSupportedException(PSDataBufferStrings.SerializationNotSupported);
                }

                _serializeInput = value;
            }
        }

        private bool _serializeInput = false;

        
        public bool IsAutoGenerated
        {
            get; set;
        }

        
        internal Guid SourceId
        {
            get
            {
                lock (SyncObject)
                {
                    return _sourceGuid;
                }
            }

            set
            {
                lock (SyncObject)
                {
                    _sourceGuid = value;
                }
            }
        }

        
        internal bool ReleaseOnEnumeration
        {
            get
            {
                lock (SyncObject)
                {
                    return _releaseOnEnumeration;
                }
            }

            set
            {
                lock (SyncObject)
                {
                    _releaseOnEnumeration = value;
                }
            }
        }

        
        internal bool IsEnumerated
        {
            get
            {
                lock (SyncObject)
                {
                    return _isEnumerated;
                }
            }

            set
            {
                lock (SyncObject)
                {
                    _isEnumerated = value;
                }
            }
        }

        
        public void Complete()
        {
            bool raiseEvents = false;
            bool raiseDataAdded = false;
            try
            {
                // Close the buffer
                lock (SyncObject)
                {
                    if (_isOpen)
                    {
                        _isOpen = false;
                        raiseEvents = true;
                        // release any threads to notify an event. Enumerator
                        // blocks on this syncObject.
                        Monitor.PulseAll(SyncObject);

                        if (_countNewData > 0)
                        {
                            raiseDataAdded = true;
                            _countNewData = 0;
                        }
                    }
                }
            }
            finally
            {
                // raise the events outside of the lock.
                if (raiseEvents)
                {
                    // unblock any readers waiting on the handle
                    _readWaitHandle?.Set();

                    // A temporary variable is used as the Completed may
                    // reach null (because of -='s) after the null check
                    Completed?.Invoke(this, EventArgs.Empty);
                }

                if (raiseDataAdded)
                {
                    RaiseDataAddedEvent(_lastPsInstanceId, _lastIndex);
                }
            }
        }

        
        public bool BlockingEnumerator
        {
            get
            {
                lock (SyncObject)
                {
                    return _blockingEnumerator;
                }
            }

            set
            {
                lock (SyncObject)
                {
                    _blockingEnumerator = value;

                    if (_blockingEnumerator)
                    {
                        if (!_refCountIncrementedForBlockingEnumerator)
                        {
                            _refCountIncrementedForBlockingEnumerator = true;
                            AddRef();
                        }
                    }
                    else
                    {
                        // TODO: false doesn't always leading to non-blocking
                        // behavior in an intuitive way. Need to follow up
                        // and fix this
                        if (_refCountIncrementedForBlockingEnumerator)
                        {
                            _refCountIncrementedForBlockingEnumerator = false;
                            DecrementRef();
                        }
                    }
                }
            }
        }

        
        public bool EnumeratorNeverBlocks { get; set; }

        #endregion

        #region IList Generic Overrides

        
        /// <param name="index">
        /// The zero-based index of the element to get or set.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Objects cannot be added to a closed buffer.
        /// Make sure the buffer is open for Add and Insert
        /// operations to succeed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is less than 0.
        /// (or)
        /// index is equal to or greater than Count.
        /// </exception>
        public T this[int index]
        {
            get
            {
                lock (SyncObject)
                {
                    return _data[index];
                }
            }

            set
            {
                lock (SyncObject)
                {
                    if ((index < 0) || (index >= _data.Count))
                    {
                        throw PSTraceSource.NewArgumentOutOfRangeException(nameof(index), index,
                            PSDataBufferStrings.IndexOutOfRange, 0, _data.Count - 1);
                    }

                    if (_serializeInput)
                    {
                        value = (T)(object)GetSerializedObject(value);
                    }

                    _data[index] = value;
                }
            }
        }

        
        /// <param name="item">
        /// The object to locate in the buffer.
        /// </param>
        /// <returns>
        /// The index of item if found in the buffer; otherwise, -1.
        /// </returns>
        public int IndexOf(T item)
        {
            lock (SyncObject)
            {
                return InternalIndexOf(item);
            }
        }

        
        /// <param name="index">
        /// The zero-based index at which item should be inserted.
        /// </param>
        /// <param name="item">
        /// The object to insert into the buffer.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Objects cannot be added to a closed buffer.
        /// Make sure the buffer is open for Add and Insert
        /// operations to succeed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The index specified is less than zero or greater
        /// than Count.
        /// </exception>
        public void Insert(int index, T item)
        {
            lock (SyncObject)
            {
                InternalInsertItem(Guid.Empty, index, item);
            }

            RaiseEvents(Guid.Empty, index);
        }

        
        /// <param name="index">
        /// The zero-based index of the item to remove.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is not a valid index in the buffer.
        /// </exception>
        public void RemoveAt(int index)
        {
            lock (SyncObject)
            {
                if ((index < 0) || (index >= _data.Count))
                {
                    throw PSTraceSource.NewArgumentOutOfRangeException(nameof(index), index,
                        PSDataBufferStrings.IndexOutOfRange, 0, _data.Count - 1);
                }

                RemoveItem(index);
            }
        }

        #endregion

        #region ICollection Generic Overrides

        
        public int Count
        {
            get
            {
                lock (SyncObject)
                {
                    if (_data == null)
                        return 0;
                    else
                        return _data.Count;
                }
            }
        }

        
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        
        /// <param name="item">
        /// item to add
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Objects cannot be added to a closed buffer.
        /// Make sure the buffer is open for Add and Insert
        /// operations to succeed.
        /// </exception>
        public void Add(T item)
        {
            InternalAdd(Guid.Empty, item);
        }

        
        public void Clear()
        {
            lock (SyncObject)
            {
                _data?.Clear();
            }
        }

        
        /// <param name="item">
        /// The object to locate in the buffer.
        /// </param>
        /// <returns>
        /// true if the element value is found in the buffer; otherwise false.
        /// </returns>
        public bool Contains(T item)
        {
            lock (SyncObject)
            {
                if (_serializeInput)
                {
                    item = (T)(object)GetSerializedObject(item);
                }

                return _data.Contains(item);
            }
        }

        
        /// <param name="array">
        /// The destination Array for the elements of type T copied from the buffer.
        /// </param>
        /// <param name="arrayIndex">
        /// The zero-based index in the array at which copying begins.
        /// </param>
        /// <exception cref="ArgumentException">
        /// array is multidimensional.
        /// (or)
        /// arrayIndex is equal to or greater than the length of array.
        /// (or)
        /// The number of elements in the source buffer is greater than the
        /// available space from arrayIndex to the end of the destination array.
        /// (or)
        /// Type T cannot be cast automatically to the type of the destination array.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// array is a null reference
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// arrayIndex is less than 0.
        /// </exception>
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncObject)
            {
                _data.CopyTo(array, arrayIndex);
            }
        }

        
        /// <param name="item">
        /// The object to remove from the buffer.
        /// </param>
        /// <returns>
        /// true if item was successfully removed from the buffer; otherwise, false.
        /// </returns>
        public bool Remove(T item)
        {
            lock (SyncObject)
            {
                int index = InternalIndexOf(item);
                if (index < 0)
                {
                    return false;
                }

                RemoveItem(index);
                return true;
            }
        }

        #endregion

        #region IEnumerable Generic Overrides

        
        /// <returns>
        /// An IEnumerator for objects of the type stored in the buffer.
        /// </returns>
        public IEnumerator<T> GetEnumerator()
        {
            return new PSDataCollectionEnumerator<T>(this, EnumeratorNeverBlocks);
        }

        #endregion

        #region IList Overrides

        
        /// <param name="value">
        /// The object to add to the buffer.
        /// </param>
        /// <returns>
        /// The position into which the new element was inserted.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// Objects cannot be added to a closed buffer.
        /// Make sure the buffer is open for Add and Insert
        /// operations to succeed.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        int IList.Add(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            int index = _data.Count;
            InternalAdd(Guid.Empty, (T)value);
            RaiseEvents(Guid.Empty, index);

            return index;
        }

        
        /// <param name="value">
        /// The object to locate in the collection
        /// </param>
        /// <returns>
        /// true if the element value is found in the collection;
        /// otherwise false.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        bool IList.Contains(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            return Contains((T)value);
        }

        
        /// <param name="value">
        /// The element in the buffer whose index is being determined.
        /// </param>
        /// <returns>
        /// The index of the value if found in the buffer; otherwise, -1.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        int IList.IndexOf(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            return IndexOf((T)value);
        }

        
        /// <param name="index">
        /// The zero-based index at which value is to be inserted.
        /// </param>
        /// <param name="value">
        /// The object to insert into the buffer.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// index is not a valid index in the buffer.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        void IList.Insert(int index, object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            Insert(index, (T)value);
        }

        
        /// <param name="value">
        /// The object to be removed from the buffer.
        /// </param>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        void IList.Remove(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            Remove((T)value);
        }

        
        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        
        bool IList.IsReadOnly
        {
            get
            {
                return false;
            }
        }

        
        /// <param name="index">
        /// The zero-based index of the element to get or set.
        /// </param>
        /// <exception cref="IndexOutOfRangeException">
        /// index is less than 0.
        /// (or)
        /// index is equal to or greater than Count.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        object IList.this[int index]
        {
            get
            {
                return this[index];
            }

            set
            {
                PSDataCollection<T>.VerifyValueType(value);
                this[index] = (T)value;
            }
        }

        #endregion

        #region ICollection Overrides

        
        bool ICollection.IsSynchronized
        {
            get
            {
                return true;
            }
        }

        
        object ICollection.SyncRoot
        {
            get
            {
                return SyncObject;
            }
        }

        
        /// <param name="array">
        /// The destination Array for the elements of type T copied
        /// from the buffer.
        /// </param>
        /// <param name="index">
        /// The zero-based index in the array at which copying begins.
        /// </param>
        /// <exception cref="ArgumentException">
        /// array is multidimensional.
        /// (or)
        /// arrayIndex is equal to or greater than the length of array.
        /// (or)
        /// The number of elements in the source buffer is greater than the
        /// available space from arrayIndex to the end of the destination array.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// array is a null reference
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// arrayIndex is less than 0.
        /// </exception>
        void ICollection.CopyTo(Array array, int index)
        {
            lock (SyncObject)
            {
                _data.CopyTo((T[])array, index);
            }
        }

        #endregion

        #region IEnumerable Overrides

        
        /// <returns>
        /// An IEnumerator for objects of the type stored in the buffer.
        /// </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PSDataCollectionEnumerator<T>(this, EnumeratorNeverBlocks);
        }

        #endregion

        #region Streaming Behavior

        
        /// <returns>
        /// A new collection with a copy of all the elements in the current collection.
        /// </returns>
        public Collection<T> ReadAll()
        {
            return ReadAndRemove(0);
        }

        
        /// <returns>
        /// A new collection with a copy of all the elements in the current collection.
        /// </returns>
        /// <param name="readCount">Maximum number of elements to read.</param>
        internal Collection<T> ReadAndRemove(int readCount)
        {
            Dbg.Assert(_data != null, "Collection cannot be null");

            Dbg.Assert(readCount >= 0, "ReadCount cannot be negative");

            int resolvedReadCount = (readCount > 0 ? readCount : Int32.MaxValue);

            lock (SyncObject)
            {
                // Copy the elements into a new collection
                // and clear.
                Collection<T> result = new Collection<T>();

                for (int i = 0; i < resolvedReadCount; i++)
                {
                    if (_data.Count > 0)
                    {
                        result.Add(_data[0]);
                        _data.RemoveAt(0);
                    }
                    else
                    {
                        break;
                    }
                }

                if (_readWaitHandle != null)
                {
                    if (_data.Count > 0 || !_isOpen)
                    {
                        // release all the waiting threads.
                        _readWaitHandle.Set();
                    }
                    else
                    {
                        // reset the handle so that future
                        // threads will block
                        _readWaitHandle.Reset();
                    }
                }

                return result;
            }
        }

        internal T ReadAndRemoveAt0()
        {
            T value = default(T);

            lock (SyncObject)
            {
                if (_data != null && _data.Count > 0)
                {
                    value = _data[0];
                    _data.RemoveAt(0);
                }
            }

            return value;
        }

        #endregion

        #region Protected Virtual Methods

        
        /// <param name="psInstanceId">
        /// InstanceId of PowerShell instance adding this data.
        /// Guid.Empty if not initiated by a PowerShell instance.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the buffer where the object is to be inserted
        /// </param>
        /// <param name="item">
        /// The object to be inserted into the buffer.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The index specified is less than zero or greater
        /// than Count.
        /// </exception>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "ps", Justification = "PS signifies PowerShell and is used at many places in the product.")]
        protected virtual void InsertItem(Guid psInstanceId, int index, T item)
        {
            RaiseDataAddingEvent(psInstanceId, item);

            if (_serializeInput)
            {
                item = (T)(object)GetSerializedObject(item);
            }

            _data.Insert(index, item);
        }

        
        /// <param name="index">
        /// The zero-based index of the buffer where the object is to be removed.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The index specified is less than zero or greater
        /// than the number of items in the buffer.
        /// </exception>
        protected virtual void RemoveItem(int index)
        {
            _data.RemoveAt(index);
        }

        #endregion

        #region Serializable

        
        /// <param name="info">Serialization information for this instance.</param>
        /// <param name="context">The streaming context for this instance.</param>
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(info));
            }

            info.AddValue("Data", _data);
            info.AddValue("BlockingEnumerator", _blockingEnumerator);
            info.AddValue("DataAddedCount", _dataAddedFrequency);
            info.AddValue("EnumeratorNeverBlocks", EnumeratorNeverBlocks);
            info.AddValue("IsOpen", _isOpen);
        }

        #endregion

        #region Internal/Private Methods and Properties

        
        internal WaitHandle WaitHandle
        {
            get
            {
                if (_readWaitHandle == null)
                {
                    lock (SyncObject)
                    {
                        // Create the handle signaled if there are objects in the buffer
                        // or the buffer has been closed.
                        _readWaitHandle ??= new ManualResetEvent(_data.Count > 0 || !_isOpen);
                    }
                }

                return _readWaitHandle;
            }
        }

        
        /// <param name="psInstanceId">
        /// PowerShell InstanceId which added this data.
        /// Guid.Empty, if the data is not added by a PowerShell
        /// instance.
        /// </param>
        /// <param name="index">
        /// Index at which the data is added.
        /// </param>
        private void RaiseEvents(Guid psInstanceId, int index)
        {
            bool raiseDataAdded = false;
            lock (SyncObject)
            {
                if (_readWaitHandle != null)
                {
                    // TODO: Should ObjectDisposedException be caught.

                    if (_data.Count > 0 || !_isOpen)
                    {
                        // release all the waiting threads.
                        _readWaitHandle.Set();
                    }
                    else
                    {
                        // reset the handle so that future
                        // threads will block
                        _readWaitHandle.Reset();
                    }
                }
                // release any threads to notify an event. Enumerator
                // blocks on this syncObject.
                Monitor.PulseAll(SyncObject);

                _countNewData++;
                if (_countNewData >= _dataAddedFrequency || (_countNewData > 0 && !_isOpen))
                {
                    raiseDataAdded = true;
                    _countNewData = 0;
                }
                else
                {
                    // store information in case _dataAddedFrequency is updated or collection completes
                    // so that event may be raised using last added data.
                    _lastPsInstanceId = psInstanceId;
                    _lastIndex = index;
                }
            }

            if (raiseDataAdded)
            {
                // We should raise the event outside of the lock
                // as the call is made into 3rd party code.
                RaiseDataAddedEvent(psInstanceId, index);
            }
        }

        private Guid _lastPsInstanceId;
        private int _lastIndex;

        private void RaiseDataAddingEvent(Guid psInstanceId, object itemAdded)
        {
            // A temporary variable is used as the DataAdding may
            // reach null (because of -='s) after the null check
            DataAdding?.Invoke(this, new DataAddingEventArgs(psInstanceId, itemAdded));
        }

        private void RaiseDataAddedEvent(Guid psInstanceId, int index)
        {
            // A temporary variable is used as the DataAdded may
            // reach null (because of -='s) after the null check
            DataAdded?.Invoke(this, new DataAddedEventArgs(psInstanceId, index));
        }

        
        /// <param name="psInstanceId">
        /// InstanceId of PowerShell instance adding this data.
        /// Guid.Empty if this is not initiated by a PowerShell instance.
        /// </param>
        /// <param name="index">
        /// The zero-based index of the buffer where the object is
        /// to be inserted.
        /// </param>
        /// <param name="item">
        /// The object to be inserted into the buffer.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Objects cannot be added to a closed buffer.
        /// Make sure the buffer is open for Add and Insert
        /// operations to succeed.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The index specified is less than zero or greater
        /// than Count.
        /// </exception>
        private void InternalInsertItem(Guid psInstanceId, int index, T item)
        {
            if (!_isOpen)
            {
                throw PSTraceSource.NewInvalidOperationException(PSDataBufferStrings.WriteToClosedBuffer);
            }

            InsertItem(psInstanceId, index, item);
        }

        
        /// <param name="psInstanceId">
        /// InstanceId of PowerShell instance adding this data.
        /// Guid.Empty if this is not initiated by a PowerShell instance.
        /// </param>
        /// <param name="item">
        /// item to add
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Objects cannot be added to a closed buffer.
        /// Make sure the buffer is open for Add and Insert
        /// operations to succeed.
        /// </exception>
        internal void InternalAdd(Guid psInstanceId, T item)
        {
            // should not rely on data.Count in "finally"
            // as another thread might add data
            int index = -1;

            lock (SyncObject)
            {
                // Add the item and set to raise events
                // so that events are raised outside of
                // lock.
                index = _data.Count;
                InternalInsertItem(psInstanceId, index, item);
            }

            if (index > -1)
            {
                RaiseEvents(psInstanceId, index);
            }
        }

        
        /// <param name="psInstanceId">
        /// InstanceId of PowerShell instance adding this data.
        /// </param>
        /// <param name="collection">
        /// The ICollection whose elements should be added to the end of
        /// the buffer.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="collection"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        internal void InternalAddRange(Guid psInstanceId, ICollection collection)
        {
            if (collection == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(collection));
            }

            int index = -1;
            bool raiseEvents = false;

            lock (SyncObject)
            {
                if (!_isOpen)
                {
                    throw PSTraceSource.NewInvalidOperationException(PSDataBufferStrings.WriteToClosedBuffer);
                }

                index = _data.Count;

                foreach (object o in collection)
                {
                    InsertItem(psInstanceId, _data.Count, (T)o);

                    // set raise events if at least one item is
                    // added.
                    raiseEvents = true;
                }
            }

            if (raiseEvents)
            {
                RaiseEvents(psInstanceId, index);
            }
        }

        
        internal void AddRef()
        {
            lock (SyncObject)
            {
                _refCount++;
            }
        }

        
        internal void DecrementRef()
        {
            lock (SyncObject)
            {
                Dbg.Assert(_refCount > 0, "RefCount cannot be <= 0");

                _refCount--;
                if (_refCount != 0 && (!_blockingEnumerator || _refCount != 1))
                {
                    return;
                }

                // release threads blocked on waithandle
                _readWaitHandle?.Set();

                // release any threads to notify refCount is 0. Enumerator
                // blocks on this syncObject and it needs to be notified
                // when the count becomes 0.
                Monitor.PulseAll(SyncObject);
            }
        }

        
        /// <param name="item">
        /// The object to locate in the buffer.
        /// </param>
        /// <returns>
        /// 0 based index of item if found,
        /// -1 otherwise.
        /// </returns>
        private int InternalIndexOf(T item)
        {
            if (_serializeInput)
            {
                item = (T)(object)GetSerializedObject(item);
            }

            int count = _data.Count;
            for (int index = 0; index < count; index++)
            {
                if (object.Equals(_data[index], item))
                {
                    return index;
                }
            }

            return -1;
        }

        
        /// <param name="value">
        /// Value to verify.
        /// </param>
        /// <exception cref="ArgumentException">
        /// value reference is null.
        /// (or)
        /// value is not of the correct generic type T for the buffer.
        /// </exception>
        private static void VerifyValueType(object value)
        {
            if (value == null)
            {
                if (typeof(T).IsValueType)
                {
                    throw PSTraceSource.NewArgumentNullException(nameof(value), PSDataBufferStrings.ValueNullReference);
                }
            }
            else if (value is not T)
            {
                throw PSTraceSource.NewArgumentException(nameof(value), PSDataBufferStrings.CannotConvertToGenericType,
                                                         value.GetType().FullName,
                                                         typeof(T).FullName);
            }
        }

        // Serializes an object, as long as it's not serialized.
        private static PSObject GetSerializedObject(object value)
        {
            // This is a safe cast, as this method is only called with "SerializeInput" is set,
            // and that method throws if the collection type is not PSObject.
            PSObject result = value as PSObject;

            // Check if serialization would be idempotent
            if (SerializationWouldHaveNoEffect(result))
            {
                return result;
            }
            else
            {
                object deserialized = PSSerializer.Deserialize(PSSerializer.Serialize(value));
                if (deserialized == null)
                {
                    return null;
                }
                else
                {
                    return PSObject.AsPSObject(deserialized);
                }
            }
        }

        private static bool SerializationWouldHaveNoEffect(PSObject result)
        {
            if (result == null)
            {
                return true;
            }

            object baseObject = PSObject.Base(result);
            if (baseObject == null)
            {
                return true;
            }

            // Check if it's a primitive known type
            if (InternalSerializer.IsPrimitiveKnownType(baseObject.GetType()))
            {
                return true;
            }

            // Check if it's a CIM type
            if (baseObject is Microsoft.Management.Infrastructure.CimInstance)
            {
                return true;
            }

            // Check if it's got "Deserialized" in its type name
            if (result.TypeNames[0].StartsWith("Deserialized", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        
        internal object SyncObject { get; } = new object();

        
        internal int RefCount
        {
            get
            {
                return _refCount;
            }

            set
            {
                lock (SyncObject)
                {
                    _refCount = value;
                }
            }
        }

        #endregion

        #region Idle event

        
        internal bool PulseIdleEvent
        {
            get { return (IdleEvent != null); }
        }

        internal event EventHandler<EventArgs> IdleEvent;

        
        internal void FireIdleEvent()
        {
            IdleEvent.SafeInvoke(this, null);
        }

        
        internal void Pulse()
        {
            lock (SyncObject)
            {
                Monitor.PulseAll(SyncObject);
            }
        }

        #endregion

        #region IDisposable Overrides

        
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        
        /// <param name="disposing">If true, release all managed resources.</param>
        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_isDisposed)
                {
                    return;
                }

                lock (SyncObject)
                {
                    if (_isDisposed)
                    {
                        return;
                    }

                    _isDisposed = true;
                }

                Complete();

                lock (SyncObject)
                {
                    if (_readWaitHandle != null)
                    {
                        _readWaitHandle.Dispose();
                        _readWaitHandle = null;
                    }

                    _data?.Clear();
                }
            }
        }
        #endregion IDisposable Overrides
    }

    #endregion

    
    /// <typeparam name="T"></typeparam>
    internal interface IBlockingEnumerator<out T> : IEnumerator<T>
    {
        bool MoveNext(bool block);
    }

    #region PSDataCollectionEnumerator

    
    /// <typeparam name="T"></typeparam>
    internal sealed class PSDataCollectionEnumerator<T> : IBlockingEnumerator<T>
    {
        #region Private Data

        private T _currentElement;
        private int _index;
        private readonly PSDataCollection<T> _collToEnumerate;
        private readonly bool _neverBlock;

        #endregion

        #region Constructor

        
        /// <param name="collection">
        /// PSDataCollection to enumerate.
        /// </param>
        /// <param name="neverBlock">
        /// Controls if the enumerator is blocking by default or not.
        /// </param>
        internal PSDataCollectionEnumerator(PSDataCollection<T> collection, bool neverBlock)
        {
            Dbg.Assert(collection != null,
                "Collection cannot be null");
            Dbg.Assert(!collection.ReleaseOnEnumeration || !collection.IsEnumerated,
                "shouldn't enumerate more than once if ReleaseOnEnumeration is true");

            _collToEnumerate = collection;
            _index = 0;
            _currentElement = default(T);
            _collToEnumerate.IsEnumerated = true;
            _neverBlock = neverBlock;
        }

        #endregion

        #region IEnumerator Overrides

        
        /// <remarks>
        /// For better performance, this property does not throw an exception
        /// if the enumerator is positioned before the first element or after
        /// the last element; the value of the property is undefined.
        /// </remarks>
        T IEnumerator<T>.Current
        {
            get
            {
                return _currentElement;
            }
        }

        
        /// <remarks>
        /// For better performance, this property does not throw an exception
        /// if the enumerator is positioned before the first element or after
        /// the last element; the value of the property is undefined.
        /// </remarks>
        public object Current
        {
            get
            {
                return _currentElement;
            }
        }

        
        /// <returns>
        /// true if the enumerator successfully advanced to the next element;
        /// otherwise, false.
        /// </returns>
        /// <remarks>
        /// This will block if the original collection is attached to any
        /// active PowerShell instances and the original collection is not
        /// closed.
        /// </remarks>
        public bool MoveNext()
        {
            return MoveNext(!_neverBlock);
        }

        
        /// <returns>
        /// true if the enumerator successfully advanced to the next element;
        /// otherwise, false.
        /// </returns>
        /// <param name="block">True - to block when no elements are available.</param>
        public bool MoveNext(bool block)
        {
            lock (_collToEnumerate.SyncObject)
            {
                while (true)
                {
                    if (_index < _collToEnumerate.Count)
                    {
                        _currentElement = _collToEnumerate[_index];
                        if (_collToEnumerate.ReleaseOnEnumeration)
                        {
                            _collToEnumerate[_index] = default(T);
                        }

                        _index++;
                        return true;
                    }

                    // we have reached the end if either the collection is closed
                    // or no powershell instance is bound to this collection.
                    if ((_collToEnumerate.RefCount == 0) || (!_collToEnumerate.IsOpen))
                    {
                        return false;
                    }

                    if (block)
                    {
                        if (_collToEnumerate.PulseIdleEvent)
                        {
                            _collToEnumerate.FireIdleEvent();
                            Monitor.Wait(_collToEnumerate.SyncObject);
                        }
                        else
                        {
                            // using light-weight monitor to block the current thread instead
                            // of AutoResetEvent. This saves using Kernel objects.
                            Monitor.Wait(_collToEnumerate.SyncObject);
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        
        public void Reset()
        {
            _currentElement = default(T);
            _index = 0;
        }

        
        void IDisposable.Dispose()
        {
        }

        #endregion
    }

    #endregion

    
    internal sealed class PSInformationalBuffers
    {
        private readonly Guid _psInstanceId;

        
        /// <param name="psInstanceId">
        /// Guid of Powershell instance creating this buffers.
        /// Whenever an item is added to one of the buffers, this id is
        /// used to notify the buffer about the PowerShell instance adding
        /// this data.
        /// </param>
        internal PSInformationalBuffers(Guid psInstanceId)
        {
            Dbg.Assert(psInstanceId != Guid.Empty,
                "PowerShell instance id cannot be Guid.Empty");

            _psInstanceId = psInstanceId;
            progress = new PSDataCollection<ProgressRecord>();
            verbose = new PSDataCollection<VerboseRecord>();
            debug = new PSDataCollection<DebugRecord>();
            Warning = new PSDataCollection<WarningRecord>();
            Information = new PSDataCollection<InformationRecord>();
        }

        #region Internal Methods / Properties

        
        internal PSDataCollection<ProgressRecord> Progress
        {
            get
            {
                return progress;
            }

            set
            {
                progress = value;
            }
        }

        internal PSDataCollection<ProgressRecord> progress;

        
        internal PSDataCollection<VerboseRecord> Verbose
        {
            get
            {
                return verbose;
            }

            set
            {
                verbose = value;
            }
        }

        internal PSDataCollection<VerboseRecord> verbose;

        
        internal PSDataCollection<DebugRecord> Debug
        {
            get
            {
                return debug;
            }

            set
            {
                debug = value;
            }
        }

        internal PSDataCollection<DebugRecord> debug;

        
        internal PSDataCollection<WarningRecord> Warning { get; set; }

        
        internal PSDataCollection<InformationRecord> Information { get; set; }

        
        /// <param name="item"></param>
        internal void AddProgress(ProgressRecord item) => progress?.InternalAdd(_psInstanceId, item);

        
        /// <param name="item"></param>
        internal void AddVerbose(VerboseRecord item) => verbose?.InternalAdd(_psInstanceId, item);

        
        /// <param name="item"></param>
        internal void AddDebug(DebugRecord item) => debug?.InternalAdd(_psInstanceId, item);

        
        /// <param name="item"></param>
        internal void AddWarning(WarningRecord item) => Warning?.InternalAdd(_psInstanceId, item);

        
        /// <param name="item"></param>
        internal void AddInformation(InformationRecord item) => Information?.InternalAdd(_psInstanceId, item);

        #endregion
    }
}
