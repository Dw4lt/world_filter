// I like C#, but sometimes it's bewildering what it lacks.
using System.Text;

public class BigEndianBinaryWriter : BinaryWriter {
    public BigEndianBinaryWriter(FileStream file, Encoding encoding, bool leaveOpen) : base(file, encoding, leaveOpen) { }

    public BigEndianBinaryWriter(BinaryWriter writer) : base(writer.BaseStream, Encoding.UTF8, leaveOpen: true) { }

    public void WriteInt16BigEndian(short value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteUInt16BigEndian(ushort value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteInt32BigEndian(int value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteInt24BigEndian(int value) {
        base.Write((byte) ((value >> 16) & 0xff));
        base.Write((byte) ((value >> 8) & 0xff));
        base.Write((byte) ((value >> 0) & 0xff));
    }

    public void WriteUInt32BigEndian(uint value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteInt64BigEndian(long value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteUInt64BigEndian(ulong value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteSingleBigEndian(float value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    public void WriteDoubleBigEndian(double value) {
        WriteBigEndian(BitConverter.GetBytes(value));
    }

    private void WriteBigEndian(byte[] bytes) {
        Array.Reverse(bytes);
        base.Write(bytes);
    }
}
