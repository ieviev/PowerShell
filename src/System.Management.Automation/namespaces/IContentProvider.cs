// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
namespace System.Management.Automation.Provider
{
    #region IContentCmdletProvider

    
    public interface IContentCmdletProvider
    {
        
        IContentReader? GetContentReader(string path);

        
        object? GetContentReaderDynamicParameters(string path);

        
        IContentWriter? GetContentWriter(string path);

        
        object? GetContentWriterDynamicParameters(string path);

        
        void ClearContent(string path);

        
        object? ClearContentDynamicParameters(string path);
    }

    #endregion IContentCmdletProvider
}
