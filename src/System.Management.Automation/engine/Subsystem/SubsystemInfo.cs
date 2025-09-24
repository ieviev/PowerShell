// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation.Internal;

namespace System.Management.Automation.Subsystem
{
    
    public abstract class SubsystemInfo
    {
        #region "Metadata of a Subsystem (public)"

        
        public SubsystemKind Kind { get; }

        
        public Type SubsystemType { get; }

        
        public bool AllowUnregistration { get; private set; }

        
        public bool AllowMultipleRegistration { get; private set; }

        
        public ReadOnlyCollection<string> RequiredCmdlets { get; private set; }

        
        public ReadOnlyCollection<string> RequiredFunctions { get; private set; }

        // 
        // public ReadOnlyCollection<SubsystemKind> DependsOn { get; private set; }

        #endregion

        #region "State of a Subsystem (public)"

        
        public bool IsRegistered => _cachedImplInfos.Count > 0;

        
        public ReadOnlyCollection<ImplementationInfo> Implementations => _cachedImplInfos;

        #endregion

        #region "private/internal instance members"

        private protected readonly object _syncObj;
        private protected ReadOnlyCollection<ImplementationInfo> _cachedImplInfos;

        private protected SubsystemInfo(SubsystemKind kind, Type subsystemType)
        {
            _syncObj = new object();
            _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();

            Kind = kind;
            SubsystemType = subsystemType;
            AllowUnregistration = false;
            AllowMultipleRegistration = false;
            RequiredCmdlets = Utils.EmptyReadOnlyCollection<string>();
            RequiredFunctions = Utils.EmptyReadOnlyCollection<string>();
        }

        private protected abstract void AddImplementation(ISubsystem rawImpl);

        private protected abstract ISubsystem RemoveImplementation(Guid id);

        internal void RegisterImplementation(ISubsystem impl)
        {
            AddImplementation(impl);
            
        }

        internal ISubsystem UnregisterImplementation(Guid id)
        {
            return RemoveImplementation(id);
        }

        #endregion

        #region "Static factory overloads"

        internal static SubsystemInfo Create<TConcreteSubsystem>(SubsystemKind kind)
            where TConcreteSubsystem : class, ISubsystem
        {
            return new SubsystemInfoImpl<TConcreteSubsystem>(kind);
        }

        internal static SubsystemInfo Create<TConcreteSubsystem>(
            SubsystemKind kind,
            bool allowUnregistration,
            bool allowMultipleRegistration) where TConcreteSubsystem : class, ISubsystem
        {
            return new SubsystemInfoImpl<TConcreteSubsystem>(kind)
            {
                AllowUnregistration = allowUnregistration,
                AllowMultipleRegistration = allowMultipleRegistration,
            };
        }

        internal static SubsystemInfo Create<TConcreteSubsystem>(
            SubsystemKind kind,
            bool allowUnregistration,
            bool allowMultipleRegistration,
            ReadOnlyCollection<string> requiredCmdlets,
            ReadOnlyCollection<string> requiredFunctions) where TConcreteSubsystem : class, ISubsystem
        {
            if (allowMultipleRegistration &&
                (requiredCmdlets.Count > 0 || requiredFunctions.Count > 0))
            {
                throw new ArgumentException(
                    StringUtil.Format(
                        SubsystemStrings.InvalidSubsystemInfo,
                        kind.ToString()));
            }

            return new SubsystemInfoImpl<TConcreteSubsystem>(kind)
            {
                AllowUnregistration = allowUnregistration,
                AllowMultipleRegistration = allowMultipleRegistration,
                RequiredCmdlets = requiredCmdlets,
                RequiredFunctions = requiredFunctions,
            };
        }

        #endregion

        #region "ImplementationInfo"

        
        public class ImplementationInfo
        {
            internal ImplementationInfo(SubsystemKind kind, ISubsystem implementation)
            {
                Id = implementation.Id;
                Kind = kind;
                Name = implementation.Name;
                Description = implementation.Description;
                ImplementationType = implementation.GetType();
            }

            
            public Guid Id { get; }

            
            public SubsystemKind Kind { get; }

            
            public string Name { get; }

            
            public string Description { get; }

            
            public Type ImplementationType { get; }
        }

        #endregion
    }

    internal sealed class SubsystemInfoImpl<TConcreteSubsystem> : SubsystemInfo
        where TConcreteSubsystem : class, ISubsystem
    {
        private ReadOnlyCollection<TConcreteSubsystem> _registeredImpls;

        internal SubsystemInfoImpl(SubsystemKind kind)
            : base(kind, typeof(TConcreteSubsystem))
        {
            _registeredImpls = Utils.EmptyReadOnlyCollection<TConcreteSubsystem>();
        }

        
        /// <remarks>
        /// In the subsystem scenario, registration operations will be minimum, and in most cases, the registered
        /// implementation will never be unregistered, so optimization for reading is more important.
        /// </remarks>
        /// <param name="rawImpl">The subsystem implementation to be added.</param>
        private protected override void AddImplementation(ISubsystem rawImpl)
        {
            lock (_syncObj)
            {
                var impl = (TConcreteSubsystem)rawImpl;

                if (_registeredImpls.Count == 0)
                {
                    _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(new[] { impl });
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(new[] { new ImplementationInfo(Kind, impl) });
                    return;
                }

                if (!AllowMultipleRegistration)
                {
                    throw new InvalidOperationException(
                        StringUtil.Format(
                            SubsystemStrings.MultipleRegistrationNotAllowed,
                            Kind.ToString()));
                }

                foreach (TConcreteSubsystem item in _registeredImpls)
                {
                    if (item.Id == impl.Id)
                    {
                        throw new InvalidOperationException(
                            StringUtil.Format(
                                SubsystemStrings.ImplementationAlreadyRegistered,
                                impl.Id,
                                Kind.ToString()));
                    }
                }

                int newCapacity = _registeredImpls.Count + 1;
                var implList = new List<TConcreteSubsystem>(newCapacity);
                implList.AddRange(_registeredImpls);
                implList.Add(impl);

                var implInfo = new List<ImplementationInfo>(newCapacity);
                implInfo.AddRange(_cachedImplInfos);
                implInfo.Add(new ImplementationInfo(Kind, impl));

                _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(implList);
                _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(implInfo);
            }
        }

        
        /// <remarks>
        /// In the subsystem scenario, registration operations will be minimum, and in most cases, the registered
        /// implementation will never be unregistered, so optimization for reading is more important.
        /// </remarks>
        /// <param name="id">The id of the subsystem implementation to be removed.</param>
        /// <returns>The subsystem implementation that was removed.</returns>
        private protected override ISubsystem RemoveImplementation(Guid id)
        {
            if (!AllowUnregistration)
            {
                throw new InvalidOperationException(
                    StringUtil.Format(
                        SubsystemStrings.UnregistrationNotAllowed,
                        Kind.ToString()));
            }

            lock (_syncObj)
            {
                if (_registeredImpls.Count == 0)
                {
                    throw new InvalidOperationException(
                        StringUtil.Format(
                            SubsystemStrings.NoImplementationRegistered,
                            Kind.ToString()));
                }

                int index = -1;
                for (int i = 0; i < _registeredImpls.Count; i++)
                {
                    if (_registeredImpls[i].Id == id)
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    throw new InvalidOperationException(
                        StringUtil.Format(
                            SubsystemStrings.ImplementationNotFound,
                            id.ToString()));
                }

                ISubsystem target = _registeredImpls[index];
                if (_registeredImpls.Count == 1)
                {
                    _registeredImpls = Utils.EmptyReadOnlyCollection<TConcreteSubsystem>();
                    _cachedImplInfos = Utils.EmptyReadOnlyCollection<ImplementationInfo>();
                }
                else
                {
                    int newCapacity = _registeredImpls.Count - 1;
                    var implList = new List<TConcreteSubsystem>(newCapacity);
                    var implInfo = new List<ImplementationInfo>(newCapacity);

                    for (int i = 0; i < _registeredImpls.Count; i++)
                    {
                        if (index == i)
                        {
                            continue;
                        }

                        implList.Add(_registeredImpls[i]);
                        implInfo.Add(_cachedImplInfos[i]);
                    }

                    _registeredImpls = new ReadOnlyCollection<TConcreteSubsystem>(implList);
                    _cachedImplInfos = new ReadOnlyCollection<ImplementationInfo>(implInfo);
                }

                return target;
            }
        }

        internal TConcreteSubsystem? GetImplementation()
        {
            var localRef = _registeredImpls;
            return localRef.Count > 0 ? localRef[localRef.Count - 1] : null;
        }

        internal ReadOnlyCollection<TConcreteSubsystem> GetAllImplementations()
        {
            return _registeredImpls;
        }
    }
}
