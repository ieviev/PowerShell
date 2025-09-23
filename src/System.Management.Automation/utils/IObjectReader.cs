// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace System.Management.Automation.Runspaces
{
    
    /// <seealso cref="System.Management.Automation.Runspaces.Pipeline.Output"/>
    /// <seealso cref="System.Management.Automation.Runspaces.Pipeline.Error"/>
    public abstract class PipelineReader<T>
    {
        
        public abstract event EventHandler DataReady;

        
        public abstract WaitHandle WaitHandle
        {
            get;
        }

        
        /// <value>True if the stream is closed and contains no data, otherwise false</value>
        /// <remarks>
        /// Attempting to read from the underlying stream if EndOfPipeline is true returns
        /// zero objects.
        /// </remarks>
        public abstract bool EndOfPipeline
        {
            get;
        }

        
        /// <value>true if the underlying stream is open, otherwise false</value>
        /// <remarks>
        /// The underlying stream may be readable after it is closed if data remains in the
        /// internal buffer. Check <see cref="EndOfPipeline"/> to determine if
        /// the underlying stream is closed and contains no data.
        /// </remarks>
        public abstract bool IsOpen
        {
            get;
        }

        
        public abstract int Count
        {
            get;
        }

        
        /// <value>
        /// The capacity of the stream.
        /// </value>
        /// <remarks>
        /// The capacity is the number of objects that stream may contain at one time.  Once this
        /// limit is reached, attempts to write into the stream block until buffer space
        /// becomes available.
        /// </remarks>
        public abstract int MaxCapacity
        {
            get;
        }

        
        /// <remarks>
        /// Causes subsequent calls to IsOpen to return false and calls to
        /// a write operation to throw an PipelineClosedException.
        /// All calls to Close() after the first call are silently ignored.
        /// </remarks>
        /// <exception cref="PipelineClosedException">
        /// The stream is already disposed
        /// </exception>
        public abstract void Close();

        
        /// <param name="count">The maximum number of objects to read.</param>
        /// <returns>The objects read.</returns>
        /// <remarks>
        /// This method blocks if the number of objects in the stream is less than <paramref name="count"/>
        /// and the stream is not closed.
        /// </remarks>
        public abstract Collection<T> Read(int count);

        
        /// <returns>The next object in the stream.</returns>
        /// <remarks>This method blocks if the stream is empty</remarks>
        public abstract T Read();

        
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// If the stream is empty, an empty collection is returned.
        /// </remarks>
        public abstract Collection<T> ReadToEnd();

        
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// This method performs a read of all objects currently in the
        /// stream.  If there are no objects in the stream,
        /// an empty collection is returned.
        /// </remarks>
        public abstract Collection<T> NonBlockingRead();

        // 892370-2003/10/29-JonN added this method
        
        /// <returns>A collection of zero or more objects.</returns>
        /// <remarks>
        /// This method performs a read of objects currently in the
        /// stream.  If there are no objects in the stream,
        /// an empty collection is returned.
        /// </remarks>
        /// <param name="maxRequested">
        /// Return no more than maxRequested objects.
        /// </param>
        public abstract Collection<T> NonBlockingRead(int maxRequested);

        
        /// <returns>
        /// The next object in the stream or AutomationNull.Value if the stream is empty
        /// </returns>
        /// <exception cref="PipelineClosedException">The stream is closed.</exception>
        public abstract T Peek();

        #region IEnumerable<T> Members

        
        internal IEnumerator<T> GetReadEnumerator()
        {
            while (!this.EndOfPipeline)
            {
                T t = this.Read();
                if (object.Equals(t, System.Management.Automation.Internal.AutomationNull.Value))
                {
                    yield break;
                }
                else
                {
                    yield return t;
                }
            }
        }

        #endregion
    }
}
