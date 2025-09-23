// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Net.Mail;
using System.Text;

namespace Microsoft.PowerShell.Commands
{
    #region SendMailMessage
    
    [Obsolete("This cmdlet does not guarantee secure connections to SMTP servers. While there is no immediate replacement available in PowerShell, we recommend you do not use Send-MailMessage at this time. See https://aka.ms/SendMailMessage for more information.")]
    [Cmdlet(VerbsCommunications.Send, "MailMessage", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2097115")]
    public sealed class SendMailMessage : PSCmdlet
    {
        #region Command Line Parameters

        
        [Parameter(ValueFromPipeline = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [Alias("PsPath")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Attachments { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Bcc { get; set; }

        
        [Parameter(Position = 2, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string Body { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("BAH")]
        public SwitchParameter BodyAsHtml { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("BE")]
        [ValidateNotNullOrEmpty]
        [ArgumentEncodingCompletions]
        [ArgumentToEncodingTransformation]
        public Encoding Encoding
        {
            get
            {
                return _encoding;
            }

            set
            {
                EncodingConversion.WarnIfObsolete(this, value);
                _encoding = value;
            }
        }

        private Encoding _encoding = Encoding.ASCII;

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Cc")]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] Cc { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Alias("DNO")]
        [ValidateNotNullOrEmpty]
        public DeliveryNotificationOptions DeliveryNotificationOption { get; set; }

        
        [Parameter(Mandatory = true, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public string From { get; set; }

        
        [Parameter(Position = 3, ValueFromPipelineByPropertyName = true)]
        [Alias("ComputerName")]
        [ValidateNotNullOrEmpty]
        public string SmtpServer { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        public MailPriority Priority { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public string[] ReplyTo { get; set; }

        
        [Parameter(Mandatory = false, Position = 1, ValueFromPipelineByPropertyName = true)]
        [Alias("sub")]
        public string Subject { get; set; }

        
        [Parameter(Mandatory = true, Position = 0, ValueFromPipelineByPropertyName = true)]
        [ValidateNotNullOrEmpty]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public string[] To { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [Credential]
        [ValidateNotNullOrEmpty]
        public PSCredential Credential { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        public SwitchParameter UseSsl { get; set; }

        
        [Parameter(ValueFromPipelineByPropertyName = true)]
        [ValidateRange(0, int.MaxValue)]
        public int Port { get; set; }

        #endregion

        #region Private variables and methods

        // Instantiate a new instance of MailMessage
        private readonly MailMessage _mMailMessage = new();

        private SmtpClient _mSmtpClient = null;

        
        private void AddAddressesToMailMessage(object address, string param)
        {
            string[] objEmailAddresses = address as string[];
            foreach (string strEmailAddress in objEmailAddresses)
            {
                try
                {
                    switch (param)
                    {
                        case "to":
                            {
                                _mMailMessage.To.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "cc":
                            {
                                _mMailMessage.CC.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "bcc":
                            {
                                _mMailMessage.Bcc.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                        case "replyTo":
                            {
                                _mMailMessage.ReplyToList.Add(new MailAddress(strEmailAddress));
                                break;
                            }
                    }
                }
                catch (FormatException e)
                {
                    ErrorRecord er = new(e, "FormatException", ErrorCategory.InvalidType, null);
                    WriteError(er);
                    continue;
                }
            }
        }

        #endregion

        #region Overrides

        
        protected override void BeginProcessing()
        {
            try
            {
                // Set the sender address of the mail message
                _mMailMessage.From = new MailAddress(From);
            }
            catch (FormatException e)
            {
                ErrorRecord er = new(e, "FormatException", ErrorCategory.InvalidType, From);
                ThrowTerminatingError(er);
            }

            // Set the recipient address of the mail message
            AddAddressesToMailMessage(To, "to");

            // Set the BCC address of the mail message
            if (Bcc != null)
            {
                AddAddressesToMailMessage(Bcc, "bcc");
            }

            // Set the CC address of the mail message
            if (Cc != null)
            {
                AddAddressesToMailMessage(Cc, "cc");
            }

            // Set the Reply-To address of the mail message
            if (ReplyTo != null)
            {
                AddAddressesToMailMessage(ReplyTo, "replyTo");
            }

            // Set the delivery notification
            _mMailMessage.DeliveryNotificationOptions = DeliveryNotificationOption;

            // Set the subject of the mail message
            _mMailMessage.Subject = Subject;

            // Set the body of the mail message
            _mMailMessage.Body = Body;

            // Set the subject and body encoding
            _mMailMessage.SubjectEncoding = Encoding;
            _mMailMessage.BodyEncoding = Encoding;

            // Set the format of the mail message body as HTML
            _mMailMessage.IsBodyHtml = BodyAsHtml;

            // Set the priority of the mail message to normal
            _mMailMessage.Priority = Priority;

            // Get the PowerShell environment variable
            // globalEmailServer might be null if it is deleted by: PS> del variable:PSEmailServer
            PSVariable globalEmailServer = SessionState.Internal.GetVariable(SpecialVariables.PSEmailServer);

            if (SmtpServer == null && globalEmailServer != null)
            {
                SmtpServer = Convert.ToString(globalEmailServer.Value, CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(SmtpServer))
            {
                ErrorRecord er = new(new InvalidOperationException(SendMailMessageStrings.HostNameValue), null, ErrorCategory.InvalidArgument, null);
                this.ThrowTerminatingError(er);
            }

            if (Port == 0)
            {
                _mSmtpClient = new SmtpClient(SmtpServer);
            }
            else
            {
                _mSmtpClient = new SmtpClient(SmtpServer, Port);
            }

            if (UseSsl)
            {
                _mSmtpClient.EnableSsl = true;
            }

            if (Credential != null)
            {
                _mSmtpClient.UseDefaultCredentials = false;
                _mSmtpClient.Credentials = Credential.GetNetworkCredential();
            }
            else if (!UseSsl)
            {
                _mSmtpClient.UseDefaultCredentials = true;
            }
        }

        
        protected override void ProcessRecord()
        {
            // Add the attachments
            if (Attachments != null)
            {
                string filepath = string.Empty;
                foreach (string attachFile in Attachments)
                {
                    try
                    {
                        filepath = PathUtils.ResolveFilePath(attachFile, this);
                    }
                    catch (ItemNotFoundException e)
                    {
                        // NOTE: This will throw
                        PathUtils.ReportFileOpenFailure(this, filepath, e);
                    }

                    Attachment mailAttachment = new(filepath);
                    _mMailMessage.Attachments.Add(mailAttachment);
                }
            }
        }

        
        protected override void EndProcessing()
        {
            try
            {
                // Send the mail message
                _mSmtpClient.Send(_mMailMessage);
            }
            catch (SmtpFailedRecipientsException ex)
            {
                ErrorRecord er = new(ex, "SmtpFailedRecipientsException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }
            catch (SmtpException ex)
            {
                if (ex.InnerException != null)
                {
                    ErrorRecord er = new(new SmtpException(ex.InnerException.Message), "SmtpException", ErrorCategory.InvalidOperation, _mSmtpClient);
                    WriteError(er);
                }
                else
                {
                    ErrorRecord er = new(ex, "SmtpException", ErrorCategory.InvalidOperation, _mSmtpClient);
                    WriteError(er);
                }
            }
            catch (InvalidOperationException ex)
            {
                ErrorRecord er = new(ex, "InvalidOperationException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }
            catch (System.Security.Authentication.AuthenticationException ex)
            {
                ErrorRecord er = new(ex, "AuthenticationException", ErrorCategory.InvalidOperation, _mSmtpClient);
                WriteError(er);
            }
            finally
            {
                _mSmtpClient.Dispose();

                // If we don't dispose the attachments, the sender can't modify or use the files sent.
                _mMailMessage.Attachments.Dispose();
            }
        }

        #endregion
    }
    #endregion
}
