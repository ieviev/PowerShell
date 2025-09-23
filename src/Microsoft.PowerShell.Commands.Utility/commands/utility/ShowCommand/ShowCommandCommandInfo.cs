// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands.ShowCommandExtension
{
    
    public class ShowCommandCommandInfo
    {
        
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandCommandInfo(CommandInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Name;
            this.ModuleName = other.ModuleName;
            this.CommandType = other.CommandType;
            this.Definition = other.Definition;

            // In a runspace with restricted security settings we catch
            // PSSecurityException when accessing ParameterSets because
            // ExternalScript commands may be evaluated.
            try
            {
                this.ParameterSets =
                    other.ParameterSets
                        .Select(static x => new ShowCommandParameterSetInfo(x))
                        .ToList()
                        .AsReadOnly();
            }
            catch (PSSecurityException)
            {
                // Since we can't access the parameter sets of this command,
                // populate the ParameterSets property with an empty list
                // so that consumers don't trip on a null value.
                this.ParameterSets = new List<ShowCommandParameterSetInfo>().AsReadOnly();
            }
            catch (ParseException)
            {
                // Could not parse the given command so don't continue initializing it
                this.ParameterSets = new List<ShowCommandParameterSetInfo>().AsReadOnly();
            }

            if (other.Module != null)
            {
                this.Module = new ShowCommandModuleInfo(other.Module);
            }
        }

        
        /// <param name="other">
        /// The object to wrap.
        /// </param>
        public ShowCommandCommandInfo(PSObject other)
        {
            ArgumentNullException.ThrowIfNull(other);

            this.Name = other.Members["Name"].Value as string;
            this.ModuleName = other.Members["ModuleName"].Value as string;
            this.Definition = other.Members["Definition"].Value as string;
            this.ParameterSets = other.Members["ParameterSets"].Value as ICollection<ShowCommandParameterSetInfo>;
            if (this.ParameterSets != null)
            {
                // Simple case - the objects are still live because they came from in-proc. Just cast them back
                this.CommandType = (CommandTypes)(other.Members["CommandType"].Value);
                this.Module = other.Members["Module"].Value as ShowCommandModuleInfo;
            }
            else
            {
                // Objects came in their deserialized form - recreate the object graph
                this.CommandType = (CommandTypes)((other.Members["CommandType"].Value as PSObject).BaseObject);

                var parameterSets = (other.Members["ParameterSets"].Value as PSObject).BaseObject as System.Collections.ArrayList;
                this.ParameterSets = GetObjectEnumerable(parameterSets).Cast<PSObject>().Select(static x => new ShowCommandParameterSetInfo(x)).ToList().AsReadOnly();

                if (other.Members["Module"]?.Value is PSObject)
                {
                    this.Module = new ShowCommandModuleInfo(other.Members["Module"].Value as PSObject);
                }
            }
        }

        
        /// <param name="enumerable">
        /// The object to enumerate.
        /// </param>
        internal static IEnumerable<object> GetObjectEnumerable(System.Collections.IEnumerable enumerable)
        {
            foreach (object obj in enumerable)
            {
                yield return obj;
            }
        }

        
        public string Name { get; }

        
        public string ModuleName { get; }

        
        public ShowCommandModuleInfo Module { get; }

        
        public CommandTypes CommandType { get; }

        
        public string Definition { get; }

        
        public ICollection<ShowCommandParameterSetInfo> ParameterSets { get; }
    }
}
