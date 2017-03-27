using System.Collections.Generic;
using System.Collections;

namespace Brainfuck.Core
{
    public delegate void OnStepStartEventHandler(OnStepStartEventArgs args);

    public sealed class OnStepStartEventArgs
    {
        public object Operation { get; }
        public IReadOnlyList<object> Buffer { get; }
        public int Pointer { get; }
        public long Step { get; }

        public OnStepStartEventArgs(object operation, IReadOnlyList<object> buffer, int pointer, long step)
        {
            Operation = operation;
            Buffer = buffer;
            Pointer = pointer;
            Step = step;
        }
    }

    internal sealed class ArrayView<T> : IReadOnlyList<object>
    {
        private readonly T[] _array;

        public ArrayView(T[] array)
        {
            _array = array;
        }

        public object this[int index] => _array[index];
        public int Count => _array.Length;

        public IEnumerator<object> GetEnumerator()
        {
            for (int i = 0; i < _array.Length; i++)
            {
                yield return _array[i];
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
