// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.IO;

#nullable enable
namespace System.Management.Automation.Provider
{
    #region IContentWriter

    
    public interface IContentWriter : IDisposable
    {
        
        IList Write(IList content);

        
        void Seek(long offset, SeekOrigin origin);

        
        void Close();
    }

    #endregion IContentWriter
}
