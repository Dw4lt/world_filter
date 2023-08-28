
using fNbt;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;

namespace WorldFilter {
    public class World {
        NbtCompound RootTag;
        DirectoryInfo WorldDir;

        public World(DirectoryInfo root_dir) {
            if (root_dir is null) throw new ArgumentNullException("Could not fetch root dir of world");
            var files = root_dir.GetFiles("level.dat");
            foreach (FileInfo f in files) {
                RootTag = ReadLevelFile(f);
                break;
            }
            if (RootTag is null) throw new Exception("World does not contain level file.");
            WorldDir = root_dir;
        }

        private NbtCompound ReadLevelFile(FileInfo path) {
            return new NbtFile(path.FullName).RootTag;
        }

        public IEnumerable<Dimension> GetDimensions() {
            yield return new Dimension(WorldDir);
            foreach (var subdir in WorldDir.GetDirectories()) {
                switch (subdir.Name) {
                    case "DIM1":
                    case "DIM-1":
                        yield return new Dimension(subdir);
                        break;
                    case "dimensions":
                        foreach(var group in subdir.GetDirectories()) {
                            foreach (var custom_dim in group.GetDirectories()) {
                                yield return new Dimension(custom_dim);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
