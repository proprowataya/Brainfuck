#pragma warning disable 0660 // 'class' defines operator == or operator != but does not override Object.Equals(object o)
#pragma warning disable 0661 // 'class' defines operator == or operator != but does not override Object.GetHashCode()

using System.Runtime.CompilerServices;

namespace Brainfuck.Core
{
    internal struct UInt8
    {
        public byte Value { get; }
        public UInt8(byte value) => Value = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte(UInt8 x) => x.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator char(UInt8 x) => (char)x.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt8(byte x) => new UInt8(x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator UInt8(int x) => new UInt8((byte)x);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt8 operator +(UInt8 x, UInt8 y) => new UInt8((byte)(x.Value + y.Value));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(UInt8 x, UInt8 y) => x.Value == y.Value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(UInt8 x, UInt8 y) => x.Value != y.Value;

        public override string ToString() => Value.ToString();
    }
}
