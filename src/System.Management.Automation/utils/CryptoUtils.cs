// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Management.Automation.Remoting;
using System.Runtime.Serialization;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation.Internal
{
    
    internal static class PSCryptoNativeConverter
    {
        #region Constants

        
        public const uint CUR_BLOB_VERSION = 0x00000002;

        
        public const uint CALG_RSA_KEYX = 0x000000a4;

        
        public const uint CALG_AES_256 = 0x00000010;

        
        public const uint PUBLICKEYBLOB = 0x00000006;

        
        public const int PUBLICKEYBLOB_HEADER_LEN = 20;

        
        public const uint SIMPLEBLOB = 0x00000001;

        
        public const int SIMPLEBLOB_HEADER_LEN = 12;

        #endregion Constants

        #region Functions

        private static int ToInt32LE(byte[] bytes, int offset)
        {
            return (bytes[offset + 3] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 1] << 8) | bytes[offset];
        }

        private static uint ToUInt32LE(byte[] bytes, int offset)
        {
            return (uint)((bytes[offset + 3] << 24) | (bytes[offset + 2] << 16) | (bytes[offset + 1] << 8) | bytes[offset]);
        }

        private static byte[] GetBytesLE(int val)
        {
            return new[] {
                (byte)(val & 0xff),
                (byte)((val >> 8) & 0xff),
                (byte)((val >> 16) & 0xff),
                (byte)((val >> 24) & 0xff)
            };
        }

        private static byte[] CreateReverseByteArray(byte[] data)
        {
            byte[] reverseData = new byte[data.Length];
            Array.Copy(data, reverseData, data.Length);
            Array.Reverse(reverseData);
            return reverseData;
        }

        internal static RSA FromCapiPublicKeyBlob(byte[] blob)
        {
            return FromCapiPublicKeyBlob(blob, 0);
        }

        private static RSA FromCapiPublicKeyBlob(byte[] blob, int offset)
        {
            ArgumentNullException.ThrowIfNull(blob);

            if (offset > blob.Length)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidOffset);
            }

            var rsap = GetParametersFromCapiPublicKeyBlob(blob, offset);

            try
            {
                RSA rsa = RSA.Create();
                rsa.ImportParameters(rsap);
                return rsa;
            }
            catch (Exception ex)
            {
                throw new CryptographicException(SecuritySupportStrings.CannotImportPublicKey, ex);
            }
        }

        private static RSAParameters GetParametersFromCapiPublicKeyBlob(byte[] blob, int offset)
        {
            ArgumentNullException.ThrowIfNull(blob);

            if (offset > blob.Length)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidOffset);
            }

            if (blob.Length < PUBLICKEYBLOB_HEADER_LEN)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidPublicKey);
            }

            try
            {
                if ((blob[offset] != PUBLICKEYBLOB) ||            // PUBLICKEYBLOB (0x06)
                    (blob[offset + 1] != CUR_BLOB_VERSION) ||       // Version (0x02)
                    (blob[offset + 2] != 0x00) ||                   // Reserved (word)
                    (blob[offset + 3] != 0x00) ||
                    (ToUInt32LE(blob, offset + 8) != 0x31415352))   // DWORD magic = RSA1
                {
                    throw new CryptographicException(SecuritySupportStrings.InvalidPublicKey);
                }

                // DWORD bitlen
                int bitLen = ToInt32LE(blob, offset + 12);

                // DWORD public exponent
                RSAParameters rsap = new RSAParameters();
                rsap.Exponent = new byte[3];
                rsap.Exponent[0] = blob[offset + 18];
                rsap.Exponent[1] = blob[offset + 17];
                rsap.Exponent[2] = blob[offset + 16];

                int pos = offset + 20;
                int byteLen = (bitLen >> 3);
                rsap.Modulus = new byte[byteLen];
                Buffer.BlockCopy(blob, pos, rsap.Modulus, 0, byteLen);
                Array.Reverse(rsap.Modulus);

                return rsap;
            }
            catch (Exception ex)
            {
                throw new CryptographicException(SecuritySupportStrings.InvalidPublicKey, ex);
            }
        }

        internal static byte[] ToCapiPublicKeyBlob(RSA rsa)
        {
            ArgumentNullException.ThrowIfNull(rsa);

            RSAParameters p = rsa.ExportParameters(false);
            int keyLength = p.Modulus.Length;   // in bytes
            byte[] blob = new byte[PUBLICKEYBLOB_HEADER_LEN + keyLength];

            blob[0] = (byte)PUBLICKEYBLOB;      // Type - PUBLICKEYBLOB (0x06)
            blob[1] = (byte)CUR_BLOB_VERSION;   // Version - Always CUR_BLOB_VERSION (0x02)
            // [2], [3]                         // RESERVED - Always 0
            blob[5] = (byte)CALG_RSA_KEYX;      // ALGID - Always 00 a4 00 00 (for CALG_RSA_KEYX)
            blob[8] = 0x52;                     // Magic - RSA1 (ASCII in hex)
            blob[9] = 0x53;
            blob[10] = 0x41;
            blob[11] = 0x31;

            byte[] bitlen = GetBytesLE(keyLength << 3);
            blob[12] = bitlen[0];               // bitlen
            blob[13] = bitlen[1];
            blob[14] = bitlen[2];
            blob[15] = bitlen[3];

            // public exponent (DWORD)
            int pos = 16;
            int n = p.Exponent.Length;

            Dbg.Assert(n <= 4, "RSA exponent byte length cannot exceed allocated segment");

            while (n > 0)
            {
                blob[pos++] = p.Exponent[--n];
            }

            // modulus
            pos = 20;
            byte[] key = p.Modulus;
            Array.Reverse(key);
            Buffer.BlockCopy(key, 0, blob, pos, keyLength);

            return blob;
        }

        internal static byte[] FromCapiSimpleKeyBlob(byte[] blob)
        {
            ArgumentNullException.ThrowIfNull(blob);

            if (blob.Length < SIMPLEBLOB_HEADER_LEN)
            {
                throw new ArgumentException(SecuritySupportStrings.InvalidSessionKey);
            }

            // just ignore the header of the capi blob and go straight for the key
            return CreateReverseByteArray(blob.Skip(SIMPLEBLOB_HEADER_LEN).ToArray());
        }

        internal static byte[] ToCapiSimpleKeyBlob(byte[] encryptedKey)
        {
            ArgumentNullException.ThrowIfNull(encryptedKey);

            // formulate the PUBLICKEYSTRUCT
            byte[] blob = new byte[SIMPLEBLOB_HEADER_LEN + encryptedKey.Length];

            blob[0] = (byte)SIMPLEBLOB;         // Type - SIMPLEBLOB (0x01)
            blob[1] = (byte)CUR_BLOB_VERSION;   // Version - Always CUR_BLOB_VERSION (0x02)
            // [2], [3]                         // RESERVED - Always 0
            blob[4] = (byte)CALG_AES_256;       // AES-256 algo id (0x10)
            blob[5] = 0x66;                     // ??
            // [6], [7], [8]                    // 0x00
            blob[9] = (byte)CALG_RSA_KEYX;      // 0xa4
            // [10], [11]                       // 0x00

            // create a reversed copy and add the encrypted key
            byte[] reversedKey = CreateReverseByteArray(encryptedKey);
            Buffer.BlockCopy(reversedKey, 0, blob, SIMPLEBLOB_HEADER_LEN, reversedKey.Length);

            return blob;
        }

        #endregion Functions
    }

    
    [SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
    internal class PSCryptoException : Exception
    {
        #region Private Members

        private readonly uint _errorCode;

        #endregion Private Members

        #region Internal Properties

        
        internal uint ErrorCode
        {
            get
            {
                return _errorCode;
            }
        }

        #endregion Internal Properties

        #region Constructors

        
        public PSCryptoException()
            : this(0, new StringBuilder(string.Empty)) { }

        
        public PSCryptoException(uint errorCode, StringBuilder message)
            : base(message.ToString())
        {
            _errorCode = errorCode;
        }

        
        public PSCryptoException(string message)
            : this(message, null) { }

        
        public PSCryptoException(string message, Exception innerException)
            : base(message, innerException)
        {
            _errorCode = unchecked((uint)-1);
        }

        
        [Obsolete("Legacy serialization support is deprecated since .NET 8", DiagnosticId = "SYSLIB0051")]
        protected PSCryptoException(SerializationInfo info, StreamingContext context)
        {
            throw new NotSupportedException();
        }

        #endregion Constructors
    }

    
    internal sealed class PSRSACryptoServiceProvider : IDisposable
    {
        #region Private Members

        // handle session key encryption/decryption
        private RSA _rsa;

        // handle to the AES provider object (houses session key and iv)
        private readonly Aes _aes;

        // this flag indicates that this class has a key imported from the
        // remote end and so can be used for encryption
        private bool _canEncrypt;

        // bool indicating if session key was generated before
        private bool _sessionKeyGenerated = false;

        private static readonly object s_syncObject = new object();

        #endregion Private Members

        #region Constructors

        
        private PSRSACryptoServiceProvider(bool serverMode)
        {
            if (serverMode)
            {
                GenerateKeyPair();
            }

            _aes = Aes.Create();
            _aes.IV = new byte[16];  // iv should be 0
        }

        #endregion Constructors

        #region Internal Methods

        
        internal string GetPublicKeyAsBase64EncodedString()
        {
            Dbg.Assert(_rsa != null, "No public key available.");

            byte[] capiPublicKeyBlob = PSCryptoNativeConverter.ToCapiPublicKeyBlob(_rsa);

            return Convert.ToBase64String(capiPublicKeyBlob);
        }

        
        internal void GenerateSessionKey()
        {
            if (_sessionKeyGenerated)
                return;

            lock (s_syncObject)
            {
                if (!_sessionKeyGenerated)
                {
                    // Aes object gens key automatically on construction, so this is somewhat redundant,
                    // but at least the actionable key will not be in-memory until it's requested fwiw.
                    _aes.GenerateKey();
                    _sessionKeyGenerated = true;
                    _canEncrypt = true;  // we can encrypt and decrypt once session key is available
                }
            }
        }

        
        internal string SafeExportSessionKey()
        {
            Dbg.Assert(_rsa != null, "No public key available.");

            // generate one if not already done.
            GenerateSessionKey();

            // encrypt it
            // codeql[cs/cryptography/rsa-unapproved-encryption-padding-scheme] - PowerShell v7.4 and later versions have deprecated the key exchange in the remoting protocol. This code is kept only for backward compatibility reason.
            byte[] encryptedKey = _rsa.Encrypt(_aes.Key, RSAEncryptionPadding.Pkcs1);

            // convert the key to capi simpleblob format before exporting
            byte[] simpleKeyBlob = PSCryptoNativeConverter.ToCapiSimpleKeyBlob(encryptedKey);
            return Convert.ToBase64String(simpleKeyBlob);
        }

        
        internal void ImportPublicKeyFromBase64EncodedString(string publicKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(publicKey), "key cannot be null or empty");

            byte[] publicKeyBlob = Convert.FromBase64String(publicKey);
            _rsa = PSCryptoNativeConverter.FromCapiPublicKeyBlob(publicKeyBlob);
        }

        
        internal void ImportSessionKeyFromBase64EncodedString(string sessionKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(sessionKey), "key cannot be null or empty");

            byte[] sessionKeyBlob = Convert.FromBase64String(sessionKey);
            byte[] rsaEncryptedKey = PSCryptoNativeConverter.FromCapiSimpleKeyBlob(sessionKeyBlob);

            // codeql[cs/cryptography/rsa-unapproved-encryption-padding-scheme] - PowerShell v7.4 and later versions have deprecated the key exchange in the remoting protocol. This code is kept only for backward compatibility reason.
            _aes.Key = _rsa.Decrypt(rsaEncryptedKey, RSAEncryptionPadding.Pkcs1);

            // now we have imported the key and will be able to
            // encrypt using the session key
            _canEncrypt = true;
        }

        
        internal byte[] EncryptWithSessionKey(byte[] data)
        {
            Dbg.Assert(_canEncrypt, "Remote key has not been imported to encrypt");

            using (ICryptoTransform encryptor = _aes.CreateEncryptor())
            using (MemoryStream targetStream = new MemoryStream())
            using (MemoryStream sourceStream = new MemoryStream(data))
            {
                using (CryptoStream cryptoStream = new CryptoStream(targetStream, encryptor, CryptoStreamMode.Write))
                {
                    sourceStream.CopyTo(cryptoStream);
                }

                return targetStream.ToArray();
            }
        }

        
        internal byte[] DecryptWithSessionKey(byte[] data)
        {
            using (ICryptoTransform decryptor = _aes.CreateDecryptor())
            using (MemoryStream sourceStream = new MemoryStream(data))
            using (MemoryStream targetStream = new MemoryStream())
            {
                using (CryptoStream csDecrypt = new CryptoStream(sourceStream, decryptor, CryptoStreamMode.Read))
                {
                    csDecrypt.CopyTo(targetStream);
                }

                return targetStream.ToArray();
            }
        }

        
        internal void GenerateKeyPair()
        {
            _rsa = RSA.Create();
            _rsa.KeySize = 2048;
        }

        
        internal bool CanEncrypt
        {
            get
            {
                return _canEncrypt;
            }

            set
            {
                _canEncrypt = value;
            }
        }

        #endregion Internal Methods

        #region Internal Static Methods

        
        internal static PSRSACryptoServiceProvider GetRSACryptoServiceProviderForClient()
        {
            return new PSRSACryptoServiceProvider(false);
        }

        
        internal static PSRSACryptoServiceProvider GetRSACryptoServiceProviderForServer()
        {
            return new PSRSACryptoServiceProvider(true);
        }

        #endregion Internal Static Methods

        #region IDisposable

        
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rsa?.Dispose();
                _aes?.Dispose();
            }
        }

        #endregion IDisposable
    }

    
    public abstract class PSRemotingCryptoHelper : IDisposable
    {
        #region Protected Members

        
        internal PSRSACryptoServiceProvider _rsaCryptoProvider;

        
        protected ManualResetEvent _keyExchangeCompleted = new ManualResetEvent(false);

        
        protected object syncObject = new object();

        private bool _keyExchangeStarted = false;

        
        protected void RunKeyExchangeIfRequired()
        {
            Dbg.Assert(Session != null, "data structure handler not set");

            if (!_rsaCryptoProvider.CanEncrypt)
            {
                try
                {
                    lock (syncObject)
                    {
                        if (!_rsaCryptoProvider.CanEncrypt)
                        {
                            if (!_keyExchangeStarted)
                            {
                                _keyExchangeStarted = true;
                                _keyExchangeCompleted.Reset();
                                Session.StartKeyExchange();
                            }
                        }
                    }
                }
                finally
                {
                    // for whatever reason if StartKeyExchange()
                    // throws an exception it should reset the
                    // wait handle, so it should pass this wait
                    // if it doesn't do so, its a bug
                    _keyExchangeCompleted.WaitOne();
                }
            }
        }

        
        private static byte[] GetBytesFromSecureString(SecureString secureString)
        {
            return secureString is null
                ? null
                : Microsoft.PowerShell.SecureStringHelper.GetData(secureString);
        }

        
        private static SecureString GetSecureStringFromBytes(byte[] data)
        {
            Dbg.Assert(data is not null, "The passed-in data cannot be null.");

            try
            {
                return Microsoft.PowerShell.SecureStringHelper.New(data);
            }
            finally
            {
                // zero out the contents
                Array.Clear(data);
            }
        }

        
        protected string ConvertSecureStringToBase64String(SecureString secureString)
        {
            string dataAsString = null;
            byte[] data = GetBytesFromSecureString(secureString);

            if (data is not null)
            {
                try
                {
                    dataAsString = Convert.ToBase64String(data);
                }
                finally
                {
                    Array.Clear(data);
                }
            }

            return dataAsString;
        }

        
        protected SecureString ConvertBase64StringToSecureString(string base64String)
        {
            try
            {
                byte[] data = Convert.FromBase64String(base64String);
                return GetSecureStringFromBytes(data);
            }
            catch (FormatException)
            {
                // do nothing
                // this catch is to ensure that the exception doesn't
                // go unhandled leading to a crash
                throw new PSCryptoException();
            }
        }

        
        protected string EncryptSecureStringCore(SecureString secureString)
        {
            string encryptedDataAsString = null;

            if (_rsaCryptoProvider.CanEncrypt)
            {
                byte[] data = GetBytesFromSecureString(secureString);

                if (data is not null)
                {
                    try
                    {
                        byte[] encryptedData = _rsaCryptoProvider.EncryptWithSessionKey(data);
                        encryptedDataAsString = Convert.ToBase64String(encryptedData);
                    }
                    finally
                    {
                        Array.Clear(data);
                    }
                }
            }
            else
            {
                throw new PSCryptoException(SecuritySupportStrings.CannotEncryptSecureString);
            }

            return encryptedDataAsString;
        }

        
        protected SecureString DecryptSecureStringCore(string encryptedString)
        {
            // removing an earlier assert from here. It is
            // possible to encrypt and decrypt empty
            // secure strings
            SecureString secureString = null;

            // before you can decrypt a key exchange should have
            // happened successfully
            if (_rsaCryptoProvider.CanEncrypt)
            {
                try
                {
                    byte[] data = Convert.FromBase64String(encryptedString);
                    byte[] decryptedData = _rsaCryptoProvider.DecryptWithSessionKey(data);
                    secureString = GetSecureStringFromBytes(decryptedData);
                }
                catch (FormatException)
                {
                    // do nothing
                    // this catch is to ensure that the exception doesn't
                    // go unhandled leading to a crash
                    throw new PSCryptoException();
                }
            }
            else
            {
                Dbg.Assert(false, "Session key not available to decrypt");
            }

            return secureString;
        }

        #endregion Protected Members

        #region Internal Methods

        
        internal abstract string EncryptSecureString(SecureString secureString);

        
        internal abstract SecureString DecryptSecureString(string encryptedString);

        
        internal abstract RemoteSession Session { get; set; }

        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        
        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                _rsaCryptoProvider?.Dispose();
                _rsaCryptoProvider = null;

                _keyExchangeCompleted.Dispose();
            }
        }

        
        internal void CompleteKeyExchange()
        {
            _keyExchangeCompleted.Set();
        }

        #endregion Internal Methods
    }

    
    internal class PSRemotingCryptoHelperServer : PSRemotingCryptoHelper
    {
        #region Private Members

        
        private RemoteSession _session;

        #endregion Private Members

        #region Constructors

        
        internal PSRemotingCryptoHelperServer()
        {
            _rsaCryptoProvider = PSRSACryptoServiceProvider.GetRSACryptoServiceProviderForServer();
        }

        #endregion Constructors

        #region Internal Methods

        internal override string EncryptSecureString(SecureString secureString)
        {
            // session!=null check required for DRTs TestEncryptSecureString* entries in CryptoUtilsTest/UTUtils.dll
            bool initiateKeyExchange = true;

            if (Session is ServerRemoteSession session)
            {
                Version clientProtocolVersion = session.Context.ClientCapability.ProtocolVersion;
                if (clientProtocolVersion >= RemotingConstants.ProtocolVersion_2_4)
                {
                    // For client v2.4+, we no longer encrypt secure strings, but rely on the underlying secure transport to do the right thing.
                    return ConvertSecureStringToBase64String(secureString);
                }

                if (clientProtocolVersion >= RemotingConstants.ProtocolVersion_2_2)
                {
                    // For client v2.2+, server will never initiate key exchange.
                    // For server, just the session key is required to encrypt/decrypt anything
                    initiateKeyExchange = false;
                    _rsaCryptoProvider.GenerateSessionKey();
                }
            }

            if (initiateKeyExchange)
            {
                // older clients.
                RunKeyExchangeIfRequired();
            }

            return EncryptSecureStringCore(secureString);
        }

        internal override SecureString DecryptSecureString(string encryptedString)
        {
            if (Session is ServerRemoteSession session && session.Context.ClientCapability.ProtocolVersion >= RemotingConstants.ProtocolVersion_2_4)
            {
                // For client v2.4+, we no longer encrypt secure strings, but rely on the underlying secure transport to do the right thing.
                return ConvertBase64StringToSecureString(encryptedString);
            }

            RunKeyExchangeIfRequired();

            return DecryptSecureStringCore(encryptedString);
        }

        
        internal bool ImportRemotePublicKey(string publicKeyAsString)
        {
            Dbg.Assert(!string.IsNullOrEmpty(publicKeyAsString), "public key passed in cannot be null");

            // generate the crypto provider to use for encryption
            // _rsaCryptoProvider = GenerateCryptoServiceProvider(false);

            try
            {
                _rsaCryptoProvider.ImportPublicKeyFromBase64EncodedString(publicKeyAsString);
            }
            catch (PSCryptoException)
            {
                return false;
            }

            return true;
        }

        
        internal override RemoteSession Session
        {
            get
            {
                return _session;
            }

            set
            {
                _session = value;
            }
        }

        
        internal bool ExportEncryptedSessionKey(out string encryptedSessionKey)
        {
            try
            {
                encryptedSessionKey = _rsaCryptoProvider.SafeExportSessionKey();
            }
            catch (PSCryptoException)
            {
                encryptedSessionKey = string.Empty;
                return false;
            }

            return true;
        }

        
        internal static PSRemotingCryptoHelperServer GetTestRemotingCryptHelperServer()
        {
            PSRemotingCryptoHelperServer helper = new PSRemotingCryptoHelperServer();
            helper.Session = new TestHelperSession();

            return helper;
        }

        #endregion Internal Methods
    }

    
    internal class PSRemotingCryptoHelperClient : PSRemotingCryptoHelper
    {
        #region Private Members

        #endregion Private Members

        #region Constructors

        
        internal PSRemotingCryptoHelperClient()
        {
            _rsaCryptoProvider = PSRSACryptoServiceProvider.GetRSACryptoServiceProviderForClient();
        }

        #endregion Constructors

        #region Protected Methods

        #endregion Protected Methods

        #region Internal Methods

        internal override string EncryptSecureString(SecureString secureString)
        {
            if (Session is ClientRemoteSession session && session.ServerProtocolVersion >= RemotingConstants.ProtocolVersion_2_4)
            {
                // For server v2.4+, we no longer encrypt secure strings, but rely on the underlying secure transport to do the right thing.
                return ConvertSecureStringToBase64String(secureString);
            }

            RunKeyExchangeIfRequired();

            return EncryptSecureStringCore(secureString);
        }

        internal override SecureString DecryptSecureString(string encryptedString)
        {
            if (Session is ClientRemoteSession session && session.ServerProtocolVersion >= RemotingConstants.ProtocolVersion_2_4)
            {
                // For server v2.4+, we no longer encrypt secure strings, but rely on the underlying secure transport to do the right thing.
                return ConvertBase64StringToSecureString(encryptedString);
            }

            RunKeyExchangeIfRequired();

            return DecryptSecureStringCore(encryptedString);
        }

        
        internal bool ExportLocalPublicKey(out string publicKeyAsString)
        {
            // generate keys - the method already takes of creating
            // only when its not already created

            try
            {
                _rsaCryptoProvider.GenerateKeyPair();
            }
            catch (PSCryptoException)
            {
                throw;

                // the caller has to ensure that they
                // complete the key exchange process
            }

            try
            {
                publicKeyAsString = _rsaCryptoProvider.GetPublicKeyAsBase64EncodedString();
            }
            catch (PSCryptoException)
            {
                publicKeyAsString = string.Empty;
                return false;
            }

            return true;
        }

        
        internal bool ImportEncryptedSessionKey(string encryptedSessionKey)
        {
            Dbg.Assert(!string.IsNullOrEmpty(encryptedSessionKey), "encrypted session key passed in cannot be null");

            try
            {
                _rsaCryptoProvider.ImportSessionKeyFromBase64EncodedString(encryptedSessionKey);
            }
            catch (PSCryptoException)
            {
                return false;
            }

            return true;
        }

        
        internal override RemoteSession Session { get; set; }

        
        internal static PSRemotingCryptoHelperClient GetTestRemotingCryptHelperClient()
        {
            PSRemotingCryptoHelperClient helper = new PSRemotingCryptoHelperClient();
            helper.Session = new TestHelperSession();

            return helper;
        }

        #endregion Internal Methods
    }

    #region TestHelpers

    internal class TestHelperSession : RemoteSession
    {
        internal override void StartKeyExchange()
        {
            // intentionally left blank
        }

        internal override RemotingDestination MySelf
        {
            get
            {
                return RemotingDestination.InvalidDestination;
            }
        }

        internal override void CompleteKeyExchange()
        {
            // intentionally left blank
        }
    }
    #endregion TestHelpers
}
