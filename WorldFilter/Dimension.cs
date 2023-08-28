namespace WorldFilter {
    public class Dimension {
        DirectoryInfo DimensionDir;

        public Dimension(DirectoryInfo dimensionDir) {
            DimensionDir = dimensionDir;
        }

        public IEnumerable<RegionFile> GetRegionFiles() {
            var region_dir = DimensionDir.EnumerateDirectories().FirstOrDefault(d => d.Name == "region");
            if (region_dir != null) {
                foreach (var file in region_dir.GetFiles("r.*.mca", new EnumerationOptions())) {
                    RegionFile? region = null;
                    try {
                        region = RegionFile.Open(file);
                    } catch (Exception e) {
                        Console.WriteLine(e.ToString());
                        Console.Write(e.StackTrace);
                    }
                    if (region != null) {
                        yield return region;
                    }
                }
            }
        }
    }
}
