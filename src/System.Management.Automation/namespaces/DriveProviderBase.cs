// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region DriveCmdletProvider

    
    /// <remarks>
    /// Although it is possible to derive from this base class to implement a Cmdlet Provider, in most
    /// cases one should derive from <see cref="System.Management.Automation.Provider.ItemCmdletProvider"/>,
    /// <see cref="System.Management.Automation.Provider.ContainerCmdletProvider"/>, or
    /// <see cref ="System.Management.Automation.Provider.NavigationCmdletProvider"/>
    /// </remarks>
    public abstract class DriveCmdletProvider : CmdletProvider
    {
        #region internal members

        #region DriveCmdletProvider method wrappers

        
        /// <param name="drive">
        /// The PSDriveInfo object the represents the drive to be mounted.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// The drive that was returned from the protected NewDrive method.
        /// </returns>
        internal PSDriveInfo NewDrive(PSDriveInfo drive, CmdletProviderContext context)
        {
            Context = context;

            // Make sure the provider supports credentials if they were passed
            // in the drive.

            if (drive.Credential != null &&
                drive.Credential != PSCredential.Empty &&
                !CmdletProviderManagementIntrinsics.CheckProviderCapabilities(ProviderCapabilities.Credentials, ProviderInfo))
            {
                throw PSTraceSource.NewNotSupportedException(
                    SessionStateStrings.NewDriveCredentials_NotSupported);
            }

            return NewDrive(drive);
        }

        
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class.
        /// </returns>
        internal object NewDriveDynamicParameters(CmdletProviderContext context)
        {
            Context = context;
            return NewDriveDynamicParameters();
        }

        
        /// <param name="drive">
        /// The PSDriveInfo object the represents the mounted drive.
        /// </param>
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// The drive that was returned from the protected RemoveDrive method.
        /// </returns>
        internal PSDriveInfo RemoveDrive(PSDriveInfo drive, CmdletProviderContext context)
        {
            Context = context;
            return RemoveDrive(drive);
        }

        
        /// <param name="context">
        /// The context under which this method is being called.
        /// </param>
        /// <returns>
        /// An array of drives returned from the protected InitializeDefaultDrives method.
        /// </returns>
        internal Collection<PSDriveInfo> InitializeDefaultDrives(CmdletProviderContext context)
        {
            Context = context;
            Context.Drive = null;

            return InitializeDefaultDrives();
        }

        #endregion DriveCmdletProvider method wrappers

        #endregion internal members

        #region Protected methods that should be overridden by derived classes

        
        /// <param name="drive">
        /// The proposed new drive.
        /// </param>
        /// <returns>
        /// The new drive that is to be added to the MSH namespace. This
        /// can either be the same <paramref name="drive"/> object that
        /// was passed in or a modified version of it.
        ///
        /// The default implementation returns the drive that was passed.
        /// </returns>
        /// <remarks>
        /// This method gives the provider an opportunity to associate
        /// provider specific data with a drive. This is done by deriving
        /// a new class from <see cref="System.Management.Automation.PSDriveInfo"/>
        /// and adding any properties, methods, or fields that are necessary.
        /// When this method gets called, the override should create an instance
        /// of the derived PSDriveInfo using the passed in PSDriveInfo. The derived
        /// PSDriveInfo should then be returned. Each subsequent call into the provider
        /// that uses this drive will have access to the derived PSDriveInfo via the
        /// PSDriveInfo property provided by the base class.
        ///
        /// Any failures should be sent to the <see cref="System.Management.Automation.Provider.CmdletProvider.WriteError(ErrorRecord)"/>
        /// method and null should be returned.
        /// </remarks>
        protected virtual PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return drive;
            }
        }

        
        /// <returns>
        /// Overrides of this method should return an object that has properties and fields decorated with
        /// parsing attributes similar to a cmdlet class or a
        /// <see cref="System.Management.Automation.RuntimeDefinedParameterDictionary"/>.
        ///
        /// The default implementation returns null. (no additional parameters)
        /// </returns>
        protected virtual object NewDriveDynamicParameters()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        /// <param name="drive">
        /// The Drive object the represents the mounted drive.
        /// </param>
        /// <returns>
        /// If the drive can be removed it should return the drive that was passed
        /// in. If the drive cannot be removed, null should be returned or an exception
        /// should be thrown.
        ///
        /// The default implementation returns the drive that was passed.
        /// </returns>
        /// <remarks>
        /// A provider should override this method to free any resources that may be associated with
        /// the drive being removed.
        /// </remarks>
        protected virtual PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return drive;
            }
        }

        
        /// <returns>
        /// A collection of the drives the provider wants to be added to the session upon initialization.
        ///
        /// The default implementation returns an empty <see cref="System.Management.Automation.PSDriveInfo"/> collection.
        /// </returns>
        /// <remarks>
        /// After the Start method is called on a provider, the InitializeDefaultDrives
        /// method is called. This is an opportunity for the provider to
        /// mount drives that are important to it. For instance, the Active Directory
        /// provider might mount a drive for the defaultNamingContext if the
        /// machine is joined to a domain.
        ///
        /// All providers should mount a root drive to help the user with discoverability.
        /// This root drive might contain a listing of a set of locations that would be
        /// interesting as roots for other mounted drives. For instance, the Active
        /// Directory provider my create a drive that lists the naming contexts found
        /// in the namingContext attributes on the RootDSE. This will help users
        /// discover interesting mount points for other drives.
        /// </remarks>
        protected virtual Collection<PSDriveInfo> InitializeDefaultDrives()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return new Collection<PSDriveInfo>();
            }
        }

        #endregion Public methods
    }

    #endregion DriveCmdletProvider
}
