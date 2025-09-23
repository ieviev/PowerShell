// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Internal
{
    using System;
    using System.Threading;
    using System.Runtime.InteropServices;
    using System.Management.Automation.Runspaces;

    
    /// <remarks>
    /// This class is not safe for multi-threaded operations.
    /// </remarks>
    internal class ObjectWriter : PipelineWriter
    {
        
        /// <param name="stream">The stream to write.</param>
        /// <exception cref="ArgumentNullException">Thrown if the specified stream is null.</exception>
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

        
        /// <value>true if the underlying stream is open, otherwise; false.</value>
        /// <remarks>
        /// Attempting to write to the underlying stream if IsOpen is false throws
        /// a <see cref="PipelineClosedException"/>.
        /// </remarks>
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

        
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects that stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// </remarks>
        public override int MaxCapacity
        {
            get
            {
                return _stream.MaxCapacity;
            }
        }

        #endregion Properties

        #region Methods

        
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw an ObjectDisposedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">
        /// The stream is already disposed
        /// </exception>
        public override void Close()
        {
            _stream.Close();
            // 2003/09/02-JonN I removed setting _stream
            // to null, now all of the tests for null can come out.
        }

        
        /// <exception cref="ObjectDisposedException">
        /// The underlying stream is disposed
        /// </exception>
        public override void Flush()
        {
            _stream.Flush();
        }

        
        /// <param name="obj">The object to add to the stream.</param>
        /// <returns>
        /// One, if the write was successful, otherwise;
        /// zero if the stream was closed before the object could be written,
        /// or if the object was AutomationNull.Value.
        /// </returns>
        /// <exception cref="PipelineClosedException">
        /// The underlying stream is closed
        /// </exception>
        /// <remarks>
        /// AutomationNull.Value is ignored
        /// </remarks>
        public override int Write(object obj)
        {
            return _stream.Write(obj);
        }

        
        /// <param name="obj">Object or enumeration to read from.</param>
        /// <param name="enumerateCollection">
        /// If enumerateCollection is true, and <paramref name="obj"/>
        /// is an enumeration according to LanguagePrimitives.GetEnumerable,
        /// the objects in the enumeration will be unrolled and
        /// written separately.  Otherwise, <paramref name="obj"/>
        /// will be written as a single object.
        /// </param>
        /// <returns>The number of objects written.</returns>
        /// <exception cref="PipelineClosedException">
        /// The underlying stream is closed
        /// </exception>
        /// <remarks>
        /// If the enumeration contains elements equal to
        /// AutomationNull.Value, they are ignored.
        /// This can cause the return value to be less than the size of
        /// the collection.
        /// </remarks>
        public override int Write(object obj, bool enumerateCollection)
        {
            return _stream.Write(obj, enumerateCollection);
        }

#if (false)
        
        /// <param name="sender">The stream raising the event.</param>
        /// <param name="args">Standard event args.</param>
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

        
        /// <remarks>Can never be null</remarks>
        private readonly ObjectStreamBase _stream;

        #endregion Private Fields
    }

    
    /// <remarks>
    /// PSDataCollection is introduced after 1.0. PSDataCollection
    /// is used to store data from the last command in
    /// the pipeline and hence the writer will not
    /// support certain features like Flush().
    /// </remarks>
    internal class PSDataCollectionWriter<T> : ObjectWriter
    {
        #region Constructors

        
        /// <param name="stream">The stream to write.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the specified stream is null
        /// </exception>
        public PSDataCollectionWriter(PSDataCollectionStream<T> stream)
            : base(stream)
        {
        }

        #endregion
    }
}
