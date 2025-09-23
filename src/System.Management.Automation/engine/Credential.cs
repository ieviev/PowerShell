// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Security;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Microsoft.PowerShell;

namespace System.Management.Automation
{
    
    [Flags]
    public enum PSCredentialTypes
    {
        
        Generic = 1,

        
        Domain = 2,

        
        Default = Generic | Domain
    }

    
    [Flags]
    public enum PSCredentialUIOptions
    {
        
        Default = ValidateUserNameSyntax,

        
        None = 0,

        
        ValidateUserNameSyntax,

        
        AlwaysPrompt,

        
        ReadOnlyUserName
    }

    
    /// <param name="context">The streaming context, which contains the serialization context.</param>
    /// <param name="key">Symmetric encryption key.</param>
    /// <param name="iv">Symmetric encryption initialization vector.</param>
    /// <returns></returns>
    public delegate bool GetSymmetricEncryptionKey(StreamingContext context, out byte[] key, out byte[] iv);

    
    [Serializable]
    public sealed class PSCredential : ISerializable
    {
        
        public static GetSymmetricEncryptionKey GetSymmetricEncryptionKeyDelegate
        {
            get
            {
                return s_delegate;
            }

            set
            {
                s_delegate = value;
            }
        }

        private static GetSymmetricEncryptionKey s_delegate = null;

        
        /// <param name="info"></param>
        /// <param name="context"></param>
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                return;

            // serialize the secure string
            string safePassword = string.Empty;

            if (_password != null && _password.Length > 0)
            {
                byte[] key;
                byte[] iv;
                if (s_delegate != null && s_delegate(context, out key, out iv))
                {
                    safePassword = SecureStringHelper.Encrypt(_password, key, iv).EncryptedData;
                }
                else
                {
                    try
                    {
                        safePassword = SecureStringHelper.Protect(_password);
                    }
                    catch (CryptographicException cryptographicException)
                    {
                        throw PSTraceSource.NewInvalidOperationException(cryptographicException, Credential.CredentialDisallowed);
                    }
                }
            }

            info.AddValue("UserName", _userName);
            info.AddValue("Password", safePassword);
        }

        
        /// <param name="info"></param>
        /// <param name="context"></param>
        private PSCredential(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                return;

            _userName = (string)info.GetValue("UserName", typeof(string));

            // deserialize to secure string
            string safePassword = (string)info.GetValue("Password", typeof(string));
            if (safePassword == string.Empty)
            {
                _password = new SecureString();
            }
            else
            {
                byte[] key;
                byte[] iv;
                if (s_delegate != null && s_delegate(context, out key, out iv))
                {
                    _password = SecureStringHelper.Decrypt(safePassword, key, iv);
                }
                else
                {
                    _password = SecureStringHelper.Unprotect(safePassword);
                }
            }
        }

        private readonly string _userName;
        private readonly SecureString _password;

        
        public string UserName
        {
            get { return _userName; }
        }

        
        public SecureString Password
        {
            get { return _password; }
        }

        
        /// <param name="userName">User's name.</param>
        /// <param name="password">User's password.</param>
        public PSCredential(string userName, SecureString password)
        {
            Utils.CheckArgForNullOrEmpty(userName, "userName");
            Utils.CheckArgForNull(password, "password");

            _userName = userName;
            _password = password;
        }

        
        /// <param name="pso"></param>
        public PSCredential(PSObject pso)
        {
            if (pso == null)
                throw PSTraceSource.NewArgumentNullException(nameof(pso));

            if (pso.Properties["UserName"] != null)
            {
                _userName = (string)pso.Properties["UserName"].Value;

                if (pso.Properties["Password"] != null)
                    _password = (SecureString)pso.Properties["Password"].Value;
            }
        }

        
        private PSCredential()
        {
        }

        private NetworkCredential _netCred;

        
        /// <returns>
        ///     null if the current object has not been initialized.
        ///     null if the current credentials are incompatible with
        ///       a NetworkCredential -- such as smart card credentials.
        ///     the appropriate network credential for this PSCredential otherwise.
        /// </returns>
        public NetworkCredential GetNetworkCredential()
        {
            if (_netCred == null)
            {
                string user = null;
                string domain = null;

                if (IsValidUserName(_userName, out user, out domain))
                {
                    _netCred = new NetworkCredential(user, _password, domain);
                }
            }

            return _netCred;
        }

        
        /// <param name="credential">PSCredential to convert.</param>
        /// <returns>
        ///     null if the current object has not been initialized.
        ///     null if the current credentials are incompatible with
        ///       a NetworkCredential -- such as smart card credentials.
        ///     the appropriate network credential for this PSCredential otherwise.
        /// </returns>
        public static explicit operator NetworkCredential(PSCredential credential)
        {
#pragma warning disable 56506

            if (credential == null)
            {
                throw PSTraceSource.NewArgumentNullException("credential");
            }

            return credential.GetNetworkCredential();

#pragma warning restore 56506
        }

        
        public static PSCredential Empty
        {
            get
            {
                return s_empty;
            }
        }

        private static readonly PSCredential s_empty = new PSCredential();

        
        private static bool IsValidUserName(string input,
                                            out string user,
                                            out string domain)
        {
            if (string.IsNullOrEmpty(input))
            {
                user = domain = null;
                return false;
            }

            SplitUserDomain(input, out user, out domain);

            if ((user == null) ||
                (domain == null) ||
                (user.Length == 0))
            {
                // UserName is the public property of Credential object. Use this as
                // parameter name in error
                // See bug NTRAID#Windows OS Bugs-1106386-2005/03/25-hiteshr
                throw PSTraceSource.NewArgumentException("UserName", Credential.InvalidUserNameFormat);
            }

            return true;
        }

        
        private static void SplitUserDomain(string input,
                                            out string user,
                                            out string domain)
        {
            int i = 0;
            user = null;
            domain = null;

            if ((i = input.IndexOf('\\')) >= 0)
            {
                user = input.Substring(i + 1);
                domain = input.Substring(0, i);
                return;
            }

            // In V1 and V2, we had a bug where email addresses (i.e. foo@bar.com)
            // were being split into Username=Foo, Domain=bar.com.
            //
            // This was breaking apps (i.e.: Exchange), so we need to make
            // Username = foo@bar.com if the domain has a dot in it (since
            // domains can't have dots).
            //
            // HOWEVER, there was a workaround for this bug in v1 and v2, where the
            // cred could be entered as "foo@bar.com@bar.com" - making:
            // Username = foo@bar.com, Domain = bar.com
            //
            // We need to keep the behaviour in this case.

            i = input.LastIndexOf('@');

            if (
                (i >= 0) &&
                (
                    (input.LastIndexOf('.') < i) ||
                    (input.IndexOf('@') != i)
                )
            )
            {
                domain = input.Substring(i + 1);
                user = input.Substring(0, i);
            }
            else
            {
                user = input;
                domain = string.Empty;
            }
        }
    }
}
