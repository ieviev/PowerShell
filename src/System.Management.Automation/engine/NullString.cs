// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation.Language
{
    
    public class NullString
    {
        #region private_members

        // Private member for instance.

        #endregion private_members

        #region public_property

        
        public override string ToString()
        {
            return null;
        }

        
        public static NullString Value { get; } = new NullString();

        #endregion public_property

        #region private Constructor

        
        private NullString()
        {
        }

        #endregion private Constructor
    }
}
