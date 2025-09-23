// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.PowerShell
{
    
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal readonly struct PropertyKey : IEquatable<PropertyKey>
    {
        #region Public Properties
        
        public Guid FormatId { get; }

        
        public int PropertyId { get; }

        #endregion

        #region Public Construction

        
        internal PropertyKey(Guid formatId, int propertyId)
        {
            this.FormatId = formatId;
            this.PropertyId = propertyId;
        }

        #endregion

        #region IEquatable<PropertyKey> Members

        
        public bool Equals(PropertyKey other)
        {
            return other.Equals((object)this);
        }

        #endregion

        #region equality and hashing

        
        public override int GetHashCode()
        {
            return FormatId.GetHashCode() ^ PropertyId;
        }

        
        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is not PropertyKey)
                return false;

            PropertyKey other = (PropertyKey)obj;
            return other.FormatId.Equals(FormatId) && (other.PropertyId == PropertyId);
        }

        
        public static bool operator ==(PropertyKey propKey1, PropertyKey propKey2)
        {
            return propKey1.Equals(propKey2);
        }

        
        public static bool operator !=(PropertyKey propKey1, PropertyKey propKey2)
        {
            return !propKey1.Equals(propKey2);
        }

        
        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "PropertyKeyFormatString",
                FormatId.ToString("B"), PropertyId);
        }

        #endregion
    }
}
