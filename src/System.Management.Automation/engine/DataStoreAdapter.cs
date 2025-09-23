// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Threading;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    public class PSDriveInfo : IComparable
    {
        
        [Dbg.TraceSource(
             "PSDriveInfo",
             "The namespace navigation tracer")]
        private static readonly Dbg.PSTraceSource s_tracer =
            Dbg.PSTraceSource.GetTracer("PSDriveInfo",
             "The namespace navigation tracer");

        
        public string CurrentLocation
        {
            get
            {
                return _currentWorkingDirectory;
            }

            set
            {
                _currentWorkingDirectory = value;
            }
        }

        
        private string _currentWorkingDirectory;

        
        public string Name
        {
            get
            {
                return _name;
            }
        }

        
        private string _name;

        
        public ProviderInfo Provider
        {
            get
            {
                return _provider;
            }
        }

        
        private ProviderInfo _provider;

        
        public string Root
        {
            get
            {
                return _root;
            }

            internal set
            {
                _root = value;
            }
        }

        
        internal void SetRoot(string path)
        {
            if (path == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(path));
            }

            if (!DriveBeingCreated)
            {
                NotSupportedException e =
                    PSTraceSource.NewNotSupportedException();
                throw e;
            }

            _root = path;
        }

        
        private string _root;

        
        public string Description { get; set; }

        
        public long? MaximumSize { get; internal set; }

        
        public PSCredential Credential { get; } = PSCredential.Empty;

        
        internal bool DriveBeingCreated { get; set; }

        
        internal bool IsAutoMounted { get; set; }

        
        internal bool IsAutoMountedManuallyRemoved { get; set; }

        
        internal bool Persist { get; } = false;

        
        internal bool IsNetworkDrive { get; set; } = false;

        
        public string DisplayRoot { get; internal set; } = null;

        
        public bool VolumeSeparatedByColon { get; internal set; } = true;

        #region ctor

        
        protected PSDriveInfo(PSDriveInfo driveInfo)
        {
            if (driveInfo == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(driveInfo));
            }

            _name = driveInfo.Name;
            _provider = driveInfo.Provider;
            Credential = driveInfo.Credential;
            _currentWorkingDirectory = driveInfo.CurrentLocation;
            Description = driveInfo.Description;
            this.MaximumSize = driveInfo.MaximumSize;
            DriveBeingCreated = driveInfo.DriveBeingCreated;
            _hidden = driveInfo._hidden;
            IsAutoMounted = driveInfo.IsAutoMounted;
            _root = driveInfo._root;
            Persist = driveInfo.Persist;
            this.Trace();
        }

        
        public PSDriveInfo(
            string name,
            ProviderInfo provider,
            string root,
            string description,
            PSCredential credential)
        {
            // Verify the parameters

            if (name == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(name));
            }

            if (provider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(provider));
            }

            if (root == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(root));
            }

            // Copy the parameters to the local members

            _name = name;
            _provider = provider;
            _root = root;
            Description = description;

            if (credential != null)
            {
                Credential = credential;
            }

            // Set the current working directory to the empty
            // string since it is relative to the root.

            _currentWorkingDirectory = string.Empty;

            Dbg.Diagnostics.Assert(
                _currentWorkingDirectory != null,
                "The currentWorkingDirectory cannot be null");

            // Trace out the fields

            this.Trace();
        }

        
        public PSDriveInfo(
            string name,
            ProviderInfo provider,
            string root,
            string description,
            PSCredential credential, string displayRoot)
            : this(name, provider, root, description, credential)
        {
            DisplayRoot = displayRoot;
        }

        
        public PSDriveInfo(
            string name,
            ProviderInfo provider,
            string root,
            string description,
            PSCredential credential,
            bool persist)
            : this(name, provider, root, description, credential)
        {
            Persist = persist;
        }

        #endregion ctor

        
        public override string ToString()
        {
            return Name;
        }

        
        internal bool Hidden
        {
            get
            {
                return _hidden;
            }

            set
            {
                _hidden = value;
            }
        }

        
        private bool _hidden;

        
        internal void SetName(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw PSTraceSource.NewArgumentException(nameof(newName));
            }

            _name = newName;
        }

        
        internal void SetProvider(ProviderInfo newProvider)
        {
            if (newProvider == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(newProvider));
            }

            _provider = newProvider;
        }

        
        internal void Trace()
        {
            s_tracer.WriteLine(
                "A drive was found:");

            if (Name != null)
            {
                s_tracer.WriteLine(
                    "\tName: {0}",
                    Name);
            }

            if (Provider != null)
            {
                s_tracer.WriteLine(
                    "\tProvider: {0}",
                    Provider);
            }

            if (Root != null)
            {
                s_tracer.WriteLine(
                    "\tRoot: {0}",
                    Root);
            }

            if (CurrentLocation != null)
            {
                s_tracer.WriteLine(
                    "\tCWD: {0}",
                    CurrentLocation);
            }

            if (Description != null)
            {
                s_tracer.WriteLine(
                    "\tDescription: {0}",
                    Description);
            }
        }

        
        public int CompareTo(PSDriveInfo drive)
        {
#pragma warning disable 56506

            if (drive == null)
            {
                throw PSTraceSource.NewArgumentNullException(nameof(drive));
            }

            return string.Compare(Name, drive.Name, StringComparison.OrdinalIgnoreCase);

#pragma warning restore 56506
        }

        
        public int CompareTo(object obj)
        {
            PSDriveInfo drive = obj as PSDriveInfo;

            if (drive == null)
            {
                ArgumentException e =
                    PSTraceSource.NewArgumentException(
                        nameof(obj),
                        SessionStateStrings.OnlyAbleToComparePSDriveInfo);
                throw e;
            }

            return (CompareTo(drive));
        }

        
        public override bool Equals(object obj)
        {
            if (obj is PSDriveInfo)
            {
                return CompareTo(obj) == 0;
            }
            else
            {
                return false;
            }
        }

        
        public bool Equals(PSDriveInfo drive)
        {
            return CompareTo(drive) == 0;
        }

        
        public static bool operator ==(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            object drive1Object = drive1;
            object drive2Object = drive2;

            if ((drive1Object == null) == (drive2Object == null))
            {
                if (drive1Object != null)
                {
                    return drive1.Equals(drive2);
                }

                return true;
            }
            else
            {
                return false;
            }
        }

        
        public static bool operator !=(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            return !(drive1 == drive2);
        }

        
        public static bool operator <(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            object drive1Object = drive1;
            object drive2Object = drive2;

            if (drive1Object == null)
            {
                return (drive2Object != null);
            }
            else
            {
                if (drive2Object == null)
                {
                    // Since drive1 is not null and drive2 is, drive1 is greater than drive2
                    return false;
                }
                else
                {
                    // Since drive1 and drive2 are not null use the CompareTo

                    return drive1.CompareTo(drive2) < 0;
                }
            }
        }

        
        public static bool operator >(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            object drive1Object = drive1;
            object drive2Object = drive2;

            if ((drive1Object == null))
            {
                // Since both drives are null, they are equal
                // Since drive1 is null it is less than drive2 which is not null
                return false;
            }
            else
            {
                if (drive2Object == null)
                {
                    // Since drive1 is not null and drive2 is, drive1 is greater than drive2
                    return true;
                }
                else
                {
                    // Since drive1 and drive2 are not null use the CompareTo

                    return drive1.CompareTo(drive2) > 0;
                }
            }
        }

        
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        private PSNoteProperty _noteProperty;

        internal PSNoteProperty GetNotePropertyForProviderCmdlets(string name)
        {
            if (_noteProperty == null)
            {
                Interlocked.CompareExchange(ref _noteProperty,
                                            new PSNoteProperty(name, this), null);
            }

            return _noteProperty;
        }
    }
}
