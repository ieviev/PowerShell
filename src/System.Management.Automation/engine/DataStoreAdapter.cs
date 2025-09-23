// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#pragma warning disable 1634, 1691

using System.Threading;
using Dbg = System.Management.Automation;

namespace System.Management.Automation
{
    
    /// <remarks>
    /// A cmdlet provider may want to derive from this class to provide their
    /// own public members or to cache information related to the drive. For instance,
    /// if a drive is a connection to a remote machine and making that connection
    /// is expensive, then the provider may want keep a handle to the connection as
    /// a member of their derived <see cref="PSDriveInfo"/> class and use it when
    /// the provider is invoked.
    /// </remarks>
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

        
        /// <param name="path">
        /// The root path to set for the drive.
        /// </param>
        /// <remarks>
        /// This method can only be called during drive
        /// creation. A NotSupportedException if this method
        /// is called outside of drive creation.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="path"/> is null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// If this method gets called any other time except
        /// during drive creation.
        /// </exception>
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

        
        /// <value>
        /// True if the drive is being created and the
        /// root can be modified through the SetRoot method.
        /// False otherwise.
        /// </value>
        internal bool DriveBeingCreated { get; set; }

        
        /// <value></value>
        internal bool IsAutoMounted { get; set; }

        
        internal bool IsAutoMountedManuallyRemoved { get; set; }

        
        internal bool Persist { get; } = false;

        
        internal bool IsNetworkDrive { get; set; } = false;

        
        public string DisplayRoot { get; internal set; } = null;

        
        public bool VolumeSeparatedByColon { get; internal set; } = true;

        #region ctor

        
        /// <param name="driveInfo">
        /// An existing PSDriveInfo object that should be copied to this instance.
        /// </param>
        /// <remarks>
        /// A protected constructor that derived classes can call with an instance
        /// of this class. This allows for easy creation of derived PSDriveInfo objects
        /// which can be created in CmdletProvider's NewDrive method using the PSDriveInfo
        /// that is passed in.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="PSDriveInfo"/> is null.
        /// </exception>
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

        
        /// <param name="name">
        /// The name of the drive.
        /// </param>
        /// <param name="provider">
        /// The name of the provider which implements the functionality
        /// for the root path of the drive.
        /// </param>
        /// <param name="root">
        /// The root path of the drive. For example, the root of a
        /// drive in the file system can be c:\windows\system32
        /// </param>
        /// <param name="description">
        /// The description for the drive.
        /// </param>
        /// <param name="credential">
        /// The credentials under which all operations on the drive should occur.
        /// If null, the current user credential is used.
        /// </param>
        /// <throws>
        /// ArgumentNullException - if <paramref name="name"/>,
        /// <paramref name="provider"/>, or <paramref name="root"/>
        /// is null.
        /// </throws>
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

        
        /// <param name="name">
        /// The name of the drive.
        /// </param>
        /// <param name="provider">
        /// The name of the provider which implements the functionality
        /// for the root path of the drive.
        /// </param>
        /// <param name="root">
        /// The root path of the drive. For example, the root of a
        /// drive in the file system can be c:\windows\system32
        /// </param>
        /// <param name="description">
        /// The description for the drive.
        /// </param>
        /// <param name="credential">
        /// The credentials under which all operations on the drive should occur.
        /// If null, the current user credential is used.
        /// </param>
        /// <param name="displayRoot">
        /// The network path of the drive. This field would be populated only if PSDriveInfo
        /// is targeting the network drive or else this filed is null for local drives.
        /// </param>
        /// <throws>
        /// ArgumentNullException - if <paramref name="name"/>,
        /// <paramref name="provider"/>, or <paramref name="root"/>
        /// is null.
        /// </throws>
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

        
        /// <param name="name">
        /// The name of the drive.
        /// </param>
        /// <param name="provider">
        /// The name of the provider which implements the functionality
        /// for the root path of the drive.
        /// </param>
        /// <param name="root">
        /// The root path of the drive. For example, the root of a
        /// drive in the file system can be c:\windows\system32
        /// </param>
        /// <param name="description">
        /// The description for the drive.
        /// </param>
        /// <param name="credential">
        /// The credentials under which all operations on the drive should occur.
        /// If null, the current user credential is used.
        /// </param>
        /// <param name="persist">
        /// It indicates if the created PSDrive would be
        /// persisted across PowerShell sessions.
        /// </param>
        /// <throws>
        /// ArgumentNullException - if <paramref name="name"/>,
        /// <paramref name="provider"/>, or <paramref name="root"/>
        /// is null.
        /// </throws>
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

        
        /// <returns>
        /// Returns a String that is that name of the drive.
        /// </returns>
        public override string ToString()
        {
            return Name;
        }

        
        /// <value>
        /// True if the drive should be hidden from the user, false
        /// otherwise.
        /// </value>
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

        
        /// <param name="newName">
        /// The new name for the drive.
        /// </param>
        /// <remarks>
        /// This must be internal so that we allow the renaming of drives
        /// via the Core Command API but not through a reference to the
        /// drive object. More goes in to renaming a drive than just modifying
        /// the name in this class.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// If <paramref name="newName"/> is null or empty.
        /// </exception>
        internal void SetName(string newName)
        {
            if (string.IsNullOrEmpty(newName))
            {
                throw PSTraceSource.NewArgumentException(nameof(newName));
            }

            _name = newName;
        }

        
        /// <param name="newProvider">
        /// The new provider for the drive.
        /// </param>
        /// <remarks>
        /// This must be internal so that we allow the renaming of providers.
        /// All drives must be associated with the new provider name and can
        /// be changed using the Core Command API but not through a reference to the
        /// drive object. More goes in to renaming a provider than just modifying
        /// the provider in this class.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        /// If <paramref name="newProvider"/> is null.
        /// </exception>
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

        
        /// <param name="drive">
        /// A PSDriveInfo object to compare.
        /// </param>
        /// <returns>
        /// A signed number indicating the relative values of this instance and object specified.
        /// Return Value: Less than zero        Meaning: This instance is less than object.
        /// Return Value: Zero                  Meaning: This instance is equal to object.
        /// Return Value: Greater than zero     Meaning: This instance is greater than object or object is a null reference.
        /// </returns>
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

        
        /// <param name="obj">
        /// An object to compare.
        /// </param>
        /// <returns>
        /// A signed number indicating the relative values of this
        /// instance and object specified.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// If <paramref name="obj"/> is not a PSDriveInfo instance.
        /// </exception>
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

        
        /// <param name="obj">
        /// An object to compare.
        /// </param>
        /// <returns>
        /// True if the drive names are equal, false otherwise.
        /// </returns>
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

        
        /// <param name="drive">
        /// An object to compare.
        /// </param>
        /// <returns>
        /// True if the drive names are equal, false otherwise.
        /// </returns>
        public bool Equals(PSDriveInfo drive)
        {
            return CompareTo(drive) == 0;
        }

        
        /// <param name="drive1">
        /// The first object to compare to the second.
        /// </param>
        /// <param name="drive2">
        /// The second object to compare to the first.
        /// </param>
        /// <returns>
        /// True if the objects are PSDriveInfo objects and have the same name,
        /// false otherwise.
        /// </returns>
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

        
        /// <param name="drive1">
        /// The first object to compare to the second.
        /// </param>
        /// <param name="drive2">
        /// The second object to compare to the first.
        /// </param>
        /// <returns>
        /// True if the PSDriveInfo objects do not have the same name,
        /// false otherwise.
        /// </returns>
        public static bool operator !=(PSDriveInfo drive1, PSDriveInfo drive2)
        {
            return !(drive1 == drive2);
        }

        
        /// <param name="drive1">
        /// The drive to determine if it is less than the other drive.
        /// </param>
        /// <param name="drive2">
        /// The drive to compare drive1 against.
        /// </param>
        /// <returns>
        /// True if the lexical comparison of drive1's name is less than drive2's name.
        /// </returns>
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

        
        /// <param name="drive1">
        /// The drive to determine if it is greater than the other drive.
        /// </param>
        /// <param name="drive2">
        /// The drive to compare drive1 against.
        /// </param>
        /// <returns>
        /// True if the lexical comparison of drive1's name is greater than drive2's name.
        /// </returns>
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

        
        /// <returns>The result of base.GetHashCode().</returns>
        /// 
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
