// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation.Host;

using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public enum CommandOrigin
    {
        
        Runspace,

        
        Internal
    }

    
    public class AuthorizationManager
    {
        #region constructor

        
        /// <param name="shellId">
        /// </param>
        public AuthorizationManager(string shellId)
        {
            ShellId = shellId;
        }

        #endregion constructor

        private readonly object _policyCheckLock = new object();

        #region methods to use internally

        
        /// <param name="commandInfo">Info on entity to be run.</param>
        /// <param name="origin">The dispatch origin of a command.</param>
        /// <param name="host">Allows access to the host.</param>
        /// <remarks>
        /// This method throws SecurityException in case running is not allowed.
        /// </remarks>
        /// <exception cref="System.Management.Automation.PSSecurityException">
        /// If the derived security manager threw an exception or returned
        /// false with a reason.
        /// </exception>
        internal void ShouldRunInternal(CommandInfo commandInfo,
                                        CommandOrigin origin,
                                        PSHost host)
        {
#if UNIX
            // TODO:PSL this is a workaround since the exception below
            // hides the internal issue of what's going on in terms of
            // execution policy.
            // On non-Windows platform Set/Get-ExecutionPolicy throw
            // PlatformNotSupportedException
            return;
#else

#if DEBUG
            // If we are debugging, let the unit tests swap the file from beneath us
            if (commandInfo.CommandType == CommandTypes.ExternalScript)
            {
                while (Environment.GetEnvironmentVariable("PSCommandDiscoveryPreDelay") != null) { System.Threading.Thread.Sleep(100); }
            }
#endif

            bool result = false;
            bool defaultCatch = false;
            Exception authorizationManagerException = null;

            try
            {
                lock (_policyCheckLock)
                {
                    result = this.ShouldRun(commandInfo, origin, host, out authorizationManagerException);
                }

#if DEBUG
                // If we are debugging, let the unit tests swap the file from beneath us
                if (commandInfo.CommandType == CommandTypes.ExternalScript)
                {
                    while (Environment.GetEnvironmentVariable("PSCommandDiscoveryPostDelay") != null) { System.Threading.Thread.Sleep(100); }
                }
#endif
            }
            catch (Exception e) // Catch-all OK. 3rd party callout
            {
                authorizationManagerException = e;

                defaultCatch = true;
                result = false;
            }

            if (!result)
            {
                if (authorizationManagerException != null)
                {
                    if (authorizationManagerException is PSSecurityException)
                    {
                        throw authorizationManagerException;
                    }
                    else
                    {
                        string message = authorizationManagerException.Message;
                        if (defaultCatch)
                        {
                            message = AuthorizationManagerBase.AuthorizationManagerDefaultFailureReason;
                        }

                        PSSecurityException securityException = new PSSecurityException(message, authorizationManagerException);
                        throw securityException;
                    }
                }
                else
                {
                    throw new PSSecurityException(AuthorizationManagerBase.AuthorizationManagerDefaultFailureReason);
                }
            }
#endif
        }

        
        internal string ShellId { get; }

        #endregion methods to use internally

        #region methods for derived class to override

        
        /// <param name="commandInfo">Information about the command to be run.</param>
        /// <param name="origin">The origin of the command.</param>
        /// <param name="host">The host running the command.</param>
        /// <param name="reason">The reason for preventing execution, if applicable.</param>
        /// <returns>True if the host should run the command.  False otherwise.</returns>
        protected internal virtual bool ShouldRun(CommandInfo commandInfo,
                                                  CommandOrigin origin,
                                                  PSHost host,
                                                  out Exception reason)
        {
            Dbg.Diagnostics.Assert(commandInfo != null, "caller should validate the parameter");

            reason = null;

            return true;
        }

        #endregion methods for derived class to override
    }
}
