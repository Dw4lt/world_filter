A utility to easily modify entire worlds.
For now it's just for personal use, so expect breaking changes.

## Example usage
Method processing an inventory of any kind
```csharp
bool RemoveEnchantments(NbtList inventory) {
    bool changed = false;
    foreach (NbtCompound item in inventory){
        changed |= item.Remove("Enchantments");

        // Remember to process nested items as well!
        if ((item["tag"]?["BlockEntityTag"]?["Items"] ?? item["tag"]?["Items"]) is NbtList nested_item) {
            changed |= RemoveEnchantments(nested_item);
        }
    }
    return changed;
}
```

Method controlling which elements of the world to process and how
```csharp
void MyEditor(DirectoryInfo input_dir, DirectoryInfo? output_dir = null) {
    if (output_dir == null) output_dir = input_dir; // In-place editing possible

    var world = new World(input_dir);

    // Modify items in all chunks of all dimensions
    foreach (Dimension dim in world.GetDimensions()) {
        foreach (RegionFile region in dim.GetRegionFiles()) {
            region.ApplyToInventoriesOfChunks(new List<InventoryModifier> {
                RemoveEnchantments,
                // ...
            });
            region.SaveIfNecessary(region.ResolveOutputPath(input_dir, output_dir));
        }
    }

    // Modify items on players
    foreach (PlayerFile player in world.GetPlayers()){
        player.ApplyToInventoriesOfPlayer(new List<InventoryModifier> {
            RemoveEnchantments,
            // ...
        });
        player.SaveIfNecessary(player.ResolveOutputPath(input, output));
    }
}
```
