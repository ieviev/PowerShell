// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Set, "Content", DefaultParameterSetName = "Path", SupportsShouldProcess = true, SupportsTransactions = true,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097142")]
    public class SetContentCommand : WriteContentCommandBase
    {
        #region protected members

        
        internal override void BeforeOpenStreams(string[] paths)
        {
            if (paths == null || paths.Length == 0)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(paths));
            }

            CmdletProviderContext context = new(GetCurrentContext());

            foreach (string path in paths)
            {
                try
                {
                    InvokeProvider.Content.Clear(path, context);
                    context.ThrowFirstErrorOrDoNothing(true);
                }
                catch (PSNotSupportedException)
                {
                    // If the provider doesn't support clear, that is fine. Continue
                    // on with the setting of the content.
                    continue;
                }
                catch (DriveNotFoundException driveNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            driveNotFound.ErrorRecord,
                            driveNotFound));
                    continue;
                }
                catch (ProviderNotFoundException providerNotFound)
                {
                    WriteError(
                        new ErrorRecord(
                            providerNotFound.ErrorRecord,
                            providerNotFound));
                    continue;
                }
                catch (ItemNotFoundException)
                {
                    // If the item is not found then there is nothing to clear so ignore this exception.
                    continue;
                }
            }
        }

        
        internal override bool CallShouldProcess(string path)
        {
            string action = NavigationResources.SetContentAction;

            string target = StringUtil.Format(NavigationResources.SetContentTarget, path);

            return ShouldProcess(target, action);
        }
        #endregion protected members
    }
}
