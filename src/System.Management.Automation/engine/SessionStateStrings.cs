// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace System.Management.Automation
{
    
    internal static class StringLiterals
    {
        // constants

        
        internal const string ProviderPathSeparator = "::";

        
        internal static readonly char DefaultPathSeparator = System.IO.Path.DirectorySeparatorChar;
        internal static readonly string DefaultPathSeparatorString = DefaultPathSeparator.ToString();

        
        internal static readonly char AlternatePathSeparator = Platform.IsWindows ? '/' : '\\';
        internal static readonly string AlternatePathSeparatorString = AlternatePathSeparator.ToString();

        
        internal const string DefaultRemotePathPrefix = "\\\\";

        
        internal const string AlternateRemotePathPrefix = "//";

        
        internal const string HomePath = "~";

        
        internal const string Global = "GLOBAL";

        
        internal const string Local = "LOCAL";

        
        internal const string Private = "PRIVATE";

        
        internal const string Script = "SCRIPT";

        
        internal const string SessionState = "SessionState";

        
        internal const string PowerShellScriptFileExtension = ".ps1";

        
        internal const string PowerShellModuleFileExtension = ".psm1";

        
        internal const string PowerShellMofFileExtension = ".mof";

        
        internal const string PowerShellCmdletizationFileExtension = ".cdxml";

        
        internal const string PowerShellDISCFileExtension = ".pssc";

        
        internal const string PowerShellRoleCapabilityFileExtension = ".psrc";

        
        internal const string PowerShellDataFileExtension = ".psd1";

        
        internal const string PowerShellILAssemblyExtension = ".dll";

        
        internal const string PowerShellNgenAssemblyExtension = ".ni.dll";

        
        internal const string PowerShellILExecutableExtension = ".exe";

        internal const string PowerShellConsoleFileExtension = ".psc1";

        
        internal const char CommandVerbNounSeparator = '-';

        
        internal const string DefaultCommandVerb = "get";

        
        internal const string HelpFileExtension = "-Help.xml";

        
        internal const string DollarNull = "$null";

        
        internal const string Null = "null";

        
        internal const string False = "false";

        
        internal const string True = "true";

        
        internal const char EscapeCharacter = '\\';

        
        internal const string DefaultCmdletAdapter = "Microsoft.PowerShell.Cmdletization.Cim.CimCmdletAdapter, Microsoft.PowerShell.Commands.Management, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35";
    }
}
