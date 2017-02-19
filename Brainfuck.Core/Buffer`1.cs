using System;
using System.Collections.Generic;
using System.Text;

namespace Brainfuck.Core
{
    internal sealed class Buffer<T>
    {
        private struct ArrayElement
        {
            public T Value;
        }

        #region Constants

        public const int DefaultSize = 16;

        #endregion

        private ArrayElement[] _elements;

        public Buffer() : this(DefaultSize)
        { }

        public Buffer(int size)
        {
            _elements = new ArrayElement[size];
        }

        public ref T this[int index]
        {
            get
            {
                EnsureSize(index + 1);
                return ref _elements[index].Value;
            }
        }

        private void EnsureSize(int size)
        {
            if (_elements.Length < size)
            {
                int newSize = Math.Max(_elements.Length * 2, size);
                ArrayElement[] newArray = new ArrayElement[newSize];
                Array.Copy(_elements, newArray, _elements.Length);
                _elements = newArray;
            }
        }
    }
}
