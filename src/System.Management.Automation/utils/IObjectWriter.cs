// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Threading;

namespace System.Management.Automation.Runspaces
{
    
    public abstract class PipelineWriter
    {
        
        public abstract WaitHandle WaitHandle
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

        
        public abstract void Flush();

        
        public abstract int Write(object obj);

        
        public abstract int Write(object obj, bool enumerateCollection);
    }

    internal class DiscardingPipelineWriter : PipelineWriter
    {
        private readonly ManualResetEvent _waitHandle = new ManualResetEvent(true);

        public override WaitHandle WaitHandle
        {
            get { return _waitHandle; }
        }

        private bool _isOpen = true;

        public override bool IsOpen
        {
            get { return _isOpen; }
        }

        private int _count = 0;

        public override int Count
        {
            get { return _count; }
        }

        public override int MaxCapacity
        {
            get { return int.MaxValue; }
        }

        public override void Close()
        {
            _isOpen = false;
        }

        public override void Flush()
        {
        }

        public override int Write(object obj)
        {
            const int numberOfObjectsWritten = 1;
            _count += numberOfObjectsWritten;
            return numberOfObjectsWritten;
        }

        public override int Write(object obj, bool enumerateCollection)
        {
            if (!enumerateCollection)
            {
                return this.Write(obj);
            }

            int numberOfObjectsWritten = 0;
            IEnumerable enumerable = LanguagePrimitives.GetEnumerable(obj);
            if (enumerable != null)
            {
                foreach (object o in enumerable)
                {
                    numberOfObjectsWritten++;
                }
            }
            else
            {
                numberOfObjectsWritten++;
            }

            _count += numberOfObjectsWritten;
            return numberOfObjectsWritten;
        }
    }
}
