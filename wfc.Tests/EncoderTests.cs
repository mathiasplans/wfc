using System;
using Xunit;

namespace wfc.Tests {
    public class EncoderTests {
        private Wave[] waves;
        private WaveFunctionEncoder wencoder;

        /**
         * Common for all the tests
         */
        public EncoderTests() {
            this.waves = new Wave[] {
                new Wave(4, "0"),
                new Wave(4, "1"),
                new Wave(4, "2"),
                new Wave(4, "3")
            };

            this.wencoder = new WaveFunctionEncoder(this.waves);
        }

        [Fact]
        public void TestSize() {
            Assert.Equal(4, this.wencoder.GetSize());
        }

        [Fact]
        public void TestEncodeSize() {
            Assert.Equal(1, this.wencoder.GetEncodeSize());
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestOrder(int order) {
            Assert.Equal(order, this.wencoder.GetOrder(this.waves[order]));
        }

        [Fact]
        public void TestFullEncoding() {
            Assert.Equal(0b1111u, this.wencoder.GetPossibilitySpace()[0]);
        }

        [Theory]
        [InlineData(new int[] {0})]
        [InlineData(new int[] {0, 1})]
        [InlineData(new int[] {2, 3})]
        [InlineData(new int[] {0, 1, 3})]
        [InlineData(new int[] {0, 1, 2, 3})]
        public void TestSelectiveEncoding(int[] indices) {
            Wave[] waves = new Wave[indices.Length];
            uint expected = 0;
            for (int i = 0; i < indices.Length; ++i) {
                // Put the wave into the input
                waves[i] = this.waves[indices[i]];

                // Expected output construction
                expected |= 1u << indices[i];
            }

            uint[] wf = this.wencoder.GetPossibilitySpace(waves);

            Assert.Equal(expected, wf[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestSoloEncoding(int index) {
            uint[] wf = this.wencoder.GetSolo(this.waves[index]);
            Assert.Equal(1u << index, wf[0]);
        }

        [Fact]
        public void TestEmptyEncoding() {
            Assert.Equal(0u, this.wencoder.GetEmpty()[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(31)]
        public void TestSetIndex(int index) {
            uint[] wf = this.wencoder.GetEmpty();
            this.wencoder.SetWave(wf, index);
            Assert.Equal(1u << index, wf[0]);
        }

        [Theory]
        [InlineData(31, 0)]
        [InlineData(32, 1)]
        [InlineData(63, 1)]
        [InlineData(64, 2)]
        public void TestSetIndexMultiWord(int index, int encodeIndex) {
            uint[] wf = new uint[3];
            this.wencoder.SetWave(wf, index);
            Assert.Equal(1u << (index % (sizeof(uint) * 8)), wf[encodeIndex]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestSetWave(int index) {
            uint[] wf = this.wencoder.GetEmpty();
            Wave wave = this.waves[index];
            this.wencoder.SetWave(wf, wave);
            Assert.Equal(1u << index, wf[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(31)]
        public void TestResetIndex(int index) {
            uint[] wf = this.wencoder.GetEmpty();
            wf[0] = ~0U;
            this.wencoder.ResetWave(wf, index);
            Assert.Equal(~0U ^ (1u << index), wf[0]);
        }

        [Theory]
        [InlineData(31, 0)]
        [InlineData(32, 1)]
        [InlineData(63, 1)]
        [InlineData(64, 2)]
        public void TestResetIndexMultiWord(int index, int encodeIndex) {
            uint[] wf = new uint[3];
            wf[0] = ~0U;
            wf[1] = ~0U;
            wf[2] = ~0U;

            this.wencoder.ResetWave(wf, index);

            Assert.Equal(~0U ^ (1u << (index % (sizeof(uint) * 8))), wf[encodeIndex]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestResetWave(int index) {
            uint[] wf = this.wencoder.GetPossibilitySpace();
            uint wfold = wf[0];

            this.wencoder.ResetWave(wf, index);

            Assert.Equal(wfold ^ (1 << index), wf[0]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        [InlineData(31)]
        public void TestSelectIndex(int index) {
            uint[] wf = this.wencoder.GetEmpty();
            wf[0] = ~0U;

            this.wencoder.SelectWave(wf, index);

            Assert.Equal(1u << index, wf[0]);
        }

        [Theory]
        [InlineData(31, 0)]
        [InlineData(32, 1)]
        [InlineData(63, 1)]
        [InlineData(64, 2)]
        public void TestSelectIndexMultiWord(int index, int encodeIndex) {
            uint[] wf = new uint[3];
            wf[0] = ~0U;
            wf[1] = ~0U;
            wf[2] = ~0U;

            uint[] expected = new uint[3];
            expected[encodeIndex] = 1U << (index % (sizeof(uint) * 8));

            this.wencoder.SelectWave(wf, index);

            Assert.Equal(expected, wf);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        public void TestSelectWave(int index) {
            uint[] wf = this.wencoder.GetPossibilitySpace();
            uint expected = wf[0] & (1u << index);

            this.wencoder.SelectWave(wf, this.waves[index]);

            Assert.Equal(expected, wf[0]);
        }

        [Theory]
        [InlineData(0b0001u, 0b1000u, 0b1001u)]
        [InlineData(0b0000u, 0b0100u, 0b0100u)]
        [InlineData(0b1100u, 0b0110u, 0b1110u)]
        public void TestMerge(uint i1, uint i2, uint o) {
            Assert.Equal(o, this.wencoder.MergeWaveFunctions(new uint[] {i1}, new uint[] {i2})[0]);
        }

        [Theory]
        [InlineData(0b0001u, 0b1000u, 0b1001u)]
        [InlineData(0b0000u, 0b0100u, 0b0100u)]
        [InlineData(0b1100u, 0b0110u, 0b1110u)]
        public void TestMergeInPlace(uint i1, uint i2, uint o) {
            uint[] wf = new uint[] {i1};
            this.wencoder.MergeInPlace(wf, new uint[] {i2});

            Assert.Equal(o, wf[0]);
        }

        [Theory]
        [InlineData(0b1111u, 0b0000u, 0b1111u)]
        [InlineData(0b0000u, 0b1111u, 0b0000u)]
        [InlineData(0b1111u, 0b1111u, 0b0000u)]
        [InlineData(0b1100u, 0b0110u, 0b1000u)]
        [InlineData(0b1100u, 0b0011u, 0b1100u)]
        public void TestRemoveInPlace(uint i1, uint i2, uint o) {
            uint[] wf = new uint[] {i1};
            this.wencoder.RemoveInPlace(wf, new uint[] {i2});

            Assert.Equal(o, wf[0]);
        }

        [Theory]
        [InlineData(0b0000u, 0b1111u, 0b0000u)]
        [InlineData(0b1111u, 0b0000u, 0b0000u)]
        [InlineData(0b1100u, 0b0011u, 0b0000u)]
        [InlineData(0b1100u, 0b0110u, 0b0100u)]
        public void TestAndInPlace(uint i1, uint i2, uint o) {
            uint[] wf = new uint[] {i1};
            this.wencoder.AndInPlace(wf, new uint[] {i2});

            Assert.Equal(o, wf[0]);
        }

        [Theory]
        [InlineData(0b1000u, 0)]
        [InlineData(0b0010u, 0)]
        [InlineData(0b0101u, 1)]
        [InlineData(0b1101u, 2)]
        [InlineData(0b1111u, 3)]
        [InlineData(0b1100110101u, 5)]
        public void TestGetEntropy(uint input, uint expected) {
            Assert.Equal(expected, this.wencoder.GetEntropy(new uint[] {input}));
        }

        [Theory]
        [InlineData(0b0000u, 0b0001u, 0)]
        [InlineData(0b0100u, 0b0000u, 0)]
        [InlineData(0b1000u, 0b0010u, 1)]
        [InlineData(0b1111u, 0b0000u, 3)]
        [InlineData(0b1111u, 0b1111u, 7)]
        public void TestGetEntropyMultiWord(uint i1, uint i2, uint expected) {
            uint[] wf = new uint[] {i1, i2};
            Assert.Equal(expected, this.wencoder.GetEntropy(wf));
        }

        [Theory]
        [InlineData(0b1100110101u)]
        [InlineData(0b0u)]
        [InlineData(0b1111111111u)]
        [InlineData(0b0000100000u)]
        public void TestForEachWaveIterations(uint input) {
            uint[] wf = new uint[] {input};
            uint entropy = this.wencoder.GetEntropy(wf);

            uint count = 0;
            this.wencoder.ForEachWave(wf, (order) => {
                count += 1;
            });

            Assert.Equal(entropy + 1, count);
        }

        [Theory]
        [InlineData(0b1100110101u)]
        [InlineData(0b0u)]
        [InlineData(0b1111111111u)]
        [InlineData(0b0000100000u)]
        public void TestForEachWaveCorrectOrder(uint input) {
            uint[] wf = new uint[] {input};
            uint product = 0;

            this.wencoder.ForEachWave(wf, (order) => {
                product |= 1u << order;
            });

            Assert.Equal(input, product);
        }

        [Fact]
        public void TestForEachWaveBreak() {
            uint[] wf = new uint[] {0b11111111u};
            uint remain = 4;
            uint count = 0;

            this.wencoder.ForEachWave(wf, (order) => {
                if (remain == 0)
                    return true;

                --remain;
                ++count;

                return false;
            });

            Assert.Equal(4u, count);
        }

        [Fact]
        public void TestGetWaves() {
            uint[] wf = this.wencoder.GetPossibilitySpace();
            Wave[] waves = this.wencoder.GetWaves(wf);
            Assert.Equal(this.waves, waves);
        }
    }
}