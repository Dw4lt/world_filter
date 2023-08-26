using System.Collections;
using System.Diagnostics;
using fNbt;

namespace WorldFilter {

    public abstract class ChunkLike {

        internal bool Dirty = false;
        public abstract Location SerializeToStream(BigEndianBinaryWriter writer, int unscaled_offset);

        protected byte Pad(BinaryWriter writer, long scaled_offset, long unpadded_scaled_length) {

            byte padded_unscaled_length = (byte) Math.Round(unpadded_scaled_length / 4096.0f, MidpointRounding.ToPositiveInfinity);

            // padding
            writer.BaseStream.Position = scaled_offset + unpadded_scaled_length;
            for (int i = (4096 - (int) unpadded_scaled_length % 4096) % 4096; i > 0; i -= 4) {
                writer.Write(Constants.SINGLE_PADDING, 0, Math.Min(i, 4));
            }

            return padded_unscaled_length;
        }
    }

    public class HalfLoadedChunk : ChunkLike {
        public int Index;
        public int Timestamp;
        public byte[] UnparsedData;
        public Location OriginalLocation;

        public HalfLoadedChunk(Location location, int timestamp, byte[] unparsed_data) {
            Index = location.Index;
            OriginalLocation = location;
            Timestamp = timestamp;
            UnparsedData = unparsed_data;
        }

        public static HalfLoadedChunk PartiallyLoadChunk(Location location, int timestamp, BigEndianBinaryReader reader) {
            reader.BaseStream.Position = location.Offset;
            var buffer = reader.ReadBytes(location.Length);
            var chunk = new HalfLoadedChunk(location, timestamp, buffer);
            return chunk;
        }

        public override Location SerializeToStream(BigEndianBinaryWriter writer, int unscaled_offset) {
            var loc = new Location(unscaled_offset, OriginalLocation.UnscaledLength, Index);
            writer.BaseStream.Position = unscaled_offset * 4096;

            writer.Write(UnparsedData, 0, UnparsedData.Length);

            return loc;
        }
    }

    public class LoadedChunk : ChunkLike {
        public NbtFile Data;
        private HalfLoadedChunk Chunk;

        public Location OriginalLocation { get => Chunk.OriginalLocation; set => Chunk.OriginalLocation = value; }
        public int Timestamp { get => Chunk.Timestamp; set => Chunk.Timestamp = value; }
        public int Index { get => Chunk.Index; set => Chunk.Index = value; }

        public LoadedChunk(HalfLoadedChunk chunk) {
            LoadChunkFromBuffer(chunk, out NbtFile file);
            Data = file;
            Chunk = chunk;
        }

        public LoadedChunk(Location location, int timestamp, NbtFile data) {
            Data = data;
            Chunk = new HalfLoadedChunk(location, timestamp, new byte[0]);
        }

        public static LoadedChunk? LoadChunk(Location location, int timestamp, BigEndianBinaryReader reader) {
            var file = LoadChunkFromBuffer(location, reader);
            if (file != null) {
                var chunk = new LoadedChunk(location, timestamp, file);
                return chunk;
            }
            return null;
        }

        private static void LoadChunkFromBuffer(HalfLoadedChunk chunk, out NbtFile file) {
            var reader = new BigEndianBinaryReader(new MemoryStream(chunk.UnparsedData));

            var declared_length = reader.ReadInt32();
            if (declared_length == 0) throw new Exception("Attempted to load chunk with no data.");

            var compression = (CompressionType) reader.ReadByte();

            file = new NbtFile();
            reader.BaseStream.Position = 5;
            var loaded_length = (int) file.LoadFromBuffer(chunk.UnparsedData, 5, declared_length - 1, NbtCompression.AutoDetect);
        }

        private static NbtFile? LoadChunkFromBuffer(Location location, BigEndianBinaryReader reader) {
            reader.BaseStream.Position = location.Offset;

            var declared_length = reader.ReadInt32();
            if (declared_length > 0) {
                var compression = (CompressionType) reader.ReadByte();

                var bytes = reader.ReadBytes(declared_length);
                var file = new NbtFile();
                var loaded_length = file.LoadFromBuffer(bytes, 0, declared_length, NbtCompression.AutoDetect);
                return file;
            }
            return null;
        }

        public override Location SerializeToStream(BigEndianBinaryWriter writer, int unscaled_offset) {
            if (!Dirty) {
                return Chunk.SerializeToStream(writer, unscaled_offset);
            } else {
                long scaled_offset = unscaled_offset * 4096L;

                // compressed chunk data
                writer.BaseStream.Position = scaled_offset + 4 + 1;
                long unpadded_scaled_length = Data.SaveToStream(writer.BaseStream, NbtCompression.ZLib);

                // length + compression byte
                writer.BaseStream.Position = scaled_offset;
                writer.WriteInt32BigEndian((int) unpadded_scaled_length + 1); // Declared length includes compression schema
                writer.Write((byte) CompressionType.Zlib); // Compression schema

                // padding
                byte padded_unscaled_length = Pad(writer, scaled_offset, unpadded_scaled_length + 4 + 1); // unpadded length including chunk header

                return new Location(unscaled_offset, padded_unscaled_length, Index);
            }
        }
    }
}
