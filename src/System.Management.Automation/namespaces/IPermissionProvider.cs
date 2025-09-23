// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable
using System.Security.AccessControl;

namespace System.Management.Automation.Provider
{
    #region ISecurityDescriptorCmdletProvider

    
    /// <remarks>
    /// A namespace provider should implement this interface if items in the
    /// namespace are protected by Security Descriptors.
    /// </remarks>
    public interface ISecurityDescriptorCmdletProvider
    {
        
        /// <param name="path">
        /// The path of the item to from which to retrieve the security descriptor.
        /// </param>
        /// <param name="includeSections">
        /// The sections of the security descriptor to retrieve, if your provider
        /// supports them.
        /// </param>
        /// <returns>
        /// Nothing.   Write the security descriptor to the context's pipeline for
        /// the item specified by the path using the WriteSecurityDescriptorObject
        /// method.
        /// </returns>
        void GetSecurityDescriptor(
            string path,
            AccessControlSections includeSections);

        
        /// <param name="path">
        /// The path of the item to for which to set the security descriptor.
        /// </param>
        /// <param name="securityDescriptor">
        /// The new security descriptor for the item.  This should replace the
        /// previously existing security descriptor.
        /// </param>
        /// <returns>
        /// Nothing.   After setting the security descriptor to the value passed in,
        /// write the new security descriptor to the context's pipeline for the
        /// item specified by the path using the WriteSecurityDescriptorObject method.
        /// </returns>
        void SetSecurityDescriptor(
            string path,
            ObjectSecurity securityDescriptor);

        
        /// <param name="path">
        /// Path of the item to use to determine the type of resulting
        /// SecurityDescriptor.
        /// </param>
        /// <param name="includeSections">
        /// The sections of the security descriptor to create.
        /// </param>
        /// <returns>
        /// A new ObjectSecurity object of the same type as
        /// the item specified by the path.
        /// </returns>
        ObjectSecurity NewSecurityDescriptorFromPath(
            string path,
            AccessControlSections includeSections);

        
        /// <param name="type">
        /// The type of Security Descriptor to create.  Your provider should
        /// understand a string representation for each of the types of
        /// SecurityDescriptors that it supports.  For example, the File System
        /// provider performs a case-insensitive comparison against "file" for a
        /// FileSecurity descriptor, and "directory" or "container" for a
        /// DirectorySecurity descriptor.
        /// </param>
        /// <param name="includeSections">
        /// The sections of the security descriptor to create.
        /// </param>
        /// <returns>
        /// A new ObjectSecurity object of the specified type.
        /// </returns>
        ObjectSecurity NewSecurityDescriptorOfType(
            string type,
            AccessControlSections includeSections);
    }

    #endregion ISecurityDescriptorCmdletProvider
}
