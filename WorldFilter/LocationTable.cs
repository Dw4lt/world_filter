using System.Collections;
using System.Diagnostics;
using fNbt;

namespace WorldFilter {

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
}
