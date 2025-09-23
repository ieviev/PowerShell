// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Security;

namespace Microsoft.PowerShell
{
    
    internal partial
    class ConsoleHostUserInterface : System.Management.Automation.Host.PSHostUserInterface
    {
        
        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName)
        {
            return PromptForCredential(caption,
                                         message,
                                         userName,
                                         targetName,
                                         PSCredentialTypes.Default,
                                         PSCredentialUIOptions.Default);
        }

        
        public override PSCredential PromptForCredential(
            string caption,
            string message,
            string userName,
            string targetName,
            PSCredentialTypes allowedCredentialTypes,
            PSCredentialUIOptions options)
        {
            PSCredential cred = null;
            SecureString password = null;
            string userPrompt = null;
            string passwordPrompt = null;

            if (!string.IsNullOrEmpty(caption))
            {
                // Should be a skin lookup

                WriteLineToConsole();
                WriteLineToConsole(PromptColor, RawUI.BackgroundColor, WrapToCurrentWindowWidth(caption));
            }

            if (!string.IsNullOrEmpty(message))
            {
                WriteLineToConsole(WrapToCurrentWindowWidth(message));
            }

            if (string.IsNullOrEmpty(userName))
            {
                userPrompt = ConsoleHostUserInterfaceSecurityResources.PromptForCredential_User;

                //
                // need to prompt for user name first
                //
                do
                {
                    WriteToConsole(userPrompt, true);
                    userName = ReadLine();
                    if (userName == null)
                    {
                        return null;
                    }
                }
                while (userName.Length == 0);
            }

            passwordPrompt = StringUtil.Format(ConsoleHostUserInterfaceSecurityResources.PromptForCredential_Password, userName
            );

            if (!InternalTestHooks.NoPromptForPassword)
            {
                WriteToConsole(passwordPrompt, transcribeResult: true);
                password = ReadLineAsSecureString();
                if (password == null)
                {
                    return null;
                }

                WriteLineToConsole();
            }
            else
            {
                password = new SecureString();
            }

            if (!string.IsNullOrEmpty(targetName))
            {
                userName = StringUtil.Format("{0}\\{1}", targetName, userName);
            }

            cred = new PSCredential(userName, password);

            return cred;
        }
    }
}
