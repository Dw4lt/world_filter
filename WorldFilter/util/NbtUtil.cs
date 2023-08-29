using fNbt;
using System.Diagnostics.CodeAnalysis;

namespace WorldFilter.util {

    public static class NbtUtil {
        public static bool GetNestedItems(this NbtCompound item, [NotNullWhen(returnValue: true)] out NbtList? nested_inventory) {
            if ((item["tag"]?["BlockEntityTag"]?["Items"] ?? item["tag"]?["Items"]) is NbtList nested_data) {
                nested_inventory = nested_data;
                return true;
            }
            nested_inventory = null;
            return false;
        }
    }
}
