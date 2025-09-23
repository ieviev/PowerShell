// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Management.Automation;
using System.Management.Automation.Internal;
using System.Management.Automation.Runspaces;

using Microsoft.PowerShell.MarkdownRender;

namespace Microsoft.PowerShell.Commands
{
    
    [Cmdlet(
        VerbsCommon.Set, "MarkdownOption",
        DefaultParameterSetName = IndividualSetting,
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2006265")]
    [OutputType(typeof(Microsoft.PowerShell.MarkdownRender.PSMarkdownOptionInfo))]
    public class SetMarkdownOptionCommand : PSCmdlet
    {
        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header1Color { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header2Color { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header3Color { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header4Color { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header5Color { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Header6Color { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string Code { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string ImageAltTextForegroundColor { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string LinkForegroundColor { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string ItalicsForegroundColor { get; set; }

        
        [ValidatePattern(@"^\[*[0-9;]*?m{1}")]
        [Parameter(ParameterSetName = IndividualSetting)]
        public string BoldForegroundColor { get; set; }

        
        [Parameter]
        public SwitchParameter PassThru { get; set; }

        
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = ThemeParamSet, Mandatory = true)]
        [ValidateSet(DarkThemeName, LightThemeName)]
        public string Theme { get; set; }

        
        [ValidateNotNullOrEmpty]
        [Parameter(ParameterSetName = InputObjectParamSet, Mandatory = true, ValueFromPipeline = true, Position = 0)]
        public PSObject InputObject { get; set; }

        private const string IndividualSetting = "IndividualSetting";
        private const string InputObjectParamSet = "InputObject";
        private const string ThemeParamSet = "Theme";
        private const string LightThemeName = "Light";
        private const string DarkThemeName = "Dark";

        
        protected override void EndProcessing()
        {
            PSMarkdownOptionInfo mdOptionInfo = null;

            switch (ParameterSetName)
            {
                case ThemeParamSet:
                    mdOptionInfo = new PSMarkdownOptionInfo();
                    if (string.Equals(Theme, LightThemeName, StringComparison.OrdinalIgnoreCase))
                    {
                        mdOptionInfo.SetLightTheme();
                    }
                    else if (string.Equals(Theme, DarkThemeName, StringComparison.OrdinalIgnoreCase))
                    {
                        mdOptionInfo.SetDarkTheme();
                    }

                    break;

                case InputObjectParamSet:
                    object baseObj = InputObject.BaseObject;
                    mdOptionInfo = baseObj as PSMarkdownOptionInfo;

                    if (mdOptionInfo == null)
                    {
                        var errorMessage = StringUtil.Format(ConvertMarkdownStrings.InvalidInputObjectType, baseObj.GetType());

                        ErrorRecord errorRecord = new(
                            new ArgumentException(errorMessage),
                            "InvalidObject",
                            ErrorCategory.InvalidArgument,
                            InputObject);
                    }

                    break;

                case IndividualSetting:
                    mdOptionInfo = new PSMarkdownOptionInfo();
                    SetOptions(mdOptionInfo);
                    break;
            }

            var setOption = PSMarkdownOptionInfoCache.Set(this.CommandInfo, mdOptionInfo);

            if (PassThru.IsPresent)
            {
                WriteObject(setOption);
            }
        }

        private void SetOptions(PSMarkdownOptionInfo mdOptionInfo)
        {
            if (!string.IsNullOrEmpty(Header1Color))
            {
                mdOptionInfo.Header1 = Header1Color;
            }

            if (!string.IsNullOrEmpty(Header2Color))
            {
                mdOptionInfo.Header2 = Header2Color;
            }

            if (!string.IsNullOrEmpty(Header3Color))
            {
                mdOptionInfo.Header3 = Header3Color;
            }

            if (!string.IsNullOrEmpty(Header4Color))
            {
                mdOptionInfo.Header4 = Header4Color;
            }

            if (!string.IsNullOrEmpty(Header5Color))
            {
                mdOptionInfo.Header5 = Header5Color;
            }

            if (!string.IsNullOrEmpty(Header6Color))
            {
                mdOptionInfo.Header6 = Header6Color;
            }

            if (!string.IsNullOrEmpty(Code))
            {
                mdOptionInfo.Code = Code;
            }

            if (!string.IsNullOrEmpty(ImageAltTextForegroundColor))
            {
                mdOptionInfo.Image = ImageAltTextForegroundColor;
            }

            if (!string.IsNullOrEmpty(LinkForegroundColor))
            {
                mdOptionInfo.Link = LinkForegroundColor;
            }

            if (!string.IsNullOrEmpty(ItalicsForegroundColor))
            {
                mdOptionInfo.EmphasisItalics = ItalicsForegroundColor;
            }

            if (!string.IsNullOrEmpty(BoldForegroundColor))
            {
                mdOptionInfo.EmphasisBold = BoldForegroundColor;
            }
        }
    }

    
    [Cmdlet(
        VerbsCommon.Get, "MarkdownOption",
        HelpUri = "https://go.microsoft.com/fwlink/?linkid=2006371")]
    [OutputType(typeof(Microsoft.PowerShell.MarkdownRender.PSMarkdownOptionInfo))]
    public class GetMarkdownOptionCommand : PSCmdlet
    {
        private const string MarkdownOptionInfoVariableName = "PSMarkdownOptionInfo";

        
        protected override void EndProcessing()
        {
            WriteObject(PSMarkdownOptionInfoCache.Get(this.CommandInfo));
        }
    }

    
    internal static class PSMarkdownOptionInfoCache
    {
        private static readonly ConcurrentDictionary<Guid, PSMarkdownOptionInfo> markdownOptionInfoCache;

        private const string MarkdownOptionInfoVariableName = "PSMarkdownOptionInfo";

        static PSMarkdownOptionInfoCache()
        {
            markdownOptionInfoCache = new ConcurrentDictionary<Guid, PSMarkdownOptionInfo>();
        }

        internal static PSMarkdownOptionInfo Get(CommandInfo command)
        {
            // If we have the moduleInfo then store are module scope variable
            if (command.Module != null)
            {
                return command.Module.SessionState.PSVariable.GetValue(MarkdownOptionInfoVariableName, new PSMarkdownOptionInfo()) as PSMarkdownOptionInfo;
            }

            // If we don't have a moduleInfo, like in PowerShell hosting scenarios, use a concurrent dictionary.
            if (markdownOptionInfoCache.TryGetValue(Runspace.DefaultRunspace.InstanceId, out PSMarkdownOptionInfo cachedOption))
            {
                // return the cached options for the runspaceId
                return cachedOption;
            }
            else
            {
                // no option cache so cache and return the default PSMarkdownOptionInfo
                var newOptionInfo = new PSMarkdownOptionInfo();
                return markdownOptionInfoCache.GetOrAdd(Runspace.DefaultRunspace.InstanceId, newOptionInfo);
            }
        }

        internal static PSMarkdownOptionInfo Set(CommandInfo command, PSMarkdownOptionInfo optionInfo)
        {
            // If we have the moduleInfo then store are module scope variable
            if (command.Module != null)
            {
                command.Module.SessionState.PSVariable.Set(MarkdownOptionInfoVariableName, optionInfo);
                return optionInfo;
            }

            // If we don't have a moduleInfo, like in PowerShell hosting scenarios with modules loaded as snapins, use a concurrent dictionary.
            return markdownOptionInfoCache.AddOrUpdate(Runspace.DefaultRunspace.InstanceId, optionInfo, (key, oldvalue) => optionInfo);
        }
    }
}
