// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Management.Automation;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

using Microsoft.Management.UI;
using Microsoft.Management.UI.Internal;
using Microsoft.Management.UI.Internal.ShowCommand;
using Microsoft.PowerShell.Commands.ShowCommandExtension;

namespace Microsoft.PowerShell.Commands.ShowCommandInternal
{
    
    internal class ShowCommandHelper : IDisposable
    {
        #region fields

        internal const string CommandTypeSegment = " -CommandType Cmdlet, Function, Script, ExternalScript";

        
        private DispatcherOperationCallback methodThatReturnsDialog;

        
        private AutoResetEvent windowClosed = new AutoResetEvent(false);

        
        private AutoResetEvent helpNeeded = new AutoResetEvent(false);

        
        private AutoResetEvent importModuleNeeded = new AutoResetEvent(false);

        
        private AutoResetEvent windowLoaded = new AutoResetEvent(false);

        
        private string commandNeedingHelp;

        
        private string commandNeedingImportModule;

        
        private string parentModuleNeedingImportModule;

        
        private string selectedModuleNeedingImportModule;

        
        private Window window;

        
        private Window hostWindow;

        
        private AllModulesViewModel allModulesViewModel;

        
        private CommandViewModel commandViewModel;

        
        private bool dialogCanceled = true;
        #endregion fields

        #region GetSerializedCommand script

        private const string ScriptGetSerializedCommand = @"
Function PSGetSerializedShowCommandInfo
{
    Function GetParameterType
    {
        param (
            [Type] $parameterType)

        $returnParameterType = new-object PSObject
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""FullName"" -Value $parameterType.FullName
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""IsEnum"" -Value $parameterType.IsEnum
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""IsArray"" -Value $parameterType.IsArray

        if ($parameterType.IsEnum)
        {
            $enumValues = [System.Enum]::GetValues($parameterType)
        }
        else
        {
            $enumValues = [string[]] @()
        }
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""EnumValues"" -Value $enumValues

        if ($parameterType.IsArray)
        {
            $hasFlagAttribute = ($parameterType.GetCustomAttributes([System.FlagsAttribute], $true).Length -gt 0)

            # Recurse into array elements.
            $elementType = GetParameterType($parameterType.GetElementType())
        }
        else
        {
            $hasFlagAttribute = $false
            $elementType = $null
        }
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""HasFlagAttribute"" -Value $hasFlagAttribute
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""ElementType"" -Value $elementType


        if (!($parameterType.IsEnum) -and !($parameterType.IsArray))
        {
            $implementsDictionary = [System.Collections.IDictionary].IsAssignableFrom($parameterType)
        }
        else
        {
            $implementsDictionary = $false
        }
        $returnParameterType | Add-Member -MemberType NoteProperty -Name ""ImplementsDictionary"" -Value $implementsDictionary

        return $returnParameterType
    }

    Function GetParameterInfo
    {
        param (
            $parameters)

        [PSObject[]] $parameterInfos = @()

        foreach ($parameter in $parameters)
        {
            $parameterInfo = new-object PSObject
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""Name"" -Value $parameter.Name
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""IsMandatory"" -Value $parameter.IsMandatory
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""ValueFromPipeline"" -Value $parameter.ValueFromPipeline
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""Position"" -Value $parameter.Position
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""ParameterType"" -Value (GetParameterType($parameter.ParameterType))

            $hasParameterSet = $false
            [string[]] $validValues = @()
            if ($PSVersionTable.PSVersion.Major -gt 2)
            {
                $validateSetAttributes = $parameter.Attributes | Where {
                    [ValidateSet].IsAssignableFrom($_.GetType())
                }
                if (($validateSetAttributes -ne $null) -and ($validateSetAttributes.Count -gt 0))
                {
                    $hasParameterSet = $true
                    $validValues = $validateSetAttributes[0].ValidValues
                }
            }
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""HasParameterSet"" -Value $hasParameterSet
            $parameterInfo | Add-Member -MemberType NoteProperty -Name ""ValidParamSetValues"" -Value $validValues

            $parameterInfos += $parameterInfo
        }

        return (,$parameterInfos)
    }

    Function GetParameterSets
    {
        param (
            [System.Management.Automation.CommandInfo] $cmdInfo
        )

        $parameterSets = $null
        try
        {
            $parameterSets = $cmdInfo.ParameterSets
        }
        catch [System.InvalidOperationException] { }
        catch [System.Management.Automation.PSNotSupportedException] { }
        catch [System.Management.Automation.PSNotImplementedException] { }

        if (($parameterSets -eq $null) -or ($parameterSets.Count -eq 0))
        {
            return (,@())
        }

        [PSObject[]] $returnParameterSets = @()

        foreach ($parameterSet in $parameterSets)
        {
            $parameterSetInfo = new-object PSObject
            $parameterSetInfo | Add-Member -MemberType NoteProperty -Name ""Name"" -Value $parameterSet.Name
            $parameterSetInfo | Add-Member -MemberType NoteProperty -Name ""IsDefault"" -Value $parameterSet.IsDefault
            $parameterSetInfo | Add-Member -MemberType NoteProperty -Name ""Parameters"" -Value (GetParameterInfo($parameterSet.Parameters))

            $returnParameterSets += $parameterSetInfo
        }

        return (,$returnParameterSets)
    }

    Function GetModuleInfo
    {
        param (
            [System.Management.Automation.CommandInfo] $cmdInfo
        )

        if ($cmdInfo.ModuleName -ne $null)
        {
            $moduleName = $cmdInfo.ModuleName
        }
        else
        {
            $moduleName = """"
        }

        $moduleInfo = new-object PSObject
        $moduleInfo | Add-Member -MemberType NoteProperty -Name ""Name"" -Value $moduleName

        return $moduleInfo
    }

    Function ConvertToShowCommandInfo
    {
        param (
            [System.Management.Automation.CommandInfo] $cmdInfo
        )

        $showCommandInfo = new-object PSObject
        $showCommandInfo | Add-Member -MemberType NoteProperty -Name ""Name"" -Value $cmdInfo.Name
        $showCommandInfo | Add-Member -MemberType NoteProperty -Name ""ModuleName"" -Value $cmdInfo.ModuleName
        $showCommandInfo | Add-Member -MemberType NoteProperty -Name ""Module"" -Value (GetModuleInfo($cmdInfo))
        $showCommandInfo | Add-Member -MemberType NoteProperty -Name ""CommandType"" -Value $cmdInfo.CommandType
        $showCommandInfo | Add-Member -MemberType NoteProperty -Name ""Definition"" -Value $cmdInfo.Definition
        $showCommandInfo | Add-Member -MemberType NoteProperty -Name ""ParameterSets"" -Value (GetParameterSets($cmdInfo))

        return $showCommandInfo
    }

    $commandList = @(""Cmdlet"", ""Function"", ""Script"", ""ExternalScript"")
    if ($PSVersionTable.PSVersion.Major -gt 2)
    {
        $commandList += ""Workflow""
    }

    foreach ($command in @(Get-Command -CommandType $commandList))
    {
        Write-Output (ConvertToShowCommandInfo($command))
    }
}";

        #endregion

        #region constructor and destructor
        
        private ShowCommandHelper()
        {
        }

        
        ~ShowCommandHelper()
        {
            this.Dispose(false);
        }
        #endregion constructor and destructor

        #region properties called using reflection
        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private static double ScreenWidth
        {
            get
            {
                return System.Windows.SystemParameters.PrimaryScreenWidth;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private static double ScreenHeight
        {
            get
            {
                return System.Windows.SystemParameters.PrimaryScreenHeight;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private AutoResetEvent WindowClosed
        {
            get
            {
                return this.windowClosed;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private AutoResetEvent HelpNeeded
        {
            get
            {
                return this.helpNeeded;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private AutoResetEvent ImportModuleNeeded
        {
            get
            {
                return this.importModuleNeeded;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private AutoResetEvent WindowLoaded
        {
            get
            {
                return this.windowLoaded;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private string CommandNeedingHelp
        {
            get
            {
                return this.commandNeedingHelp;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private string ParentModuleNeedingImportModule
        {
            get
            {
                return this.parentModuleNeedingImportModule;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private bool HasHostWindow
        {
            get
            {
                return this.hostWindow != null;
            }
        }
        #endregion properties called using reflection

        #region public Dispose
        
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion public Dispose

        #region internal static methods called using reflection from show-command
        
        internal static void SetClipboardText(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (System.Runtime.InteropServices.COMException)
            {
                // This is the recommended way to set clipboard text
                System.Threading.Thread.Sleep(0);
                try
                {
                    Clipboard.SetText(text);
                }
                catch (System.Runtime.InteropServices.COMException)
                {
                }
            }
        }

        
        internal static string GetShowAllModulesCommand(bool isRemoteRunspace = false, bool isFirstChance = true)
        {
            string scriptBase;

            if (isRemoteRunspace)
            {
                if (isFirstChance)
                {
                    // Return command to run.
                    scriptBase = "@(Get-Command " + ShowCommandHelper.CommandTypeSegment + @" -ShowCommandInfo)";
                }
                else
                {
                    // Return script to run.
                    scriptBase = GetSerializedCommandScript();
                }
            }
            else
            {
                scriptBase = "@(Get-Command " + ShowCommandHelper.CommandTypeSegment + ")";
            }

            scriptBase += ShowCommandHelper.GetGetModuleSuffix();
            return scriptBase;
        }

        
        private static string GetSerializedCommandScript()
        {
            return string.Format(
                CultureInfo.InvariantCulture,
                "@({0};{1};{2})",
                ScriptGetSerializedCommand,
                @"PSGetSerializedShowCommandInfo",
                @"Remove-Item -Path 'function:\PSGetSerializedShowCommandInfo' -Force");
        }

        
        internal static string GetHelpCommand(string command)
        {
            return "Get-Help " + ShowCommandHelper.SingleQuote(command);
        }

        
        internal static Dictionary<string, ShowCommandModuleInfo> GetImportedModulesDictionary(object[] moduleObjects)
        {
            Dictionary<string, ShowCommandModuleInfo> returnValue = new Dictionary<string, ShowCommandModuleInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (PSObject rawModule in moduleObjects)
            {
                ShowCommandModuleInfo wrappedModule = null;
                PSModuleInfo module = rawModule.BaseObject as PSModuleInfo;
                if (module != null)
                {
                    wrappedModule = new ShowCommandModuleInfo(module);
                }
                else
                {
                    wrappedModule = new ShowCommandModuleInfo(rawModule);
                }

                // It is probably an issue somewhere else that a module would show up twice in the list, but we want to avoid
                // throwing an exception regarding that in returnValue.Add
                if (!returnValue.ContainsKey(wrappedModule.Name))
                {
                    returnValue.Add(wrappedModule.Name, wrappedModule);
                }
            }

            return returnValue;
        }

        
        internal static List<ShowCommandCommandInfo> GetCommandList(object[] commandObjects)
        {
            List<ShowCommandCommandInfo> returnValue = new List<ShowCommandCommandInfo>();
            foreach (PSObject rawCommand in commandObjects)
            {
                CommandInfo command = rawCommand.BaseObject as CommandInfo;
                if (command != null)
                {
                    returnValue.Add(new ShowCommandCommandInfo(command));
                }
                else
                {
                    PSObject obj = rawCommand as PSObject;
                    if (obj != null)
                    {
                        returnValue.Add(new ShowCommandCommandInfo(obj));
                    }
                }
            }

            return returnValue;
        }

        
        internal static object[] ObjectArrayFromObjectCollection(object commandObjects)
        {
            object[] objectArray = commandObjects as object[] ?? ((System.Collections.ArrayList)commandObjects).ToArray();

            return objectArray;
        }

        
        internal static AllModulesViewModel GetNewAllModulesViewModel(AllModulesViewModel oldViewModel, Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, string selectedModuleNeedingImportModule, string parentModuleNeedingImportModule, string commandNeedingImportModule)
        {
            string oldFilter = null;

            if (oldViewModel.SelectedModule != null)
            {
                // this will allow the old view model to stop listening for events before we
                // replace it with a new view model
                oldViewModel.SelectedModule.SelectedCommand = null;
                oldViewModel.SelectedModule = null;
                oldFilter = oldViewModel.CommandNameFilter;
            }

            AllModulesViewModel returnValue = new AllModulesViewModel(importedModules, commands, oldViewModel.NoCommonParameter);
            if (!string.IsNullOrEmpty(oldFilter))
            {
                returnValue.CommandNameFilter = oldFilter;
            }

            if (selectedModuleNeedingImportModule == null || parentModuleNeedingImportModule == null)
            {
                return returnValue;
            }

            ModuleViewModel moduleToSelect = returnValue.Modules.Find(
                new Predicate<ModuleViewModel>((module) =>
                {
                    return module.Name.Equals(selectedModuleNeedingImportModule, StringComparison.OrdinalIgnoreCase) ? true : false;
                }));

            if (moduleToSelect == null)
            {
                return returnValue;
            }

            returnValue.SelectedModule = moduleToSelect;

            CommandViewModel commandToSelect = moduleToSelect.Commands.Find(
                new Predicate<CommandViewModel>((command) =>
                {
                    return command.ModuleName.Equals(parentModuleNeedingImportModule, StringComparison.OrdinalIgnoreCase) &&
                        command.Name.Equals(commandNeedingImportModule, StringComparison.OrdinalIgnoreCase) ? true : false;
                }));

            if (commandToSelect == null)
            {
                return returnValue;
            }

            moduleToSelect.SelectedCommand = commandToSelect;
            return returnValue;
        }

        
        internal static string GetImportModuleFailedMessage(string command, string module, string error)
        {
            return string.Format(
                CultureInfo.CurrentUICulture,
                ShowCommandResources.ImportModuleFailedFormat,
                command,
                module,
                error);
        }

        
        internal static string SingleQuote(string str)
        {
            if (str == null)
            {
                str = string.Empty;
            }

            return "\'" + System.Management.Automation.Language.CodeGeneration.EscapeSingleQuotedStringContent(str) + "\'";
        }
        #endregion internal static methods called using reflection from show-command

        #region internal static methods used internally in this assembly
        
        internal static Window GetHostWindow(PSCmdlet cmdlet)
        {
            PSPropertyInfo windowProperty = cmdlet.Host.PrivateData.Properties["Window"];
            if (windowProperty == null)
            {
                return null;
            }

            try
            {
                return windowProperty.Value as Window;
            }
            catch (ExtendedTypeSystemException)
            {
                return null;
            }
        }
        #endregion internal static methods used internally in this assembly

        #region static private methods used only on this file

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from a method called using reflection")]
        private static object GetPropertyValue(Type type, object obj, string propertyName, BindingFlags bindingFlags)
        {
            PropertyInfo property = type.GetProperty(propertyName, bindingFlags);
            if (property == null)
            {
                return null;
            }

            try
            {
                return property.GetValue(obj, Array.Empty<object>());
            }
            catch (ArgumentException)
            {
                return null;
            }
            catch (TargetException)
            {
                return null;
            }
            catch (TargetParameterCountException)
            {
                return null;
            }
            catch (MethodAccessException)
            {
                return null;
            }
            catch (TargetInvocationException)
            {
                return null;
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from a method called using reflection")]
        private static bool SetPropertyValue(Type type, object obj, string propertyName, object value, BindingFlags bindingFlags)
        {
            PropertyInfo property = type.GetProperty(propertyName, bindingFlags);
            if (property == null)
            {
                return false;
            }

            try
            {
                property.SetValue(obj, value, Array.Empty<object>());
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (TargetException)
            {
                return false;
            }
            catch (TargetParameterCountException)
            {
                return false;
            }
            catch (MethodAccessException)
            {
                return false;
            }
            catch (TargetInvocationException)
            {
                return false;
            }

            return true;
        }

        
        private static string GetGetModuleSuffix()
        {
            return ",@(get-module)";
        }

        #endregion static private methods used only on this file

        #region private methods called using reflection from show-command
        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private static string GetShowCommandCommand(string commandName, bool includeAliasAndModules)
        {
            string quotedCommandName = ShowCommandHelper.SingleQuote(commandName);
            return "@(get-command " + quotedCommandName + " " + ShowCommandHelper.CommandTypeSegment +
                (includeAliasAndModules ? ",Alias" : string.Empty) + ")" +
                (includeAliasAndModules ? ShowCommandHelper.GetGetModuleSuffix() : string.Empty);
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private static object GetCommandViewModel(ShowCommandCommandInfo command, bool noCommonParameter, Dictionary<string, ShowCommandModuleInfo> importedModules, bool moduleQualify)
        {
            CommandViewModel returnValue = CommandViewModel.GetCommandViewModel(new ModuleViewModel(command.ModuleName, importedModules), command, noCommonParameter);
            returnValue.ModuleQualifyCommandName = moduleQualify;
            return returnValue;
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from ActivateWindow() which is called using reflection")]
        private static void ActivateWindow(Window window)
        {
            window.Dispatcher.Invoke(
                new SendOrPostCallback(
                    (_) => window.Activate()),
                string.Empty);
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private void ShowAllModulesWindow(PSCmdlet cmdlet, Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands, bool noCommonParameter, double windowWidth, double windowHeight, bool passThrough)
        {
            this.methodThatReturnsDialog = new DispatcherOperationCallback((object ignored) =>
            {
                ShowAllModulesWindow allModulesWindow = new ShowAllModulesWindow();
                this.allModulesViewModel = new AllModulesViewModel(importedModules, commands, noCommonParameter);

                this.SetupButtonEvents(allModulesWindow.Run, allModulesWindow.Copy, allModulesWindow.Cancel, passThrough);
                this.SetupWindow(allModulesWindow);
                this.SetupViewModel();
                CommonHelper.SetStartingPositionAndSize(
                    allModulesWindow,
                    ShowCommandSettings.Default.ShowCommandsTop,
                    ShowCommandSettings.Default.ShowCommandsLeft,
                    windowWidth != 0.0 && windowWidth > allModulesWindow.MinWidth ? windowWidth : ShowCommandSettings.Default.ShowCommandsWidth,
                    windowHeight != 0.0 && windowHeight > allModulesWindow.MinHeight ? windowHeight : ShowCommandSettings.Default.ShowCommandsHeight,
                    allModulesWindow.Width,
                    allModulesWindow.Height,
                    ShowCommandSettings.Default.ShowCommandsWindowMaximized);

                return allModulesWindow;
            });

            this.CallShowDialog(cmdlet);
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from a method called using reflection")]
        private void CallShowDialog(PSCmdlet cmdlet)
        {
            this.hostWindow = ShowCommandHelper.GetHostWindow(cmdlet);
            if (this.hostWindow == null)
            {
                Thread guiThread = new Thread(new ThreadStart(this.PlainInvokeAndShowDialog));
                guiThread.SetApartmentState(ApartmentState.STA);
                guiThread.Start();
                return;
            }

            this.hostWindow.Dispatcher.Invoke(
                new SendOrPostCallback(
                    (_) =>
                    {
                        Window childWindow = (Window)this.methodThatReturnsDialog.Invoke(null);
                        childWindow.Owner = this.hostWindow;
                        childWindow.Show();
                    }),
                    string.Empty);
        }

        
        private void PlainInvokeAndShowDialog()
        {
            ((Window)this.methodThatReturnsDialog.Invoke(null)).ShowDialog();
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private void ShowCommandWindow(PSCmdlet cmdlet, object commandViewModelObj, double windowWidth, double windowHeight, bool passThrough)
        {
            this.methodThatReturnsDialog = new DispatcherOperationCallback((object ignored) =>
            {
                this.commandViewModel = (CommandViewModel)commandViewModelObj;
                ShowCommandWindow showCommandWindow = new ShowCommandWindow();

                this.commandViewModel.HelpNeeded += this.CommandNeedsHelp;
                showCommandWindow.DataContext = this.commandViewModel;

                this.SetupButtonEvents(showCommandWindow.Run, showCommandWindow.Copy, showCommandWindow.Cancel, passThrough);
                this.SetupWindow(showCommandWindow);

                CommonHelper.SetStartingPositionAndSize(
                    showCommandWindow,
                    ShowCommandSettings.Default.ShowOneCommandTop,
                    ShowCommandSettings.Default.ShowOneCommandLeft,
                    windowWidth != 0.0 && windowWidth > showCommandWindow.MinWidth ? windowWidth : ShowCommandSettings.Default.ShowOneCommandWidth,
                    windowHeight != 0.0 && windowHeight > showCommandWindow.MinHeight ? windowHeight : ShowCommandSettings.Default.ShowOneCommandHeight,
                    showCommandWindow.Width,
                    showCommandWindow.Height,
                    ShowCommandSettings.Default.ShowOneCommandWindowMaximized);

                return showCommandWindow;
            });

            this.CallShowDialog(cmdlet);
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private void ImportModuleDone(Dictionary<string, ShowCommandModuleInfo> importedModules, IEnumerable<ShowCommandCommandInfo> commands)
        {
            this.allModulesViewModel.WaitMessageDisplayed = false;
            if (this.window != null)
            {
                this.window.Dispatcher.Invoke(
                    new SendOrPostCallback(
                        delegate(object ignored)
                        {
                            this.allModulesViewModel = ShowCommandHelper.GetNewAllModulesViewModel(
                                this.allModulesViewModel,
                                importedModules,
                                commands,
                                this.selectedModuleNeedingImportModule,
                                this.parentModuleNeedingImportModule,
                                this.commandNeedingImportModule);
                            this.SetupViewModel();
                        }),
                        string.Empty);
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private void ImportModuleFailed(Exception reason)
        {
            this.allModulesViewModel.WaitMessageDisplayed = false;
            if (this.window != null)
            {
                this.window.Dispatcher.Invoke(
                    new SendOrPostCallback(
                        (_) =>
                        {
                            string message = ShowCommandHelper.GetImportModuleFailedMessage(
                                this.commandNeedingImportModule,
                                this.parentModuleNeedingImportModule,
                                reason.Message);
                            MessageBox.Show(this.window, message, ShowCommandResources.ShowCommandError, MessageBoxButton.OK, MessageBoxImage.Error);
                        }),
                        string.Empty);
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private void DisplayHelp(Collection<PSObject> getHelpResults)
        {
            if (this.window != null && getHelpResults != null && getHelpResults.Count > 0)
            {
                this.window.Dispatcher.Invoke(
                    new SendOrPostCallback(
                        delegate(object ignored)
                        {
                            HelpWindow help = new HelpWindow(getHelpResults[0]);
                            help.Owner = this.window;
                            help.Show();
                        }),
                        string.Empty);
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private void ActivateWindow()
        {
            if (this.window != null)
            {
                ShowCommandHelper.ActivateWindow(this.window);
            }
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called using reflection")]
        private string GetScript()
        {
            if (this.dialogCanceled)
            {
                return null;
            }

            return this.InternalGetScript();
        }
        #endregion private methods called using reflection from show-command

        #region instance private methods used only on this file
        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from ShowAllModulesWindow and ShowCommandWindow which are called with reflection")]
        private void SetupWindow(Window commandWindow)
        {
            this.window = commandWindow;
            this.window.Closed += this.Window_Closed;
            this.window.Loaded += this.Window_Loaded;
        }

        
        private void CommandNeedsImportModule(object sender, ImportModuleEventArgs e)
        {
            this.commandNeedingImportModule = e.CommandName;
            this.parentModuleNeedingImportModule = e.ParentModuleName;
            this.selectedModuleNeedingImportModule = e.SelectedModuleName;
            this.allModulesViewModel.WaitMessageDisplayed = true;
            this.ImportModuleNeeded.Set();
        }

        
        private void CommandNeedsHelp(object sender, HelpNeededEventArgs e)
        {
            this.commandNeedingHelp = e.CommandName;
            this.HelpNeeded.Set();
        }

        
        private void Window_Closed(object sender, EventArgs e)
        {
            if (this.hostWindow != null)
            {
                this.hostWindow.Focus();
            }

            this.window = null;
            this.windowClosed.Set();
        }

        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.window.Loaded -= this.Window_Loaded;
            this.windowLoaded.Set();
        }

        
        [SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Called from methods called using reflection")]
        private void SetupButtonEvents(Button run, Button copy, Button cancel, bool passThrough)
        {
            if (passThrough)
            {
                run.Content = ShowCommandResources.ActionButtons_Button_Ok;
            }

            run.Click += this.Buttons_RunClick;
            copy.Click += this.Buttons_CopyClick;
            cancel.Click += this.Buttons_CancelClick;
        }

        
        private void SetupViewModel()
        {
            this.allModulesViewModel.SelectedCommandInSelectedModuleNeedsHelp += this.CommandNeedsHelp;
            this.allModulesViewModel.SelectedCommandInSelectedModuleNeedsImportModule += this.CommandNeedsImportModule;
            this.window.DataContext = this.allModulesViewModel;
        }

        
        private void Buttons_CopyClick(object sender, RoutedEventArgs e)
        {
            string script = this.InternalGetScript();
            if (script == null)
            {
                return;
            }

            this.window.Dispatcher.Invoke(new ThreadStart(delegate { ShowCommandHelper.SetClipboardText(script); }));
        }

        
        private void Buttons_RunClick(object sender, RoutedEventArgs e)
        {
            this.dialogCanceled = false;
            this.CloseWindow();
        }

        
        private void Buttons_CancelClick(object sender, RoutedEventArgs e)
        {
            this.CloseWindow();
        }

        
        private void CloseWindow()
        {
            if (this.window == null)
            {
                return;
            }

            this.window.Dispatcher.Invoke(new ThreadStart(delegate
            {
                // This can happen if ISE is closed while show-command is up
                if (this.window != null)
                {
                    this.window.Close();
                }
            }));
        }

        
        private void ShowErrorString(string errorString)
        {
            if (errorString != null && errorString.Trim().Length > 0)
            {
                MessageBox.Show(
                    string.Format(
                        CultureInfo.CurrentUICulture,
                        ShowCommandResources.EndProcessingErrorMessage,
                        errorString),
                     "Show-Command",
                     MessageBoxButton.OK,
                     MessageBoxImage.Error);
            }
        }

        
        private string InternalGetScript()
        {
            if (this.allModulesViewModel != null)
            {
                return this.allModulesViewModel.GetScript();
            }

            if (this.commandViewModel == null)
            {
                return null;
            }

            return this.commandViewModel.GetScript();
        }

        
        private void Dispose(bool isDisposing)
        {
            if (isDisposing)
            {
                this.windowClosed.Dispose();
                this.helpNeeded.Dispose();
                this.windowLoaded.Dispose();
                this.importModuleNeeded.Dispose();
            }
        }
        #endregion instance private methods used only on this file
    }
}
