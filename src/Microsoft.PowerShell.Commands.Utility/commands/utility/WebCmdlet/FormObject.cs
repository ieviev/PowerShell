// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System.Collections.Generic;

namespace Microsoft.PowerShell.Commands
{
    
    public class FormObject
    {
        
        public string Id { get; }

        
        public string Method { get; }

        
        public string Action { get; }

        
        public Dictionary<string, string> Fields { get; }

        
        /// <param name="id"></param>
        /// <param name="method"></param>
        /// <param name="action"></param>
        public FormObject(string id, string method, string action)
        {
            Id = id;
            Method = method;
            Action = action;
            Fields = new Dictionary<string, string>();
        }

        internal void AddField(string key, string value)
        {
            if (key is not null && !Fields.TryGetValue(key, out string? _))
            {
                Fields[key] = value;
            }
        }
    }
}
