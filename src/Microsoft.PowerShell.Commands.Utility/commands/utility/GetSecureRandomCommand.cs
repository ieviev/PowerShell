// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsCommon.Get, "SecureRandom", DefaultParameterSetName = GetRandomCommandBase.RandomNumberParameterSet,
        HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2235055", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(int), typeof(long), typeof(double))]
    public sealed class GetSecureRandomCommand : GetRandomCommandBase
    {
        // nothing unique from base class
    }
}
