// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691
#pragma warning disable 56506

using Microsoft.PowerShell.Commands;

namespace System.Management.Automation.Runspaces
{
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RunspaceAttribute : ArgumentTransformationAttribute
    {
        
        public override object Transform(EngineIntrinsics engineIntrinsics, object inputData)
        {
            if (engineIntrinsics?.Host?.UI == null)
            {
                throw PSTraceSource.NewArgumentNullException("engineIntrinsics");
            }

            if (inputData == null)
            {
                return null;
            }

            // Try to coerce the input as a runspace
            Runspace runspace = LanguagePrimitives.FromObjectAs<Runspace>(inputData);
            if (runspace != null)
            {
                return runspace;
            }

            // Try to coerce the runspace if the user provided a string, int, or guid
            switch (inputData)
            {
                case string name:
                    var runspacesByName = GetRunspaceUtils.GetRunspacesByName(new[] { name });
                    if (runspacesByName.Count == 1)
                    {
                        return runspacesByName[0];
                    }

                    break;

                case int id:
                    var runspacesById = GetRunspaceUtils.GetRunspacesById(new[] { id });
                    if (runspacesById.Count == 1)
                    {
                        return runspacesById[0];
                    }

                    break;

                case Guid guid:
                    var runspacesByGuid = GetRunspaceUtils.GetRunspacesByInstanceId(new[] { guid });
                    if (runspacesByGuid.Count == 1)
                    {
                        return runspacesByGuid[0];
                    }

                    break;

                default:
                    // Non-convertible type
                    break;
            }

            // If we couldn't get a single runspace, return the inputData
            return inputData;
        }

        
        public override bool TransformNullOptionalParameters { get { return false; } }
    }
}

#pragma warning restore 56506
