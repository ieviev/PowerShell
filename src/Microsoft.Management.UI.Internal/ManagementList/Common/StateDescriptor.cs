// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Microsoft.Management.UI.Internal
{
    
    [SuppressMessage("Microsoft.MSInternal", "CA903:InternalNamespaceShouldNotContainPublicTypes")]
    public abstract class StateDescriptor<T>
    {
        private Guid id;
        private string name;

        
        protected StateDescriptor()
        {
            this.Id = Guid.NewGuid();
        }

        
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

        
        public abstract void SaveState(T subject);

        
        public abstract void RestoreState(T subject);
    }
}
