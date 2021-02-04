using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace wfc.Tests {
    public class WFC2Tests {
        Wave[] waves1, waves2, waves3;
        public WFC2Tests() {
            // Create sets of waves for testing
            // 1. A simple two wave system
            // Typical output should be something like this
            // ~~~~~~~~~~~~~
            // ~~~~~~~~~~~~~
            // ~~~~~~~~~~~~~
            // ^^^^^^^^^^^^^
            // ^^^^^^^^^^^^^
            // ^^^^^^^^^^^^^
            Wave wave1Sea = new Wave(4, "~");
            Wave wave1Land = new Wave(4, "^");

            // Sea tile that is surrounded by sea, or bordered by land on the south side
            wave1Sea.AddConstraints(
                /* NORTH */ new Wave[] {wave1Sea},
                /* EAST  */ new Wave[] {wave1Sea},
                /* SOUTH */ new Wave[] {wave1Sea, wave1Land},
                /* WEST  */ new Wave[] {wave1Sea}
            );

            // Land tile that is surrounded by land, or bordered by sea on the north side
            wave1Land.AddConstraints(
                /* NORTH */ new Wave[] {wave1Land, wave1Sea},
                /* EAST  */ new Wave[] {wave1Land},
                /* SOUTH */ new Wave[] {wave1Land},
                /* WEST  */ new Wave[] {wave1Land}
            );

            this.waves1 = new Wave[] {wave1Sea, wave1Land};

            // 2. A bit more complicated 3 tile system
            // Typical output should be something like this
            // ~~~~~~~~~~~~~
            // ~~~~~^^~~~~~~
            // ~~~^^^^^~~~~~
            // ~~~~^MM^^^~~~
            // ~~~^^M^^~~~~~
            // ~~~~^^~~~~~~~
            // ~~~~~~~~~~~~~
            Wave wave2Sea = new Wave(4, "~");
            Wave wave2Land = new Wave(4, "^");
            Wave wave2Mountain = new Wave(4, "M");

            // Sea tile that is surrounded by sea or land
            wave2Sea.AddConstraints(
                /* NORTH */ new Wave[] {wave2Sea, wave2Land},
                /* EAST  */ new Wave[] {wave2Sea, wave2Land},
                /* SOUTH */ new Wave[] {wave2Sea, wave2Land},
                /* WEST  */ new Wave[] {wave2Sea, wave2Land}
            );

            // Land tile that is surrounded by land, sea, or mountains
            wave2Land.AddConstraints(
                /* NORTH */ new Wave[] {wave2Sea, wave2Land, wave2Mountain},
                /* EAST  */ new Wave[] {wave2Sea, wave2Land, wave2Mountain},
                /* SOUTH */ new Wave[] {wave2Sea, wave2Land, wave2Mountain},
                /* WEST  */ new Wave[] {wave2Sea, wave2Land, wave2Mountain}
            );

            // Mountain tile that is surrounded by mountains or land
            wave2Mountain.AddConstraints(
                /* NORTH */ new Wave[] {wave2Land, wave2Mountain},
                /* EAST  */ new Wave[] {wave2Land, wave2Mountain},
                /* SOUTH */ new Wave[] {wave2Land, wave2Mountain},
                /* WEST  */ new Wave[] {wave2Land, wave2Mountain}
            );

            this.waves2 = new Wave[] {wave2Sea, wave2Land, wave2Mountain};

            // 3. A complicated 5 tile system
            // Typical output should look something like this
            // ~~~~~~~~~~~~~~~
            // ~~~~_^^^~~~~~~~
            // ~~~_^M^^~~~~~~~
            // ~~~~_^^^^~~~~~~
            // ~~~_A^^^~~~~~~~
            // ~~~~_^~~~~~~~~~
            // ~~~~~~~~~~~~~~~
            Wave wave3Sea = new Wave(4, "~");
            Wave wave3Land = new Wave(4, "^");
            Wave wave3Mountain = new Wave(4, "M");
            Wave wave3Coast = new Wave(4, "_");
            Wave wave3City = new Wave(4, "A");

            // Sea tile that is surrounded by sea, land, or coast
            wave3Sea.AddConstraints(
                /* NORTH */ new Wave[] {wave3Sea, wave3Land, wave3Coast},
                /* EAST  */ new Wave[] {wave3Sea, wave3Land, wave3Coast},
                /* SOUTH */ new Wave[] {wave3Sea, wave3Land, wave3Coast},
                /* WEST  */ new Wave[] {wave3Sea, wave3Land, wave3Coast}
            );

            // Land tile that is surrounded by sea, land, mountain, or is bordered by coast or city on west
            wave3Land.AddConstraints(
                /* NORTH */ new Wave[] {wave3Sea, wave3Land, wave3Mountain},
                /* EAST  */ new Wave[] {wave3Sea, wave3Land, wave3Mountain},
                /* SOUTH */ new Wave[] {wave3Sea, wave3Land, wave3Mountain},
                /* WEST  */ new Wave[] {wave3Sea, wave3Land, wave3Mountain, wave3Coast, wave3City}
            );

            // Mountain tile that is surrounded by mountians or land
            wave3Mountain.AddConstraints(
                /* NORTH */ new Wave[] {wave3Mountain, wave3Land},
                /* EAST  */ new Wave[] {wave3Mountain, wave3Land},
                /* SOUTH */ new Wave[] {wave3Mountain, wave3Land},
                /* WEST  */ new Wave[] {wave3Mountain, wave3Land}
            );

            // Coast tile that is surrounded by sea, city, or bordered by land to east
            wave3Coast.AddConstraints(
                /* NORTH */ new Wave[] {wave3Sea, wave3City},
                /* EAST  */ new Wave[] {wave3Sea, wave3City, wave3Land},
                /* SOUTH */ new Wave[] {wave3Sea, wave3City},
                /* WEST  */ new Wave[] {wave3Sea}
            );

            // City tile that is bordered by land to east and other sides are coast
            wave3City.AddConstraints(
                /* NORTH */ new Wave[] {wave3Coast},
                /* EAST  */ new Wave[] {wave3Land},
                /* SOUTH */ new Wave[] {wave3Coast},
                /* WEST  */ new Wave[] {wave3Coast}
            );

            this.waves3 = new Wave[] {wave3Sea, wave3Land, wave3Mountain, wave3Coast, wave3City};
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(10)]
        public void StableNoCollapse(uint dimensions) {
            // Create a grid
            WFC2 simpleGrid = new WFC2(dimensions, dimensions);

            // Just add one wave possibility
            simpleGrid.AddWaves(this.waves1);

            // Do the stuff
            simpleGrid.Encode();

            // Fill with mono
            for (uint i = 0; i < dimensions; ++i) {
                for (uint j = 0; j < dimensions; ++j) {
                    simpleGrid.AddConstraint(i, j, this.waves1[0]);
                }
            }

            simpleGrid.Collapse();

            // Get all of the waves
            char[,] expected = new char[dimensions, dimensions];
            char[,] actual = new char[dimensions, dimensions];
            for (uint i = 0; i < dimensions; ++i) {
                for (uint j = 0; j < dimensions; ++j) {
                    // Expected
                    expected[i, j] = this.waves1[0].name.ToCharArray()[0];

                    // Actual
                    actual[i, j] = simpleGrid.GetWave(i, j).name.ToCharArray()[0];
                }
            }

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(10)]
        public void CollapseToMono(uint dimensions) {
            // Create a grid
            WFC2 simpleGrid = new WFC2(dimensions, dimensions);

            // Just add one wave possibility
            simpleGrid.AddWaves(this.waves1);

            // Do the stuff
            simpleGrid.Encode();

            // Add a constraint
            simpleGrid.AddConstraint(dimensions - 1, dimensions - 1, this.waves1[0]);

            // Fill the grid
            simpleGrid.FillGrid();

            simpleGrid.Collapse();

            // Get all of the waves
            char[,] expected = new char[dimensions, dimensions];
            char[,] actual = new char[dimensions, dimensions];
            for (uint i = 0; i < dimensions; ++i) {
                for (uint j = 0; j < dimensions; ++j) {
                    // Expected
                    expected[i, j] = this.waves1[0].name.ToCharArray()[0];

                    // Actual
                    actual[i, j] = simpleGrid.GetWave(i, j).name.ToCharArray()[0];
                }
            }

            Assert.Equal(expected, actual);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(10)]
        public void CollapseSplit(uint dimensions) {
            // Create a grid
            WFC2 simpleGrid = new WFC2(dimensions, dimensions);

            // Just add one wave possibility
            simpleGrid.AddWaves(this.waves1);

            // Do the stuff
            simpleGrid.Encode();

            // Add a constraint
            simpleGrid.AddConstraint(dimensions / 2, dimensions / 2 - 1, this.waves1[0]);
            simpleGrid.AddConstraint(dimensions / 2, dimensions / 2, this.waves1[1]);

            // Fill the grid
            simpleGrid.FillGrid();

            simpleGrid.Collapse();

            // Get all of the waves
            List<char> expectedList = new List<char>();
            List<char> actualList = new List<char>();
            for (uint y = 0; y < dimensions; ++y) {
                for (uint x = 0; x < dimensions; ++x) {
                    // Expected
                    if (y < (dimensions / 2))
                        expectedList.Add(this.waves1[0].name.ToCharArray()[0]);

                    else
                        expectedList.Add(this.waves1[1].name.ToCharArray()[0]);

                    // Actual
                    actualList.Add(simpleGrid.GetWave(x, y).name.ToCharArray()[0]);
                }
            }

            Assert.Equal(expectedList, actualList);
        }

        [Theory]
        [InlineData(3)]
        [InlineData(7)]
        [InlineData(10)]
        public void CollapseFloor(uint dimensions) {
            // Create a grid
            WFC2 simpleGrid = new WFC2(dimensions, dimensions);

            // Just add one wave possibility
            simpleGrid.AddWaves(this.waves1);

            // Do the stuff
            simpleGrid.Encode();

            // Add a constraint
            simpleGrid.AddConstraint(dimensions - 1, dimensions - 2, this.waves1[0]);
            simpleGrid.AddConstraint(dimensions - 1, dimensions - 1, this.waves1[1]);

            // Fill the grid
            simpleGrid.FillGrid();

            simpleGrid.Collapse();

            // Get all of the waves
            List<char> expectedList = new List<char>();
            List<char> actualList = new List<char>();
            for (uint y = 0; y < dimensions; ++y) {
                for (uint x = 0; x < dimensions; ++x) {
                    // Expected
                    if (y < dimensions - 1)
                        expectedList.Add(this.waves1[0].name.ToCharArray()[0]);

                    else
                        expectedList.Add(this.waves1[1].name.ToCharArray()[0]);

                    // Actual
                    actualList.Add(simpleGrid.GetWave(x, y).name.ToCharArray()[0]);
                }
            }

            Assert.Equal(expectedList, actualList);
        }

        [Fact]
        public void CollapseIsland() {
            // Create the grid
            WFC2 grid = new WFC2(8, 8);

            // Just add one wave possibility
            grid.AddWaves(this.waves2);

            // Do the stuff
            grid.Encode();

            // Add a constraint
            grid.AddConstraint(4, 4, this.waves2[2]);

            // Fill the grid
            grid.FillGrid();

            grid.Collapse();

            char[] expected = new char[] {'^', '^', '^', '^', 'M'};
            char[] actual = new char[expected.Length];
            actual[0] = grid.GetWave(5, 4).name.ToCharArray()[0];
            actual[1] = grid.GetWave(4, 5).name.ToCharArray()[0];
            actual[2] = grid.GetWave(3, 4).name.ToCharArray()[0];
            actual[3] = grid.GetWave(4, 3).name.ToCharArray()[0];
            actual[4] = grid.GetWave(4, 4).name.ToCharArray()[0];

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CollapseCity() {
            // Create the grid
            WFC2 grid = new WFC2(8, 8);

            // Just add one wave possibility
            grid.AddWaves(this.waves3);

            // Do the stuff
            grid.Encode();

            // Add a constraint
            grid.AddConstraint(4, 4, this.waves3[4]);

            // Fill the grid
            grid.FillGrid();

            grid.Collapse();

            char[] expected = new char[] {'^', '_', '_', '_', 'A'};
            char[] actual = new char[expected.Length];
            actual[0] = grid.GetWave(5, 4).name.ToCharArray()[0];
            actual[1] = grid.GetWave(4, 5).name.ToCharArray()[0];
            actual[2] = grid.GetWave(3, 4).name.ToCharArray()[0];
            actual[3] = grid.GetWave(4, 3).name.ToCharArray()[0];
            actual[4] = grid.GetWave(4, 4).name.ToCharArray()[0];

            Assert.Equal(expected, actual);
        }
    }
}