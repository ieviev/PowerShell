// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace Microsoft.PowerShell.Commands
{
    
    public class WebResponseObject
    {
        #region Properties

        
        public HttpResponseMessage BaseResponse { get; set; }

        
        public byte[]? Content { get; protected set; }

        
        public Dictionary<string, IEnumerable<string>> Headers => _headers ??= WebResponseHelper.GetHeadersDictionary(BaseResponse);

        private Dictionary<string, IEnumerable<string>>? _headers;

        
        public string? RawContent { get; protected set; }

        
        public long RawContentLength => RawContentStream is null ? -1 : RawContentStream.Length;

        
        public MemoryStream RawContentStream { get; protected set; }

        
        public Dictionary<string, string>? RelationLink { get; internal set; }

        
        public int StatusCode => WebResponseHelper.GetStatusCode(BaseResponse);

        
        public string StatusDescription => WebResponseHelper.GetStatusDescription(BaseResponse);

        
        public string? OutFile { get; internal set; }

        #endregion Properties

        #region Protected Fields

        
        protected TimeSpan perReadTimeout;

        #endregion Protected Fields

        #region Constructors

        
        public WebResponseObject(HttpResponseMessage response, TimeSpan perReadTimeout, CancellationToken cancellationToken) : this(response, null, perReadTimeout, cancellationToken) { }

        
        public WebResponseObject(HttpResponseMessage response, Stream? contentStream, TimeSpan perReadTimeout, CancellationToken cancellationToken)
        {
            this.perReadTimeout = perReadTimeout;
            SetResponse(response, contentStream, cancellationToken);
            InitializeContent();
            InitializeRawContent(response);
        }

        #endregion Constructors

        #region Methods

        
        private void InitializeContent()
        {
            Content = RawContentStream.ToArray();
        }

        private void InitializeRawContent(HttpResponseMessage baseResponse)
        {
            StringBuilder raw = ContentHelper.GetRawContentHeader(baseResponse);

            // Use ASCII encoding for the RawContent visual view of the content.
            if (Content?.Length > 0)
            {
                raw.Append(ToString());
            }

            RawContent = raw.ToString();
        }

        private static bool IsPrintable(char c) => char.IsLetterOrDigit(c)
                                                || char.IsPunctuation(c)
                                                || char.IsSeparator(c)
                                                || char.IsSymbol(c)
                                                || char.IsWhiteSpace(c);

        [MemberNotNull(nameof(RawContentStream))]
        [MemberNotNull(nameof(BaseResponse))]
        private void SetResponse(HttpResponseMessage response, Stream? contentStream, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(response);

            BaseResponse = response;

            if (contentStream is MemoryStream ms)
            {
                RawContentStream = ms;
            }
            else
            {
                Stream st = contentStream ?? StreamHelper.GetResponseStream(response, cancellationToken);

                long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
                if (contentLength <= 0)
                {
                    contentLength = StreamHelper.DefaultReadBuffer;
                }

                int initialCapacity = (int)Math.Min(contentLength, StreamHelper.DefaultReadBuffer);
                RawContentStream = new WebResponseContentMemoryStream(st, initialCapacity, cmdlet: null, response.Content.Headers.ContentLength.GetValueOrDefault(), perReadTimeout, cancellationToken);
            }

            // Set the position of the content stream to the beginning
            RawContentStream.Position = 0;
        }

        
        public sealed override string ToString()
        {
            if (Content is null)
            {
                return string.Empty;
            }

            char[] stringContent = Encoding.ASCII.GetChars(Content);
            for (int counter = 0; counter < stringContent.Length; counter++)
            {
                if (!IsPrintable(stringContent[counter]))
                {
                    stringContent[counter] = '.';
                }
            }

            return new string(stringContent);
        }

        #endregion Methods
    }
}
