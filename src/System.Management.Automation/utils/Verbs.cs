// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Language;
using System.Reflection;
using Microsoft.PowerShell.Commands;
using Dbg = System.Management.Automation.Diagnostics;

namespace System.Management.Automation
{
    #region VERBS

    
    public static class VerbsCommon
    {
        
        public const string Add = "Add";

        
        public const string Clear = "Clear";

        
        public const string Close = "Close";

        
        public const string Copy = "Copy";

        
        public const string Enter = "Enter";

        
        public const string Exit = "Exit";

        
        public const string Find = "Find";

        
        public const string Format = "Format";

        
        public const string Get = "Get";

        
        public const string Hide = "Hide";

        
        public const string Join = "Join";

        
        public const string Lock = "Lock";

        
        public const string Move = "Move";

        
        public const string New = "New";

        
        public const string Open = "Open";

        
        public const string Optimize = "Optimize";

        
        public const string Push = "Push";

        
        public const string Pop = "Pop";

        
        public const string Redo = "Redo";

        
        public const string Remove = "Remove";

        
        public const string Rename = "Rename";

        
        public const string Reset = "Reset";

        
        public const string Resize = "Resize";

        
        public const string Search = "Search";

        
        public const string Select = "Select";

        
        public const string Set = "Set";

        
        public const string Show = "Show";

        
        public const string Skip = "Skip";

        
        public const string Split = "Split";

        
        public const string Step = "Step";

        
        public const string Switch = "Switch";

        
        public const string Undo = "Undo";

        
        public const string Unlock = "Unlock";

        
        public const string Watch = "Watch";
    }

    
    public static class VerbsData
    {
        
        public const string Backup = "Backup";

        
        public const string Checkpoint = "Checkpoint";

        
        public const string Compare = "Compare";

        
        public const string Compress = "Compress";

        
        public const string Convert = "Convert";

        
        public const string ConvertFrom = "ConvertFrom";

        
        public const string ConvertTo = "ConvertTo";

        
        public const string Dismount = "Dismount";

        
        public const string Edit = "Edit";

        
        public const string Expand = "Expand";

        
        public const string Export = "Export";

        
        public const string Group = "Group";

        
        public const string Import = "Import";

        
        public const string Initialize = "Initialize";

        
        public const string Limit = "Limit";

        
        public const string Merge = "Merge";

        
        public const string Mount = "Mount";

        
        public const string Out = "Out";

        
        public const string Publish = "Publish";

        
        public const string Restore = "Restore";

        
        public const string Save = "Save";

        
        public const string Sync = "Sync";

        
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Unpublish")]
        public const string Unpublish = "Unpublish";

        
        public const string Update = "Update";
    }

    
    public static class VerbsLifecycle
    {
        
        public const string Approve = "Approve";

        
        public const string Assert = "Assert";

        
        public const string Build = "Build";

        
        public const string Complete = "Complete";

        
        public const string Confirm = "Confirm";

        
        public const string Deny = "Deny";

        
        public const string Deploy = "Deploy";

        
        public const string Disable = "Disable";

        
        public const string Enable = "Enable";

        
        public const string Install = "Install";

        
        public const string Invoke = "Invoke";

        
        public const string Register = "Register";

        
        public const string Request = "Request";

        
        public const string Restart = "Restart";

        
        public const string Resume = "Resume";

        
        public const string Start = "Start";

        
        public const string Stop = "Stop";

        
        public const string Submit = "Submit";

        
        public const string Suspend = "Suspend";

        
        public const string Uninstall = "Uninstall";

        
        public const string Unregister = "Unregister";

        
        public const string Wait = "Wait";
    }

    
    public static class VerbsDiagnostic
    {
        
        public const string Debug = "Debug";

        
        public const string Measure = "Measure";

        
        public const string Ping = "Ping";

        
        public const string Repair = "Repair";

        
        public const string Resolve = "Resolve";

        
        public const string Test = "Test";

        
        public const string Trace = "Trace";
    }

    
    public static class VerbsCommunications
    {
        
        public const string Connect = "Connect";

        
        public const string Disconnect = "Disconnect";

        
        public const string Read = "Read";

        
        public const string Receive = "Receive";

        
        public const string Send = "Send";

        
        public const string Write = "Write";
    }

    
    public static class VerbsSecurity
    {
        
        public const string Block = "Block";

        
        public const string Grant = "Grant";

        
        public const string Protect = "Protect";

        
        public const string Revoke = "Revoke";

        
        public const string Unblock = "Unblock";

        
        public const string Unprotect = "Unprotect";
    }

    
    public static class VerbsOther
    {
        
        public const string Use = "Use";
    }

    
    internal static class VerbDescriptions
    {
        
        public static string GetVerbDescription(string verb)
        {
            return VerbDescriptionStrings.ResourceManager.GetString(verb);
        }
    }

    
    internal static class VerbAliasPrefixes
    {
        
        public const string Add = "a";

        
        public const string Approve = "ap";

        
        public const string Assert = "as";

        
        public const string Backup = "ba";

        
        public const string Block = "bl";

        
        public const string Build = "bd";

        
        public const string Checkpoint = "ch";

        
        public const string Clear = "cl";

        
        public const string Close = "cs";

        
        public const string Compare = "cr";

        
        public const string Complete = "cmp";

        
        public const string Compress = "cm";

        
        public const string Confirm = "cn";

        
        public const string Connect = "cc";

        
        public const string Convert = "cv";

        
        public const string ConvertFrom = "cf";

        
        public const string ConvertTo = "ct";

        
        public const string Copy = "cp";

        
        public const string Debug = "db";

        
        public const string Deny = "dn";

        
        public const string Deploy = "dp";

        
        public const string Disable = "d";

        
        public const string Disconnect = "dc";

        
        public const string Dismount = "dm";

        
        public const string Edit = "ed";

        
        public const string Enable = "e";

        
        public const string Enter = "et";

        
        public const string Exit = "ex";

        
        public const string Expand = "en";

        
        public const string Export = "ep";

        
        public const string Find = "fd";

        
        public const string Format = "f";

        
        public const string Get = "g";

        
        public const string Grant = "gr";

        
        public const string Group = "gp";

        
        public const string Hide = "h";

        
        public const string Import = "ip";

        
        public const string Initialize = "in";

        
        public const string Install = "is";

        
        public const string Invoke = "i";

        
        public const string Join = "j";

        
        public const string Limit = "l";

        
        public const string Lock = "lk";

        
        public const string Measure = "ms";

        
        public const string Merge = "mg";

        
        public const string Mount = "mt";

        
        public const string Move = "m";

        
        public const string New = "n";

        
        public const string Open = "op";

        
        public const string Optimize = "om";

        
        public const string Out = "o";

        
        public const string Ping = "pi";

        
        public const string Pop = "pop";

        
        public const string Protect = "pt";

        
        public const string Publish = "pb";

        
        public const string Push = "pu";

        
        public const string Read = "rd";

        
        public const string Receive = "rc";

        
        public const string Redo = "re";

        
        public const string Register = "rg";

        
        public const string Remove = "r";

        
        public const string Rename = "rn";

        
        public const string Repair = "rp";

        
        public const string Request = "rq";

        
        public const string Reset = "rs";

        
        public const string Resize = "rz";

        
        public const string Resolve = "rv";

        
        public const string Restart = "rt";

        
        public const string Restore = "rr";

        
        public const string Resume = "ru";

        
        public const string Revoke = "rk";

        
        public const string Save = "sv";

        
        public const string Search = "sr";

        
        public const string Select = "sc";

        
        public const string Send = "sd";

        
        public const string Set = "s";

        
        public const string Show = "sh";

        
        public const string Sync = "sy";

        
        public const string Skip = "sk";

        
        public const string Split = "sl";

        
        public const string Start = "sa";

        
        public const string Step = "st";

        
        public const string Stop = "sp";

        
        public const string Submit = "sb";

        
        public const string Suspend = "ss";

        
        public const string Switch = "sw";

        
        public const string Test = "t";

        
        public const string Trace = "tr";

        
        public const string Unblock = "ul";

        
        public const string Undo = "un";

        
        public const string Uninstall = "us";

        
        public const string Unlock = "uk";

        
        public const string Unprotect = "up";

        
        public const string Unpublish = "ub";

        
        public const string Unregister = "ur";

        
        public const string Update = "ud";

        
        public const string Use = "u";

        
        public const string Wait = "w";

        
        public const string Watch = "wc";

        
        public const string Write = "wr";

        
        public static string GetVerbAliasPrefix(string verb)
        {
            FieldInfo aliasField = typeof(VerbAliasPrefixes).GetField(verb);
            if (aliasField != null)
            {
                return (string)aliasField.GetValue(null);
            }
            else
            {
                return string.Empty;
            }
        }
    }

    
    public class VerbInfo
    {
        
        public string Verb
        {
            get; set;
        }

        
        public string AliasPrefix
        {
            get; set;
        }

        
        public string Group
        {
            get; set;
        }

        
        public string Description
        {
            get; set;
        }
    }

    internal static class Verbs
    {
        static Verbs()
        {
            foreach (Type type in VerbTypes)
            {
                foreach (FieldInfo field in type.GetFields())
                {
                    if (field.IsLiteral)
                    {
                        s_validVerbs.Add((string)field.GetValue(null), true);
                    }
                }
            }

            s_recommendedAlternateVerbs.Add("accept", new string[] { "Receive" });
            s_recommendedAlternateVerbs.Add("acquire", new string[] { "Get", "Read" });
            s_recommendedAlternateVerbs.Add("allocate", new string[] { "New" });
            s_recommendedAlternateVerbs.Add("allow", new string[] { "Enable", "Grant", "Unblock" });
            s_recommendedAlternateVerbs.Add("amend", new string[] { "Edit" });
            s_recommendedAlternateVerbs.Add("analyze", new string[] { "Measure", "Test" });
            s_recommendedAlternateVerbs.Add("append", new string[] { "Add" });
            s_recommendedAlternateVerbs.Add("assign", new string[] { "Set" });
            s_recommendedAlternateVerbs.Add("associate", new string[] { "Join", "Merge" });
            s_recommendedAlternateVerbs.Add("attach", new string[] { "Add", "Debug" });
            s_recommendedAlternateVerbs.Add("bc", new string[] { "Compare" });
            s_recommendedAlternateVerbs.Add("boot", new string[] { "Start" });
            s_recommendedAlternateVerbs.Add("break", new string[] { "Disconnect" });
            s_recommendedAlternateVerbs.Add("broadcast", new string[] { "Send" });
            s_recommendedAlternateVerbs.Add("burn", new string[] { "Backup" });
            s_recommendedAlternateVerbs.Add("calculate", new string[] { "Measure" });
            s_recommendedAlternateVerbs.Add("cancel", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("cat", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("change", new string[] { "Convert", "Edit", "Rename" });
            s_recommendedAlternateVerbs.Add("clean", new string[] { "Uninstall" });
            s_recommendedAlternateVerbs.Add("clone", new string[] { "Copy" });
            s_recommendedAlternateVerbs.Add("combine", new string[] { "Join", "Merge" });
            s_recommendedAlternateVerbs.Add("compact", new string[] { "Compress" });
            s_recommendedAlternateVerbs.Add("compile", new string[] { "Build" });
            s_recommendedAlternateVerbs.Add("concatenate", new string[] { "Add" });
            s_recommendedAlternateVerbs.Add("configure", new string[] { "Set" });
            s_recommendedAlternateVerbs.Add("create", new string[] { "New" });
            s_recommendedAlternateVerbs.Add("cut", new string[] { "Remove" });
            // recommendedAlternateVerbs.Add("debug",      new string[] {"Ping"});
            s_recommendedAlternateVerbs.Add("delete", new string[] { "Remove" });
            s_recommendedAlternateVerbs.Add("detach", new string[] { "Dismount", "Remove" });
            s_recommendedAlternateVerbs.Add("determine", new string[] { "Measure", "Resolve" });
            s_recommendedAlternateVerbs.Add("diagnose", new string[] { "Debug", "Test" });
            s_recommendedAlternateVerbs.Add("diff", new string[] { "Checkpoint", "Compare" });
            s_recommendedAlternateVerbs.Add("difference", new string[] { "Checkpoint", "Compare" });
            s_recommendedAlternateVerbs.Add("dig", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("dir", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("discard", new string[] { "Remove" });
            s_recommendedAlternateVerbs.Add("display", new string[] { "Show", "Write" });
            s_recommendedAlternateVerbs.Add("dispose", new string[] { "Remove" });
            s_recommendedAlternateVerbs.Add("divide", new string[] { "Split" });
            s_recommendedAlternateVerbs.Add("dump", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("duplicate", new string[] { "Copy" });
            s_recommendedAlternateVerbs.Add("empty", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("end", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("erase", new string[] { "Clear", "Remove" });
            s_recommendedAlternateVerbs.Add("examine", new string[] { "Get" });
            s_recommendedAlternateVerbs.Add("execute", new string[] { "Invoke" });
            s_recommendedAlternateVerbs.Add("explode", new string[] { "Expand" });
            s_recommendedAlternateVerbs.Add("extract", new string[] { "Export" });
            s_recommendedAlternateVerbs.Add("fix", new string[] { "Repair", "Restore" });
            s_recommendedAlternateVerbs.Add("flush", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("follow", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("generate", new string[] { "New" });
            // recommendedAlternateVerbs.Add("get",        new string[] {"Read"});
            s_recommendedAlternateVerbs.Add("halt", new string[] { "Disable" });
            s_recommendedAlternateVerbs.Add("in", new string[] { "ConvertTo" });
            s_recommendedAlternateVerbs.Add("index", new string[] { "Update" });
            s_recommendedAlternateVerbs.Add("initiate", new string[] { "Start" });
            s_recommendedAlternateVerbs.Add("input", new string[] { "ConvertTo", "Unregister" });
            s_recommendedAlternateVerbs.Add("insert", new string[] { "Add", "Unregister" });
            s_recommendedAlternateVerbs.Add("inspect", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("kill", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("launch", new string[] { "Start" });
            s_recommendedAlternateVerbs.Add("load", new string[] { "Import" });
            s_recommendedAlternateVerbs.Add("locate", new string[] { "Search", "Select" });
            s_recommendedAlternateVerbs.Add("logoff", new string[] { "Disconnect" });
            s_recommendedAlternateVerbs.Add("mail", new string[] { "Send" });
            s_recommendedAlternateVerbs.Add("make", new string[] { "New" });
            s_recommendedAlternateVerbs.Add("match", new string[] { "Select" });
            s_recommendedAlternateVerbs.Add("migrate", new string[] { "Move" });
            s_recommendedAlternateVerbs.Add("modify", new string[] { "Edit" });
            s_recommendedAlternateVerbs.Add("name", new string[] { "Move" });
            s_recommendedAlternateVerbs.Add("nullify", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("obtain", new string[] { "Get" });
            // recommendedAlternateVerbs.Add("out",        new string[] {"ConvertFrom"});
            s_recommendedAlternateVerbs.Add("output", new string[] { "ConvertFrom" });
            s_recommendedAlternateVerbs.Add("pause", new string[] { "Suspend", "Wait" });
            s_recommendedAlternateVerbs.Add("peek", new string[] { "Receive" });
            s_recommendedAlternateVerbs.Add("permit", new string[] { "Enable" });
            s_recommendedAlternateVerbs.Add("purge", new string[] { "Clear", "Remove" });
            s_recommendedAlternateVerbs.Add("pick", new string[] { "Select" });
            // recommendedAlternateVerbs.Add("pop",        new string[] {"Enter", "Exit"});
            s_recommendedAlternateVerbs.Add("prevent", new string[] { "Block" });
            s_recommendedAlternateVerbs.Add("print", new string[] { "Write" });
            s_recommendedAlternateVerbs.Add("prompt", new string[] { "Read" });
            // recommendedAlternateVerbs.Add("push",       new string[] {"Enter", "Exit"});
            s_recommendedAlternateVerbs.Add("put", new string[] { "Send", "Write" });
            s_recommendedAlternateVerbs.Add("puts", new string[] { "Write" });
            s_recommendedAlternateVerbs.Add("quota", new string[] { "Limit" });
            s_recommendedAlternateVerbs.Add("quote", new string[] { "Limit" });
            s_recommendedAlternateVerbs.Add("rebuild", new string[] { "Initialize" });
            s_recommendedAlternateVerbs.Add("recycle", new string[] { "Restart" });
            s_recommendedAlternateVerbs.Add("refresh", new string[] { "Update" });
            s_recommendedAlternateVerbs.Add("reinitialize", new string[] { "Initialize" });
            s_recommendedAlternateVerbs.Add("release", new string[] { "Clear", "Install", "Publish", "Unlock" });
            s_recommendedAlternateVerbs.Add("reload", new string[] { "Update" });
            s_recommendedAlternateVerbs.Add("renew", new string[] { "Initialize", "Update" });
            s_recommendedAlternateVerbs.Add("replicate", new string[] { "Copy" });
            s_recommendedAlternateVerbs.Add("resample", new string[] { "Convert" });
            // recommendedAlternateVerbs.Add("reset",      new string[] {"Set"});
            // recommendedAlternateVerbs.Add("resize",     new string[] {"Convert"});
            s_recommendedAlternateVerbs.Add("restrict", new string[] { "Lock" });
            s_recommendedAlternateVerbs.Add("return", new string[] { "Repair", "Restore" });
            s_recommendedAlternateVerbs.Add("revert", new string[] { "Unpublish" });
            s_recommendedAlternateVerbs.Add("revise", new string[] { "Edit" });
            s_recommendedAlternateVerbs.Add("run", new string[] { "Invoke", "Start" });
            s_recommendedAlternateVerbs.Add("salvage", new string[] { "Test" });
            // recommendedAlternateVerbs.Add("save",       new string[] {"Backup"});
            s_recommendedAlternateVerbs.Add("secure", new string[] { "Lock" });
            s_recommendedAlternateVerbs.Add("separate", new string[] { "Split" });
            s_recommendedAlternateVerbs.Add("setup", new string[] { "Initialize", "Install" });
            s_recommendedAlternateVerbs.Add("sleep", new string[] { "Suspend", "Wait" });
            s_recommendedAlternateVerbs.Add("starttransaction", new string[] { "Checkpoint" });
            s_recommendedAlternateVerbs.Add("telnet", new string[] { "Connect" });
            s_recommendedAlternateVerbs.Add("terminate", new string[] { "Stop" });
            s_recommendedAlternateVerbs.Add("track", new string[] { "Trace" });
            s_recommendedAlternateVerbs.Add("transfer", new string[] { "Move" });
            s_recommendedAlternateVerbs.Add("type", new string[] { "Get" });
            // recommendedAlternateVerbs.Add("undo",       new string[] {"Repair", "Restore"});
            s_recommendedAlternateVerbs.Add("unite", new string[] { "Join", "Merge" });
            s_recommendedAlternateVerbs.Add("unlink", new string[] { "Dismount" });
            s_recommendedAlternateVerbs.Add("unmark", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("unrestrict", new string[] { "Unlock" });
            s_recommendedAlternateVerbs.Add("unsecure", new string[] { "Unlock" });
            s_recommendedAlternateVerbs.Add("unset", new string[] { "Clear" });
            s_recommendedAlternateVerbs.Add("verify", new string[] { "Test" });

#if DEBUG
            foreach (KeyValuePair<string, string[]> entry in s_recommendedAlternateVerbs)
            {
                Dbg.Assert(!IsStandard(entry.Key), "prohibited verb is standard");
                foreach (string suggested in entry.Value)
                {
                    Dbg.Assert(IsStandard(suggested), "suggested verb is not standard");
                }
            }
#endif
        }

        
        private static Type[] VerbTypes => new Type[] {
            typeof(VerbsCommon),
            typeof(VerbsCommunications),
            typeof(VerbsData),
            typeof(VerbsDiagnostic),
            typeof(VerbsLifecycle),
            typeof(VerbsOther),
            typeof(VerbsSecurity)
        };

        
        private static string GetVerbGroupDisplayName(Type verbType) => verbType.Name.Substring(5);

        
        internal static IEnumerable<VerbInfo> FilterByVerbsAndGroups(string[] verbs, string[] groups)
        {
            if (groups is null || groups.Length == 0)
            {
                foreach (Type verbType in VerbTypes)
                {
                    foreach (VerbInfo verb in FilterVerbsByType(verbs, verbType))
                    {
                        yield return verb;
                    }
                }

                yield break;
            }

            foreach (Type verbType in VerbTypes)
            {
                if (GroupsContainVerbType(groups, verbType))
                {
                    foreach (VerbInfo verb in FilterVerbsByType(verbs, verbType))
                    {
                        yield return verb;
                    }
                }
            }
        }

        
        private static bool GroupsContainVerbType(string[] groups, Type verbType)
            => SessionStateUtilities.CollectionContainsValue(
                groups,
                GetVerbGroupDisplayName(verbType),
                StringComparer.OrdinalIgnoreCase);

        
        private static IEnumerable<string> EnumerateFieldNamesFromVerbType(Type verbType)
        {
            foreach (FieldInfo field in verbType.GetFields())
            {
                if (field.IsLiteral)
                {
                    yield return field.Name;
                }
            }
        }

        
        private static IEnumerable<string> EnumerateFieldNamesFromAllVerbTypes()
        {
            foreach (Type verbType in VerbTypes)
            {
                foreach (string fieldName in EnumerateFieldNamesFromVerbType(verbType))
                {
                    yield return fieldName;
                }
            }
        }

        
        private static IEnumerable<string> EnumerateCommandVerbNames(Collection<CmdletInfo> commands)
        {
            foreach (CmdletInfo command in commands)
            {
                yield return command.Verb;
            }
        }

        
        private static IEnumerable<VerbInfo> FilterVerbsByType(string[] verbs, Type verbType)
        {
            if (verbs is null || verbs.Length == 0)
            {
                foreach (string fieldName in EnumerateFieldNamesFromVerbType(verbType))
                {
                    yield return CreateVerbFromField(fieldName, verbType);
                }

                yield break;
            }

            Collection<WildcardPattern> verbPatterns = SessionStateUtilities.CreateWildcardsFromStrings(
                verbs,
                WildcardOptions.IgnoreCase);

            foreach (string fieldName in EnumerateFieldNamesFromVerbType(verbType))
            {
                if (SessionStateUtilities.MatchesAnyWildcardPattern(
                        fieldName,
                        verbPatterns,
                        defaultValue: false))
                {
                    yield return CreateVerbFromField(fieldName, verbType);
                }
            }
        }

        
        private static VerbInfo CreateVerbFromField(string fieldName, Type verbType) => new()
        {
            Verb = fieldName,
            AliasPrefix = VerbAliasPrefixes.GetVerbAliasPrefix(fieldName),
            Group = GetVerbGroupDisplayName(verbType),
            Description = VerbDescriptions.GetVerbDescription(fieldName)
        };

        
        public class VerbArgumentCompleter : IArgumentCompleter
        {
            
            public IEnumerable<CompletionResult> CompleteArgument(
                string commandName,
                string parameterName,
                string wordToComplete,
                CommandAst commandAst,
                IDictionary fakeBoundParameters)
            {
                // Completion: Get-Verb -Group <group> -Verb <wordToComplete>
                if (commandName.Equals("Get-Verb", StringComparison.OrdinalIgnoreCase)
                    && fakeBoundParameters.Contains("Group"))
                {
                    string[] groups = null;

                    object groupParameterValue = fakeBoundParameters["Group"];
                    Type groupParameterValueType = groupParameterValue.GetType();

                    if (groupParameterValueType == typeof(string))
                    {
                        groups = new string[] { groupParameterValue.ToString() };
                    }

                    else if (groupParameterValueType.IsArray
                             && groupParameterValueType.GetElementType() == typeof(object))
                    {
                        groups = Array.ConvertAll((object[])groupParameterValue, group => group.ToString());
                    }

                    return CompleteVerbWithGroups(wordToComplete, groups);
                }

                // Completion: Get-Command -Noun <noun> -Verb <wordToComplete>
                else if (commandName.Equals("Get-Command", StringComparison.OrdinalIgnoreCase)
                         && fakeBoundParameters.Contains("Noun"))
                {
                    using var ps = PowerShell.Create(RunspaceMode.CurrentRunspace);

                    var commandInfo = new CmdletInfo("Get-Command", typeof(GetCommandCommand));

                    ps.AddCommand(commandInfo);
                    ps.AddParameter("Noun", fakeBoundParameters["Noun"]);

                    if (fakeBoundParameters.Contains("Module"))
                    {
                        ps.AddParameter("Module", fakeBoundParameters["Module"]);
                    }

                    Collection<CmdletInfo> commands = ps.Invoke<CmdletInfo>();

                    return CompleteVerbWithCommands(wordToComplete, commands);
                }

                // Complete all verbs by default if above cases not completed
                return CompleteVerbForAllTypes(wordToComplete);
            }

            
            private static IEnumerable<CompletionResult> CompleteVerbWithGroups(string wordToComplete, string[] groups)
            {
                foreach (Type verbType in VerbTypes)
                {
                    if (GroupsContainVerbType(groups, verbType))
                    {
                        foreach (CompletionResult result in CompletionHelpers.GetMatchingResults(
                            wordToComplete,
                            possibleCompletionValues: EnumerateFieldNamesFromVerbType(verbType)))
                        {
                            yield return result;
                        }
                    }
                }
            }

            
            private static IEnumerable<CompletionResult> CompleteVerbWithCommands(string wordToComplete, Collection<CmdletInfo> commands)
                => CompletionHelpers.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: EnumerateCommandVerbNames(commands));

            
            private static IEnumerable<CompletionResult> CompleteVerbForAllTypes(string wordToComplete)
                => CompletionHelpers.GetMatchingResults(
                    wordToComplete,
                    possibleCompletionValues: EnumerateFieldNamesFromAllVerbTypes());
        }

        private static readonly Dictionary<string, bool> s_validVerbs = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, string[]> s_recommendedAlternateVerbs = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        internal static bool IsStandard(string verb)
        {
            return s_validVerbs.ContainsKey(verb);
        }

        internal static string[] SuggestedAlternates(string verb)
        {
            string[] result = null;
            s_recommendedAlternateVerbs.TryGetValue(verb, out result);
            return result;
        }
    }
    #endregion VERBS
}
