namespace WorldFilter {
    internal class Constants {
        /// <summary>
        /// Single 4 byte arry of zero-padding
        /// </summary>
        public static readonly byte[] SINGLE_PADDING = { 0, 0, 0, 0 };
    }

    /// <summary>
    /// Chunk compression types.
    /// </summary>
    internal enum CompressionType {
        Gzip = 1,
        Zlib = 2
    }
}
