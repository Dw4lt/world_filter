using fNbt;

namespace WorldFilter {
    public class PlayerFile : SavableFile {
        private NbtCompound Data;
        private bool _dirty = false;

        public PlayerFile(FileInfo file) : base(file) {
            Data = ReadPlayerFile(file);
        }

        protected override bool Dirty => _dirty;

        public override bool SaveIfNecessary(FileInfo path) {
            if (Dirty || path != LoadedFrom) {
                Console.WriteLine($"{LoadedFrom} -> {path}");
                EnsureDirectoryExists(path);

                new NbtFile(Data).SaveToFile(path.FullName, NbtCompression.GZip);
                return true;
            }
            return false;
        }

        private NbtCompound ReadPlayerFile(FileInfo path) {
            return new NbtFile(path.FullName).RootTag;
        }

        public void ApplyToInventoriesOfPlayer(IEnumerable<InventoryModifier> funcs) {
            string[] inventory_keywords = { "EnderItems", "Inventory" };
            foreach (var  keyword in inventory_keywords) {
                if (Data.TryGet(keyword, out NbtList inventory)) {
                    foreach (var foo in funcs) {
                        _dirty |= foo(inventory);
                    }
                }
            }
        }
    }
}
