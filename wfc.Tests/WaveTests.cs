using System;
using Xunit;

namespace wfc.Tests {
    public class WaveTests {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        public void TestConstraints(int nrOfCons) {
            Wave wave = new Wave(1, "A");
            Wave[] constraints = new Wave[nrOfCons];
            wave.AddConstraints(0, constraints);

            Assert.Equal(nrOfCons, wave.GetConstraints(0).Length);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(10)]
        public void TestManySides(uint nrOfSides) {
            Wave wave = new Wave(nrOfSides, "A");
            Assert.Equal(nrOfSides, wave.GetSides());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(4)]
        public void TestWaveShift(uint nrOfShifts) {
            Wave wave = new Wave(4, "A");
            Wave interest = new Wave(4, "i");
            wave.AddConstraints(0, new Wave[] {interest});
            wave.AddConstraints(1, new Wave[] {wave});
            wave.AddConstraints(2, new Wave[] {wave});
            wave.AddConstraints(3, new Wave[] {wave});

            // Shift
            Wave swave = wave;
            for (uint i = 0; i < nrOfShifts; ++i) {
                swave = swave.ShiftedWave();
            }

            // Check that the the interest wave is in the right place
            Assert.Equal("i", swave.GetConstraints(nrOfShifts % 4)[0].name);
        }
    }
}