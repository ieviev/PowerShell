// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.ObjectModel;

namespace Microsoft.PowerShell.Commands
{
    
    public class FormObjectCollection : Collection<FormObject>
    {
        
        public FormObject? this[string key]
        {
            get
            {
                FormObject? form = null;
                foreach (FormObject f in this)
                {
                    if (string.Equals(key, f.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        form = f;
                        break;
                    }
                }

                return form;
            }
        }
    }
}
