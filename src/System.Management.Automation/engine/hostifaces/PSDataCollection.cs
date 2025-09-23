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

        
        public PSDataCollection(IEnumerable<T> items) : this(new List<T>(items))
        {
            this.Complete();
        }

        
        public PSDataCollection(int capacity) : this(new List<T>(capacity))
        {
        }

        #endregion

        #region type converters

        
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(bool valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(string valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(int valueToConvert)
        {
            return CreateAndInitializeFromExplicitValue(valueToConvert);
        }

        
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

        
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(Hashtable valueToConvert)
        {
            PSDataCollection<T> psdc = new PSDataCollection<T>();
            psdc.Add(LanguagePrimitives.ConvertTo<T>(valueToConvert));
            psdc.Complete();
            return psdc;
        }

        
        [SuppressMessage("Microsoft.Usage", "CA2225:OperatorOverloadsHaveNamedAlternates",
            Justification = "There are already alternates to the implicit casts, ToXXX and FromXXX methods are unnecessary and redundant")]
        public static implicit operator PSDataCollection<T>(T valueToConvert)
        {
            PSDataCollection<T> psdc = new PSDataCollection<T>();
            psdc.Add(LanguagePrimitives.ConvertTo<T>(valueToConvert));
            psdc.Complete();
            return psdc;
        }

        
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

        
        internal PSDataCollection(IList<T> listToUse)
        {
            _data = listToUse;
        }

        
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

        
        public int IndexOf(T item)
        {
            lock (SyncObject)
            {
                return InternalIndexOf(item);
            }
        }

        
        public void Insert(int index, T item)
        {
            lock (SyncObject)
            {
                InternalInsertItem(Guid.Empty, index, item);
            }

            RaiseEvents(Guid.Empty, index);
        }

        
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

        
        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (SyncObject)
            {
                _data.CopyTo(array, arrayIndex);
            }
        }

        
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

        
        public IEnumerator<T> GetEnumerator()
        {
            return new PSDataCollectionEnumerator<T>(this, EnumeratorNeverBlocks);
        }

        #endregion

        #region IList Overrides

        
        int IList.Add(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            int index = _data.Count;
            InternalAdd(Guid.Empty, (T)value);
            RaiseEvents(Guid.Empty, index);

            return index;
        }

        
        bool IList.Contains(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            return Contains((T)value);
        }

        
        int IList.IndexOf(object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            return IndexOf((T)value);
        }

        
        void IList.Insert(int index, object value)
        {
            PSDataCollection<T>.VerifyValueType(value);
            Insert(index, (T)value);
        }

        
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

        
        void ICollection.CopyTo(Array array, int index)
        {
            lock (SyncObject)
            {
                _data.CopyTo((T[])array, index);
            }
        }

        #endregion

        #region IEnumerable Overrides

        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return new PSDataCollectionEnumerator<T>(this, EnumeratorNeverBlocks);
        }

        #endregion

        #region Streaming Behavior

        
        public Collection<T> ReadAll()
        {
            return ReadAndRemove(0);
        }

        
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

        
        protected virtual void RemoveItem(int index)
        {
            _data.RemoveAt(index);
        }

        #endregion

        #region Serializable

        
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

        
        private void InternalInsertItem(Guid psInstanceId, int index, T item)
        {
            if (!_isOpen)
            {
                throw PSTraceSource.NewInvalidOperationException(PSDataBufferStrings.WriteToClosedBuffer);
            }

            InsertItem(psInstanceId, index, item);
        }

        
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

    
    internal interface IBlockingEnumerator<out T> : IEnumerator<T>
    {
        bool MoveNext(bool block);
    }

    #region PSDataCollectionEnumerator

    
    internal sealed class PSDataCollectionEnumerator<T> : IBlockingEnumerator<T>
    {
        #region Private Data

        private T _currentElement;
        private int _index;
        private readonly PSDataCollection<T> _collToEnumerate;
        private readonly bool _neverBlock;

        #endregion

        #region Constructor

        
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

        
        T IEnumerator<T>.Current
        {
            get
            {
                return _currentElement;
            }
        }

        
        public object Current
        {
            get
            {
                return _currentElement;
            }
        }

        
        public bool MoveNext()
        {
            return MoveNext(!_neverBlock);
        }

        
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

        
        internal void AddProgress(ProgressRecord item) => progress?.InternalAdd(_psInstanceId, item);

        
        internal void AddVerbose(VerboseRecord item) => verbose?.InternalAdd(_psInstanceId, item);

        
        internal void AddDebug(DebugRecord item) => debug?.InternalAdd(_psInstanceId, item);

        
        internal void AddWarning(WarningRecord item) => Warning?.InternalAdd(_psInstanceId, item);

        
        internal void AddInformation(InformationRecord item) => Information?.InternalAdd(_psInstanceId, item);

        #endregion
    }
}
