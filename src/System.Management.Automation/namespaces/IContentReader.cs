// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.IO;

#nullable enable
namespace System.Management.Automation.Provider
{
    #region IContentReader

    
    public interface IContentReader : IDisposable
    {
        
        IList Read(long readCount);

        
        void Seek(long offset, SeekOrigin origin);

        
        void Close();
    }

    #endregion IContentReader
}
