

using System.Collections.Generic;
using System.Globalization;

namespace System.Management.Automation.Interpreter
{
    internal sealed class LoadObjectInstruction : Instruction
    {
        private readonly object _value;

        internal LoadObjectInstruction(object value)
        {
            _value = value;
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = _value;
            return +1;
        }

        public override string ToString()
        {
            return "LoadObject(" + (_value ?? "null") + ")";
        }
    }

    internal sealed class LoadCachedObjectInstruction : Instruction
    {
        private readonly uint _index;

        internal LoadCachedObjectInstruction(uint index)
        {
            _index = index;
        }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = frame.Interpreter._objects[_index];
            return +1;
        }

        public override string ToDebugString(int instructionIndex, object cookie, Func<int, int> labelIndexer, IList<object> objects)
        {
            return string.Format(CultureInfo.InvariantCulture, "LoadCached({0}: {1})", _index, objects[(int)_index]);
        }

        public override string ToString()
        {
            return "LoadCached(" + _index + ")";
        }
    }

    internal sealed class PopInstruction : Instruction
    {
        internal static readonly PopInstruction Instance = new PopInstruction();

        private PopInstruction() { }

        public override int ConsumedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Pop();
            return +1;
        }

        public override string ToString()
        {
            return "Pop()";
        }
    }

    internal sealed class DupInstruction : Instruction
    {
        internal static readonly DupInstruction Instance = new DupInstruction();

        private DupInstruction() { }

        public override int ConsumedStack { get { return 0; } }

        public override int ProducedStack { get { return 1; } }

        public override int Run(InterpretedFrame frame)
        {
            frame.Data[frame.StackIndex++] = frame.Peek();
            return +1;
        }

        public override string ToString()
        {
            return "Dup()";
        }
    }
}
