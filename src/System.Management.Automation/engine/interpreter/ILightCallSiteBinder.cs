

#nullable enable
#if !CLR2
#else
using Microsoft.Scripting.Ast;
#endif

namespace System.Management.Automation.Interpreter
{
    internal interface ILightCallSiteBinder
    {
        bool AcceptsArgumentArray { get; }
    }
}
