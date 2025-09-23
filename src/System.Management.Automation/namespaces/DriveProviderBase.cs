// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Provider
{
    #region DriveCmdletProvider

    
    public abstract class DriveCmdletProvider : CmdletProvider
    {
        #region internal members

        #region DriveCmdletProvider method wrappers

        
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

        
        internal object NewDriveDynamicParameters(CmdletProviderContext context)
        {
            Context = context;
            return NewDriveDynamicParameters();
        }

        
        internal PSDriveInfo RemoveDrive(PSDriveInfo drive, CmdletProviderContext context)
        {
            Context = context;
            return RemoveDrive(drive);
        }

        
        internal Collection<PSDriveInfo> InitializeDefaultDrives(CmdletProviderContext context)
        {
            Context = context;
            Context.Drive = null;

            return InitializeDefaultDrives();
        }

        #endregion DriveCmdletProvider method wrappers

        #endregion internal members

        #region Protected methods that should be overridden by derived classes

        
        protected virtual PSDriveInfo NewDrive(PSDriveInfo drive)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return drive;
            }
        }

        
        protected virtual object NewDriveDynamicParameters()
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return null;
            }
        }

        
        protected virtual PSDriveInfo RemoveDrive(PSDriveInfo drive)
        {
            using (PSTransactionManager.GetEngineProtectionScope())
            {
                return drive;
            }
        }

        
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
