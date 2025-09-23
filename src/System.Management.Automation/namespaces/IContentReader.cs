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
        
        /// <param name="readCount">
        /// The number of "blocks" of data to be read from the item.
        /// </param>
        /// <returns>
        /// An array of the blocks of data read from the item.
        /// </returns>
        /// <remarks>
        /// A "block" of content is provider specific.  For the file system
        /// a "block" may be considered a line of text, a byte, a character, or delimited string.
        ///
        /// The implementation of this method should break the content down into meaningful blocks
        /// that the user may want to manipulate individually. The number of blocks to return is
        /// indicated by the <paramref name="readCount"/> parameter.
        /// </remarks>
        IList Read(long readCount);

        
        /// <param name="offset">
        /// An offset of the number of blocks to seek from the origin.
        /// </param>
        /// <param name="origin">
        /// The place in the stream to start the seek from.
        /// </param>
        /// <remarks>
        /// The implementation of this method moves the content reader <paramref name="offset"/>
        /// number of blocks from the specified <paramref name="origin"/>. See <see cref="IContentReader.Read"/>
        /// for a description of what a block is.
        /// </remarks>
        void Seek(long offset, SeekOrigin origin);

        
        /// <remarks>
        /// The implementation of this method should close any resources held open by the
        /// reader.
        /// </remarks>
        void Close();
    }

    #endregion IContentReader
}
