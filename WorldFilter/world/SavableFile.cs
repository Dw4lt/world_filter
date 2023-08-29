namespace WorldFilter {
    public abstract class SavableFile {
        public readonly FileInfo LoadedFrom;

        protected abstract bool Dirty { get; }

        public SavableFile(FileInfo file) {
            LoadedFrom = file;
        }

        protected void EnsureDirectoryExists(FileInfo file) {
            if (file.Directory != null) {
                Directory.CreateDirectory(file.Directory.FullName);
            }
        }

        public FileInfo ResolveOutputPath(DirectoryInfo input, DirectoryInfo output) {
            var path_delta = Path.GetRelativePath(input.FullName, LoadedFrom.FullName);
            return new FileInfo(Path.Join(output.FullName, path_delta));
        }

        public abstract bool SaveIfNecessary(FileInfo path);
    }
}
