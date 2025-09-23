// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Management.UI.Internal
{
    
    /// <typeparam name="T">There are no restrictions on T.</typeparam>
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class StateDescriptor<T>
    {
        private Guid id;
        private string name;

        
        protected StateDescriptor()
        {
            this.Id = Guid.NewGuid();
        }

        
        /// <param name="name">The friendly name for the StateDescriptor.</param>
        protected StateDescriptor(string name)
            : this()
        {
            this.Name = name;
        }

        
        public Guid Id
        {
            get
            {
                return this.id;
            }

            protected set
            {
                this.id = value;
            }
        }

        
        public string Name
        {
            get
            {
                return this.name;
            }

            set
            {
                this.name = value;
            }
        }

        
        /// <param name="subject">The object whose state will be saved.</param>
        public abstract void SaveState(T subject);

        
        /// <param name="subject">The object whose state will be restored.</param>
        public abstract void RestoreState(T subject);
    }
}
