// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Diagnostics.CodeAnalysis;
using System.Management.Automation.Runspaces;

namespace System.Management.Automation.Host
{
    public abstract class PSHost
    {
        internal const int MaximumNestedPromptLevel = 128;

        internal static bool IsStdOutputRedirected;

        protected PSHost()
        {
            // do nothing
        }

        public abstract string Name
        {
            get;
        }

        public abstract System.Version Version
        {
            get;
        }

        public abstract System.Guid InstanceId
        {
            get;
        }

        public abstract System.Management.Automation.Host.PSHostUserInterface UI
        {
            get;
        }

        public abstract System.Globalization.CultureInfo CurrentCulture
        {
            get;
        }

        public abstract System.Globalization.CultureInfo CurrentUICulture
        {
            get;
        }

        public abstract void SetShouldExit(int exitCode);

        public abstract void EnterNestedPrompt();

        public abstract void ExitNestedPrompt();

        public virtual PSObject PrivateData
        {
            get
            {
                return null;
            }
        }

        public abstract void NotifyBeginApplication();

        public abstract void NotifyEndApplication();

        internal bool ShouldSetThreadUILanguageToZero { get; set; }

        public virtual bool DebuggerEnabled
        {
            get { return false; }

            set { throw new PSNotImplementedException(); }
        }
    }

#nullable enable
    public interface IHostSupportsInteractiveSession
    {
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "runspace")]
        void PushRunspace(Runspace runspace);

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        void PopRunspace();

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        bool IsRunspacePushed { get; }

        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Runspace")]
        Runspace? Runspace { get; }
    }
}
