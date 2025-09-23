// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;

namespace System.Management.Automation.Subsystem
{
    
    /// <remarks>
    /// This enum uses power of 2 as the values for the enum elements, so as to make sure
    /// the bitwise 'or' operation of the elements always results in an invalid value.
    /// </remarks>
    public enum SubsystemKind : uint
    {
        
        CommandPredictor = 1,

        
        CrossPlatformDsc = 2,

        
        FeedbackProvider = 4,
    }

    
    /// <remarks>
    /// A user should not directly implement <see cref="ISubsystem"/>, but instead should derive from one of the concrete subsystem interfaces or abstract classes.
    /// The instance of a type that only implements 'ISubsystem' cannot be registered to the <see cref="SubsystemManager"/>.
    /// </remarks>
    public interface ISubsystem
    {
        
        Guid Id { get; }

        
        string Name { get; }

        
        string Description { get; }

        
        Dictionary<string, string>? FunctionsToDefine { get; }
    }
}
