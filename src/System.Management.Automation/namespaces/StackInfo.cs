// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace System.Management.Automation
{
    
    public sealed class PathInfoStack : Stack<PathInfo>
    {
        
        internal PathInfoStack(string stackName, Stack<PathInfo> locationStack) : base()
        {
            if (locationStack == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(locationStack));
            }

            if (string.IsNullOrEmpty(stackName))
            {
                throw PSTraceSource.NewArgumentException(nameof(stackName));
            }

            Name = stackName;

            // Since the Stack<T> constructor takes an IEnumerable and
            // not a Stack<T> the stack actually gets enumerated in the
            // wrong order.  I have to push them on manually in the
            // appropriate order.

            PathInfo[] stackContents = new PathInfo[locationStack.Count];
            locationStack.CopyTo(stackContents, 0);

            for (int index = stackContents.Length - 1; index >= 0; --index)
            {
                this.Push(stackContents[index]);
            }
        }

        
        public string Name { get; } = null;
    }
}
