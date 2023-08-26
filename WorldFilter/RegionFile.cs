using System.Collections;
using System.Diagnostics;
using fNbt;

namespace WorldFilter {
    public delegate bool CompoundModifier(NbtCompound compound);
    public delegate bool InventoryModifier(NbtList compound);
    public delegate bool ChunkModifier(LoadedChunk chunk);

    internal class Constants {
        /// <summary>
        /// Single 4 byte arry of zero-padding
        /// </summary>
        public static readonly byte[] SINGLE_PADDING = { 0, 0, 0, 0 };
    }


    public class RegionFile : IDisposable {

        private BigEndianBinaryReader r;

        private RegionMetadata Metadata;
        private Dictionary<Location, ChunkLike> LoadedChunkNbt = new();

        private RegionFile(BigEndianBinaryReader reader) {
            r = reader;
            Metadata = new RegionMetadata(reader);
        }

        public static RegionFile Open(FileInfo file) => Open(file.FullName);

        public static RegionFile Open(string path) {
            var stream = StreamCreator.Create(path);
            var stream_reader = new BigEndianBinaryReader(stream);
            var region = new RegionFile(stream_reader);

            foreach (var x in region.Metadata.locations) {
                var timestamp = region.Metadata.TimestampTable[x.Value.Index];
                var chunk = HalfLoadedChunk.PartiallyLoadChunk(x.Value, timestamp, stream_reader);
                region.LoadedChunkNbt.Add(x.Value, chunk);
            }

            return region;
        }

        public void ApplyToChunks(IEnumerable<ChunkModifier> funcs) {
            foreach (LoadedChunk chunk in GetAllChunks()) {
                foreach (var foo in funcs) {
                    chunk.Dirty |= foo(chunk);
                }
            }
        }

        public void ApplyToInventoriesOfChunks(IEnumerable<InventoryModifier> funcs) {
            string[] inventory_keywords = { "Items", "Inventory" };
            foreach (LoadedChunk chunk in GetAllChunks()) {
                if (chunk.Data.RootTag.TryGet("block_entities", out NbtList block_entities)) {
                    foreach(NbtCompound entity in block_entities) {
                        if (entity.TryGet("Items", out NbtList inventory)) {
                            foreach(var foo in funcs) {
                                chunk.Dirty |= foo(inventory);
                            }
                        }
                    }
                }
            }
        }

        private LoadedChunk GetChunk(Location location) {
            // if unscaled_offset is 0, the chunk doesn't exist in the file
            if (location.UnscaledOffset == 0)
                throw new ArgumentOutOfRangeException("Chunk doesn't exist.", nameof(location.UnscaledOffset));

            var found = LoadedChunkNbt.TryGetValue(location, out ChunkLike chunk);
            if (found && chunk is LoadedChunk fullyLoadedChunk) {
                return fullyLoadedChunk;
            } else if (found && chunk is HalfLoadedChunk halfLoadedChunk) {
                var loadedChunk = new LoadedChunk(halfLoadedChunk);
                LoadedChunkNbt[location] = loadedChunk;
                return loadedChunk;
            }
            throw new Exception("Unsupported ChunkLike derivative.");
        }

        public bool SaveIfNecessary(string path) {
            bool modified = LoadedChunkNbt.Any(x => x.Value.Dirty);
            if (modified || true) {
                var file = File.Open(path, FileMode.Create);
                using (var writer = new BigEndianBinaryWriter(file, System.Text.Encoding.UTF8, false)) {

                    // Save all dirty chunks to the stream
                    var location = new Location();
                    foreach (var entry in LoadedChunkNbt.OrderBy(e => e.Key.Offset)) {
                        var old = Metadata.locations.GetLocation(entry.Key.Index);
                        var new_loc = entry.Value.SerializeToStream(writer, Math.Max(old.UnscaledOffset, location.UnscaledOffset + (int) location.UnscaledLength));
                        // var new_loc = entry.Value.SerializeToStream(writer, location.UnscaledOffset + (int) location.UnscaledLength);

                        Metadata.locations.SetLocation(new_loc.Index, new_loc);
                        location = new_loc;
                    }

                    // Go to the start of the stream and update the region Metadata
                    writer.BaseStream.Position = 0;
                    Metadata.ToBytes(writer);
                    writer.Flush();
                }
            }
            return modified;
        }

        public IEnumerable<LoadedChunk> GetAllChunks() {
            foreach (var x in Metadata.locations) {
                var chunk = GetChunk(x.Value);
                if (chunk != null) yield return chunk;
            }
        }

        public void Dispose() {
            r.Dispose();
        }
    }


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
                UnpaddedScaledLength = (int) value;
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

    public struct LocationTable : IEnumerable<KeyValuePair<long, Location>> {
        private const int TableSize = 32 * 32;

        private SortedDictionary<long, Location> LocationData;

        public LocationTable() {
            LocationData = new SortedDictionary<long, Location>();
        }

        public void Add(long index, Location location) => LocationData.Add(index, location);

        public IEnumerator<KeyValuePair<long, Location>> GetEnumerator() => LocationData.GetEnumerator();

        public static LocationTable FromBuffer(BigEndianBinaryReader reader) {
            var result = new LocationTable();
            for (int i = 0; i < TableSize; i++) {
                var location = Location.FromBytes(reader, i);
                if (location is not null) {
                    result.Add(i, location.Value);
                }
            }
            return result;
        }

        public void ToBytes(BigEndianBinaryWriter writer) {
            long previous = -1;
            foreach (var item in LocationData) {
                // Add paddding to previous known location
                if (item.Key - previous > 1) {
                    for (int i = 0; i < item.Key - previous - 1; i++) {
                        writer.Write(Constants.SINGLE_PADDING);
                    }
                }
                item.Value.ToBytes(writer);

                previous = item.Key;
            }
            for (int i = 0; i < TableSize - previous - 1; i++) {
                writer.Write(Constants.SINGLE_PADDING);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => LocationData.GetEnumerator();

        public Location GetLocation(long index) {
            if (!LocationData.TryGetValue(index, out Location ret)) {
                throw new Exception($"OriginalLocation with Index {index} does not exist");
            }
            return ret;
        }

        public void SetLocation(long index, Location loc) {
            LocationData[index] = loc;
        }
    }

    struct RegionMetadata {
        private const int Dimension = 32;

        public LocationTable locations;

        private const int TimestampTableSize = Dimension * Dimension;
        public int[] TimestampTable = new int[TimestampTableSize];

        public RegionMetadata(BigEndianBinaryReader reader) {
            locations = ReadLocationTable(reader);
            ReadTimestampTable(reader);
        }

        private static LocationTable ReadLocationTable(BigEndianBinaryReader reader) {
            return LocationTable.FromBuffer(reader);
        }

        private void ReadTimestampTable(BigEndianBinaryReader reader) {
            for (int i = 0; i < TimestampTableSize; i++) {
                TimestampTable[i] = reader.ReadInt32();
            }
        }

        public void ToBytes(BigEndianBinaryWriter writer) {
            var result = new byte[TimestampTableSize * 2];
            locations.ToBytes(writer);
            foreach (var t in TimestampTable) {
                writer.WriteInt32BigEndian(t);
            }
        }
    }

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
            var loaded_length = (int) file.LoadFromBuffer(chunk.UnparsedData, 5, declared_length, NbtCompression.AutoDetect);
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
