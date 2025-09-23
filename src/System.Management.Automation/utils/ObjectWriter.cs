// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Management.Automation.Runspaces;

    
    internal class ObjectWriter : PipelineWriter
    {
        
        public ObjectWriter([In, Out] ObjectStreamBase stream)
        {
            ArgumentNullException.ThrowIfNull(stream);

            _stream = stream;
#if (false)
            stream.WriteReady += new EventHandler (this.OnWriteReady);
#endif
        }

        #region Properties

        
        public override WaitHandle WaitHandle
        {
            get
            {
                return _stream.WriteHandle;
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

        #endregion Properties

        #region Methods

        
        public override void Close()
        {
            _stream.Close();
            // 2003/09/02-JonN I removed setting _stream
            // to null, now all of the tests for null can come out.
        }

        
        public override void Flush()
        {
            _stream.Flush();
        }

        
        public override int Write(object obj)
        {
            return _stream.Write(obj);
        }

        
        public override int Write(object obj, bool enumerateCollection)
        {
            return _stream.Write(obj, enumerateCollection);
        }

#if (false)
        
        private void OnWriteReady (object sender, EventArgs args)
        {
            if (WriteReady != null)
            {
                // call any event handlers on this, replacing the
                // ObjectStream sender with 'this' since receivers
                // are expecting an PipelineWriter
                WriteReady (this, args);
            }
        }
#endif

        #endregion Methods

        #region Private fields

        
        private readonly ObjectStreamBase _stream;

        #endregion Private Fields
    }

    
    internal class PSDataCollectionWriter<T> : ObjectWriter
    {
        #region Constructors

        
        public PSDataCollectionWriter(PSDataCollectionStream<T> stream)
            : base(stream)
        {
        }

        #endregion
    }
}
