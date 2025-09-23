// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

[assembly: CLSCompliant(true)]

namespace Microsoft.WSMan.Management
{
    
    public sealed class SessionOption
    {
        
        public bool SkipCACheck { get; set; }

        
        public bool SkipCNCheck { get; set; }

        
        public bool SkipRevocationCheck { get; set; }

        
        public bool UseEncryption { get; set; } = true;

        
        public bool UseUtf16 { get; set; }

        
        public ProxyAuthentication ProxyAuthentication { get; set; }

        
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "SPN")]
        public int SPNPort { get; set; }

        
        public int OperationTimeout { get; set; }

        
        public NetworkCredential ProxyCredential { get; set; }

        
        public ProxyAccessType ProxyAccessType { get; set; }
    }

    
    public enum ProxyAccessType
    {
        
        ProxyIEConfig = 0,
        
        ProxyWinHttpConfig = 1,
        
        ProxyAutoDetect = 2,
        
        ProxyNoProxyServer = 3
    }

    
    [SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags")]
    [SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue")]
    public enum ProxyAuthentication
    {
        
        Negotiate = 1,
        
        Basic = 2,
        
        Digest = 4
    }
}
