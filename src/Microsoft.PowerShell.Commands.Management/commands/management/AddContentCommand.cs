// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Add, "Content", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2096489")]
    public class AddContentCommand : WriteContentCommandBase
    {
        #region protected members

        
        internal override void SeekContentPosition(List<ContentHolder> contentHolders)
        {
            foreach (ContentHolder holder in contentHolders)
            {
                if (holder.Writer != null)
                {
                    try
                    {
                        holder.Writer.Seek(0, System.IO.SeekOrigin.End);
                    }
                    catch (Exception e) // Catch-all OK, 3rd party callout
                    {
                        ProviderInvocationException providerException =
                            new(
                                "ProviderSeekError",
                                SessionStateStrings.ProviderSeekError,
                                holder.PathInfo.Provider,
                                holder.PathInfo.Path,
                                e);

                        // Log a provider health event

                        MshLog.LogProviderHealthEvent(
                            this.Context,
                            holder.PathInfo.Provider.Name,
                            providerException,
                            Severity.Warning);

                        throw providerException;
                    }
                }
            }
        }

        
        internal override bool CallShouldProcess(string path)
        {
            string action = NavigationResources.AddContentAction;

            string target = StringUtil.Format(NavigationResources.AddContentTarget, path);

            return ShouldProcess(target, action);
        }

        #endregion protected members
    }
}
