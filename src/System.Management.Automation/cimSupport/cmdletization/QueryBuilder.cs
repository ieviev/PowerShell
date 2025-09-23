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
        
        public virtual void FilterByProperty(string propertyName, IEnumerable allowedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        public virtual void ExcludeByProperty(string propertyName, IEnumerable excludedPropertyValues, bool wildcardsEnabled, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        public virtual void FilterByMinPropertyValue(string propertyName, object minPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        public virtual void FilterByMaxPropertyValue(string propertyName, object maxPropertyValue, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        public virtual void FilterByAssociatedInstance(object associatedInstance, string associationName, string sourceRole, string resultRole, BehaviorOnNoMatch behaviorOnNoMatch)
        {
            throw new NotImplementedException();
        }

        
        public virtual void AddQueryOption(string optionName, object optionValue)
        {
            throw new NotImplementedException();
        }
    }
}
