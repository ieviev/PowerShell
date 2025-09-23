

namespace System.Management.Automation.Interpreter
{
    internal sealed class UpdatePositionInstruction : Instruction
    {
        private readonly int _sequencePoint;
        private readonly bool _checkBreakpoints;

        private UpdatePositionInstruction(bool checkBreakpoints, int sequencePoint)
        {
            _checkBreakpoints = checkBreakpoints;
            _sequencePoint = sequencePoint;
        }

        public override int Run(InterpretedFrame frame)
        {
            var functionContext = frame.FunctionContext;
            var context = frame.ExecutionContext;

            functionContext._currentSequencePointIndex = _sequencePoint;
            if (_checkBreakpoints)
            {
                if (context._debuggingMode > 0)
                {
                    context.Debugger.OnSequencePointHit(functionContext);
                }
            }

            return +1;
        }

        public static Instruction Create(int sequencePoint, bool checkBreakpoints)
        {
            return new UpdatePositionInstruction(checkBreakpoints, sequencePoint);
        }
    }
}
