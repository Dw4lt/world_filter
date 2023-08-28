/*
    MIT License

    Copyright (c) 2020 skzk

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in all
    copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/

using System.Text;

namespace WorldFilter {
    /// <summary>
    /// A BinaryReader for big endian Data.
    /// </summary>
    public class BigEndianBinaryReader : BinaryReader {
        public BigEndianBinaryReader(Stream input)
            : base(input) { }

        public BigEndianBinaryReader(Stream input, Encoding encoding)
            : base(input, encoding) { }

        public BigEndianBinaryReader(Stream input, Encoding encoding, bool leaveOpen)
            : base(input, encoding, leaveOpen) { }

        public override short ReadInt16() {
            var data = base.ReadBytes(sizeof(short));
            Array.Reverse(data);
            return BitConverter.ToInt16(data, 0);
        }

        public override ushort ReadUInt16() {
            var data = base.ReadBytes(sizeof(ushort));
            Array.Reverse(data);
            return BitConverter.ToUInt16(data, 0);
        }

        public override int ReadInt32() {
            var data = base.ReadBytes(sizeof(int));
            Array.Reverse(data);
            return BitConverter.ToInt32(data, 0);
        }

        public override uint ReadUInt32() {
            var data = base.ReadBytes(sizeof(uint));
            Array.Reverse(data);
            return BitConverter.ToUInt32(data, 0);
        }

        public override long ReadInt64() {
            var data = base.ReadBytes(sizeof(long));
            Array.Reverse(data);
            return BitConverter.ToInt64(data, 0);
        }

        public override ulong ReadUInt64() {
            var data = base.ReadBytes(sizeof(ulong));
            Array.Reverse(data);
            return BitConverter.ToUInt64(data, 0);
        }

        public override float ReadSingle() {
            var data = base.ReadBytes(sizeof(float));
            Array.Reverse(data);
            return BitConverter.ToSingle(data, 0);
        }

        public override double ReadDouble() {
            var data = base.ReadBytes(sizeof(double));
            Array.Reverse(data);
            return BitConverter.ToDouble(data, 0);
        }

        public override string ReadString() {
            var length = ReadUInt16();
            return Encoding.UTF8.GetString(ReadBytes(length));
        }

        internal object Read<T>() {
            // method for simplifying reading array types.

            switch (Type.GetTypeCode(typeof(T))) {
                case TypeCode.SByte: { return ReadSByte(); }
                case TypeCode.Int32: { return ReadInt32(); }
                case TypeCode.Int64: { return ReadInt64(); }
                default: throw new NotSupportedException();
            }
        }

    }
}
