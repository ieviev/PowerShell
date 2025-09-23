

using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace System.Management.Automation.Interpreter
{
    /// <summary>
    /// A simple forth-style stack machine for executing Expression trees
    /// without the need to compile to IL and then invoke the JIT.  This trades
    /// off much faster compilation time for a slower execution performance.
    /// For code that is only run a small number of times this can be a
    /// sweet spot.
    ///
    /// The core loop in the interpreter is the RunInstructions method.
    /// </summary>
    internal sealed class Interpreter
    {
        internal static readonly object NoValue = new object();

        internal const int RethrowOnReturn = Int32.MaxValue;

        // zero: sync compilation
        // negative: default
        internal readonly int _compilationThreshold;

        internal readonly object[] _objects;
        internal readonly RuntimeLabel[] _labels;

        internal readonly string _name;
        internal readonly DebugInfo[] _debugInfos;

        internal Interpreter(string name, LocalVariables locals, HybridReferenceDictionary<LabelTarget, BranchLabel> labelMapping,
            InstructionArray instructions, DebugInfo[] debugInfos, int compilationThreshold)
        {
            _name = name;
            LocalCount = locals.LocalCount;
            ClosureVariables = locals.ClosureVariables;

            Instructions = instructions;
            _objects = instructions.Objects;
            _labels = instructions.Labels;
            LabelMapping = labelMapping;

            _debugInfos = debugInfos;
            _compilationThreshold = compilationThreshold;
        }

        internal int ClosureSize
        {
            get
            {
                if (ClosureVariables == null)
                {
                    return 0;
                }

                return ClosureVariables.Count;
            }
        }

        internal int LocalCount { get; }

        internal bool CompileSynchronously
        {
            get { return _compilationThreshold <= 1; }
        }

        internal InstructionArray Instructions { get; }

        internal Dictionary<ParameterExpression, LocalVariable> ClosureVariables { get; }

        internal HybridReferenceDictionary<LabelTarget, BranchLabel> LabelMapping { get; }

        /// <summary>
        /// Runs instructions within the given frame.
        /// </summary>
        /// <remarks>
        /// Interpreted stack frames are linked via Parent reference so that each CLR frame of this method corresponds
        /// to an interpreted stack frame in the chain. It is therefore possible to combine CLR stack traces with
        /// interpreted stack traces by aligning interpreted frames to the frames of this method.
        /// Each group of subsequent frames of Run method corresponds to a single interpreted frame.
        /// </remarks>
        [SpecialName, MethodImpl(MethodImplOptions.NoInlining)]
        public void Run(InterpretedFrame frame)
        {
            var instructions = Instructions.Instructions;
            int index = frame.InstructionIndex;
            while (index < instructions.Length)
            {
                index += instructions[index].Run(frame);
                frame.InstructionIndex = index;
            }
        }
    }
}
