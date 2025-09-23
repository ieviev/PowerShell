// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.ObjectModel;

namespace System.Management.Automation
{
    
    internal class NativeCommandParameterBinderController : ParameterBinderController
    {
        #region ctor

        
        internal NativeCommandParameterBinderController(NativeCommand command)
            : base(command.MyInvocation, command.Context, new NativeCommandParameterBinder(command))
        {
        }

        #endregion ctor

        
        internal string Arguments
        {
            get
            {
                return ((NativeCommandParameterBinder)DefaultParameterBinder).Arguments;
            }
        }

        
        internal string[] ArgumentList
        {
            get
            {
                return ((NativeCommandParameterBinder)DefaultParameterBinder).ArgumentList;
            }
        }

        
        internal NativeArgumentPassingStyle ArgumentPassingStyle
        {
            get
            {
                return ((NativeCommandParameterBinder)DefaultParameterBinder).ArgumentPassingStyle;
            }
        }

        
        internal override bool BindParameter(
            CommandParameterInternal argument,
            ParameterBindingFlags flags)
        {
            Diagnostics.Assert(false, "Unreachable code");

            throw new InvalidOperationException();
        }

        
        internal override Collection<CommandParameterInternal> BindParameters(Collection<CommandParameterInternal> parameters)
        {
            ((NativeCommandParameterBinder)DefaultParameterBinder).BindParameters(parameters);

            Diagnostics.Assert(s_emptyReturnCollection.Count == 0, "This list shouldn't be used for anything as it's shared.");

            return s_emptyReturnCollection;
        }

        private static readonly Collection<CommandParameterInternal> s_emptyReturnCollection = new Collection<CommandParameterInternal>();
    }
}
