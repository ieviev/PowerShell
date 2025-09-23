// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Management.Automation;
using System.Management.Automation.Internal;

namespace Microsoft.PowerShell.Commands
{
    
    public class MemberDefinition
    {
        
        public override string ToString()
        {
            return Definition;
        }
        
        public MemberDefinition(string typeName, string name, PSMemberTypes memberType, string definition)
        {
            Name = name;
            Definition = definition;
            MemberType = memberType;
            TypeName = typeName;
        }

        
        public string TypeName { get; }

        
        public string Name { get; }

        
        public PSMemberTypes MemberType { get; }

        
        public string Definition { get; }
    }

    
    [Cmdlet(VerbsCommon.Get, "Member", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096704", RemotingCapability = RemotingCapability.None)]
    [OutputType(typeof(MemberDefinition))]
    public class GetMemberCommand : PSCmdlet
    {
        
        [Parameter(ValueFromPipeline = true)]
        public PSObject InputObject { get; set; }

        
        [Parameter(Position = 0)]
        [ValidateNotNullOrEmpty]
        public string[] Name { get; set; } = new string[] { "*" };

        
        [Parameter]
        [Alias("Type")]
        public PSMemberTypes MemberType { get; set; } = PSMemberTypes.All;

        
        [Parameter]
        public PSMemberViewTypes View { get; set; } = PSMemberViewTypes.Adapted | PSMemberViewTypes.Extended;

        private bool _staticParameter = false;
        
        [Parameter]
        public SwitchParameter Static
        {
            get { return _staticParameter; }

            set { _staticParameter = value; }
        }

        
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return (_matchOptions == MshMemberMatchOptions.IncludeHidden);
            }

            set
            {
                if (value)
                {
                    // Include hidden members if force parameter is set
                    _matchOptions = MshMemberMatchOptions.IncludeHidden;
                }
                else
                {
                    _matchOptions = MshMemberMatchOptions.None;
                }
            }
        }

        private MshMemberMatchOptions _matchOptions = MshMemberMatchOptions.None;

        private readonly HybridDictionary _typesAlreadyDisplayed = new();

        
        protected override void ProcessRecord()
        {
            if (this.InputObject == null || this.InputObject == AutomationNull.Value)
            {
                return;
            }

            Type baseObjectAsType = null;

            string typeName;
            Adapter staticAdapter = null;
            if (this.Static == true)
            {
                staticAdapter = PSObject.DotNetStaticAdapter;
                object baseObject = this.InputObject.BaseObject;
                baseObjectAsType = baseObject as System.Type ?? baseObject.GetType();
                typeName = baseObjectAsType.FullName;
            }
            else
            {
                var typeNames = this.InputObject.InternalTypeNames;
                if (typeNames.Count != 0)
                {
                    typeName = typeNames[0];
                }
                else
                {
                    // This is never used for display.  It is used only as a key to typesAlreadyDisplayed
                    typeName = "<null>";
                }
            }

            if (_typesAlreadyDisplayed.Contains(typeName))
            {
                return;
            }
            else
            {
                _typesAlreadyDisplayed.Add(typeName, string.Empty);
            }

            PSMemberTypes memberTypeToSearch = MemberType;
            PSMemberViewTypes viewToSearch = View;

            if (((View & PSMemberViewTypes.Extended) == 0) &&
                (!typeof(PSMemberSet).ToString().Equals(typeName, StringComparison.OrdinalIgnoreCase)))
            {
                // PSMemberSet is an internal memberset and its properties/methods are populated differently.
                // PSMemberSet instance is created to represent PSExtended, PSAdapted, PSBase, PSObject hidden
                // properties. We should honor extended properties for such case.

                // request is to search dotnet or adapted or both members.
                // dotnet,adapted members cannot be Script*,Note*,Code*
                memberTypeToSearch ^= (PSMemberTypes.AliasProperty | PSMemberTypes.CodeMethod | PSMemberTypes.CodeProperty
                | PSMemberTypes.MemberSet | PSMemberTypes.NoteProperty | PSMemberTypes.PropertySet | PSMemberTypes.ScriptMethod
                | PSMemberTypes.ScriptProperty);
            }

            if (((View & PSMemberViewTypes.Adapted) == 0) && (View & PSMemberViewTypes.Base) == 0)
            {
                // base and adapted are not mentioned in the view so ignore respective properties
                memberTypeToSearch ^= (PSMemberTypes.Property | PSMemberTypes.ParameterizedProperty | PSMemberTypes.Method);
            }

            if (((View & PSMemberViewTypes.Base) == PSMemberViewTypes.Base) &&
                (InputObject.InternalBaseDotNetAdapter == null))
            {
                // the input object don't have a custom adapter..
                // for this case adapted view and base view are the same.
                viewToSearch |= PSMemberViewTypes.Adapted;
            }

            PSMemberInfoCollection<PSMemberInfo> membersToSearch;
            if (this.Static == true)
            {
                membersToSearch = staticAdapter.BaseGetMembers<PSMemberInfo>(baseObjectAsType);
            }
            else
            {
                Collection<CollectionEntry<PSMemberInfo>> memberCollection = PSObject.GetMemberCollection(viewToSearch);
                membersToSearch = new PSMemberInfoIntegratingCollection<PSMemberInfo>(this.InputObject, memberCollection);
            }

            foreach (string nameElement in this.Name)
            {
                ReadOnlyPSMemberInfoCollection<PSMemberInfo> readOnlyMembers;
                readOnlyMembers = membersToSearch.Match(nameElement, memberTypeToSearch, _matchOptions);

                MemberDefinition[] members = new MemberDefinition[readOnlyMembers.Count];
                int resultCount = 0;
                foreach (PSMemberInfo member in readOnlyMembers)
                {
                    if (!Force)
                    {
                        if ((member is PSMethod memberAsPSMethod) && (memberAsPSMethod.IsSpecial))
                        {
                            continue;
                        }
                    }

                    members[resultCount] = new MemberDefinition(typeName, member.Name, member.MemberType, member.ToString());
                    resultCount++;
                }

                Array.Sort<MemberDefinition>(members, 0, resultCount, new MemberComparer());
                for (int index = 0; index < resultCount; index++)
                {
                    this.WriteObject(members[index]);
                }
            }
        }

        private sealed class MemberComparer : System.Collections.Generic.IComparer<MemberDefinition>
        {
            public int Compare(MemberDefinition first, MemberDefinition second)
            {
                int result = string.Compare(first.MemberType.ToString(), second.MemberType.ToString(),
                    StringComparison.OrdinalIgnoreCase);
                if (result != 0)
                {
                    return result;
                }

                return string.Compare(first.Name, second.Name, StringComparison.OrdinalIgnoreCase);
            }
        }

        
        protected override void EndProcessing()
        {
            if (_typesAlreadyDisplayed.Count == 0)
            {
                ErrorRecord errorRecord = new(
                    new InvalidOperationException(GetMember.NoObjectSpecified),
                    "NoObjectInGetMember",
                    ErrorCategory.CloseError,
                    null);
                WriteError(errorRecord);
            }
        }
    }
}
