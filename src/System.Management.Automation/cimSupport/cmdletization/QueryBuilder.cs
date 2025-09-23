// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;

namespace Microsoft.PowerShell.Cmdletization
{
    
    public enum BehaviorOnNoMatch
    {
        
        Default = 0,

        
        ReportErrors,

        
        SilentlyContinue,
    }

    
    public abstract class QueryBuilder
    {
        
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="allowedPropertyValues">Property values to accept in the query.</param>
        /// <param name="wildcardsEnabled">
        /// <see langword="true"/> if <paramref name="allowedPropertyValues"/> should be treated as a <see cref="System.String"/> containing a wildcard pattern;
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByProperty(string propertyName, IEnumerable allowedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="excludedPropertyValues">Property values to reject in the query.</param>
        /// <param name="wildcardsEnabled">
        /// <see langword="true"/> if <paramref name="excludedPropertyValues"/> should be treated as a <see cref="System.String"/> containing a wildcard pattern;
        /// <see langword="false"/> otherwise.
        /// </param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void ExcludeByProperty(string propertyName, IEnumerable excludedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="minPropertyValue">Minimum property value.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByMinPropertyValue(string propertyName, object minPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="propertyName">Property name to query on.</param>
        /// <param name="maxPropertyValue">Maximum property value.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="associatedInstance">Object that query results have to be associated with.</param>
        /// <param name="associationName">Name of the association.</param>
        /// <param name="resultRole">Name of the role that <paramref name="associatedInstance"/> has in the association.</param>
        /// <param name="sourceRole">Name of the role that query results have in the association.</param>
        /// <param name="behaviorOnNoMatch">
        /// Describes how to handle filters that didn't match any objects
        /// </param>
        public virtual void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        /// <param name="optionName"></param>
        /// <param name="optionValue"></param>
        public virtual void AddQueryOption(string optionName, object optionValue)
        {
            throw new NotImplementedException();
        }
    }
}
