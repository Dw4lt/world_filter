using System.Collections;
using System.Diagnostics;
using fNbt;

namespace WorldFilter {

    [DebuggerDisplay("Idx: {Index}   Pos:{UnscaledOffset}   Len:{(int) UnscaledLength}")]
    public struct Location {
        public int UnscaledOffset;
        private int UnpaddedScaledLength;
        public int Index;

        public Location() : this(2, 0, -1) { }

        public Location(int unscaled_offset, byte length, int index) {
            UnscaledOffset = unscaled_offset;
            UnpaddedScaledLength = length * 4096;
            Index = index;
        }

        public readonly long Offset {
            get {
                return UnscaledOffset * 4096;
            }
        }

        public readonly byte UnscaledLength {
            get {
                return (byte) Math.Round(UnpaddedScaledLength / 4096f, MidpointRounding.ToPositiveInfinity);
            }
        }

        public int Length {
            get {
                return UnpaddedScaledLength;
            }
            set {
                UnpaddedScaledLength = value;
            }
        }

        public Location Clone() {
            return new Location(UnscaledOffset, UnscaledLength, Index);
        }

        public void ToBytes(BigEndianBinaryWriter writer) {
            writer.WriteInt24BigEndian(UnscaledOffset);
            writer.Write(UnscaledLength);
        }

        public static Location? FromBytes(BigEndianBinaryReader reader, int index) {
            if (index == 0x1e0) {
                ;
            }
            var oBytes = reader.ReadBytes(3);
            var length = reader.ReadByte();
            if (length > 0) {
                var oBytes4 = new byte[4];
                oBytes.CopyTo(oBytes4, 1);
                Array.Reverse(oBytes4);
                var chunkOffset = BitConverter.ToInt32(oBytes4, 0);
                return new Location(chunkOffset, length, index);
            }
            return null;
        }
    }
}
