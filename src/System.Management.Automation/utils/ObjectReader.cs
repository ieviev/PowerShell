// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using System.Runtime.InteropServices;
using System.Threading;

namespace System.Management.Automation.Internal
{
    
    internal abstract class ObjectReaderBase<T> : PipelineReader<T>, IDisposable
    {
        
        protected ObjectReaderBase([In, Out] ObjectStreamBase stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _stream = stream;
        }

        #region Events

        
        public override event EventHandler DataReady
        {
            add
            {
                lock (_monitorObject)
                {
                    bool firstRegistrant = (InternalDataReady == null);
                    InternalDataReady += value;
                    if (firstRegistrant)
                    {
                        _stream.DataReady += this.OnDataReady;
                    }
                }
            }

            remove
            {
                lock (_monitorObject)
                {
                    InternalDataReady -= value;
                    if (InternalDataReady == null)
                    {
                        _stream.DataReady -= this.OnDataReady;
                    }
                }
            }
        }

        public event EventHandler InternalDataReady = null;

        #endregion Events

        #region Public Properties

        
        public override WaitHandle WaitHandle
        {
            get
            {
                return _stream.ReadHandle;
            }
        }

        
        public override bool EndOfPipeline
        {
            get
            {
                return _stream.EndOfPipeline;
            }
        }

        
        public override bool IsOpen
        {
            get
            {
                return _stream.IsOpen;
            }
        }

        
        public override int Count
        {
            get
            {
                return _stream.Count;
            }
        }

        
        public override int MaxCapacity
        {
            get
            {
                return _stream.MaxCapacity;
            }
        }

        #endregion Public Properties

        #region Public Methods

        
        public override void Close()
        {
            // 2003/09/02-JonN added call to close underlying stream
            _stream.Close();
        }

        #endregion Public Methods

        #region Private Methods

        
        private void OnDataReady(object sender, EventArgs args)
        {
            // call any event handlers on this, replacing the
            // ObjectStream sender with 'this' since receivers
            // are expecting a PipelineReader<object>
            InternalDataReady.SafeInvoke(this, args);
        }

        #endregion Private Methods

        #region Private fields

        
        protected ObjectStreamBase _stream;

        
        private readonly object _monitorObject = new object();

        #endregion Private fields

        #region IDisposable

        
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        
        protected abstract void Dispose(bool disposing);

        #endregion IDisposable
    }

    
    internal class ObjectReader : ObjectReaderBase<object>
    {
        #region ctor
        
        public ObjectReader([In, Out] ObjectStream stream)
            : base(stream)
        { }
        #endregion ctor

        
        public override Collection<object> Read(int count)
        {
            return _stream.Read(count);
        }

        
        public override object Read()
        {
            return _stream.Read();
        }

        
        public override Collection<object> ReadToEnd()
        {
            return _stream.ReadToEnd();
        }

        
        public override Collection<object> NonBlockingRead()
        {
            return _stream.NonBlockingRead(Int32.MaxValue);
        }

        
        public override Collection<object> NonBlockingRead(int maxRequested)
        {
            return _stream.NonBlockingRead(maxRequested);
        }

        
        public override object Peek()
        {
            return _stream.Peek();
        }

        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Close();
            }
        }
    }

    
    internal class PSObjectReader : ObjectReaderBase<PSObject>
    {
        #region ctor
        
        public PSObjectReader([In, Out] ObjectStream stream)
            : base(stream)
        { }
        #endregion ctor

        
        public override Collection<PSObject> Read(int count)
        {
            return MakePSObjectCollection(_stream.Read(count));
        }

        
        public override PSObject Read()
        {
            return MakePSObject(_stream.Read());
        }

        
        public override Collection<PSObject> ReadToEnd()
        {
            return MakePSObjectCollection(_stream.ReadToEnd());
        }

        
        public override Collection<PSObject> NonBlockingRead()
        {
            return MakePSObjectCollection(_stream.NonBlockingRead(Int32.MaxValue));
        }

        
        public override Collection<PSObject> NonBlockingRead(int maxRequested)
        {
            return MakePSObjectCollection(_stream.NonBlockingRead(maxRequested));
        }

        
        public override PSObject Peek()
        {
            return MakePSObject(_stream.Peek());
        }

        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Close();
            }
        }

        #region Private
        private static PSObject MakePSObject(object o)
        {
            if (o == null)
                return null;

            return PSObject.AsPSObject(o);
        }

        // It might ultimately be more efficient to
        // make ObjectStream generic and convert the objects to PSObject
        // before inserting them into the initial Collection, so that we
        // don't have to convert the collection later.
        private static Collection<PSObject> MakePSObjectCollection(
            Collection<object> coll)
        {
            if (coll == null)
                return null;
            Collection<PSObject> retval = new Collection<PSObject>();
            foreach (object o in coll)
            {
                retval.Add(MakePSObject(o));
            }

            return retval;
        }
        #endregion Private
    }

    
    internal class PSDataCollectionReader<T, TResult>
        : ObjectReaderBase<TResult>
    {
        #region Private Data

        private readonly PSDataCollectionEnumerator<T> _enumerator;

        #endregion

        #region ctor
        
        public PSDataCollectionReader(PSDataCollectionStream<T> stream)
            : base(stream)
        {
            System.Management.Automation.Diagnostics.Assert(stream.ObjectStore != null,
                "Stream should have a valid data store");
            _enumerator = (PSDataCollectionEnumerator<T>)stream.ObjectStore.GetEnumerator();
        }

        #endregion ctor

        
        public override Collection<TResult> Read(int count)
        {
            throw new NotSupportedException();
        }

        
        public override TResult Read()
        {
            object result = AutomationNull.Value;
            if (_enumerator.MoveNext())
            {
                result = _enumerator.Current;
            }

            return ConvertToReturnType(result);
        }

        
        public override Collection<TResult> ReadToEnd()
        {
            throw new NotSupportedException();
        }

        
        public override Collection<TResult> NonBlockingRead()
        {
            return NonBlockingRead(Int32.MaxValue);
        }

        
        public override Collection<TResult> NonBlockingRead(int maxRequested)
        {
            if (maxRequested < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(maxRequested), maxRequested);
            }

            if (maxRequested == 0)
            {
                return new Collection<TResult>();
            }

            Collection<TResult> result = new Collection<TResult>();
            int readCount = maxRequested;

            while (readCount > 0)
            {
                if (_enumerator.MoveNext(false))
                {
                    result.Add(ConvertToReturnType(_enumerator.Current));
                    continue;
                }

                break;
            }

            return result;
        }

        
        public override TResult Peek()
        {
            throw new NotSupportedException();
        }

        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _stream.Close();
            }
        }

        private static TResult ConvertToReturnType(object inputObject)
        {
            Type resultType = typeof(TResult);
            if (typeof(PSObject) == resultType || typeof(object) == resultType)
            {
                TResult result;
                LanguagePrimitives.TryConvertTo(inputObject, out result);
                return result;
            }

            System.Management.Automation.Diagnostics.Assert(false,
                "ReturnType should be either object or PSObject only");
            throw PSTraceSource.NewNotSupportedException();
        }
    }

    
    internal class PSDataCollectionPipelineReader<T, TReturn>
        : ObjectReaderBase<TReturn>
    {
        #region Private Data

        private readonly PSDataCollection<T> _datastore;

        #endregion Private Data

        #region ctor
        
        internal PSDataCollectionPipelineReader(PSDataCollectionStream<T> stream,
            string computerName, Guid runspaceId)
            : base(stream)
        {
            System.Management.Automation.Diagnostics.Assert(stream.ObjectStore != null,
                "Stream should have a valid data store");
            _datastore = stream.ObjectStore;
            ComputerName = computerName;
            RunspaceId = runspaceId;
        }

        #endregion ctor

        
        internal string ComputerName { get; }

        
        internal Guid RunspaceId { get; }

        
        public override Collection<TReturn> Read(int count)
        {
            throw new NotSupportedException();
        }

        
        public override TReturn Read()
        {
            object result = AutomationNull.Value;
            if (_datastore.Count > 0)
            {
                Collection<T> resultCollection = _datastore.ReadAndRemove(1);

                // ReadAndRemove returns a Collection<T> type but we
                // just want the single object contained in the collection.
                if (resultCollection.Count == 1)
                {
                    result = resultCollection[0];
                }
            }

            return ConvertToReturnType(result);
        }

        
        public override Collection<TReturn> ReadToEnd()
        {
            throw new NotSupportedException();
        }

        
        public override Collection<TReturn> NonBlockingRead()
        {
            return NonBlockingRead(Int32.MaxValue);
        }

        
        public override Collection<TReturn> NonBlockingRead(int maxRequested)
        {
            if (maxRequested < 0)
            {
                throw PSTraceSource.NewArgumentOutOfRangeException(nameof(maxRequested), maxRequested);
            }

            if (maxRequested == 0)
            {
                return new Collection<TReturn>();
            }

            Collection<TReturn> results = new Collection<TReturn>();
            int readCount = maxRequested;

            while (readCount > 0)
            {
                if (_datastore.Count > 0)
                {
                    results.Add(ConvertToReturnType((_datastore.ReadAndRemove(1))[0]));
                    readCount--;
                    continue;
                }

                break;
            }

            return results;
        }

        
        public override TReturn Peek()
        {
            throw new NotSupportedException();
        }

        
        private static TReturn ConvertToReturnType(object inputObject)
        {
            Type resultType = typeof(TReturn);
            if (typeof(PSObject) == resultType || typeof(object) == resultType)
            {
                TReturn result;
                LanguagePrimitives.TryConvertTo(inputObject, out result);
                return result;
            }

            System.Management.Automation.Diagnostics.Assert(false,
                "ReturnType should be either object or PSObject only");
            throw PSTraceSource.NewNotSupportedException();
        }

        #region IDisposable

        
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _datastore.Dispose();
            }
        }

        #endregion IDisposable
    }
}
