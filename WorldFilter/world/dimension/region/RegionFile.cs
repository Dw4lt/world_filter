using fNbt;

namespace WorldFilter {
    public delegate bool ItemModifier(NbtCompound item);
    public delegate bool InventoryModifier(NbtList inventory);
    public delegate bool ChunkModifier(LoadedChunk chunk);

    public class RegionFile : SavableFile, IDisposable {

        private BigEndianBinaryReader r;

        private RegionMetadata Metadata;
        private Dictionary<Location, ChunkLike> LoadedChunkNbt = new();

        protected override bool Dirty => LoadedChunkNbt.Any(x => x.Value.Dirty);

        public RegionFile(FileInfo file) : base(file) {
            var stream = StreamCreator.Create(file.FullName);
            r = new BigEndianBinaryReader(stream);

            Metadata = new RegionMetadata(r);

            foreach (var x in Metadata.locations) {
                var timestamp = Metadata.TimestampTable[x.Value.Index];
                var chunk = HalfLoadedChunk.PartiallyLoadChunk(x.Value, timestamp, r);
                LoadedChunkNbt.Add(x.Value, chunk);
            }
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
                    foreach (NbtCompound entity in block_entities) {
                        if (entity.TryGet("Items", out NbtList inventory)) {
                            foreach (var foo in funcs) {
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

            var found = LoadedChunkNbt.TryGetValue(location, out ChunkLike? chunk);
            if (found && chunk is LoadedChunk fullyLoadedChunk) {
                return fullyLoadedChunk;
            } else if (found && chunk is HalfLoadedChunk halfLoadedChunk) {
                var loadedChunk = new LoadedChunk(halfLoadedChunk);
                LoadedChunkNbt[location] = loadedChunk;
                return loadedChunk;
            }
            throw new Exception("Unsupported ChunkLike derivative.");
        }

        public override bool SaveIfNecessary(FileInfo path) {
            if (path != LoadedFrom || Dirty) {
                Console.WriteLine($"Saving region to '{path}'");

                EnsureDirectoryExists(path);

                var file = File.Open(path.FullName, FileMode.Create);
                using (var writer = new BigEndianBinaryWriter(file, System.Text.Encoding.UTF8, false)) {

                    // Save all dirty chunks to the stream
                    var location = new Location();
                    foreach (var entry in LoadedChunkNbt.OrderBy(e => e.Key.Offset)) {
                        var old = Metadata.locations.GetLocation(entry.Key.Index);
                        var new_loc = entry.Value.SerializeToStream(writer, location.UnscaledOffset + (int) location.UnscaledLength);

                        Metadata.locations.SetLocation(new_loc.Index, new_loc);
                        location = new_loc;
                    }

                    // Go to the start of the stream and update the region Metadata
                    writer.BaseStream.Position = 0;
                    Metadata.ToBytes(writer);
                    writer.Flush();
                }
                return true;
            }
            return false;
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
}
