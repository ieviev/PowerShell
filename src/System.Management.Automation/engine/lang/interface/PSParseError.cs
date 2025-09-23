// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.



using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    
    public sealed class PSParseError
    {
        internal PSParseError(RuntimeException rte)
        {
            Dbg.Assert(rte != null, "exception argument should not be null");
            Dbg.Assert(rte.ErrorToken != null, "token for exception should not be null");

            Message = rte.Message;
            Token = new PSToken(rte.ErrorToken);
        }

        internal PSParseError(Language.ParseError error)
        {
            Message = error.Message;
            Token = new PSToken(error.Extent);
        }

        
        public PSToken Token { get; }

        
        public string Message { get; }
    }
}
