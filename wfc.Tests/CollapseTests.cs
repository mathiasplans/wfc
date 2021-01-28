using System;
using Xunit;

namespace wfc.Tests {
    public class CollapseTests {
        WaveFunction wf;
        WaveFunctionEncoder wencoder;

        [Fact]
        public void MonoSpace1D() {
            Wave[] monowave = new Wave[] {new Wave(1, "A")};
            monowave[0].AddConstraints(monowave);

            this.wencoder = new WaveFunctionEncoder(monowave);

            uint[][] testSpace = new uint[2][];
            uint[][] control = new uint[2][];

            for (uint i = 0; i < 2; ++i) {
                testSpace[i] = this.wencoder.GetPossibilitySpace();
                control[i] = testSpace[i];
            }

            this.wf = new WaveFunction(monowave, 1);

            // Collapse
            this.wf.Collapse(testSpace[0], testSpace[1]);

            Assert.Equal(control, testSpace);
        }

        [Fact]
        public void MonoSpace2D() {
            Wave[] monowave = new Wave[] {new Wave(4, "A")};
            monowave[0].AddConstraints(0, monowave);
            monowave[0].AddConstraints(1, monowave);
            monowave[0].AddConstraints(2, monowave);
            monowave[0].AddConstraints(3, monowave);

            this.wencoder = new WaveFunctionEncoder(monowave);
            uint[,][] testSpace = new uint[3,3][];
            uint[,][] control = new uint[3,3][];

            for (uint i = 0; i < 3; ++i) {
                for (uint j = 0; j < 3; ++j) {
                    testSpace[i, j] = this.wencoder.GetPossibilitySpace();
                    control[i, j] = testSpace[i, j];
                }
            }

            this.wf = new WaveFunction(monowave, 4);

            // Collapse
            this.wf.Collapse(testSpace[1, 1], testSpace[0, 1]);
            this.wf.Collapse(testSpace[1, 1], testSpace[1, 0]);
            this.wf.Collapse(testSpace[1, 1], testSpace[2, 1]);
            this.wf.Collapse(testSpace[1, 1], testSpace[1, 2]);

            Assert.Equal(control, testSpace);
        }

        [Fact]
        public void BlackWhite() {
            Wave[] waves = new Wave[] {new Wave(1, "black"), new Wave(1, "white")};

            waves[0].AddConstraints(new Wave[] {waves[0]});
            waves[1].AddConstraints(new Wave[] {waves[1]});

            this.wencoder = new WaveFunctionEncoder(waves);

            uint[][] testSpace = new uint[2][];
            uint[][] control = new uint[2][];

            // Construct a control
            control[0] = this.wencoder.GetSolo(waves[0]);
            control[1] = this.wencoder.GetSolo(waves[0]);

            // Construct a test space
            testSpace[0] = this.wencoder.GetSolo(waves[0]);
            testSpace[1] = this.wencoder.GetPossibilitySpace();

            this.wf = new WaveFunction(waves, 1);

            // Collapse
            this.wf.Collapse(testSpace[0], testSpace[1]);

            Assert.Equal(control, testSpace);
        }

        [Fact]
        public void Choise() {
            Wave[] waves = new Wave[] {new Wave(1, "A"), new Wave(1, "B"), new Wave(1, "C")};

            waves[0].AddConstraints(new Wave[] {waves[1]});
            waves[1].AddConstraints(new Wave[] {waves[1]});
            waves[2].AddConstraints(new Wave[] {waves[2]});

            this.wencoder = new WaveFunctionEncoder(waves);

            uint[][] testSpace = new uint[2][];
            uint[][] control = new uint[2][];

            // Construct a control
            control[0] = this.wencoder.GetSolo(waves[0]);
            control[1] = this.wencoder.GetSolo(waves[1]);

            // Construct the test space
            testSpace[0] = this.wencoder.GetSolo(waves[0]);
            testSpace[1] = this.wencoder.GetPossibilitySpace();

            this.wf = new WaveFunction(waves);

            // Collapse
            this.wf.Collapse(testSpace[0], testSpace[1]);

            Assert.Equal(control, testSpace);
        }

        [Fact]
        public void Impossible() {
            Wave[] waves = new Wave[] {new Wave(1, "A"), new Wave(1, "B")};

            waves[0].AddConstraints(new Wave[] {waves[0]});
            waves[1].AddConstraints(new Wave[] {waves[1]});

            this.wencoder = new WaveFunctionEncoder(waves);

            uint[][] testSpace = new uint[2][];
            uint[][] control = new uint[2][];

            // Construct the control
            control[0] = this.wencoder.GetSolo(waves[0]);
            control[1] = this.wencoder.GetEmpty();

            // Construct the test space
            testSpace[0] = this.wencoder.GetSolo(waves[0]);
            testSpace[1] = this.wencoder.GetSolo(waves[1]);

            this.wf = new WaveFunction(waves);

            // Collapse
            this.wf.Collapse(testSpace[0], testSpace[1]);

            Assert.Equal(control, testSpace);
        }

        [Fact]
        public void MultipleCollapses() {
            Wave[] waves = new Wave[] {new Wave(1, "A"), new Wave(1, "B"), new Wave(1, "C")};

            waves[0].AddConstraints(new Wave[] {waves[1]});
            waves[1].AddConstraints(new Wave[] {waves[0], waves[2]});
            waves[2].AddConstraints(new Wave[] {waves[1]});

            this.wencoder = new WaveFunctionEncoder(waves);

            uint[][] testSpace = new uint[3][];
            uint[][] control = new uint[3][];

            // Construct the control
            control[0] = this.wencoder.GetSolo(waves[0]);
            control[1] = this.wencoder.GetSolo(waves[1]);
            control[2] = this.wencoder.GetSolo(waves[2]);

            // Construct the test space
            testSpace[0] = this.wencoder.GetSolo(waves[0]);
            testSpace[1] = this.wencoder.GetPossibilitySpace();
            testSpace[2] = this.wencoder.GetSolo(waves[2]);

            this.wf = new WaveFunction(waves);

            // Collapse
            this.wf.Collapse(testSpace[0], testSpace[1]);
            this.wf.Collapse(testSpace[2], testSpace[1]);

            Assert.Equal(control, testSpace);
        }

        [Fact] public void EntropyChange() {
            Wave[] waves = new Wave[] {new Wave(1, "A"), new Wave(1, "B")};

            waves[0].AddConstraints(new Wave[] {waves[1]});
            waves[1].AddConstraints(waves);

            this.wencoder = new WaveFunctionEncoder(waves);

            uint[][] testSpace = new uint[2][];

            testSpace[0] = this.wencoder.GetSolo(waves[0]);
            testSpace[1] = this.wencoder.GetPossibilitySpace();

            this.wf = new WaveFunction(waves);

            // Collapse
            uint i = this.wf.Collapse(testSpace[0], testSpace[1]);

            Assert.Equal(0u, i);
        }
    }
}