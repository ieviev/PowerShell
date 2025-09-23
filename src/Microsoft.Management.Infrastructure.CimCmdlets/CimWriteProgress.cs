// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#region Using directives

using System;
using System.Management.Automation;

#endregion

namespace Microsoft.Management.Infrastructure.CimCmdlets
{
    
    internal sealed class CimWriteProgress : CimBaseAction
    {
        
        /// <param name="activity">
        ///  Activity identifier of the given activity
        /// </param>
        /// <param name="currentOperation">
        /// current operation description of the given activity
        /// </param>
        /// <param name="statusDescription">
        /// current status description of the given activity
        /// </param>
        /// <param name="percentageCompleted">
        /// percentage completed of the given activity
        /// </param>
        /// <param name="secondsRemaining">
        /// how many seconds remained for the given activity
        /// </param>
        public CimWriteProgress(
            string theActivity,
            int theActivityID,
            string theCurrentOperation,
            string theStatusDescription,
            uint thePercentageCompleted,
            uint theSecondsRemaining)
        {
            this.Activity = theActivity;
            this.ActivityID = theActivityID;
            this.CurrentOperation = theCurrentOperation;
            if (string.IsNullOrEmpty(theStatusDescription))
            {
                this.StatusDescription = CimCmdletStrings.DefaultStatusDescription;
            }
            else
            {
                this.StatusDescription = theStatusDescription;
            }

            this.PercentageCompleted = thePercentageCompleted;
            this.SecondsRemaining = theSecondsRemaining;
        }

        
        /// <param name="cmdlet"></param>
        public override void Execute(CmdletOperationBase cmdlet)
        {
            DebugHelper.WriteLog(
                "...Activity {0}: id={1}, remain seconds ={2}, percentage completed = {3}",
                4,
                this.Activity,
                this.ActivityID,
                this.SecondsRemaining,
                this.PercentageCompleted);

            ValidationHelper.ValidateNoNullArgument(cmdlet, "cmdlet");
            ProgressRecord record = new(
                this.ActivityID,
                this.Activity,
                this.StatusDescription);
            record.Activity = this.Activity;
            record.ParentActivityId = 0;
            record.SecondsRemaining = (int)this.SecondsRemaining;
            record.PercentComplete = (int)this.PercentageCompleted;
            cmdlet.WriteProgress(record);
        }

        #region members

        
        internal string Activity { get; }

        
        internal int ActivityID { get; }

        
        internal string CurrentOperation { get; }

        
        internal string StatusDescription { get; }

        
        internal uint PercentageCompleted { get; }

        
        internal uint SecondsRemaining { get; }

        #endregion
    }
}
