// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(VerbsData.Export, "FormatData", DefaultParameterSetName = "ByPath", HelpUri = "https://go.microsoft.com/fwlink/?LinkID=2096834")]
    public class ExportFormatDataCommand : PSCmdlet
    {
        private ExtendedTypeDefinition[] _typeDefinition;

        
        [Parameter(Mandatory = true, ValueFromPipeline = true)]
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
        public ExtendedTypeDefinition[] InputObject
        {
            get
            {
                return _typeDefinition;
            }

            set
            {
                _typeDefinition = value;
            }
        }

        private string _filepath;

        
        [Parameter(ParameterSetName = "ByPath", Mandatory = true)]
        [Alias("FilePath")]
        public string Path
        {
            get
            {
                return _filepath;
            }

            set
            {
                _filepath = value;
            }
        }

        
        [Parameter(ParameterSetName = "ByLiteralPath", Mandatory = true)]
        [Alias("PSPath", "LP")]
        public string LiteralPath
        {
            get
            {
                return _filepath;
            }

            set
            {
                _filepath = value;
                _isLiteralPath = true;
            }
        }

        private bool _isLiteralPath = false;

        private readonly List<ExtendedTypeDefinition> _typeDefinitions = new();

        private bool _force;

        
        [Parameter]
        public SwitchParameter Force
        {
            get
            {
                return _force;
            }

            set
            {
                _force = value;
            }
        }

        
        [Parameter]
        [Alias("NoOverwrite")]
        public SwitchParameter NoClobber
        {
            get
            {
                return _noclobber;
            }

            set
            {
                _noclobber = value;
            }
        }

        private bool _noclobber;

        
        [Parameter]
        public SwitchParameter IncludeScriptBlock
        {
            get
            {
                return _includescriptblock;
            }

            set
            {
                _includescriptblock = value;
            }
        }

        private bool _includescriptblock;

        
        protected override void ProcessRecord()
        {
            foreach (ExtendedTypeDefinition typedef in _typeDefinition)
            {
                _typeDefinitions.Add(typedef);
            }
        }

        
        protected override void EndProcessing()
        {
            FormatXmlWriter.WriteToPs1Xml(this, _typeDefinitions, _filepath, _force, _noclobber, _includescriptblock, _isLiteralPath);
        }
    }
}
