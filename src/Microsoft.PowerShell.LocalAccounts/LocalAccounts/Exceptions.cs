// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Management.Automation;
using System.Management.Automation.SecurityAccountsManager;
using System.Runtime.Serialization;

using Microsoft.PowerShell.LocalAccounts;

namespace Microsoft.PowerShell.Commands
{
    
    public class LocalAccountsException : Exception
    {
#region Public Properties
        
        public ErrorCategory ErrorCategory
        {
            get;
            private set;
        }

        
        public object Target
        {
            get;
            private set;
        }

        
        public string ErrorName
        {
            get
            {
                string exname = "Exception";
                var exlen = exname.Length;
                var name = this.GetType().Name;

                if (name.EndsWith(exname, StringComparison.OrdinalIgnoreCase) && name.Length > exlen)
                    name = name.Substring(0, name.Length - exlen);
                return name;
            }
        }
#endregion Public Properties

        internal LocalAccountsException(string message, object target, ErrorCategory errorCategory)
            : base(message)
        {
            ErrorCategory = errorCategory;
            Target = target;
        }

        
        public LocalAccountsException() : base() { }
        
        public LocalAccountsException(string message) : base(message) { }
        
        public LocalAccountsException(string message, Exception ex) : base(message, ex) { }

        
        protected LocalAccountsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class InternalException : LocalAccountsException
    {
#region Public Properties
        
        public UInt32 StatusCode
        {
            get;
            private set;
        }
#endregion Public Properties

        internal InternalException(UInt32 ntStatus,
                                   string message,
                                   object target,
                                   ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : base(message, target, errorCategory)
        {
            StatusCode = ntStatus;
        }

        internal InternalException(UInt32 ntStatus,
                                   object target,
                                   ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : this(ntStatus,
                   StringUtil.Format(Strings.UnspecifiedErrorNtStatus, ntStatus),
                   target,
                   errorCategory)
        {
        }

        
        public InternalException() : base() { }
        
        public InternalException(string message) : base(message) { }
        
        public InternalException(string message, Exception ex) : base(message, ex) { }
        
        protected InternalException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class Win32InternalException : LocalAccountsException
    {
#region Public Properties
        
        public int NativeErrorCode
        {
            get;
            private set;
        }
#endregion Public Properties

        internal Win32InternalException(int errorCode,
                                        string message,
                                        object target,
                                        ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : base(message, target, errorCategory)
        {
            NativeErrorCode = errorCode;
        }

        internal Win32InternalException(int errorCode,
                                        object target,
                                        ErrorCategory errorCategory = ErrorCategory.NotSpecified)
            : this(errorCode,
                   StringUtil.Format(Strings.UnspecifiedErrorWin32Error, errorCode),
                   target,
                   errorCategory)
        {
        }

        
        public Win32InternalException() : base() {}
        
        public Win32InternalException(string message) : base(message) { }
        
        public Win32InternalException(string message, Exception ex) : base(message, ex) { }
        
        protected Win32InternalException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class InvalidPasswordException : LocalAccountsException
    {
        
        public InvalidPasswordException()
            : base(Strings.InvalidPassword, null, ErrorCategory.InvalidArgument)
        {
        }

        
        public InvalidPasswordException(string message)
            : base(message, null, ErrorCategory.InvalidArgument)
        {
        }

        
        public InvalidPasswordException(uint errorCode)
            : base(StringUtil.GetSystemMessage(errorCode), null, ErrorCategory.InvalidArgument)
        {
        }

        
        public InvalidPasswordException(string message, Exception ex) : base(message, ex) { }
        
        protected InvalidPasswordException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class InvalidParametersException : LocalAccountsException
    {
        
        public InvalidParametersException(string message)
            : base(message, null, ErrorCategory.InvalidArgument)
        {
        }

        internal InvalidParametersException(string parameterA, string parameterB)
            : this(StringUtil.Format(Strings.InvalidParameterPair, parameterA, parameterB))
        {
        }

        
        public InvalidParametersException() : base() { }
        
        public InvalidParametersException(string message, Exception ex) : base(message, ex) { }
        
        protected InvalidParametersException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class AccessDeniedException : LocalAccountsException
    {
        internal AccessDeniedException(object target)
            : base(Strings.AccessDenied, target, ErrorCategory.PermissionDenied)
        {
        }

        
        public AccessDeniedException() : base() { }
        
        public AccessDeniedException(string message) : base(message) { }
        
        public AccessDeniedException(string message, Exception ex) : base(message, ex) { }
        
        protected AccessDeniedException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class InvalidNameException : LocalAccountsException
    {
        internal InvalidNameException(string name, object target)
            : base(StringUtil.Format(Strings.InvalidName, name), target, ErrorCategory.InvalidArgument)
        {
        }

        
        public InvalidNameException() : base() { }
        
        public InvalidNameException(string message) : base(message) { }
        
        public InvalidNameException(string message, Exception ex) : base(message, ex) { }
        
        protected InvalidNameException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class NameInUseException : LocalAccountsException
    {
        internal NameInUseException(string name, object target)
            : base(StringUtil.Format(Strings.NameInUse, name), target, ErrorCategory.InvalidArgument)
        {
        }

        
        public NameInUseException() : base() { }
        
        public NameInUseException(string message) : base(message) { }
        
        public NameInUseException(string message, Exception ex) : base(message, ex) { }
        
        protected NameInUseException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class NotFoundException : LocalAccountsException
    {
        internal NotFoundException(string message, object target)
          : base(message, target, ErrorCategory.ObjectNotFound)
        {
        }

        
        public NotFoundException() : base() { }
        
        public NotFoundException(string message) : base(message) { }
        
        public NotFoundException(string message, Exception ex) : base(message, ex) { }
        
        protected NotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class PrincipalNotFoundException : NotFoundException
    {
        internal PrincipalNotFoundException(string principal, object target)
            : base(StringUtil.Format(Strings.PrincipalNotFound, principal), target)
        {
        }

        
        public PrincipalNotFoundException() : base() { }
        
        public PrincipalNotFoundException(string message) : base(message) { }
        
        public PrincipalNotFoundException(string message, Exception ex) : base(message, ex) { }
        
        protected PrincipalNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class GroupNotFoundException : NotFoundException
    {
        internal GroupNotFoundException(string group, object target)
            : base(StringUtil.Format(Strings.GroupNotFound, group), target)
        {
        }

        
        public GroupNotFoundException() : base() { }
        
        public GroupNotFoundException(string message) : base(message) { }
        
        public GroupNotFoundException(string message, Exception ex) : base(message, ex) { }
        
        protected GroupNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class UserNotFoundException : NotFoundException
    {
        internal UserNotFoundException(string user, object target)
            : base(StringUtil.Format(Strings.UserNotFound, user), target)
        {
        }

        
        public UserNotFoundException() : base() { }
        
        public UserNotFoundException(string message) : base(message) { }
        
        public UserNotFoundException(string message, Exception ex) : base(message, ex) { }
        
        protected UserNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class MemberNotFoundException : NotFoundException
    {
        internal MemberNotFoundException(string member, string group)
            : base(StringUtil.Format(Strings.MemberNotFound, member, group), member)
        {
        }

        
        public MemberNotFoundException() : base() { }
        
        public MemberNotFoundException(string message) : base(message) { }
        
        public MemberNotFoundException(string message, Exception ex) : base(message, ex) { }
        
        protected MemberNotFoundException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class ObjectExistsException : LocalAccountsException
    {
        internal ObjectExistsException(string message, object target)
            : base(message, target, ErrorCategory.ResourceExists)
        {
        }

        
        public ObjectExistsException() : base() { }
        
        public ObjectExistsException(string message) : base(message) { }
        
        public ObjectExistsException(string message, Exception ex) : base(message, ex) { }
        
        protected ObjectExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class GroupExistsException : ObjectExistsException
    {
        internal GroupExistsException(string group, object target)
            : base(StringUtil.Format(Strings.GroupExists, group), target)
        {
        }

        
        public GroupExistsException() : base() { }
        
        public GroupExistsException(string message) : base(message) { }
        
        public GroupExistsException(string message, Exception ex) : base(message, ex) { }
        
        protected GroupExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class UserExistsException : ObjectExistsException
    {
        internal UserExistsException(string user, object target)
            : base(StringUtil.Format(Strings.UserExists, user), target)
        {
        }

        
        public UserExistsException() : base() { }
        
        public UserExistsException(string message) : base(message) { }
        
        public UserExistsException(string message, Exception ex) : base(message, ex) { }
        
        protected UserExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }

    
    public class MemberExistsException : ObjectExistsException
    {
        internal MemberExistsException(string member, string group, object target)
            : base(StringUtil.Format(Strings.MemberExists, member, group), target)
        {
        }

        
        public MemberExistsException() : base() { }
        
        public MemberExistsException(string message) : base(message) { }
        
        public MemberExistsException(string message, Exception ex) : base(message, ex) { }
        
        protected MemberExistsException(SerializationInfo info, StreamingContext ctx) : base(info, ctx) { }
    }
}
