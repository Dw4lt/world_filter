using WorldFilter;
using fNbt;

namespace WorldFilter.test {
    [TestClass]
    public class IntegrationTests {

        [TestMethod]
        [DeploymentItem(@"resources/TestUnchanging/in.mca")]
        public void TestUnchanging() {
            FileInfo f = new(@"resources/TestUnchanging/in.mca");
            var region = new RegionFile(f);

            region.ApplyToChunks(new List<ChunkModifier> { x => false });

            var out_path = Path.GetTempFileName();
            region.SaveIfNecessary(new FileInfo(out_path));

            AssertEqualFiles(f.FullName, out_path);
        }

        [TestMethod]
        [DeploymentItem(@"resources/TestUnchangingWithFullSerialization/in.mca")]
        [DeploymentItem(@"resources/TestUnchangingWithFullSerialization/out.mca")]
        public void TestUnchangingWithFullSerialization() {
            FileInfo in_file = new(@"resources/TestUnchangingWithFullSerialization/in.mca");
            FileInfo out_file = new(@"resources/TestUnchangingWithFullSerialization/out.mca");
            var region = new RegionFile(in_file);

            region.ApplyToChunks(new List<ChunkModifier> { x => true });

            var out_path = Path.GetTempFileName();
            region.SaveIfNecessary(new FileInfo(out_path));

            AssertEqualFiles(out_file.FullName, out_path);
        }

        [TestMethod]
        [DeploymentItem(@"resources/TestInventoryPurge/in.mca")]
        [DeploymentItem(@"resources/TestInventoryPurge/out.mca")]
        public void TestInventoryPurge() {
            FileInfo in_file = new(@"resources/TestInventoryPurge/in.mca");
            var region = new RegionFile(in_file);

            region.ApplyToInventoriesOfChunks(new List<InventoryModifier> { x => {
                var ret = x.Count;
                x.Clear();
                return ret > 0;
            } });

            var out_path = Path.GetTempFileName();
            region.SaveIfNecessary(new FileInfo(out_path));

            FileInfo out_file = new(@"resources/TestInventoryPurge/out.mca");
            AssertEqualFiles(out_file.FullName, out_path);
        }

        private void AssertEqualFiles(string a, string b) {
            int a1;
            int b1;

            using (
                FileStream fs1 = new FileStream(a, FileMode.Open, FileAccess.Read),
                            fs2 = new FileStream(b, FileMode.Open, FileAccess.Read)
            ) {
                Assert.AreEqual(fs1.Length, fs2.Length);
                do {
                    a1 = fs1.ReadByte();
                    b1 = fs2.ReadByte();
                    Assert.AreEqual(a1, b1);
                }
                while (a1 != -1 && b1 != -1);
            }
        }
    }
}
