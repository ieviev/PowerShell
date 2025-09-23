// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    
    public class WebRequestSession : IDisposable
    {
        #region Fields

        private HttpClient? _client;
        private CookieContainer _cookies;
        private bool _useDefaultCredentials;
        private ICredentials? _credentials;
        private X509CertificateCollection? _certificates;
        private IWebProxy? _proxy;
        private int _maximumRedirection;
        private WebSslProtocol _sslProtocol;
        private bool _allowAutoRedirect;
        private bool _skipCertificateCheck;
        private bool _noProxy;
        private bool _disposed;
        private TimeSpan _connectionTimeout;
        private UnixDomainSocketEndPoint? _unixSocket;

        
        private bool _disposedClient;

        #endregion Fields

        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public Dictionary<string, string> Headers { get; set; }

        
        internal Dictionary<string, string> ContentHeaders { get; set; }

        
        public CookieContainer Cookies { get => _cookies; set => SetClassVar(ref _cookies, value); }

        #region Credentials

        
        public bool UseDefaultCredentials { get => _useDefaultCredentials; set => SetStructVar(ref _useDefaultCredentials, value); }

        
        public ICredentials? Credentials { get => _credentials; set => SetClassVar(ref _credentials, value); }

        
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
        public X509CertificateCollection? Certificates { get => _certificates; set => SetClassVar(ref _certificates, value); }

        #endregion Credentials

        
        public string UserAgent { get; set; }

        
        public IWebProxy? Proxy
        {
            get => _proxy;
            set
            {
                SetClassVar(ref _proxy, value);
                if (_proxy is not null)
                {
                    NoProxy = false;
                }
            }
        }

        
        public int MaximumRedirection { get => _maximumRedirection; set => SetStructVar(ref _maximumRedirection, value); }

        
        public int MaximumRetryCount { get; set; }

        
        public int RetryIntervalInSeconds { get; set; }

        
        public WebRequestSession()
        {
            // Build the headers collection
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ContentHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Build the cookie jar
            _cookies = new CookieContainer();

            // Initialize the credential and certificate caches
            _useDefaultCredentials = false;
            _credentials = null;
            _certificates = null;

            // Setup the default UserAgent
            UserAgent = PSUserAgent.UserAgent;

            _proxy = null;
            _maximumRedirection = -1;
            _allowAutoRedirect = true;
        }

        internal WebSslProtocol SslProtocol { set => SetStructVar(ref _sslProtocol, value); }

        internal bool SkipCertificateCheck { set => SetStructVar(ref _skipCertificateCheck, value); }

        internal TimeSpan ConnectionTimeout { set => SetStructVar(ref _connectionTimeout, value); }

        internal UnixDomainSocketEndPoint UnixSocket { set => SetClassVar(ref _unixSocket, value); }

        internal bool NoProxy
        {
            set
            {
                SetStructVar(ref _noProxy, value);
                if (_noProxy)
                {
                    Proxy = null;
                }
            }
        }

        
        internal void AddCertificate(X509Certificate certificate)
        {
            Certificates ??= new X509CertificateCollection();
            if (!Certificates.Contains(certificate))
            {
                ResetClient();
                Certificates.Add(certificate);
            }
        }

        
        internal HttpClient GetHttpClient(bool suppressHttpClientRedirects, out bool clientWasReset)
        {
            // Do not auto redirect if the caller does not want it, or maximum redirections is 0
            SetStructVar(ref _allowAutoRedirect, !(suppressHttpClientRedirects || MaximumRedirection == 0));

            clientWasReset = _disposedClient;

            if (_client is null)
            {
                _client = CreateHttpClient();
                _disposedClient = false;
            }

            return _client;
        }

        private HttpClient CreateHttpClient()
        {
            SocketsHttpHandler handler = new();

            if (_unixSocket is not null)
            {
                handler.ConnectCallback = async (context, token) =>
                {
                    Socket socket = new(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                    await socket.ConnectAsync(_unixSocket).ConfigureAwait(false);

                    return new NetworkStream(socket, ownsSocket: false);
                };
            }

            handler.CookieContainer = Cookies;
            handler.AutomaticDecompression = DecompressionMethods.All;

            if (Credentials is not null)
            {
                handler.Credentials = Credentials;
            }
            else if (UseDefaultCredentials)
            {
                handler.Credentials = CredentialCache.DefaultCredentials;
            }

            if (_noProxy)
            {
                handler.UseProxy = false;
            }
            else if (Proxy is not null)
            {
                handler.Proxy = Proxy;
            }

            if (Certificates is not null)
            {
                handler.SslOptions.ClientCertificates = new X509CertificateCollection(Certificates);
            }

            if (_skipCertificateCheck)
            {
                handler.SslOptions.RemoteCertificateValidationCallback = delegate { return true; };
            }

            handler.AllowAutoRedirect = _allowAutoRedirect;
            if (_allowAutoRedirect && MaximumRedirection > 0)
            {
                handler.MaxAutomaticRedirections = MaximumRedirection;
            }

            handler.SslOptions.EnabledSslProtocols = (SslProtocols)_sslProtocol;

            // Check timeout setting (in seconds)
            return new HttpClient(handler)
            {
                Timeout = _connectionTimeout
            };
        }

        private void SetClassVar<T>(ref T oldValue, T newValue) where T : class?
        {
            if (oldValue != newValue)
            {
                ResetClient();
                oldValue = newValue;
            }
        }

        private void SetStructVar<T>(ref T oldValue, T newValue) where T : struct
        {
            if (!oldValue.Equals(newValue))
            {
                ResetClient();
                oldValue = newValue;
            }
        }

        private void ResetClient()
        {
            if (_client is not null)
            {
                _disposedClient = true;
                _client.Dispose();
                _client = null;
            }
        }

        
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _client?.Dispose();
                }

                _disposed = true;
            }
        }

        
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
