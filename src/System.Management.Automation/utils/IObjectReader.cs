// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace System.Management.Automation.Runspaces
{
    
    public abstract class PipelineReader<T>
    {
        
        public abstract event EventHandler DataReady;

        
        public abstract WaitHandle WaitHandle
        {
            get;
        }

        
        public abstract bool EndOfPipeline
        {
            get;
        }

        
        public abstract bool IsOpen
        {
            get;
        }

        
        public abstract int Count
        {
            get;
        }

        
        public abstract int MaxCapacity
        {
            get;
        }

        
        public abstract void Close();

        
        public abstract Collection<T> Read(int count);

        
        public abstract T Read();

        
        public abstract Collection<T> ReadToEnd();

        
        public abstract Collection<T> NonBlockingRead();

        // 892370-2003/10/29-JonN added this method
        
        public abstract Collection<T> NonBlockingRead(int maxRequested);

        
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
